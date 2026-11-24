using System;
using System.Collections;
using Rouge.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rouge.Unity
{
    internal sealed class BattleArena
    {
        private SpriteRenderer fighter, target;
        private EnemyActor opponent;
        private SwordImpact impact;
        private BattleStrike strike;
        private SpellCast spells;
        private ScoutFrames scout;

        internal IEnumerator Open(Battle battle, ScoutFrames frames, Camera mapCamera, bool winged, bool boss, string snowStage = null,
            string backdrop = null, string opponentSheet = null)
        {
            scout = frames;
            yield return SceneManager.LoadSceneAsync("Battle",LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName("Battle");
            SceneManager.SetActiveScene(scene);
            mapCamera.enabled = false;
            Camera arenaCamera = null;
            SpriteRenderer floor = null;
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.TryGetComponent<Camera>(out var view)) arenaCamera = view;
                if (obj.name == "Inscribed nave") floor = obj.GetComponent<SpriteRenderer>();
                if (obj.name == "Seraph" || obj.name == "Adventurer")
                {
                    bool active = (obj.name == "Seraph") == winged;
                    obj.SetActive(active);
                    if (active) fighter = obj.GetComponent<SpriteRenderer>();
                }
                if ((obj.name=="Watchman" && !boss) ||
                    (obj.name=="Crowned watchman" && boss))
                    target=obj.GetComponent<SpriteRenderer>();
                if (obj.name == "Watchman") obj.SetActive(!boss);
                if (obj.name == "Crowned watchman") obj.SetActive(boss);
            }
            if (snowStage != null || backdrop != null)
            {
                floor.sprite = Resources.Load<Sprite>(backdrop ?? ("Frosthold/" + snowStage));
                var centerline = arenaCamera.transform.position.z;
                var offset = Vector3.forward * (centerline - fighter.transform.position.z);
                fighter.transform.position += offset;
                target.transform.position += offset;
            }
            fighter.sprite = scout.FrameAt(0);
            impact = new SwordImpact(fighter.transform,fighter.sharedMaterial);
            opponent=target.gameObject.AddComponent<EnemyActor>();
            opponent.Bind(battle,fighter,impact,opponentSheet ?? (snowStage=="ShadowHall" ? "Frosthold/Shadow" : null));
            spells = new GameObject("Elemental magic").AddComponent<SpellCast>();
            spells.View = arenaCamera;
            strike = new BattleStrike(fighter,target,floor,arenaCamera,impact,scout);
        }

        internal IEnumerator Close(Scene mapScene, Camera mapCamera)
        {
            SceneManager.SetActiveScene(mapScene);
            mapCamera.enabled = true;
            yield return SceneManager.UnloadSceneAsync("Battle");
        }

        internal IEnumerator Act(Battle battle, Action resolve) => opponent.Act(battle,resolve);
        internal IEnumerator Fall() => opponent.Fall();

        internal IEnumerator CloakFalls()
        {
            fighter.sprite = scout.FrameAt(0);
            var start = fighter.transform.position;
            var upright = fighter.transform.rotation;
            for (float elapsed=0; elapsed<.85f; elapsed+=Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0,1,elapsed/.8f);
                fighter.transform.rotation = upright * Quaternion.Euler(0,0,76*t);
                fighter.transform.position = start + Vector3.back * (.45f*t);
                fighter.color = Color.Lerp(Color.white,new Color(.42f,.46f,.53f),t);
                yield return null;
            }
        }

        internal void Idle(Battle battle)
        {
            fighter.sprite = scout.Loop("Idle", Time.unscaledTime);
            opponent.Idle(battle);
        }

        internal IEnumerator Animate(CardPlay play)
        {
            if (play.Card.Type == CardType.Spell)
                yield return spells.Play(play,fighter,target,opponent.Hurt);
            else if (play.Card.Type == CardType.Attack)
                yield return strike.Play(play.Swing, play.Dive,opponent.Hurt);
            else if (play.Card.effect == CardEffect.Lift)
            {
                var clip = scout.Get("Flap");
                for (int f=clip.from; f<=clip.to; f++)
                {
                    fighter.sprite=scout.FrameAt(f);
                    if (f==clip.from+2) impact.Lift(fighter.transform.position);
                    yield return new WaitForSeconds(scout.Duration(f));
                }
            }
            else yield return new WaitForSeconds(.12f);
        }
    }
}

