using System;
using System.Collections;
using Rouge.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class Frosthold : MonoBehaviour
    {
        [SerializeField] private StyleSheet interfaceStyle;
        [SerializeField] private StyleSheet fortressStyle;
        private static readonly Vector2[] Trail = {
            new(85,337),new(125,310),new(181,308),new(209,281),new(258,272),
            new(289,239),new(326,227),new(343,202),new(335,159),new(332,139),
            new(371,121),new(408,109),new(428,100),new(432,86)
        };
        private const int Outpost = 4, Gate = 12, Inside = 13;
        private readonly FrostholdAssault assault = new();
        private readonly BattleArena arena = new();
        private ScoutFrames frames;
        private Camera mapCamera;
        private VisualElement root, map, marker, sidebar, combat;
        private ScrollView drawer;
        private StoryReader reader;
        private Button destinationPin, here;
        private ExpeditionFoley foley;
        private int waypoint;
        private bool busy, inBattle, drawerOpen;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(interfaceStyle);
            root.styleSheets.Add(fortressStyle);
            root.AddToClassList("expedition");
            root.AddToClassList("frosthold");
            mapCamera = Camera.main;
            frames = new ScoutFrames("Frosthold/CloakCombat");
            foley = gameObject.AddComponent<ExpeditionFoley>();
            map = Group(root, "frost-map");
            map.style.backgroundImage = Plate("Pass");
            root.RegisterCallback<GeometryChangedEvent>(e => FitMap(e.newRect.size));
            marker = Group(map, "cloak");
            marker.style.backgroundImage = Plate("Cloak");
            marker.pickingMode = PickingMode.Ignore;
            Position(marker, Trail[0]);
            here = AddButton(map, "", () => SetDrawer(!drawerOpen));
            here.AddToClassList("frost-here");
            destinationPin = AddButton(map, "", () => Go(Next));
            destinationPin.AddToClassList("frost-pin");
            drawer = new ScrollView(ScrollViewMode.Vertical);
            drawer.AddToClassList("frost-side");
            root.Add(drawer);
            sidebar = drawer.contentContainer;
            reader = new StoryReader(drawer);
            combat = Group(root,"frost-combat");
            combat.style.display = DisplayStyle.None;
            RefreshMap(false);
        }

        private void FitMap(Vector2 size)
        {
            float scale = Mathf.Max(size.x / 640f, size.y / 400f);
            map.style.width = 640f * scale;
            map.style.height = 400f * scale;
            map.style.left = (size.x - 640f * scale) / 2;
            map.style.top = (size.y - 400f * scale) / 2;
        }
        private static Texture2D Plate(string name) => Resources.Load<Texture2D>("Frosthold/" + name);

        private static VisualElement Group(VisualElement parent, string style)
        {
            var item = new VisualElement();
            item.AddToClassList(style);
            parent.Add(item);
            return item;
        }

        private static Label Text(VisualElement parent, string value, string style)
        {
            var label = new Label(value);
            label.AddToClassList(style);
            parent.Add(label);
            return label;
        }

        private static Button AddButton(VisualElement parent, string label, Action act)
        {
            var button = new Button(act) { text = label };
            parent.Add(button);
            return button;
        }

        private static void Position(VisualElement item, Vector2 point)
        {
            item.style.left = Length.Percent(point.x / 640f * 100);
            item.style.top = Length.Percent(point.y / 400f * 100);
        }

        private int Next => waypoint < Outpost ? Outpost : waypoint < Gate ? Gate : Inside;
        private bool Encounter => (waypoint == Outpost && assault.Victories == 0) ||
                                  (waypoint == Gate && assault.Victories == 1) ||
                                  (waypoint == Inside && assault.Victories == 2);

        private void SetDrawer(bool open)
        {
            drawerOpen = open;
            drawer.EnableInClassList("frost-open",open);
            drawer.SetEnabled(open);
        }

        private void Go(int destination)
        {
            if (busy || Encounter || assault.Health == 0 || destination <= waypoint) return;
            StartCoroutine(Walk(destination));
        }

        private IEnumerator Walk(int destination)
        {
            busy = true;
            SetDrawer(false);
            here.style.display = destinationPin.style.display = DisplayStyle.None;
            while (waypoint < destination)
            {
                Vector2 from = Trail[waypoint], to = Trail[waypoint+1];
                float duration = Vector2.Distance(from,to) / 145f;
                for (float t=0; t<duration; t+=Time.deltaTime)
                {
                    var pos = Vector2.Lerp(from,to,t/duration);
                    pos.y -= Mathf.Abs(Mathf.Sin(Time.time*14))*1.4f;
                    Position(marker,pos);
                    yield return null;
                }
                waypoint++;
            }
            Position(marker,Trail[waypoint]);
            busy = false;
            RefreshMap(true);
        }

        private void RefreshMap(bool showDrawer)
        {
            bool lost = assault.Health == 0;
            bool interior = waypoint == Inside;
            string site = interior ? "堡内影厅" : waypoint == Gate ? "北门" : waypoint == Outpost ? "风口哨站" : "雪线营地";
            string route = Next == Outpost ? "前往哨站" : Next == Gate ? "跨桥攻向北门" : "进入堡垒";
            here.text = site;
            Position(here,Trail[waypoint]);
            here.style.display = DisplayStyle.Flex;
            destinationPin.text = route;
            Position(destinationPin,Trail[Next]);
            destinationPin.style.display = interior || Encounter || lost ? DisplayStyle.None : DisplayStyle.Flex;
            sidebar.Clear();
            var image = Group(sidebar,"frost-plate");
            StoryPlate.Fit(image);
            image.style.backgroundImage = Plate(interior ? "Courtyard" : waypoint==Gate ? "Gate" : waypoint==Outpost ? "Watch" : "Ridge");
            Text(sidebar, lost ? "风雪吞没了足迹" : site, "title");
            if (lost)
            {
                AddButton(sidebar,"重玩序章",()=>SceneManager.LoadScene("Frosthold"));
            }
            else if (Encounter)
            {
                Text(sidebar,interior ? "壁灯照着一把空椅。你停下脚步，椅前的黑影却又向前走了一步。你握住刀柄，它也抬起了手。" :
                    waypoint==Gate ? "守关者拽紧锁链。先打出水牌，再接冰牌，可以冻结他一回合。" :
                    "抽牌后，连续打出三张斩击。斩星和永夜返刃都能接；中途改用法术会断开刀势。","note");
                AddButton(sidebar,"迎战",()=>{if(!busy) StartCoroutine(EnterBattle());}).AddToClassList("cover-primary");
            }
            else
            {
                Text(sidebar,waypoint==0 ? "黑斗篷下，六张旧牌在发烫。山腰的哨兵已经举起号角。" :
                    waypoint==Outpost ? "哨火熄灭，北门前只剩最后一班守卫。" : "铁闸脱离绞盘，门缝里透出一线暖光。","note");
                AddButton(sidebar,route,()=>Go(Next));
            }
            reader.Prepare(image);
            AddButton(sidebar,"收起",()=>SetDrawer(false));
            AddButton(sidebar,"返回标题",()=>SceneManager.LoadScene("Expedition"));
            SetDrawer(showDrawer);
        }

        private IEnumerator EnterBattle()
        {
            busy = true;
            SetDrawer(false);
            map.SetEnabled(false);
            assault.Engage();
            yield return arena.Open(assault.Fight, frames, mapCamera, false, waypoint==Gate,
                waypoint==Inside ? "ShadowHall" : waypoint==Gate ? "NorthGate" : "Windpost");
            map.style.display = drawer.style.display = DisplayStyle.None;
            combat.style.display = DisplayStyle.Flex;
            inBattle = true;
            busy = false;
            ShowBattle();
        }

        private void ShowBattle()
        {
            combat.Clear();
            var bar = Group(combat,"bar");
            Text(bar,waypoint==Inside ? "霜脊堡 · 影厅" : waypoint==Gate ? "霜脊堡 · 北门" : "霜脊堡 · 风口哨站","brand");
            Text(bar,$"黑斗篷　　生命 {assault.Health} / 36","status");
            var panel = Group(combat,"panel");
            ExpeditionHud.RenderBattle(combat,panel,assault.Fight,assault.Card,foley,Play,Commit,ShowBattle,
                waypoint==Inside ? "无面黑影" : waypoint==Gate ? "北门守关者" : "风口哨卫", NextCombo);
            ShowComboGuide(assault.Fight);
        }

        private string NextCombo(CardRule card)
        {
            var fight = assault.Fight;
            var reaction = fight.PreviewReaction(card);
            if (reaction != null) return reaction.Name;
            if (card.IsSlash && fight.SlashChain > 0) return fight.SlashChain == 2 ? "重劈" : "反手";
            if (fight.TurnPlays.Count != 0) return null;
            if (waypoint == Outpost && card.IsSlash) return "起手";
            if (waypoint == Gate && card.School == Element.Water) return "起手";
            return null;
        }

        private void ShowComboGuide(Battle fight)
        {
            var guide = Group(combat,"combo-guide");
            Text(guide,$"本回合出牌　{fight.TurnPlays.Count} / {fight.Limit}","combo-heading");
            var slots = Group(guide,"combo-slots");
            for (int i=0; i<fight.Limit; i++)
            {
                var slot = Group(slots,"combo-slot");
                if (i >= fight.TurnPlays.Count) continue;
                var play = fight.TurnPlays[i];
                slot.style.backgroundImage = Resources.Load<Texture2D>("Folio/Cards/"+play.Card.id);
                slot.tooltip = play.Report;
            }
            string tip = waypoint == Outpost ? "连续三张斩击：正斩 → 反手 → 重劈（+4）" :
                "水 → 冰：冻结　　水 → 雷：导电（+5）";
            if (fight.Remaining == 0 || (fight.Phase == TurnPhase.Plan && fight.Hand.Count == 0)) tip = "本轮出牌完毕，结束回合后重新起手。";
            else if (fight.SlashChain > 0 && System.Linq.Enumerable.Any(fight.Hand,id=>assault.Card(id).IsSlash))
                tip = fight.SlashChain == 1 ? "接亮边斩击 → 反手斩" : "再接一张亮边斩击 → 重劈，伤害 +4";
            else if (fight.LastElement != Element.None)
            {
                var next = new System.Collections.Generic.List<string>();
                foreach (string id in fight.Hand)
                {
                    var card = assault.Card(id);
                    var reaction = fight.PreviewReaction(card);
                    if (reaction != null) next.Add(card.title+" → "+reaction.Name);
                }
                if (next.Count > 0) tip = string.Join("　/　",next);
            }
            Text(guide,tip,"combo-tip");
        }

        private void Play(int index)
        {
            if (busy || !inBattle) return;
            var play = assault.Play(index);
            if (play != null) StartCoroutine(AnimatePlay(play));
        }

        private IEnumerator AnimatePlay(CardPlay play)
        {
            busy = true;
            foley.PlayCard(play.Card.Type);
            if (assault.Fight != null) ShowBattle();
            else combat.Q(className:"card-fan")?.RemoveFromHierarchy();
            string combo = play.Reaction?.Name ?? (play.Swing == Swing.Overhead ? "三连 · 重劈" :
                play.Swing == Swing.Backhand ? "二连 · 反手斩" : null);
            if (combo != null)
            {
                var banner = Group(combat,"combo-banner");
                Text(banner,combo,"combo-name");
                Text(banner,play.Reaction?.Description ?? play.Report,"combo-tip");
                banner.schedule.Execute(()=>banner.RemoveFromHierarchy()).StartingIn(1250);
            }
            combat.SetEnabled(false);
            yield return arena.Animate(play);
            if (assault.Fight == null)
            {
                combat.Clear();
                yield return arena.Fall();
                if (waypoint == Inside)
                {
                    yield return EndPrologue();
                    yield break;
                }
                yield return LeaveBattle();
            }
            combat.SetEnabled(true);
            busy = false;
        }

        private IEnumerator EndPrologue()
        {
            var epilogue = Group(combat,"frost-epilogue");
            Text(epilogue,"胜利 · 黑影已消散","title");
            yield return new WaitForSeconds(1.6f);
            Text(epilogue,"刀已入鞘。你低头，灯下空空的。刚才倒下的，是你的影子。","note");
            yield return new WaitForSeconds(2.1f);
            yield return arena.CloakFalls();
            assault.FinishStory();
            Text(epilogue,"影子散尽，黑斗篷也停止了呼吸。","note");
            Text(epilogue,"序章完","title");
            AddButton(epilogue,"进入第一章 · 门前的梨树",()=>SceneManager.LoadScene("Village"));
            AddButton(epilogue,"重玩序章",()=>SceneManager.LoadScene("Frosthold"));
            AddButton(epilogue,"返回标题",()=>SceneManager.LoadScene("Expedition"));
            combat.SetEnabled(true);
            busy = false;
        }

        private void Commit()
        {
            if (!busy && inBattle && assault.Fight?.Phase == TurnPhase.Plan) StartCoroutine(EnemyTurn());
        }

        private IEnumerator EnemyTurn()
        {
            busy = true;
            combat.SetEnabled(false);
            yield return arena.Act(assault.Fight,assault.EndTurn);
            if (assault.Fight == null) yield return LeaveBattle();
            else ShowBattle();
            combat.SetEnabled(true);
            busy = false;
        }

        private IEnumerator LeaveBattle()
        {
            inBattle = false;
            combat.style.display = DisplayStyle.None;
            yield return arena.Close(gameObject.scene,mapCamera);
            map.style.display = drawer.style.display = DisplayStyle.Flex;
            map.SetEnabled(true);
            RefreshMap(true);
        }

        private void Update()
        {
            if (inBattle && !busy && assault.Fight != null) arena.Idle(assault.Fight);
            if (!inBattle && !busy && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true)
                SetDrawer(!drawerOpen);
        }

        private void OnDestroy() => frames?.Dispose();
    }
}

