using System;
using System.Linq;
using Rouge.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    internal sealed partial class ExpeditionHud
    {
        private readonly VisualElement root;
        private readonly RunRules rules;
        private readonly Action restart, commit;
        internal RouteBoard Map { private get; set; }
        private readonly Action<int> travel, play;
        private readonly ExpeditionFoley foley;
        private RunSession run;
        private VisualElement panel;

        internal ExpeditionHud(VisualElement root, RunRules rules, Action restart, Action<int> travel, Action commit, Action<int> play, ExpeditionFoley foley,
            Action pause,Action home,System.Collections.Generic.IReadOnlyList<string> itinerary)
        {
            this.root = root;
            this.rules = rules;
            this.restart = restart;
            this.travel = travel;
            this.commit = commit;
            this.play = play;
            this.foley=foley;
            this.pause=pause;
            this.home=home;
            this.itinerary=itinerary;
            root.AddToClassList("expedition");
        }

        internal void Setup(Talent talent, Loadout pack, DeckStyle deckStyle, Action<Talent> setTalent, Action<Loadout> setPack, Action<DeckStyle> setDeck, Action next,Action back)
        {
            Landing("整备行装");
            Map.Overview(MapWindow(),null);
            var talents=Group(panel,"row");
            Label(talents,"天赋","prompt");
            Choice(talents,"翅膀 · 飞越障碍",talent==Talent.Wings,()=>setTalent(Talent.Wings));
            Choice(talents,"斥候 · 扩展视野",talent==Talent.Pathfinder,()=>setTalent(Talent.Pathfinder));
            Choice(talents,"渡湖 · 穿越水域",talent==Talent.Ferryman,()=>setTalent(Talent.Ferryman));
            Choice(talents,"攀岩 · 翻越断崖",talent==Talent.Climber,()=>setTalent(Talent.Climber));
            var decks=Group(panel,"row");
            Label(decks,"牌组","prompt");
            Choice(decks,"游侠 · 均衡",deckStyle==DeckStyle.Balanced,()=>setDeck(DeckStyle.Balanced));
            Choice(decks,"剑舞 · 连斩",deckStyle==DeckStyle.Blades,()=>setDeck(DeckStyle.Blades));
            Choice(decks,"铁卫 · 格挡",deckStyle==DeckStyle.Sentinel,()=>setDeck(DeckStyle.Sentinel));
            Choice(decks,"元素使 · 六系",deckStyle==DeckStyle.Arcanist,()=>setDeck(DeckStyle.Arcanist));
            var packs=Group(panel,"row");
            Label(packs,"出身","prompt");
            Choice(packs,"先遣队 · 移动点上限 +4",pack==Loadout.Provisions,()=>setPack(Loadout.Provisions));
            Choice(packs,"行商 · 多带 5 金币",pack==Loadout.Purse,()=>setPack(Loadout.Purse));
            Button(packs,"从营地出发",next);
            Button(packs,"返回标题",back);
        }

        internal void Atlas(WorldAtlas atlas,Action<int> enter,Action back)
        {
            Landing("边境行记");
            Map.Overview(MapWindow(),enter);
            Label(panel,"点选金色地标出发。在区域内选择岔路，走到出口即进入沿途所示的下一张地图。","note");
            if (back != null) Button(Group(panel,"row"),"调整行装",back);
        }

        private VisualElement MapWindow()
        {
            var window=new VisualElement();
            window.AddToClassList("map-window");
            root.Insert(1,window);
            return window;
        }

        internal void ZoomSurvey(int region,float t) => Map.Zoom(region,t);

        private void Landing(string heading)
        {
            root.Clear();
            var bar=Group(root,"bar");
            Crest(bar,"雾境远征");
            Label(bar,"世界地图","status");
            panel=Group(root,"panel");
            Label(panel,heading,"title");
        }

        private static void Choice(VisualElement parent,string text,bool selected,Action action)
        {
            Button(parent,text,action).EnableInClassList("selected",selected);
        }

        internal void Show(RunSession session)
        {
            run = session;
            if(run.Phase==RunPhase.Victory || run.Phase==RunPhase.Defeat){Result();return;}
            var battle = run.Battle;
            root.Clear();
            var bar = Group(root, "bar");
            Crest(bar,WorldAtlas.Names[run.Map.Region]);
            var stats = Group(bar,"vitals");
            Meter(stats,"生命",run.Health,rules.maxHealth,"health");
            Meter(stats,"移动点",run.Movement,run.MaxMovement,"movement");
            LedgerStat(stats,"金币",run.Gold,"coin");
            LedgerStat(stats,"牌组",run.Deck.Count,"cards");
            Button(stats,"菜单",pause).AddToClassList("menu-trigger");
            panel = Group(root, "panel");
            if(run.Phase!=RunPhase.Battle && run.Phase!=RunPhase.Map && run.Phase!=RunPhase.Travelling) Map.Local(MapWindow(),run,travel);
            switch (run.Phase)
            {
                case RunPhase.Map:
                case RunPhase.Travelling:
                    Map.Local(MapWindow(),run,travel);
                    var navigation=Group(panel,"nav-footer");
                    string note = run.Phase==RunPhase.Travelling ? "沿路线探索中…" : "悬停地标查看路线，双击地标出发。";
                    Label(navigation, $"第 {run.Current.Layer} 层   ·   {note}", "note");
                    if(run.Phase==RunPhase.Map && run.Movement<run.MaxMovement)
                        Button(navigation,"扎营补满移动点",()=>run.Regroup());
                    if (!string.IsNullOrEmpty(run.EventReport)) Label(panel,run.EventReport,"note");
                    break;
                case RunPhase.Battle:
                    RenderBattle(root,panel,battle,run.Card,foley,play,commit,()=>Show(run),battle.Enemy.Name());
                    break;
                case RunPhase.Reward:
                    Title("战利品", "选择一项带走");
                    var loot = Group(panel, "row");
                    loot.Add(new CardFace(run.Card(run.Offer),false));
                    Button(loot, "收入牌组", () => {if(run.ClaimReward(true))foley.Draw(1);});
                    Button(loot, $"领取 {rules.victoryGold} 金币", () => {if(run.ClaimReward(false))foley.Coins();});
                    break;
                case RunPhase.Shop:
                    Title("行商营地", $"今日货物   ·   {rules.shopPrice} 金币");
                    var shop = Group(panel, "row");
                    shop.Add(new CardFace(run.Card(run.Offer),false));
                    Button(shop, "购买卡牌", () => {if(run.ResolveShop(true))foley.Coins();})
                        .SetEnabled(run.Gold >= rules.shopPrice);
                    Button(shop, "离开", () => run.ResolveShop(false));
                    break;
                case RunPhase.Event:
                    RenderEvent();
                    break;
                case RunPhase.RoadsideResult:
                    Title("途中奇遇",run.EventReport);
                    Button(Group(panel,"row"),ContinueLabel(),()=>run.ContinueAfterEvent());
                    break;
                case RunPhase.Passage:
                    Label(panel,"前往 " + WorldAtlas.Names[run.Current.OnwardRegion],"title");
                    break;
            }
        }

        private string ContinueLabel() => run.Current.Kind switch {
            RoomKind.Battle or RoomKind.Boss => "迎战"+run.EnemyAt(run.Current).Name(),
            RoomKind.Shop => "进入行商营地", RoomKind.Passage => "进入"+WorldAtlas.Names[run.Current.OnwardRegion],
            _ => "探索此地"
        };

        private void RenderEvent()
        {
            var encounter=run.Encounter;
            var heading=Group(panel,"event-heading");
            Icon(heading,encounter.Kind.ToString(),"event-seal");
            var entry=Group(heading,"event-copy");
            Label(entry,(run.IsRoadside?"途中奇遇 · ":"")+encounter.Title,"title");
            Label(entry,encounter.Story,"note");
            if(run.Offer!=null)Label(entry,"可得卡牌　"+CardDescription(run.Offer),"event-loot");
            var row=Group(panel,"row");
            foreach(var choice in encounter.Choices)
            {
                var button=Button(row,"",()=>{
                    int cash=run.Gold, deckSize=run.Deck.Count;
                    if(!run.ChooseEvent(choice))return;
                    if(choice.Threshold>0)foley.Dice();
                    if(run.Gold!=cash)foley.Coins();
                    if(run.Deck.Count>deckSize)foley.Draw(1);
                });
                button.AddToClassList("event-choice");
                Label(button,choice.Name,"option-name");
                Label(button,choice.Terms,"option-terms");
                button.SetEnabled(run.CanChoose(choice));
            }
            Button(row,run.IsRoadside ? "绕过 · "+ContinueLabel() : "继续赶路",()=>run.LeaveEvent(encounter)).AddToClassList("leave-event");
        }

        internal void SetPlaying(bool active)
        {
            root.Q(className:"card-fan")?.SetEnabled(!active);
            root.Q<Button>(className:"end-action")?.SetEnabled(!active);
        }

        internal static void RenderBattle(VisualElement root, VisualElement panel, Battle battle,
            Func<string,CardRule> cardById, ExpeditionFoley foley, Action<int> play, Action commit, Action refresh, string enemy,
            Func<CardRule,string> continuation = null)
        {
            panel.AddToClassList("combat-readout");
            string status = battle.Airborne ? "   ·   升空" : "";
            if(battle.Frozen) status += "   ·   冻结";
            if(battle.LastElement!=Element.None)status+="   ·   上一系 "+Reaction.School(battle.LastElement);
            if(battle.Exposed>0) status += $"   ·   破绽 {battle.Exposed}";
            string armor=battle.Armor>0 ? $"   护甲 {battle.Armor}" : "";
            Label(panel,$"{enemy}   {battle.EnemyHealth} 生命"+armor,"title");
            Label(panel,$"第 {battle.Turn} 回合   ·   己方格挡 {battle.Guard}"+status,"note");
            Label(panel,battle.Frozen ? "敌方意图　冻结，本回合跳过行动" :
                $"敌方意图　{battle.Intent.Name} · {battle.Intent.Description}","enemy-intent");
            var counter=Group(root,"play-counter");
            Label(counter,"剩余出牌","counter-caption");
            Label(counter,$"{battle.Remaining} / {battle.Limit}","counter-number");
            if (!string.IsNullOrEmpty(battle.LastTurn)) Label(panel,battle.LastTurn,"combat-report");
            if (battle.Phase==TurnPhase.Draw)
            {
                Button(counter,"抽牌",()=>{
                    int before=battle.Hand.Count;
                    if(!battle.DrawHand())return;
                    foley.Draw(battle.Hand.Count-before);
                    refresh();
                });
                return;
            }
            if(battle.Phase==TurnPhase.Finished)return;
            Button(counter,"结束回合",commit).AddToClassList("end-action");
            if(battle.SlashChain>0)
                Label(panel,battle.SlashChain==1?"下张斩击 · 反手斩":"下张斩击 · 重劈 +4","chain-hint");
            var tray=Group(root,"card-fan");
            var cards=new CardFace[battle.Hand.Count];
            int hovered=-1;
            void Layout(int active)
            {
                hovered=active;
                float half=(cards.Length-1)*.5f;
                float width=tray.contentRect.width;
                float spacing=Mathf.Clamp((width-210)/Mathf.Max(1,cards.Length-1),24,90);
                for(int i=0;i<cards.Length;i++)
                {
                    float distance=i-half;
                    bool lifted=i==active;
                    float push=active<0 || lifted ? 0 : Mathf.Sign(i-active)*Mathf.Min(36,spacing*.42f);
                    var c=cards[i];
                    c.style.left=width*.5f-76+spacing*distance+push;
                    c.style.bottom=lifted ? 8 : -91-Mathf.Abs(distance)*4;
                    c.style.rotate=new Rotate(new Angle(lifted ? 0 : distance*5,AngleUnit.Degree));
                    c.EnableInClassList("fan-selected",lifted);
                }
                if(active>=0)cards[active].BringToFront();
            }
            for(int i=0;i<cards.Length;i++)
            {
                int slot=i;
                var rule=cardById(battle.Hand[i]);
                var face=new CardFace(rule,false,battle.PreviewReaction(rule));
                string hint=continuation?.Invoke(rule);
                if (!string.IsNullOrEmpty(hint) && battle.Remaining>0)
                {
                    face.AddToClassList("combo-ready");
                    face.Q<Label>(className:"card-title").text = rule.title+" · "+hint;
                }
                cards[i]=face;
                tray.Add(face);
                face.SetEnabled(battle.Remaining>0);
                face.RegisterCallback<PointerEnterEvent>(_=>Layout(slot));
                face.RegisterCallback<PointerLeaveEvent>(_=>{if(hovered==slot) Layout(-1);});
                face.Bind(root,rule,()=>play(slot),p=>{if(p.y<tray.worldBound.yMax-110)play(slot);});
            }
            tray.RegisterCallback<GeometryChangedEvent>(_=>Layout(hovered));
        }

        private string CardDescription(string id)
        {
            var card = run.Card(id);
            return $"【{card.Grade}】{card.title} · {card.Text}";
        }

        private static void Meter(VisualElement parent,string title,int value,int max,string style)
        {
            var meter=Group(parent,"stat-meter");
            meter.AddToClassList(style);
            var heading=Group(meter,"meter-head");
            var key=Group(heading,"meter-key");
            Icon(key,style=="health"?"life":"boot","small-icon");
            Label(key,title,"meter-label");
            Label(heading,$"{value} / {max}","meter-value");
            var track=Group(meter,"meter-track");
            var fill=Group(track,"meter-fill");
            fill.style.width=Length.Percent(100f*value/max);
        }

        private static void Icon(VisualElement parent,string art,string style)
        {
            var icon=Group(parent,style);
            icon.pickingMode=PickingMode.Ignore;
            icon.AddToClassList("icon-"+art);
        }

        private static void Crest(VisualElement bar,string title)
        {
            var crest=Group(bar,"crest");
            Icon(crest,"compass","compass");
            Label(crest,title,"brand");
        }

        private static void LedgerStat(VisualElement parent,string title,int value,string art)
        {
            var stat=Group(parent,"ledger-stat");
            Icon(stat,art,"ledger-icon");
            var copy=Group(stat,"ledger-copy");
            Label(copy,title,"ledger-key");
            Label(copy,value.ToString(),"ledger-value");
        }

        private void Title(string title, string note)
        {
            Label(panel, title, "title");
            Label(panel, note, "note");
        }

        private static string RoomName(RoomKind kind) => kind switch
        {
            RoomKind.Battle => "战斗", RoomKind.Boss => "守关者", RoomKind.Event => "事件",
            RoomKind.Shop => "商店", _ => "营地"
        };

        private static VisualElement Group(VisualElement parent, string style)
        {
            var group = new VisualElement();
            group.AddToClassList(style);
            parent.Add(group);
            return group;
        }

        private static void Label(VisualElement parent, string text, string style)
        {
            var label = new Label(text){pickingMode=PickingMode.Ignore};
            label.AddToClassList(style);
            parent.Add(label);
        }

        private static Button Button(VisualElement parent, string text, Action action)
        {
            var button = new Button(action) { text = text };
            parent.Add(button);
            return button;
        }
    }
}
