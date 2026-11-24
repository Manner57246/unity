#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperDodgeball.Editor
{
    public sealed partial class DodgePlayChecks
    {
        IEnumerator CrowdChecks()
        {
            var crowd = FindFirstObjectByType<DodgeCrowd>();
            var actor = game.player.GetComponent<DodgeActor>();
            var bot = GameObject.Find("Target 1").GetComponent<DodgeBot>();
            foreach (var other in FindObjectsByType<DodgeBot>(FindObjectsSortMode.None)) other.enabled = false;
            game.crowdEnabled = true;
            game.botsEnabled = false;
            crowd.Automatic = false;
            game.ResetRound(); game.CaptureInput();
            yield return new WaitForSeconds(.3f);
            Require(crowd && crowd.SpectatorCount == DodgeBleachers.Count, "Both sidelines have tiered individual seats and interactive spectators");
            Require(crowd.Spectators.Count==DodgeBleachers.Count && crowd.Spectators[0].Ready && FindObjectsByType<DodgeActor>(FindObjectsSortMode.None).Length == 5,
                "Audience has its own physical bodies and does not become a scoring player");
            Require(crowd.ScheduleThrow(actor, DodgeCrowdThrow.RottenEgg, 3), "A spectator can wind up an egg throw toward the player");
            Vector3 locked = crowd.LockedTargetPoint;
            yield return new WaitForSeconds(.3f);
            Require(crowd.Pending && crowd.EggsThrown == 0, "Spectators visibly telegraph their throw before releasing the egg");
            game.UnlockInput();
            yield return new WaitForSeconds(.7f);
            Require(crowd.Pending && crowd.EggsThrown == 0, "Releasing the cursor pauses the spectator windup");
            game.CaptureInput();
            // Leave the committed landing point using the actual movement controller.
            Keys(Key.D);
            yield return new WaitForSeconds(.8f);
            Keys();
            Require(crowd.EggsThrown == 1 && crowd.LockedTargetPoint == locked, "The egg is released toward the committed point and never homes in");
            yield return new WaitForSeconds(1.9f);
            Require(!actor.CrowdStatus.Obscured, "Moving away during the warning avoids the egg");

            game.ResetRound(); game.CaptureInput();
            yield return new WaitForSeconds(.3f);
            int originalHits = actor.HitCount;
            Vector3 hitPoint = actor.Puppet.BodyPath[3];
            var egg = DodgeBall.SpawnEgg(hitPoint, Vector3.forward * 8f, actor);
            yield return new WaitForSeconds(.08f);
            Require(actor.CrowdStatus.Obscured && (!egg || !egg.enabled), "A real egg projectile hitting the articulated body applies a temporary splat and breaks");
            Require(actor.HitCount == originalHits && !actor.IsStunned && game.TimesHit == 0, "Egg interference does not count as ball damage or force a ragdoll");
            Require(DodgeHud.SplatColor(new Vector2(.265f, .55f)).a > .8f && DodgeHud.SplatColor(new Vector2(.265f, .55f)).g < .75f,
                "The screen splat has an opaque yellow yolk that visibly obstructs the scene");
            Require(DodgeHud.SplatColor(new Vector2(.5f, .5f)).a < .01f && DodgeHud.SplatColor(Vector2.zero).a < .01f,
                "Egg splatter leaves the reticle and part of the screen clear");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("work/crowd-egg-screen.png");
            float remaining = actor.CrowdStatus.EggRemaining;
            Require(!actor.CrowdStatus.ApplyEgg() && actor.CrowdStatus.EggRemaining <= remaining, "Repeated eggs cannot extend an existing obstruction indefinitely");
            game.UnlockInput();
            remaining = actor.CrowdStatus.EggRemaining;
            yield return new WaitForSeconds(.3f);
            Require(Mathf.Abs(remaining - actor.CrowdStatus.EggRemaining) < .001f, "Pausing freezes the egg obstruction timer");
            game.CaptureInput();
            yield return new WaitForSeconds(3.7f);
            Require(!actor.CrowdStatus.Obscured, "Egg obstruction fades away automatically without requiring an item");

            actor.CrowdStatus.ApplyEgg();
            actor.Arsenal.Equip(DodgeWeapon.Bowling, 2);
            var normalDrop = game.SpawnAirdrop(new Vector3(-4f, 0f, 2f), DodgeWeapon.Burst, true);
            Require(crowd.ScheduleThrow(actor, DodgeCrowdThrow.Soap, 25), "A spectator can throw a soap gift instead of an egg");
            yield return new WaitForSeconds(1f);
            var soap = crowd.LastGift;
            Require(soap && soap.FromCrowd && soap.Kind == DodgeGiftKind.Soap && !soap.Landed && game.ActiveDrop == normalDrop,
                "Crowd soap travels into the arena without replacing the normal parachute crate");
            game.UnlockInput();
            Vector3 giftPosition = soap.transform.position;
            yield return new WaitForSeconds(.3f);
            Require(Vector3.Distance(giftPosition, soap.transform.position) < .001f, "Pausing also freezes a thrown gift in flight");
            game.CaptureInput();
            yield return new WaitForSeconds(1.6f);
            Require(soap && soap.Landed, "Crowd gifts land inside the arena and remain available to shoot");
            var ownerless = DodgeBall.SpawnEgg(soap.AimPosition, Vector3.forward, actor);
            Require(!soap.TryClaim(ownerless), "A spectator egg cannot claim a gift");
            ownerless.enabled = false; Destroy(ownerless.gameObject);
            var claimant = DodgeBall.Spawn(soap.AimPosition, Vector3.forward, actor, false, game.ballMaterial);
            yield return new WaitForSeconds(.08f);
            Require(!soap && !actor.CrowdStatus.Obscured && actor.CrowdStatus.SoapRemaining > 7f,
                "Hitting the soap gift with an owned ball washes the winner and grants temporary egg protection");
            Require(actor.Arsenal.Selected == DodgeWeapon.Bowling && actor.Arsenal.Charges == 2, "Soap does not replace the winner's weapon or ammunition");
            var immuneEgg = DodgeBall.SpawnEgg(actor.Puppet.BodyPath[3], Vector3.forward, actor);
            yield return new WaitForSeconds(.08f);
            Require(!actor.CrowdStatus.Obscured && (!immuneEgg || !immuneEgg.enabled), "Soap protection blocks an actual egg hit");
            var damaging = DodgeBall.Spawn(actor.Puppet.BodyPath[3], Vector3.forward * 8f, bot.Actor, true, game.ballMaterial);
            yield return new WaitForSeconds(.08f);
            Require(actor.HitCount == 1, "Soap protects against eggs but does not grant immunity to dodgeballs");

            game.ResetRound(); game.CaptureInput();
            yield return new WaitForSeconds(.3f);
            bot.Actor.animatedTarget = false;
            bot.Actor.SetLeanImmediate(0f);
            DodgeBall.SpawnEgg(bot.Actor.Puppet.BodyPath[3], Vector3.forward, bot.Actor);
            yield return new WaitForSeconds(.08f);
            Require(bot.Actor.CrowdStatus.Obscured && bot.EffectiveReaction > bot.reactionSeconds && bot.EffectiveAimError > bot.aimError,
                "Eggs impair bot reaction and accuracy as well as the player's view");
            var botSoap = DodgeAirdrop.Spawn(game, bot.transform.position + Vector3.forward * 3f, DodgeWeapon.Burst, true, DodgeGiftKind.Soap);
            game.botsEnabled = true;
            bot.enabled = true;
            yield return new WaitForSeconds(.25f);
            Require(bot.WantedDrop == botSoap, "An egg-covered bot recognizes a soap gift as a useful target");
            bot.enabled = false;
            game.botsEnabled = false;
            bot.Actor.CrowdStatus.Wash();
            Require(!bot.Actor.CrowdStatus.Obscured && Mathf.Approximately(bot.EffectiveAimError, bot.aimError), "Soap restores a bot's normal aim");

            game.ResetRound(); game.CaptureInput();
            yield return new WaitForSeconds(.3f);
            Vector3 target = actor.transform.position + Vector3.up * 1.35f;
            botTestWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            botTestWall.name = "Temporary egg cover";
            botTestWall.transform.position = target + Vector3.forward * 2f;
            botTestWall.transform.localScale = new Vector3(3f, 4f, .3f);
            botTestWall.AddComponent<DodgeSurface>();
            Physics.SyncTransforms();
            var blockedEgg = DodgeBall.SpawnEgg(target + Vector3.forward * 4f,
                DodgeBall.SolveLaunchVelocity(target + Vector3.forward * 4f, target, 15f), actor);
            yield return new WaitForSeconds(.5f);
            Require(!actor.CrowdStatus.Obscured && !blockedEgg, "A solid obstacle breaks an incoming egg before it reaches the player");
            Destroy(botTestWall); botTestWall = null;

            foreach (var item in new[] { DodgeCrowdThrow.Burst, DodgeCrowdThrow.Bowling })
            {
                Require(crowd.ScheduleThrow(actor, item, 4), "A spectator winds up a " + item + " weapon gift");
                yield return new WaitForSeconds(1f);
                var gift = crowd.LastGift;
                var winner = item == DodgeCrowdThrow.Bowling ? bot.Actor : actor;
                DodgeBall.Spawn(gift.AimPosition, Vector3.forward, winner, !winner.isPlayer, game.ballMaterial);
                yield return new WaitForSeconds(.08f);
                Require(winner.Arsenal.Selected == (item == DodgeCrowdThrow.Bowling ? DodgeWeapon.Bowling : DodgeWeapon.Burst)
                    && winner.Arsenal.Charges == 3, "An owned ball claims a crowd " + item + " gift while it is airborne");
            }

            game.ResetRound(); game.CaptureInput();
            crowd.Automatic = true;
            yield return new WaitForSeconds(5f);
            Require(!crowd.Pending && crowd.EggsThrown + crowd.GiftsThrown == 0, "Spectators allow an opening grace period after each reset");
            yield return new WaitForSeconds(2.2f);
            Require(crowd.EggsThrown + crowd.GiftsThrown == 1, "Spectators autonomously begin random egg or gift throws during play");
            actor.CrowdStatus.Wash();
            DodgeAirdrop.Toss(game, new Vector3(13f, 3f, 0f), Vector3.zero, DodgeGiftKind.Soap, DodgeWeapon.Standard);
            DodgeBall.SpawnEgg(new Vector3(0f, 7f, 0f), Vector3.zero, actor);
            game.ResetRound();
            Require(!crowd.Pending && crowd.EggsThrown == 0 && DodgeAirdrop.Active.Count == 0 && DodgeBall.Active.Count == 0
                && actor.CrowdStatus.SoapRemaining == 0f && !actor.CrowdStatus.Obscured, "Round reset clears all crowd projectiles, gifts, warnings and statuses");
            Keys(Key.V);
            yield return new WaitForSeconds(.1f);
            Keys();
            Require(!game.crowdEnabled, "V switches off spectator interference for quiet practice");
            crowd.Automatic = originalCrowdAutomatic;
        }
    }
}
#endif
