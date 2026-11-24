using System;
using System.Collections;
using Rouge.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class Village : MonoBehaviour
    {
        [SerializeField] private StyleSheet interfaceStyle;
        [SerializeField] private StyleSheet mapStyle;
        private enum Site { Home, Mill, Green, Wood, Ford, South }
        private enum Season { Spring, Summer, Autumn, Winter }
        private enum Evening { Undecided, Pears, Mill }
        private enum Raid { Quiet, Riders, Flight, Door, AfterDoor }
        private static readonly string[] Names = { "梨树下的家", "水磨坊", "村口井台", "东边林场", "梨湾浅口", "南岸旧渡屋" };
        private static readonly Vector2[] Places = { new(186,145), new(107,156), new(360,151), new(544,82), new(212,192), new(212,296) };
        private static readonly DateTime[] Dates = {
            new(1000,3,4), new(1000,6,18), new(1000,9,23), new(1000,12,8)
        };
        private VisualElement map, walker, menu, plate, combat;
        private ScrollView drawer;
        private StoryReader reader;
        private readonly Button[] pins = new Button[Places.Length];
        private DateTime date = Dates[0];
        private Site site;
        private Season season;
        private Evening evening;
        private Raid raid;
        private bool doorOpened;
        private bool woodStacked, flour, metElder, hunted, broughtMeat, open, busy;
        private Vector2 position = Places[0];

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(interfaceStyle);
            root.styleSheets.Add(mapStyle);
            root.AddToClassList("expedition");
            root.AddToClassList("frosthold");
            root.style.backgroundColor = (Color)new Color32(52,72,68,255);
            map = Group(root,"frost-map");
            walker = Group(map,"cloak");
            walker.style.width = 23; walker.style.height = 31;
            walker.pickingMode = PickingMode.Ignore;
            Paint(walker,"Crossing/Ferryman");
            for (int i=0; i<Places.Length; i++)
            {
                var dest = (Site)i;
                pins[i] = new Button(()=>Visit(dest)) { text=Names[i] };
                pins[i].AddToClassList("frost-pin"); map.Add(pins[i]); Locate(pins[i],Places[i]);
            }
            map.RegisterCallback<ClickEvent>(evt=>
            {
                if (busy || hunt != null || evt.target != map || raid == Raid.Door || raid == Raid.Riders) return;
                Vector2 local = map.WorldToLocal(evt.position);
                int cell = RiverGrid.ToCell(new Vector2(local.x*640/map.layout.width, local.y*400/map.layout.height));
                if (Walkable(cell)) StartCoroutine(Walk(RiverGrid.FromCell(cell),null));
            });
            root.RegisterCallback<GeometryChangedEvent>(evt=>
            {
                Vector2 size = evt.newRect.size;
                // The inhabited strip stays in frame on wide screens.
                float scale = Mathf.Min(size.x/600f, size.y/265f);
                map.style.width = 640*scale; map.style.height = 400*scale;
                map.style.left = (size.x-640*scale)/2; map.style.top = (size.y-400*scale)*.34f;
            });
            drawer = new ScrollView(); drawer.AddToClassList("frost-side"); root.Add(drawer);
            menu = drawer.contentContainer;
            reader = new StoryReader(drawer);
            combat = Group(root,"frost-combat"); combat.style.display = DisplayStyle.None;
            mapCamera = Camera.main;
            foley = gameObject.AddComponent<ExpeditionFoley>();
            Refresh();
            Show(true);
        }

        private static VisualElement Group(VisualElement parent,string style)
        {
            var e = new VisualElement(); e.AddToClassList(style); parent.Add(e); return e;
        }
        private static void Paint(VisualElement e,string path) =>
            e.style.backgroundImage = Resources.Load<Texture2D>(path);
        private static void Locate(VisualElement e,Vector2 p)
        {
            e.style.left = Length.Percent(p.x/640*100); e.style.top = Length.Percent(p.y/400*100);
        }
        private bool Walkable(int cell)
        {
            var p = RiverGrid.FromCell(cell);
            return (season == Season.Winter && Mathf.Abs(p.x-212) <= 12) ||
                   Mathf.Abs(p.y-RiverGrid.RiverY(p.x)) > 28+5*Mathf.Sin(p.x/62) ||
                   Mathf.Abs(p.x-(483-(p.y-RiverGrid.RiverY(483))*.39f)) < 12;
        }
        private static bool IsVillageCentre(Site location) => location == Site.Home || location == Site.Mill || location == Site.Green;

        private void Visit(Site dest)
        {
            if (busy || hunt != null) return;
            if (raid == Raid.Door || raid == Raid.Riders) { Show(true); return; }
            if (season == Season.Winter && raid == Raid.Quiet && IsVillageCentre(dest)) dest = Site.Green;
            StartCoroutine(Walk(Places[(int)dest],dest));
        }
        private IEnumerator Walk(Vector2 end,Site? dest)
        {
            var path = RiverGrid.Path(position,end,Walkable);
            if (path == null) yield break;
            busy = true; Show(false);
            while (path.Count > 0)
            {
                var step = path.Peek();
                position = Vector2.MoveTowards(position,step,(raid == Raid.Flight ? 225 : 125)*Time.deltaTime);
                Locate(walker,position);
                if (position == step) path.Pop();
                if (season == Season.Winter && raid == Raid.Quiet && Vector2.Distance(position,Places[(int)Site.Green]) < 12)
                {
                    raid = Raid.Riders; dest = Site.Green; break;
                }
                if (raid == Raid.Flight && Vector2.Distance(position,Places[0]) < 5) { dest=Site.Home; break; }
                yield return null;
            }
            busy = false;
            if (!dest.HasValue)
            {
                for (int i=0; i<Places.Length; i++)
                    if ((i != (int)Site.South || season == Season.Winter) && Vector2.Distance(position,Places[i]) < 8) { dest=(Site)i; break; }
            }
            if (!dest.HasValue) yield break;
            if (season == Season.Winter && raid == Raid.Quiet && IsVillageCentre(dest.Value))
            {
                yield return Walk(Places[(int)Site.Green],Site.Green);
                yield break;
            }
            site = dest.Value;
            if (raid == Raid.Flight && site == Site.Home) raid = Raid.Door;
            Refresh(); Show(true);
        }
        private void Show(bool value)
        {
            open = value; drawer.EnableInClassList("frost-open",value); drawer.SetEnabled(value);
        }
        private void Update()
        {
            if (busy) return;
            if (hunt != null) arena.Idle(hunt.Fight);
            else if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true &&
                     (open || raid == Raid.Flight || raid == Raid.Riders || raid == Raid.Door || Vector2.Distance(position,Places[(int)site]) < 8)) Show(!open);
        }
        private void Text(string value,string style="note")
        {
            var label=new Label(value); label.AddToClassList(style); menu.Add(label);
        }
        private void Choice(string label,Action action)
        {
            menu.Add(new Button(()=>{ if (!busy) action(); }) { text=label });
        }
        private void Refresh()
        {
            string terrain = "Village/"+season;
            Paint(map,terrain); Locate(walker,position);
            for (int i=0;i<pins.Length;i++)
                pins[i].style.display = (raid == Raid.Flight && i != 0) || (i == (int)Site.South && season != Season.Winter)
                    ? DisplayStyle.None : DisplayStyle.Flex;
            pins[(int)Site.Ford].text = season == Season.Winter ? "梨湾 · 冰道" : Names[(int)Site.Ford];
            if (dawn != Dawn.None) pins[(int)DawnSite].text=DawnLandmark;
            menu.Clear(); drawer.scrollOffset=Vector2.zero;
            plate = Group(menu,"frost-plate"); StoryPlate.Fit(plate);
            string art = site switch { Site.Mill=>"Village/MillHouse", Site.Wood=>"Village/Forest",
                Site.Ford=>season == Season.Winter ? "Village/Ice" : "Crossing/Landing", Site.South=>"Village/Ice",
                Site.Green=>"Village/Well", _=>"Village/Hearth" };
            if (raid == Raid.Riders) art="Village/Riders";
            if (raid == Raid.Flight) art="Village/Scatter";
            if (raid == Raid.Door) art="Village/Door";
            if (nightSettled && site == Site.South)
                art=nightChoice == NightChoice.Ring ? "Village/Bells" : "Village/Refuge";
            if (nightSettled && site == Site.Mill && nightChoice == NightChoice.Shelter) art="Village/Cellar";
            if (dawn != Dawn.None)
                art=site switch {
                    Site.South or Site.Ford => "Village/Ice", Site.Mill=>"Village/MillHouse", _=>art
                };
            Paint(plate,art);
            Text($"第一章　门前的梨树　·　{date.Month}月{date.Day}日{(dawn != Dawn.None ? " 清晨" : nightSettled ? " 入夜" : "")}","note");
            Text(Names[(int)site],"title");
            if (raid == Raid.AfterDoor) AfterDoorPage();
            else if (raid != Raid.Quiet) RaidPage();
            else if (site == Site.Ford || site == Site.South) RiverPage();
            else if (season == Season.Winter) WinterPage();
            else if (season == Season.Spring) SpringPage();
            else if (season == Season.Summer) SummerPage();
            else AutumnPage();
            reader.Prepare(plate);
            Choice("收起",()=>Show(false));
            Choice("返回标题",()=>SceneManager.LoadScene("Expedition"));
        }
        private void NextSeason(Season next,Site start)
        {
            season=next; date=Dates[(int)next]; site=start; position=Places[(int)start]; Refresh();
        }
        private void SpringPage()
        {
            switch (site)
            {
                case Site.Home:
                    Text(woodStacked ? "柴垛已经高过窗沿。莉娅从后门探出头：“去磨坊讨点面粉吧。顺路把井台边那个老头叫回来吃饭。”" :
                        "你抱着一捆湿柴进院，腾不出手。屈膝顶了两下门，妻子莉娅就在里面替你拨开了闩。“一听就知道是你。”她把最干的那根抽走，塞进灶膛。");
                    if (!woodStacked) Choice("劈柴，和莉娅垒好柴垛 · 1 天",()=>{woodStacked=true; date=date.AddDays(1); Refresh();});
                    if (woodStacked && flour && metElder)
                        Choice("在村里接春工，做到 6 月 18 日",()=>NextSeason(Season.Summer,Site.Home));
                    break;
                case Site.Mill:
                    Text(flour ? "磨坊主蓓特用炭在你的面袋上画了一只歪梨。“省得下次又拿成我的。”" :
                        "蓓特一肩抵着磨盘，冲你踢来一只空袋：“帮我装完这一车。袋底剩下的，归你们两口子。”");
                    if (!flour) Choice("装完这车面粉 · 1 天",()=>{flour=true; date=date.AddDays(1); Refresh();});
                    break;
                case Site.Green:
                    Text(metElder ? "村长递回木勺。“跟莉娅说，多留口汤。我把这道毛刺磨掉就来。”" :
                        "你听见井下有人哼歌。老村长趴在井沿，正替孩子捞一只木勺。他一抬头，帽子也掉进去了。");
                    if (!metElder) Choice("替他拽住井绳",()=>{metElder=true; Refresh();});
                    break;
                default: Text("林边的泥软得没过鞋面。猎人正给一把缺口小刀磨刃，说等秋天土干了再带你进山。"); break;
            }
        }
        private void SummerPage()
        {
            if (site == Site.Home)
            {
                Text(evening == Evening.Undecided ? "梨压弯了枝。莉娅找来梯子，想趁今晚把梨切片晒干。河湾那头的磨坊也在招手，蓓特得赶在涨水前把最后一车粮推上坡。天亮就要落雨。" :
                    evening == Evening.Pears ? "屋檐挂了七串梨干。莉娅在最高的一串上咬了个牙印：“这串留到落雪。”" :
                    "夜里果然下了雨。落梨摔烂了一半，门口却多了一袋磨坊送来的白面。莉娅拿手背蹭掉你眉心的粉。");
                if (evening == Evening.Undecided) Choice("留下陪莉娅收梨 · 今晚",()=>SpendEvening(Evening.Pears));
                else Choice("做完夏天的农活，到 9 月 23 日秋收",()=>NextSeason(Season.Autumn,Site.Home));
            }
            else if (site == Site.Mill)
            {
                Text(evening == Evening.Undecided ? "蓓特一脚蹬住往下滑的板车。坡头的云翻得发黑。若留下推粮，今晚便赶不上替莉娅扶梯子了。" :
                    evening == Evening.Mill ? "蓓特把后院的闩一拨：“这条巷直通你家。往后想抄近路就走这里。”" : "湿面摊了一地。蓓特脱了鞋，在石阶上拧袜子。");
                if (evening == Evening.Undecided) Choice("留下帮蓓特推粮 · 今晚",()=>SpendEvening(Evening.Mill));
            }
            else Text(site == Site.Green ? "村长把捞回来的木勺拴在井架上。过路的人舀了水，总会把勺子挂回原处。" : "林口堆着一季的干枝。收完秋粮才是进山的时候。");
        }
        private void SpendEvening(Evening choice) { evening=choice; date=date.AddDays(1); Refresh(); }

        private void AutumnPage()
        {
            if (site == Site.Wood)
            {
                Text(hunted ? broughtMeat ? "你和猎人把肉抬回林棚。熏架上终于挂起了东西。" : "猎人扶你坐回林棚，把最后一块饼掰成两半。两个人空着手也得吃饭。" :
                    "一头野猪正在翻拱落果。猎人把那把缺口小刀塞给你，自己绕去下风口。你手里还有一根拨草的木棍。");
                if (!hunted) Choice("跟猎人进林 · 1 天",()=>StartCoroutine(StartHunt()));
                else Choice("留在林场做冬工，到 12 月 8 日结清工钱",()=>NextSeason(Season.Winter,Site.Wood));
            }
            else Text(site switch {
                Site.Home => "莉娅替你把鞋底的线拉紧，面饼装进布袋。“去东边林场吧。路上饿了先吃，别又留到发硬。”",
                Site.Green => "村长把猎人扔在井边的绳圈递给你：“去林场的话，替我捎上。”",
                _ => "新麦磨起来呛鼻。蓓特往你手里按了一把炒麦粒，留着进林时嚼。"
            });
        }
        private void WinterPage()
        {
            Text("今天结了冬工。河心还有流水，梨湾两根红桩之间却冻成了一整片亮冰。隔着林子望得见家那头的烟。莉娅要你顺道把井台边借的桶带回来。");
            Choice("沿来路回村",()=>Visit(Site.Green));
        }
        private void RaidPage()
        {
            if (raid == Raid.Riders)
            {
                Text("井台边拴满了马。你从两排肩膀之间认出王旗上的金冠。村长手里还拎着木勺，正踮起脚跟骑手讲话。马嚼子叮叮地响，你一个字也听不清。");
                Choice("踮脚，再看一眼",()=>StartCoroutine(StrikeAtWell()));
            }
            else if (raid == Raid.Flight)
            {
                Text("木勺在井沿上弹了一下。村长倒在马蹄前，再没动。围着井的人一下子全散了。你被撞得踉跄，只想到莉娅还在家里。");
                if (evening == Evening.Mill) Choice("跟着蓓特，从磨坊后巷跑回家",()=>StartCoroutine(MillLane()));
                Choice("沿平日的路跑回家",()=>Visit(Site.Home));
            }
            else
            {
                if (doorOpened) { DoorChoices(); return; }
                Text(evening == Evening.Pears ? "你冲进院子，额头碰上那串梨干。最上面的一片还带着她的牙印。" :
                    "门槛上留着面粉印。你踩过去，用肩膀顶住了门。");
                Text("“莉娅？”你想起她认得你的敲门声。你在门板上敲了两下。");
                Choice("等她应门",()=>{doorOpened=true; Refresh();});
            }
        }
        private IEnumerator MillLane()
        {
            yield return Walk(new Vector2(259,132),null);
            yield return Walk(Places[(int)Site.Home],Site.Home);
        }
    }
}
