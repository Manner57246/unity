using System.Collections;
using Rouge.Core;
using UnityEngine;

namespace Rouge.Unity
{
    // The arena owns these renderers. All offsets are relative to the authored battle poses.
    internal sealed class BattleStrike
    {
        private readonly SpriteRenderer fighter, enemy, floor, trace, echo;
        private readonly Camera camera;
        private readonly SwordImpact particles;
        private readonly AudioSource sound;
        private readonly AudioClip draw, hit, backhandAir, backhandHit, overheadAir, overheadHit;
        private readonly ScoutFrames frames;
        private readonly Sprite gather, sweep, fullCut, brokenEdge, flecks;

        internal BattleStrike(SpriteRenderer fighter, SpriteRenderer enemy, SpriteRenderer floor,
            Camera camera, SwordImpact particles, ScoutFrames frames)
        {
            this.fighter = fighter;
            this.enemy = enemy;
            this.floor = floor;
            this.camera = camera;
            this.particles = particles;
            this.frames = frames;
            trace = Layer("Blade trace", 7);
            gather = Resources.Load<Sprite>("BladeFX/bladeGather");
            sweep = Resources.Load<Sprite>("BladeFX/bladeSweep");
            fullCut = Resources.Load<Sprite>("BladeFX/bladeTrace");
            brokenEdge = Resources.Load<Sprite>("BladeFX/bladeBreak");
            flecks = Resources.Load<Sprite>("BladeFX/bladeFleck");
            echo = Layer("Dash silhouette", 1);
            echo.transform.localScale = fighter.transform.lossyScale;
            sound = fighter.gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            sound.spatialBlend = 0;
            draw = Resources.Load<AudioClip>("BladeFX/bladeDraw");
            hit = Resources.Load<AudioClip>("BladeFX/ironImpact");
            backhandAir = Resources.Load<AudioClip>("BladeFX/backhandAir");
            backhandHit = Resources.Load<AudioClip>("BladeFX/backhandImpact");
            overheadAir = Resources.Load<AudioClip>("BladeFX/overheadAir");
            overheadHit = Resources.Load<AudioClip>("BladeFX/overheadImpact");
        }

        private SpriteRenderer Layer(string name, int order)
        {
            var layer = new GameObject(name).AddComponent<SpriteRenderer>();
            layer.transform.rotation = fighter.transform.rotation;
            layer.sharedMaterial = fighter.sharedMaterial;
            layer.sortingOrder = order;
            layer.enabled = false;
            return layer;
        }

        internal IEnumerator Play(Swing swing,bool dive,System.Action hurt)
        {
            var start = fighter.transform.position;
            var target = enemy.transform.position;
            var enemyRotation = enemy.transform.rotation;
            var cameraStart = camera.transform.position;
            float lens = camera.orthographicSize;
            Color stone = floor.color;
            var direction = (target - start).normalized;
            var contact = target - direction * 1.74f;
            bool heavy=swing==Swing.Overhead;
            bool reverse=swing==Swing.Backhand;
            var crouch = start - direction * .37f + Vector3.forward * (dive ? .44f : -.08f);
            var focus = direction * .48f + Vector3.forward * .45f;
            float weight = heavy ? 1.55f : dive ? 1.35f : 1f;
            int first = frames?.Get(swing.ToString()).from ?? 0;
            void Pose(int i) { if (frames != null) fighter.sprite = frames.FrameAt(first + i); }
            // All drawings share the same wrist-relative origin on a 19 px/unit grid.
            var traceOffset = direction * (17f/19f) - Vector3.forward * (3f/19f);
            if(heavy)traceOffset=direction*1.35f;
            trace.transform.rotation=fighter.transform.rotation * Quaternion.Euler(0,0,heavy ? -95 : 0);
            trace.flipY=reverse;
            trace.transform.localScale = heavy ? new Vector3(.46f,.74f,1) : new Vector3(1,dive ? 1 : .78f,1);
            void DrawEdge(Sprite sprite)
            {
                trace.sprite = sprite;
                trace.transform.position = fighter.transform.position + traceOffset;
                trace.enabled = true;
            }

            // Hold the compressed pose long enough to read the movement that follows.
            Pose(0);
            if (heavy) sound.PlayOneShot(overheadAir, .84f);
            float windup = heavy ? .30f : reverse ? .12f : .17f;
            for (float elapsed = 0; elapsed < windup; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / windup);
                Pose(t < .45f ? 0 : 1);
                fighter.transform.position = Vector3.Lerp(start, crouch, t);
                camera.orthographicSize = lens * (1 - .065f * t);
                camera.transform.position = cameraStart + focus * t;
                yield return null;
            }

