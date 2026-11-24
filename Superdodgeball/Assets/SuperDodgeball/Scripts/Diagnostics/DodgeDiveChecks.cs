#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperDodgeball.Editor
{
    public sealed partial class DodgePlayChecks
    {
        IEnumerator DiveChecks()
        {
            var recording = RecordDive();
            while (recording.MoveNext()) yield return recording.Current;
            game.practiceLauncher = false;
            game.ResetRound(); game.CaptureInput(); Keys(); MouseButtons(0);
            yield return new WaitForSeconds(.15f);
            var actor = game.player.GetComponent<DodgeActor>();
            var motor = game.player.GetComponent<CharacterController>();
            Vector3 start = actor.transform.position;
            Keys(Key.W, Key.Space);
            yield return new WaitForSeconds(.13f);
            Keys();
            Require(game.player.IsDiving && actor.transform.position.y > start.y + .35f,
                "Space launches the real controller upward, rather than sliding it along the floor");
            Require(actor.transform.position.z > start.z + .8f && actor.DiveAmount > .9f,
                "The takeoff keeps its chosen direction while the body extends into a dive");
            Require(!game.player.TryShoot() && actor.InvulnerableUntil <= Time.time,
                "An airborne dive blocks throwing without granting invulnerability");
            game.UnlockInput();
            yield return new WaitForSeconds(.04f);
            Vector3 pausedPosition = actor.transform.position;
            yield return new WaitForSeconds(.16f);
            Require(game.player.IsDiving && Vector3.Distance(pausedPosition, actor.transform.position) < .01f,
                "Pausing preserves an airborne dive instead of expiring its flight timer");
            game.CaptureInput();
            yield return new WaitForSeconds(.14f);
            float peak = actor.transform.position.y;
            Require(peak > start.y + .7f && !motor.isGrounded, "The dive has a visible airborne apex");
            Vector3 hand = actor.transform.InverseTransformPoint(actor.HandPosition);
            Require(hand.z > .6f, "The physical hands reach in front of the body during flight (physical " + hand + ", target " + actor.transform.InverseTransformPoint(actor.heldBall.position) + ")");
            var buffer = new RaycastHit[64];
            Vector3 lowFrom = actor.transform.position + new Vector3(-3f, -.65f, .1f);
            bool lowHit = DodgeBall.Trace(lowFrom, lowFrom + Vector3.right * 6f, DodgeBall.Radius, null, buffer, out _, out var lowActor) && lowActor == actor;
            Require(!lowHit, "A low ball can pass beneath the elevated body");
            Vector3 bodyFrom = actor.Puppet.BodyCenter - Vector3.right * 3f;
            bool bodyHit = DodgeBall.Trace(bodyFrom, bodyFrom + Vector3.right * 6f, DodgeBall.Radius, null, buffer, out _, out var bodyActor) && bodyActor == actor;
            Require(bodyHit, "A ball through the actual airborne body still hits it");
            Keys(Key.Space); yield return null; Keys();
            yield return new WaitForSeconds(.15f);
            Require(actor.transform.position.y < peak && !motor.isGrounded, "Gravity brings the extended body back down without a second air jump");
            float deadline = Time.time + 1f;
            while (game.player.IsDiving && Time.time < deadline) yield return null;
            Require(!game.player.IsDiving && motor.isGrounded && actor.transform.position.y < start.y + .1f,
                "The dive finishes only after real ground contact and a short landing cushion");
            float distance = actor.transform.position.z - start.z;
            Require(distance > 4f && distance < 6.5f, "A full dive travels a controlled distance (" + distance + " metres)");
            Keys(Key.Space); yield return new WaitForSeconds(.05f); Keys();
            Require(!game.player.IsDiving && game.player.DiveReady < 1f, "Landing recovery prevents immediately chaining dives");
            yield return new WaitForSeconds(.25f);
            Require(actor.DiveAmount < .05f && actor.Puppet.JointError < .35f, "The physical body smoothly returns to its standing pose");

            game.ResetRound(); game.CaptureInput();
            yield return new WaitForSeconds(.15f);
            Keys(Key.Space); yield return new WaitForSeconds(.2f); Keys();
            Require(actor.ReceiveHit(actor.Puppet.BodyCenter, true, Vector3.right * 2f), "A ball can interrupt an airborne dive");
            yield return new WaitForSeconds(.1f);
            Require(actor.IsStunned && !game.player.IsDiving, "A midair hit releases the dive to the physical ragdoll");
            game.ResetRound(); game.CaptureInput();
        }
        IEnumerator RecordDive()
        {
            // Capture the real articulated character from the side for visual review.
            game.ResetRound(); game.CaptureInput();
            yield return new WaitForSeconds(.15f);
            game.view.enabled = false;
            var lens = game.view.GetComponent<Camera>();
            bool wasOrtho = lens.orthographic; float oldSize = lens.orthographicSize;
            try
            {
                lens.orthographic = true; lens.orthographicSize = 3.2f;
                lens.transform.position = new Vector3(8f, 3.2f, -3.5f);
                lens.transform.LookAt(new Vector3(0f, 1.1f, -3.5f));
                Keys(Key.W, Key.Space);
                float captureStart = Time.time;
                float[] times = { .06f, .18f, .3f, .46f, .62f, .82f, 1.06f };
                for (int i = 0; i < times.Length; i++)
                {
                    while (Time.time - captureStart < times[i]) yield return null;
                    Keys();
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot("work/dive-side-" + i + ".png");
                }
            }
            finally
            {
                lens.orthographic = wasOrtho; lens.orthographicSize = oldSize;
                game.view.enabled = true; game.view.SetView(0f, 9f); Keys();
            }
        }
    }
}
#endif
