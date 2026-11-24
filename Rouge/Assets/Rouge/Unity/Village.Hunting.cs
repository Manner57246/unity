using System.Collections;
using Rouge.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    public sealed partial class Village
    {
        private readonly BattleArena arena = new();
        private Camera mapCamera;
        private ExpeditionFoley foley;
        private ScoutFrames frames;
        private VillageHunt hunt;

        private IEnumerator StartHunt()
        {
            busy = true;
            Show(false);
            hunt = new VillageHunt();
            frames ??= new ScoutFrames("Village/Hunter");
            yield return arena.Open(hunt.Fight,frames,mapCamera,false,false,
                backdrop:"Village/Clearing",opponentSheet:"Village/Boar");
            map.style.display = drawer.style.display = DisplayStyle.None;
            combat.parent.style.backgroundColor = Color.clear;
            combat.style.display = DisplayStyle.Flex;
            busy = false;
            ShowHunt();
        }

        private void ShowHunt()
        {
            combat.Clear();
            var bar = Group(combat,"bar");
            var title = new Label("秋猎　·　东边林场"); title.AddToClassList("brand"); bar.Add(title);
            var health = new Label($"体力　{hunt.Health} / 7"); health.AddToClassList("status"); bar.Add(health);
            var panel = Group(combat,"panel");
            ExpeditionHud.RenderBattle(combat,panel,hunt.Fight,hunt.Card,foley,PlayHunt,
                ()=>{if (!busy) StartCoroutine(HuntEnemyTurn());},ShowHunt,"林地野猪");
        }

        private void PlayHunt(int index)
        {
            if (busy) return;
            var play = hunt.Fight.Play(index);
            if (play != null) StartCoroutine(UseTool(play));
        }

        private IEnumerator UseTool(CardPlay play)
        {
            busy = true;
            ShowHunt(); combat.SetEnabled(false);
            foley.PlayCard(play.Card.Type);
            yield return arena.Animate(play);
            if (hunt.Fight.EnemyHealth == 0)
            {
                yield return arena.Fall();
                yield return FinishHunt(true);
            }
            combat.SetEnabled(true); busy=false;
        }

        private IEnumerator HuntEnemyTurn()
        {
            busy = true; combat.SetEnabled(false);
            yield return arena.Act(hunt.Fight,hunt.EndTurn);
            if (hunt.Health == 0) yield return FinishHunt(false);
            else ShowHunt();
            combat.SetEnabled(true); busy=false;
        }

        private IEnumerator FinishHunt(bool success)
        {
            combat.Clear(); combat.style.display=DisplayStyle.None;
            combat.parent.style.backgroundColor=(Color)new Color32(52,72,68,255);
            yield return arena.Close(gameObject.scene,mapCamera);
            hunt=null; hunted=true; broughtMeat=success;
            date=date.AddDays(1);
            map.style.display=drawer.style.display=DisplayStyle.Flex;
            Refresh(); Show(true);
        }

        private void OnDestroy() => frames?.Dispose();
    }
}