            Pose(2);
            echo.sprite = fighter.sprite;
            echo.transform.position = crouch;
            echo.color = new Color(.36f,.60f,.65f,.63f);
            echo.enabled = true;
            particles.KickDust(start - Vector3.forward * .85f);
            if (!heavy) sound.PlayOneShot(reverse ? backhandAir : draw, .78f);
            const float dash = .08f;
            for (float elapsed = 0; elapsed < dash; elapsed += Time.deltaTime)
            {
                fighter.transform.position = Vector3.Lerp(crouch, contact, elapsed / dash) + (heavy ? Vector3.forward * .9f*Mathf.Sin(Mathf.PI*elapsed/dash) : Vector3.zero);
                DrawEdge(elapsed < dash * .45f ? gather : sweep);
                yield return null;
            }

            Pose(3);
            fighter.transform.position = contact;
            var impactPoint = target - direction * .12f + Vector3.forward * .14f;
            particles.Strike(impactPoint);
            hurt();
            AudioClip contactSound = heavy ? overheadHit : reverse ? backhandHit : hit;
            sound.PlayOneShot(contactSound, heavy || dive ? 1f : .90f);
            DrawEdge(fullCut);
            if(heavy)particles.KickDust(impactPoint-Vector3.forward*.85f);
            Color impactStone = Color.Lerp(stone, new Color(.18f,.21f,.28f), .80f);
            floor.color = impactStone;
            var cameraHit = cameraStart + focus + direction * (.16f * weight);
            camera.transform.position = cameraHit;
            camera.orthographicSize = lens * .88f;
            var stagger = Quaternion.AngleAxis(10 * weight, Vector3.up) * enemyRotation;
            enemy.transform.rotation = stagger;
            enemy.transform.position = target + direction * .14f;
            float stop = heavy ? .13f : dive ? .100f : .067f;
            for (float elapsed = 0; elapsed < stop; elapsed += Time.deltaTime) yield return null;

            Pose(4);
            const float recoil = .19f;
            for (float elapsed = 0; elapsed < recoil; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / recoil);
                float ease = 1 - (1-t)*(1-t)*(1-t);
                enemy.transform.position = target + direction * Mathf.Lerp(.14f,.76f*weight,ease);
                enemy.transform.rotation = Quaternion.Slerp(stagger, enemyRotation, t * .55f);
                fighter.transform.position = contact + direction * (.36f * ease);
                camera.transform.position = cameraStart + focus * (1 - .3f*t)
                    + direction * (Mathf.Cos(t*9) * .16f * weight * (1-t)*(1-t));
                camera.orthographicSize = Mathf.Lerp(lens*.88f, lens*.93f, ease);
                if (elapsed < .055f) DrawEdge(brokenEdge);
                else if (elapsed < .115f) DrawEdge(flecks);
                else trace.enabled = false;
                echo.color = new Color(.36f,.60f,.65f,.63f*(1-ease));
                floor.color = Color.Lerp(impactStone, stone, ease);
                yield return null;
            }

            echo.enabled = trace.enabled = false;
            Pose(5);
            var finish = fighter.transform.position;
            var pushed = enemy.transform.position;
            var lean = enemy.transform.rotation;
            var cameraFinish = camera.transform.position;
            float lensFinish = camera.orthographicSize;
            float recovery = heavy ? .25f : .16f;
            for (float elapsed = 0; elapsed < recovery; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / recovery);
                fighter.transform.position = Vector3.Lerp(finish,start,t) + Vector3.forward * (.27f*Mathf.Sin(t*Mathf.PI));
                enemy.transform.position = Vector3.Lerp(pushed,target,t);
                enemy.transform.rotation = Quaternion.Slerp(lean,enemyRotation,t);
                camera.transform.position = Vector3.Lerp(cameraFinish,cameraStart,t);
                camera.orthographicSize = Mathf.Lerp(lensFinish,lens,t);
                yield return null;
            }
            fighter.transform.position = start;
            enemy.transform.SetPositionAndRotation(target,enemyRotation);
            floor.color = stone;
            camera.transform.position = cameraStart;
            camera.orthographicSize = lens;
        }
    }
}
