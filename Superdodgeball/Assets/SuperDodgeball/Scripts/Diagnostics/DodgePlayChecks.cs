#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SuperDodgeball.Editor
{
    // Runs only in the editor. Virtual input devices exercise the actual input path.
    [DefaultExecutionOrder(-1000)]
    public sealed partial class DodgePlayChecks : MonoBehaviour
    {
        [Serializable]
        public class Report
        {
            public bool passed;
            public List<string> checks = new List<string>();
            public string failure;
        }

        readonly Report report = new Report();
        Keyboard keyboard, originalKeyboard;
        Mouse mouse, originalMouse;
        InputSettings.BackgroundBehavior originalBackground;
        InputSettings.EditorInputBehaviorInPlayMode originalEditorInput;
        bool settingsOverridden;
        bool originalAirdrops;
        bool originalBots;
        bool originalCrowd;
        bool originalCrowdAutomatic;
        readonly Dictionary<DodgeBot, bool> botEnabledStates = new Dictionary<DodgeBot, bool>();
        GameObject botTestWall;
        DodgeBot tunedBot;
        float originalBotSpeed;
        DodgeGame game;
        bool crowdOnly, diveOnly;

        [MenuItem("Super Dodgeball/Run Play checks")]
        public static void Begin()
        {
            StartVerification(false);
        }

        public static void BeginCrowd() => StartVerification(true);

        [MenuItem("Super Dodgeball/Run airborne dive checks")]
        public static void BeginDive() => StartVerification(false, true);

        static void StartVerification(bool crowdOnly, bool diveOnly = false)
        {
            if (!EditorApplication.isPlaying || !DodgeGame.Instance)
                throw new InvalidOperationException("Enter Play mode in DodgeballPlayground first.");
            if (DodgeApp.Instance && !DodgeApp.Instance.Initialized)
                throw new InvalidOperationException("Wait for the main menu to finish initializing.");
            if (FindFirstObjectByType<DodgePlayChecks>()) throw new InvalidOperationException("Checks already running.");
            var checks = new GameObject("Temporary play verification").AddComponent<DodgePlayChecks>();
            checks.crowdOnly = crowdOnly;
            checks.diveOnly = diveOnly;
            checks.StartCoroutine("GuardedChecks");
        }

        IEnumerator GuardedChecks()
        {
            game = DodgeGame.Instance;
            DodgeApp.BypassForChecks = true;
            if (DodgeApp.Instance)
            {
                foreach (var controller in DodgeApp.Instance.Controllers) controller.enabled = false;
                game.player.enabled = true;
            }
            originalAirdrops = game.airdropsEnabled;
            originalBots = game.botsEnabled;
            originalCrowd = game.crowdEnabled;
            var crowd = FindFirstObjectByType<DodgeCrowd>();
            if (crowd) originalCrowdAutomatic = crowd.Automatic;
            game.crowdEnabled = false;
            foreach (var bot in FindObjectsByType<DodgeBot>(FindObjectsSortMode.None)) botEnabledStates.Add(bot, bot.enabled);
            game.botsEnabled = false;
            game.airdropsEnabled = false;
            // Synthetic input must keep reaching the game while MCP has desktop focus.
            originalBackground = InputSystem.settings.backgroundBehavior;
            originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            settingsOverridden = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            originalKeyboard = Keyboard.current;
            originalMouse = Mouse.current;
            keyboard = InputSystem.AddDevice<Keyboard>("Dodgeball verification keyboard");
            mouse = InputSystem.AddDevice<Mouse>("Dodgeball verification mouse");
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            IEnumerator tests = diveOnly ? DiveChecks() : crowdOnly ? CrowdChecks() : Checks();
            while (true)
            {
                object next;
                try
                {
                    if (!tests.MoveNext()) { report.passed = true; break; }
                    next = tests.Current;
                }
                catch (Exception error)
                {
                    report.failure = error.ToString();
                    break;
                }
                yield return next;
            }
            Cleanup();
            Directory.CreateDirectory("work");
            File.WriteAllText(diveOnly ? "work/dive-check.json" : crowdOnly ? "work/crowd-check.json" : "work/play-check.json", JsonUtility.ToJson(report, true));
            if (report.passed) Debug.Log("[Dodgeball] Play verification passed: " + report.checks.Count + " checks.");
            else Debug.LogError("[Dodgeball] Play verification failed: " + report.failure);
            DodgeApp.BypassForChecks = false;
            if (DodgeApp.Instance) DodgeApp.Instance.ReturnHome("");
            Destroy(gameObject);
        }

        void Require(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            report.checks.Add(name);
        }

        void Keys(params Key[] keys)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        }
        void MouseButtons(ushort buttons)
        {
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = buttons });
        }

        void Update()
        {
            // Physical mouse events from the desktop must not replace the synthetic device.
            if (keyboard != null && keyboard.added) keyboard.MakeCurrent();
            if (mouse != null && mouse.added) mouse.MakeCurrent();
        }

        IEnumerator Checks()
        {
            game.practiceLauncher = false;
            game.ResetRound();
            game.CaptureInput();
            yield return null;
            Require(DodgeGame.InputCaptured, "Game accepts captured input");
            var puppet = game.player.GetComponent<DodgeRagdoll>();
            Require(puppet && puppet.Ready && puppet.BodyCount == 18, "An elastic character has eight physical spine sections and eighteen rigidbodies");
            Vector3 initial = game.player.transform.position;
            Keys(Key.W);
            yield return new WaitForSeconds(0.25f);
            Keys();
            yield return null;
            Require(Vector3.Distance(initial, game.player.transform.position) > 0.7f, "W moves the player through CharacterController");
            Require(FindObjectsByType<DodgePuff>(FindObjectsSortMode.None).Length > 0, "Running emits soft white footstep bubbles");

            Keys(Key.Q);
            yield return new WaitForSeconds(0.22f);
            var actor = game.player.GetComponent<DodgeActor>();
            Require(actor.Lean < -0.8f, "Q bends the real body left");
            yield return new WaitForSeconds(0.85f);
            Require(Mathf.Abs(actor.Lean) < 0.05f, "Holding Q does not grant a permanent dodge");
            Keys();
            yield return new WaitForSeconds(0.05f);
            Keys(Key.E);
            yield return new WaitForSeconds(0.22f);
            Require(actor.Lean > 0.8f, "E bends the real body right");
            Keys();
            yield return new WaitForSeconds(0.85f);

            float wideFov = game.view.GetComponent<Camera>().fieldOfView;
            MouseButtons(2); // Right mouse button.
            yield return new WaitForSeconds(0.3f);
            Require(game.player.IsAiming && game.view.GetComponent<Camera>().fieldOfView < wideFov - 10f,
                "Right mouse tightens the third-person camera");
            MouseButtons(0);
            yield return new WaitForSeconds(0.25f);

            var target = GameObject.Find("Target 1").GetComponent<DodgeActor>();
            game.view.LookAtPoint(target.Puppet.BodyPath[4]);
            yield return new WaitForSeconds(0.1f);
            Require(game.view.AimedAtTarget, "Camera reticle resolves a real target from the shoulder");
            int shots = game.player.ShotsFired;
            MouseButtons(1);
            yield return new WaitForSeconds(0.12f);
            MouseButtons(0);
            float hitDeadline = Time.time + 2f;
            while (game.Score == 0 && Time.time < hitDeadline) yield return null;
            Require(game.player.ShotsFired == shots + 1 && game.Score == 10,
                "Left click throws one ball and a target hit scores ten (shots " + game.player.ShotsFired + ", score " + game.Score + ")");
            Require(target.Puppet.IsStunned, "A projectile hit knocks the target into physical ragdoll");

            float normalSpeed = game.player.LastShotSpeed;
            MouseButtons(2);
            float chargeDeadline = Time.time + 1.5f;
            while (game.player.Charge < 0.99f && Time.time < chargeDeadline) yield return null;
            Require(game.player.Charge > 0.98f, "Holding right mouse builds a full charge (actual " + game.player.Charge + ")");
            Require(game.view.FocusAmount > 0.75f && game.view.GetComponent<Camera>().fieldOfView < 47f,
                "Camera progressively pushes in while charge builds");
            Vector3 hand = game.player.transform.InverseTransformPoint(actor.HandPosition);
            Require(hand.y > 1.3f && hand.z < -0.05f, "A full windup cocks the physical throwing arm behind the body (" + hand + ")");
            Require(actor.upperBody.localEulerAngles.x > 320f, "Windup leans the torso back like a baseball pitcher");
            CaptureWorld("work/windup-full.png");
            int chargedShotsBefore = game.player.ShotsFired;
            Require(game.player.TryShoot() && game.player.ShotsFired == chargedShotsBefore && actor.heldBall.gameObject.activeSelf,
                "Click starts the windup release while the ball is still held in the hand");
            float releaseLimit = Time.time + 0.4f;
            while (game.player.LastShotCharge < 0.98f && Time.time < releaseLimit) yield return null;
            Require(game.player.LastShotCharge > 0.98f && game.player.LastShotSpeed > normalSpeed * 1.8f,
                "A charged throw is faster than a normal throw and consumes charge");
            Require(game.player.Charge < 0.01f, "Charge stays empty during the shot cooldown");
            yield return null;
            Require(!actor.heldBall.gameObject.activeSelf, "The held ball disappears only when the real projectile leaves the hand");
            Require(actor.upperBody.localEulerAngles.x > 5f && actor.upperBody.localEulerAngles.x < 60f,
                "Release carries the torso forward into the follow-through");
            CaptureWorld("work/throw-follow-through.png");
            MouseButtons(0);
            yield return new WaitForSeconds(0.65f);

            actor.Arsenal.Equip(DodgeWeapon.Burst, 1);
            Keys(Key.Digit2);
            yield return new WaitForSeconds(0.12f);
            Keys();
            Require(game.player.BurstMode, "Key 2 selects the five-shot launcher");
            shots = game.player.ShotsFired;
            MouseButtons(1);
            yield return new WaitForSeconds(0.12f);
            MouseButtons(0);
            yield return new WaitForSeconds(0.7f);
            Require(game.player.ShotsFired == shots + 5, "One burst click emits exactly five projectiles");
            Require(actor.Arsenal.Charges == 0 && game.player.CurrentWeapon == DodgeWeapon.Standard,
                "The last burst charge still fires all five balls before returning to standard");
            Require(!game.player.TryShoot(), "The five-shot launcher enforces its cooldown");
            Keys(Key.Digit1);
            yield return new WaitForSeconds(0.12f);
            Keys();
            Require(!game.player.BurstMode, "Key 1 restores the standard ball");

            Keys(Key.H);
            yield return new WaitForSeconds(0.12f);
            Keys();
            Require(DodgeGame.ShowHitboxes && GameObject.Find("Hitbox overlay"), "H shows the actual body hit regions");
            game.ToggleHitboxes();
            Keys(Key.T);
            yield return new WaitForSeconds(0.05f);
            Keys();
            Require(game.practiceLauncher, "T enables the practice cannon");
            yield return new WaitForSeconds(2.15f);
            bool incoming = false;
            foreach (var ball in FindObjectsByType<DodgeBall>(FindObjectsSortMode.None)) incoming |= ball.hostile;
            Require(incoming, "Practice cannon fires a hostile ball after telegraphing");
            game.practiceLauncher = false;
            game.ClearBalls();

            game.ResetRound();
            game.CaptureInput();
            game.player.enabled = false;
            actor.enabled = false;
            actor.SetLeanImmediate(0f);
            Vector3 from = actor.transform.position + new Vector3(0f, 1.72f, 6f);
            DodgeBall.Spawn(from, Vector3.back * 50f, null, true, game.ballMaterial);
            yield return new WaitForSeconds(0.2f);
            Require(game.TimesHit == 1, "A fast incoming ball hits the standing player");
            Require(actor.Puppet.IsLoose && !game.player.GetComponent<CharacterController>().enabled,
                "A hit releases the pelvis and temporarily yields movement to physics");
            yield return new WaitForSeconds(0.25f);
            CaptureWorld("work/ragdoll-impact.png");
            yield return new WaitForSeconds(2.1f);
            Require(!actor.IsStunned && game.player.GetComponent<CharacterController>().enabled && actor.Puppet.JointError < 0.08f && actor.Puppet.PoseError < 0.55f,
                "The physical character gets up and restores control without detached limbs (error " + actor.Puppet.PoseError + ", stunned " + actor.IsStunned + ", motor " + game.player.GetComponent<CharacterController>().enabled + ")");
            game.ClearBalls();
            actor.ResetActor();
            actor.SetLeanImmediate(-1f);
            DodgeBall.Spawn(from, Vector3.back * 50f, null, true, game.ballMaterial);
            yield return new WaitForSeconds(0.25f);
            Require(game.TimesHit == 1 && game.Dodges == 1, "A left lean evades the same live projectile and registers a dodge");
            game.ClearBalls();
            actor.ResetActor();
            actor.SetLeanImmediate(1f);
            DodgeBall.Spawn(from, Vector3.back * 50f, null, true, game.ballMaterial);
            yield return new WaitForSeconds(0.25f);
            Require(game.TimesHit == 1 && game.Dodges == 2, "A right lean also evades the same live projectile");
            game.ClearBalls();
            game.view.SetView(0f, 9f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("work/prototype-lean.png");
            yield return new WaitForSeconds(0.1f);
            actor.enabled = true;
            game.player.enabled = true;

            var dives = DiveChecks();
            while (dives.MoveNext()) yield return dives.Current;
            var rayBuffer = new RaycastHit[64];

            var falling = DodgeBall.Spawn(new Vector3(9f, 2f, -2f), Vector3.zero, null, false, game.ballMaterial);
            yield return new WaitForSeconds(0.3f);
            Require(falling && falling.transform.position.y < 1.8f && falling.velocity.y < -2f,
                "Dodgeballs fall under gravity");
            yield return new WaitForSeconds(0.42f);
            Require(falling && falling.BounceCount >= 1 && falling.transform.position.y >= DodgeBall.Radius - 0.02f,
                "Dodgeballs bounce off the floor without tunnelling");
            var bank = DodgeBall.Spawn(new Vector3(11.5f*DodgeCourt.Scale-1.5f, 0.65f, -3f), Vector3.right * 12f, null, false, game.ballMaterial);
            yield return new WaitForSeconds(0.18f);
            Require(bank && bank.BounceCount > 0 && bank.velocity.x < 0f, "Walls reflect the ball and reduce its speed");

            game.ResetRound();
            game.CaptureInput();
            yield return new WaitForSeconds(0.25f);
            Keys(Key.Z);
            yield return new WaitForSeconds(0.55f);
            Require(actor.Spine.LAmount > 0.98f && game.player.IsShaping, "Z smoothly bends the continuous body into an L pose");
            Require(!game.player.TryShoot(), "Extreme dodge poses cannot fire or charge a ball");
            CaptureWorld("work/elastic-L-play.png");
            Keys();
            yield return new WaitForSeconds(1.6f);
            Require(actor.ShapeAmount < 0.01f && puppet.JointError < 0.08f && puppet.PoseError < 0.4f,
                "Releasing a shape restores the physical body with intact joints (tracking " + puppet.PoseError + ", joint gap " + puppet.JointError + ")");
            Keys(Key.X);
            yield return new WaitForSeconds(0.6f);
            Require(actor.Spine.OAmount > 0.98f, "X curls the multi-section spine into an O pose");
            Physics.SyncTransforms();
            Vector3 center = actor.Spine.RingCenter;
            bool holeBlocked = DodgeBall.Trace(center - actor.transform.forward * 3f, center + actor.transform.forward * 3f,
                DodgeBall.Radius, null, rayBuffer, out var holeHit, out var holeActor) && holeActor == actor;
            Require(!holeBlocked, "The O opening is clear of all actual body colliders, including arms and head");
            int beforeHole = game.TimesHit;
            var through = DodgeBall.Spawn(center - actor.transform.forward * 2f,
                DodgeBall.SolveLaunchVelocity(center - actor.transform.forward * 2f, center + actor.transform.forward * 2f, 28f),
                null, true, game.ballMaterial);
            yield return new WaitForSeconds(0.17f);
            Require(through && game.TimesHit == beforeHole, "A live swept dodgeball really passes through the O opening");
            // Do not let this ball bounce back into the later wardrobe checks.
            Destroy(through.gameObject);
            CaptureWorld("work/elastic-O-play.png");
            yield return new WaitForSeconds(0.9f);
            Require(actor.ShapeAmount < 0.15f, "Holding X does not maintain a permanent O dodge");
            Keys();
            yield return new WaitForSeconds(0.8f);
            game.ResetRound();
            yield return new WaitForSeconds(0.25f);
            Keys(Key.C);
            yield return new WaitForSeconds(0.12f);
            Keys();
            Require(puppet.Wardrobe.Style == 1 && puppet.Wardrobe.Hat.gameObject.activeSelf, "C equips a hat attached to the physical head socket");
            yield return new WaitForSeconds(0.12f);
            Keys(Key.C);
            yield return new WaitForSeconds(0.12f);
            Keys();
            Require(puppet.Wardrobe.Style == 2 && puppet.Wardrobe.GarmentVisible, "C equips a jersey and shorts on the same deformation skeleton");
            Keys(Key.X);
            yield return new WaitForSeconds(0.6f);
            Require(actor.Spine.OAmount > 0.98f, "The clothed character can enter an O pose through normal input (amount "
                + actor.ShapeAmount + ", stunned " + actor.IsStunned + ")");
            Physics.SyncTransforms();
            center = actor.Spine.RingCenter;
            bool clothedHole = DodgeBall.Trace(center - actor.transform.forward * 3f, center + actor.transform.forward * 3f,
                DodgeBall.Radius, null, rayBuffer, out _, out var clothedActor) && clothedActor == actor;
            Require(!clothedHole, "Clothing and hats add no hidden hitboxes across the O opening");
            CaptureWorld("work/elastic-O-outfit.png");
            Require(puppet.JointError < 0.08f && puppet.PoseError < 0.4f,
                "The physical O pose stays assembled with clothes equipped (tracking " + puppet.PoseError + ", joint gap " + puppet.JointError + ")");
            actor.ReceiveHit(actor.Puppet.BodyPath[3], false, Vector3.right * 3f);
            yield return new WaitForSeconds(0.25f);
            Require(actor.Puppet.IsLoose, "Extreme bent poses can still be hit and collapse into ragdoll");
            Keys();
            game.ResetRound();
            yield return new WaitForSeconds(0.3f);
            Require(actor.ShapeAmount < 0.01f && !actor.IsStunned, "Reset clears both deformation and ragdoll state");
            puppet.Wardrobe.SetStyle(0);

            game.ResetRound();
            game.CaptureInput();
            Keys(Key.Digit2);
            yield return new WaitForSeconds(0.08f);
            Keys();
            Require(game.player.CurrentWeapon == DodgeWeapon.Standard, "Selecting an empty special slot cannot create free weapons");
            game.airdropsEnabled = true;
            yield return new WaitForSeconds(3.1f);
            Require(game.ActiveDrop && !game.ActiveDrop.Landed, "A random weapon crate descends on a parachute after play begins");
            Vector3 pausedDrop = game.ActiveDrop.transform.position;
            game.UnlockInput();
            yield return new WaitForSeconds(0.15f);
            Require(Vector3.Distance(pausedDrop, game.ActiveDrop.transform.position) < 0.001f, "Releasing the cursor pauses the airdrop descent");
            game.CaptureInput();
            game.airdropsEnabled = false;
            game.ClearAirdrop();

            var touchCrate = game.SpawnAirdrop(actor.transform.position, DodgeWeapon.Bowling, true);
            yield return new WaitForSeconds(0.15f);
            Require(touchCrate && !touchCrate.Claimed && actor.Arsenal.Charges == 0,
                "Touching a crate with the character does not claim its weapon");

            var crate = game.SpawnAirdrop(new Vector3(0f, 0f, -1.5f), DodgeWeapon.Bowling, true);
            Vector3 crateCenter = crate.AimPosition;
            DodgeBall.Spawn(crateCenter - Vector3.forward * 2f, Vector3.forward * 35f, null, true, game.ballMaterial);
            yield return new WaitForSeconds(0.2f);
            Require(crate && !crate.Claimed && actor.Arsenal.Charges == 0, "The ownerless practice cannon cannot claim a weapon crate");
            game.ClearBalls();
            game.view.LookAtPoint(crateCenter);
            yield return new WaitForSeconds(0.1f);
            Require(game.view.AimedAtTarget, "The reticle targets a claimable crate as well as characters");
            CaptureWorld("work/airdrop-claim-target.png");
            MouseButtons(1);
            yield return new WaitForSeconds(0.1f);
            MouseButtons(0);
            float claimDeadline = Time.time + 1.5f;
            while (actor.Arsenal.Charges == 0 && Time.time < claimDeadline) yield return null;
            Require(actor.Arsenal.Selected == DodgeWeapon.Bowling && actor.Arsenal.Charges == 3,
                "A real click throws a ball that claims the crate and equips three bowling attacks");
            Require(game.Score == 0 && !game.ActiveDrop, "Claiming a crate consumes it without scoring a character hit");
            yield return new WaitForSeconds(0.55f);
            CaptureWorld("work/bowling-equipped.png");

            // Exercise the normal release path with the last charge: the queued ball
            // must keep its weapon even though the arsenal already falls back to standard.
            actor.Arsenal.Equip(DodgeWeapon.Bowling, 1);
            game.view.LookAtPoint(target.Puppet.BodyPath[4]);
            yield return new WaitForSeconds(0.2f);
            Require(game.player.TryShoot(), "A claimed bowling weapon uses the expressive throwing sequence");
            float bowlingDeadline = Time.time + 0.5f;
            DodgeBall releasedBowling = null;
            while (!releasedBowling && Time.time < bowlingDeadline)
            {
                foreach (var ball in FindObjectsByType<DodgeBall>(FindObjectsSortMode.None))
                    if (ball.Weapon == DodgeWeapon.Bowling) releasedBowling = ball;
                yield return null;
            }
            Require(releasedBowling && releasedBowling.CollisionRadius > DodgeBall.Radius * 1.5f,
                "The last charge releases a visibly larger bowling ball with a matching sweep radius");
            Require(actor.Arsenal.Charges == 0 && game.player.CurrentWeapon == DodgeWeapon.Standard,
                "Empty special ammunition automatically restores the unlimited standard ball");
            game.ClearBalls();

            game.ResetRound();
            game.CaptureInput();
            yield return new WaitForSeconds(0.25f);
            var contested = game.SpawnAirdrop(new Vector3(0f, 0f, -1f), DodgeWeapon.Burst, true);
            Vector3 contestPoint = contested.AimPosition;
            DodgeBall.Spawn(contestPoint - Vector3.forward, Vector3.forward * 35f, target, false, game.ballMaterial);
            DodgeBall.Spawn(contestPoint - Vector3.forward * 2f, Vector3.forward * 35f, actor, false, game.ballMaterial);
            yield return new WaitForSeconds(0.2f);
            Require(target.Arsenal.Selected == DodgeWeapon.Burst && target.Arsenal.Charges == 3 && actor.Arsenal.Charges == 0,
                "Two actors shooting the same crate award only the first projectile owner");
            game.ClearBalls();
            game.ResetRound();
            game.CaptureInput();
            yield return new WaitForSeconds(0.25f);
            var impactPoint = target.Puppet.BodyPath[3];
            float beforeLift = target.transform.position.y;
            DodgeBall.Spawn(impactPoint - Vector3.forward * 2f, Vector3.forward * 30f, actor, false, game.ballMaterial, DodgeWeapon.Bowling);
            yield return new WaitForSeconds(0.4f);
            Require(target.HitCount == 1 && target.Puppet.IsLoose && target.transform.position.y > beforeLift + 1f,
                "A live bowling hit launches the entire ragdoll above the court");
            CaptureWorld("work/bowling-launch.png");
            var spent = FindFirstObjectByType<DodgeBall>();
            var spentCrate = game.SpawnAirdrop(new Vector3(7f, 0f, -3f), DodgeWeapon.Bowling, true);
            Require(spent && !spent.IsDangerous && !spentCrate.TryClaim(spent),
                "A ball that already hit a character cannot claim a weapon crate");
            var physicalRoot = GameObject.Find(target.name + " - physical puppet");
            var physicalPelvis = physicalRoot.transform.Find("Pelvis").GetComponent<Rigidbody>();
            game.UnlockInput();
            yield return new WaitForFixedUpdate();
            Vector3 pausedPelvis = physicalPelvis.position;
            yield return new WaitForSeconds(0.2f);
            Require(Vector3.Distance(pausedPelvis, physicalPelvis.position) < 0.01f && target.IsStunned,
                "Pausing freezes a launched ragdoll instead of advancing its recovery");
            game.CaptureInput();
            yield return new WaitForSeconds(0.12f);
            Require(Vector3.Distance(pausedPelvis, physicalPelvis.position) > 0.1f && target.Puppet.IsLoose,
                "Resuming preserves the launched character's flight rather than snapping it upright");
            float landingDeadline = Time.time + 4.5f;
            while (target.IsStunned && Time.time < landingDeadline) yield return null;
            yield return new WaitForSeconds(0.5f);
            Require(!target.IsStunned && !target.Puppet.IsLoose && target.transform.position.y < 0.2f && target.Puppet.JointError < 0.1f,
                "A launched character lands and recovers without a midair snap or broken joints");

            // Walls still precede crates in the shared swept query.
            game.ResetRound();
            game.CaptureInput();
            var blockedCrate = game.SpawnAirdrop(new Vector3(0f, 0f, -1f), DodgeWeapon.Bowling, true);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Temporary airdrop occlusion wall";
            wall.transform.position = blockedCrate.AimPosition - Vector3.forward * 1.2f;
            wall.transform.localScale = new Vector3(3f, 2f, 0.2f);
            wall.AddComponent<DodgeSurface>();
            Physics.SyncTransforms();
            DodgeBall.Spawn(blockedCrate.AimPosition - Vector3.forward * 2.5f, Vector3.forward * 40f, actor, false, game.ballMaterial);
            yield return new WaitForSeconds(0.2f);
            bool occluded = blockedCrate && !blockedCrate.Claimed;
            Destroy(wall);
            Require(occluded && actor.Arsenal.Charges == 0, "A solid wall blocks a projectile from stealing a crate behind it");
            actor.Arsenal.Equip(DodgeWeapon.Bowling);
            game.ResetRound();
            Require(!game.ActiveDrop && actor.Arsenal.Charges == 0 && target.Arsenal.Charges == 0,
                "Restart removes airdrops and clears every actor's temporary weapons");

            Keys(Key.R);
            yield return new WaitForSeconds(0.08f);
            Keys();
            Require(game.Score == 0 && game.TimesHit == 0 && game.Dodges == 0 && game.SecondsLeft > 89f,
                "R resets the score, hits, dodges and round clock");
            Keys(Key.Escape);
            yield return new WaitForSeconds(0.05f);
            Keys();
            Require(!DodgeGame.InputCaptured, "Escape releases the cursor");
            float pausedClock = game.SecondsLeft;
            yield return new WaitForSeconds(0.15f);
            Require(Mathf.Abs(game.SecondsLeft - pausedClock) < 0.01f, "The round clock pauses when the cursor is released");
            game.CaptureInput();
            typeof(DodgeGame).GetProperty(nameof(DodgeGame.SecondsLeft)).SetValue(game, 0.03f);
            yield return new WaitForSeconds(0.1f);
            Require(!game.RoundOver && DodgeGame.InputCaptured, "Minimal free practice has no hidden timer that stops the game");
            var botChecks = BotChecks();
            while (botChecks.MoveNext()) yield return botChecks.Current;
            var crowdChecks = CrowdChecks();
            while (crowdChecks.MoveNext()) yield return crowdChecks.Current;
        }

        IEnumerator BotChecks()
        {
            var all = FindObjectsByType<DodgeBot>(FindObjectsSortMode.None);
            Require(all.Length == 4, "The four practice characters have physical movement and bot controllers");
            var styles = new HashSet<DodgeBotStyle>();
            foreach (var bot in all) styles.Add(bot.style);
            Require(styles.Count == 4, "The opponents use four different tactical personalities");
            var one = GameObject.Find("Target 1").GetComponent<DodgeBot>();
            foreach (var bot in all) bot.enabled = bot == one;
            game.botsEnabled = true;
            game.airdropsEnabled = false;
            game.ResetRound();
            game.CaptureInput();
            Vector3 start = one.transform.position;
            yield return new WaitForSeconds(0.75f);
            Require(Vector3.Distance(start, one.transform.position) > 0.4f, "A bot navigates with its real CharacterController");
            Require(one.ShotsFired == 0, "The bot gives the player an opening instead of firing immediately");
            bool observedCharge = false;
            float deadline = Time.time + 4f;
            while (one.ShotsFired == 0 && Time.time < deadline)
            {
                observedCharge |= one.IsCharging && one.Actor.Windup > 0.1f;
                yield return null;
            }
            Require(observedCharge && one.ShotsFired > 0, "A bot visibly winds up before releasing a real projectile from its physical hand");
            bool ownedShot = false;
            foreach (var ball in DodgeBall.Active) ownedShot |= ball.owner == one.Actor;
            Require(ownedShot, "Bot projectiles retain their actor owner for hits and airdrop claims");

            game.ResetRound();
            game.CaptureInput();
            var controller = one.GetComponent<CharacterController>();
            controller.enabled = false;
            one.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 3.5f), Quaternion.Euler(0f, 180f, 0f));
            controller.enabled = true;
            one.Actor.SetLeanImmediate(0f);
            var drop = game.SpawnAirdrop(new Vector3(0f, 0f, -1.5f), DodgeWeapon.Bowling, true);
            yield return new WaitForSeconds(0.25f);
            Require(one.WantedDrop == drop && one.Actor.Arsenal.Charges == 0, "An unarmed bot prioritizes a visible crate but does not receive its weapon for free");
            deadline = Time.time + 5f;
            while (one.CratesClaimed == 0 && Time.time < deadline) yield return null;
            Require(one.CratesClaimed == 1 && one.Actor.Arsenal.Selected == DodgeWeapon.Bowling && one.Actor.Arsenal.Charges == 3,
                "An autonomous bot hits the crate with its own ball and earns the same three-charge reward as the player");
            CaptureWorld("work/bot-claimed-bowling.png");
            deadline = Time.time + 5f;
            while (one.LastWeaponFired != DodgeWeapon.Bowling && Time.time < deadline) yield return null;
            Require(one.LastWeaponFired == DodgeWeapon.Bowling && one.Actor.Arsenal.Charges == 2,
                "A bot attacks with the bowling weapon it earned and consumes finite ammunition");

            game.ResetRound();
            game.CaptureInput();
            deadline = Time.time + 4f;
            while (!one.IsCharging && Time.time < deadline) yield return null;
            Require(one.IsCharging, "The bot enters a charge window that can be interrupted");
            int beforeInterruption = one.ShotsFired;
            one.Actor.ReceiveHit(one.Actor.Puppet.BodyPath[3], true, Vector3.right * 2f);
            yield return new WaitForSeconds(0.3f);
            Require(one.State == DodgeBotState.Recovering && one.ShotsFired == beforeInterruption,
                "A hit cancels a bot's windup and makes it yield to physical ragdoll");
            deadline = Time.time + 3f;
            while (one.Actor.IsStunned && Time.time < deadline) yield return null;
            yield return new WaitForSeconds(0.2f);
            Require(!one.Actor.IsStunned && one.State != DodgeBotState.Recovering && controller.enabled,
                "The bot gets up and resumes its decisions after a hit");

            game.ResetRound();
            game.CaptureInput();
            controller.enabled = false;
            one.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 3.5f), Quaternion.Euler(0f, 180f, 0f));
            controller.enabled = true;
            one.Actor.SetLeanImmediate(0f);
            var hiddenDrop = game.SpawnAirdrop(new Vector3(0f, 0f, -1.5f), DodgeWeapon.Burst, true);
            // Isolate the occluded target. Other visible opponents on the bot's side
            // of the wall are legitimate targets and would make ShotsFired == 0 invalid.
            var hiddenOpponents = new List<DodgeActor>();
            foreach (var other in FindObjectsByType<DodgeActor>(FindObjectsSortMode.None))
                if (other != one.Actor && other != game.player.GetComponent<DodgeActor>())
                { hiddenOpponents.Add(other); other.gameObject.SetActive(false); }
            botTestWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            botTestWall.name = "Temporary bot visibility wall";
            botTestWall.transform.position = new Vector3(0f, 2.5f, 0f);
            botTestWall.transform.localScale = new Vector3(20f, 5f, 0.3f);
            botTestWall.AddComponent<DodgeSurface>();
            Physics.SyncTransforms();
            yield return new WaitForSeconds(3.2f);
            Require(hiddenDrop && !hiddenDrop.Claimed && one.ShotsFired == 0 && one.Actor.Arsenal.Charges == 0,
                "The bot repositions instead of shooting through solid cover to claim a crate");
            foreach (var other in hiddenOpponents) other.gameObject.SetActive(true);
            Destroy(botTestWall);
            botTestWall = null;

            game.ResetRound();
            game.CaptureInput();
            controller.enabled = false;
            one.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 3.5f), Quaternion.Euler(0f, 180f, 0f));
            controller.enabled = true;
            one.Actor.SetLeanImmediate(0f);
            tunedBot = one;
            originalBotSpeed = one.moveSpeed;
            one.moveSpeed = 0.05f;
            Vector3 dangerOrigin = one.transform.position + one.transform.forward * 8f + Vector3.up * 1.5f;
            Vector3 dangerTarget = one.transform.position + Vector3.up * 1.5f;
            DodgeBall.Spawn(dangerOrigin, DodgeBall.SolveLaunchVelocity(dangerOrigin, dangerTarget, 12f),
                game.player.GetComponent<DodgeActor>(), false, game.ballMaterial);
            yield return new WaitForSeconds(0.16f);
            Require(one.DodgesAttempted == 0, "Seeing a projectile does not let the bot dodge before its reaction delay");
            yield return new WaitForSeconds(0.65f);
            Require(one.DodgesAttempted > 0, "The bot predicts a visible incoming ball and attempts a timed physical dodge");
            one.moveSpeed = originalBotSpeed;
            tunedBot = null;

            Keys(Key.B);
            yield return new WaitForSeconds(0.1f);
            Keys();
            Vector3 practicePosition = one.transform.position;
            yield return new WaitForSeconds(0.25f);
            Require(!game.botsEnabled && one.State == DodgeBotState.Practice && one.ShotsFired == 0
                && Vector3.Distance(practicePosition, one.transform.position) < 0.01f,
                "B resets the court and returns bots to stationary practice targets");
            Keys(Key.B);
            yield return new WaitForSeconds(0.1f);
            Keys();
            Require(game.botsEnabled, "B can restore bot play without rebuilding the scene");
            foreach (var bot in all) bot.enabled = true;
            game.ResetRound();
            game.CaptureInput();
            game.airdropsEnabled = true;
            deadline = Time.time + 10f;
            bool bounded = true, fairPressure = true;
            while (Time.time < deadline)
            {
                int attackers = 0;
                foreach (var bot in all)
                {
                    if (bot.Opponent && bot.Opponent.isPlayer) attackers++;
                    if (!bot.Actor.IsStunned) bounded &= Mathf.Abs(bot.transform.position.x) < 10.8f*DodgeCourt.Scale && Mathf.Abs(bot.transform.position.z) < 13.8f*DodgeCourt.Scale;
                }
                fairPressure &= attackers <= 2;
                yield return new WaitForSeconds(0.1f);
            }
            int totalShots = 0;
            foreach (var bot in all) totalShots += bot.ShotsFired;
            Require(totalShots >= 6 && bounded, "Four autonomous opponents can play together and navigate inside the court");
            Require(fairPressure, "At most two opponents deliberately target the player at once");
            CaptureWorld("work/bots-free-for-all.png");
            game.UnlockInput();
            yield return new WaitForSeconds(0.15f);
            Vector3 frozen = one.transform.position;
            int frozenShots = one.ShotsFired;
            yield return new WaitForSeconds(0.25f);
            Require(one.State == DodgeBotState.Paused && one.ShotsFired == frozenShots && Vector3.Distance(frozen, one.transform.position) < 0.01f,
                "Releasing the cursor pauses bot movement, decisions and firing");
            game.CaptureInput();
            game.botsEnabled = false;
            game.airdropsEnabled = false;
            game.ResetRound();
        }

        static void CaptureWorld(string path)
        {
            var camera = Camera.main;
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Destroy(texture);
            }
        }

        void Cleanup()
        {
            RestoreSettings();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            if (game)
            {
                if (botTestWall) Destroy(botTestWall);
                if (tunedBot) tunedBot.moveSpeed = originalBotSpeed;
                foreach (var pair in botEnabledStates) if (pair.Key) pair.Key.enabled = pair.Value;
                game.player.enabled = true;
                game.player.GetComponent<DodgeActor>().enabled = true;
                if (DodgeGame.ShowHitboxes) game.ToggleHitboxes();
                game.practiceLauncher = false;
                game.airdropsEnabled = originalAirdrops;
                game.botsEnabled = originalBots;
                game.crowdEnabled = originalCrowd;
                var crowd = FindFirstObjectByType<DodgeCrowd>();
                if (crowd) crowd.Automatic = originalCrowdAutomatic;
                game.ResetRound();
                game.UnlockInput();
            }
        }

        void OnDestroy()
        {
            RestoreSettings();
            // Also restore devices if Play mode is stopped in the middle of a check.
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
        }

        void RestoreSettings()
        {
            if (!settingsOverridden) return;
            InputSystem.settings.backgroundBehavior = originalBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
            settingsOverridden = false;
        }
    }
}
#endif
