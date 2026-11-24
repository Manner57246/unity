using static Rouge.Unity.RiverGrid;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class Crossing : MonoBehaviour
    {
        [SerializeField] private StyleSheet interfaceStyle;
        [SerializeField] private StyleSheet mapStyle;
        private enum Site { Landing, Mill, Bridge, Beach, Channel }
        private enum Fare { None, Shadow, Workers, Hull }
        private static readonly string[] Names = { "北岸渡口", "旧磨坊", "铁链桥", "浅滩", "旧水道" };
        private static readonly Vector2[] Landmarks = {
            new(303,193), new(120,158), new(471,133), new(376,242), new(88,188)
        };
        private static readonly Vector2[] Berths = {
            new(313,219), new(125,210), new(470,145), new(376,242), new(88,188)
        };
        private VisualElement map, boat, walker, picture, menu;
        private ScrollView drawer;
        private StoryReader reader;
        private Button[] pins;
        private Site site;
        private Fare fare;
        private bool raised, rabbitFixed, told, silent, channel, savedWindow, returned, open, busy;
        private int halfDays;
        private Vector2 boatAt, walkerAt;
        private int Day => 1 + halfDays / 2;
        private bool Flood => Day >= 24;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(interfaceStyle);
            root.styleSheets.Add(mapStyle);
            root.AddToClassList("expedition");
            root.AddToClassList("frosthold");
            map = new VisualElement();
            root.Add(map);
            map.AddToClassList("frost-map");
            Paint(map,"River");
            map.RegisterCallback<ClickEvent>(e=>
            {
                if(busy || e.target!=map || map.layout.width<=0)return;
                var local=map.WorldToLocal(e.position);
                var point=new Vector2(local.x*640/map.layout.width,local.y*400/map.layout.height);
                var cell=ToCell(point);
                if(OnFoot(cell))StartCoroutine(WalkTo(FromCell(cell),null));
            });
            root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                var size=e.newRect.size;
                // Keep every landmark reachable, including on narrow Game views.
                float scale=Mathf.Min(size.x/640f,size.y/400f);
                map.style.width=640*scale; map.style.height=400*scale;
                map.style.left=(size.x-640*scale)/2; map.style.top=(size.y-400*scale)/2;
            });
            root.style.backgroundColor=(Color)new Color32(56,81,82,255);
            boat=Token("Boat",44,27); walker=Token("Ferryman",22,30);
            boatAt=Berths[0]; walkerAt=Landmarks[0];
            pins=new Button[Names.Length];
            for (int i=0;i<pins.Length;i++)
            {
                var destination=(Site)i;
                var pin=new Button(()=>Visit(destination));
                pins[i]=pin; map.Add(pin); pin.AddToClassList("frost-pin");
                Locate(pin,Landmarks[i]);
            }
            drawer=new ScrollView(ScrollViewMode.Vertical);
            drawer.AddToClassList("frost-side"); root.Add(drawer); menu=drawer.contentContainer;
            reader = new StoryReader(drawer);
            Render();
            drawer.schedule.Execute(()=>Show(true)).StartingIn(120);
        }
        private static void Paint(VisualElement e,string asset)
        {
            e.style.backgroundImage=Resources.Load<Texture2D>("Crossing/"+asset);
            e.style.backgroundSize=new BackgroundSize(Length.Percent(100),Length.Percent(100));
        }
        private VisualElement Token(string asset,int width,int height)
        {
            var e=new VisualElement { pickingMode=PickingMode.Ignore };
            e.style.position=Position.Absolute; e.style.width=width; e.style.height=height;
            e.style.translate=new Translate(Length.Percent(-50),Length.Percent(-95));
            Paint(e,asset);map.Add(e);return e;
        }
        private static void Locate(VisualElement e,Vector2 point)
        {
            e.style.left=Length.Percent(point.x/640*100);
            e.style.top=Length.Percent(point.y/400*100);
        }
        private void Show(bool value)
        {
            open=value; drawer.EnableInClassList("frost-open",value); drawer.SetEnabled(value);
        }
        private void Update()
        {
            if (!busy && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true) Show(!open);
        }
        private void Visit(Site destination)
        {
            if (busy) return;
            if(site==destination && Vector2.Distance(walkerAt,Landmarks[(int)destination])<5){Show(true);return;}
            StartCoroutine(WalkTo(Landmarks[(int)destination],destination));
        }
        private bool OnFoot(int cell)
        {
            Vector2 p=FromCell(cell);
            if(Mathf.Abs(p.y-RiverY(p.x))>28+5*Mathf.Sin(p.x/62))return true;
            if(!Flood && Mathf.Abs(p.x-(483-(p.y-RiverY(483))*.39f))<12)return true;
            foreach(var land in Landmarks)if(Vector2.Distance(p,land)<16)return true;
            return false;
        }
        private IEnumerator WalkTo(Vector2 end, Site? destination)
        {
            var path=RiverGrid.Path(walkerAt,end,OnFoot);
            if(path==null)yield break;
            busy=true;Show(false);
            while(path.Count>0)
            {
                var next=path.Peek();
                walkerAt=Vector2.MoveTowards(walkerAt,next,200*Time.deltaTime);
                Locate(walker,walkerAt);if(walkerAt==next)path.Pop();
                yield return null;
            }
            busy=false;
            if(destination.HasValue){site=destination.Value;Render();Show(true);}
        }
        private void MapState()
        {
            Locate(boat,boatAt);Locate(walker,walkerAt);
            for(int i=0;i<pins.Length;i++) pins[i].text=Names[i];
            pins[(int)Site.Channel].style.display=channel?DisplayStyle.Flex:DisplayStyle.None;
            if(fare==Fare.Workers)pins[2].text="铁链桥 · 检修中";
            if(fare==Fare.Hull)pins[3].text="浅滩 · 刻痕";
            if(Day>=8)pins[1].text=savedWindow?"坡上的窗":"旧磨坊 · 空窗";
            if(Flood){pins[0].text="旧渡口 · 淹没";pins[2].text="铁链桥 · 起闸";}
            if(Flood)Paint(map,"Flood");
            if(returned&&!Flood)pins[0].text="北岸渡口 · 已返航";
        }
        private void Text(string value,string style="note")
        {
            var label=new Label(value);label.AddToClassList(style);menu.Add(label);
        }
        private void Choice(string name,Action act)
        {
            var b=new Button(()=>{if(!busy)act();}){text=name};menu.Add(b);
        }
        private VisualElement Layer(string asset)
        {
            var e=new VisualElement {pickingMode=PickingMode.Ignore};
            e.style.position=Position.Absolute;e.style.width=Length.Percent(100);e.style.height=Length.Percent(100);
            Paint(e,asset);picture.Add(e);return e;
        }
        private void Render()
        {
            MapState();menu.Clear(); drawer.scrollOffset=Vector2.zero;
            string plate=Flood && savedWindow ? "Mill" : site switch {Site.Mill=>"Mill",Site.Bridge=>"Chain",Site.Beach=>"Hull",Site.Channel=>"Chain",_=>"Landing"};
            picture=new VisualElement();picture.AddToClassList("frost-plate");menu.Add(picture);Paint(picture,plate);
            picture.style.overflow=Overflow.Hidden;
            Text("第一章　过河钱","title");
            Text($"第 {Day} 天 · {(halfDays%2==0?"清晨":"午后")}　　{Names[(int)site]}");
            if(Flood) FloodPage();
            else switch(site)
            {
                case Site.Landing: Landing();break;
                case Site.Mill: Mill();break;
                case Site.Bridge: Bridge();break;
                case Site.Beach: Beach();break;
                case Site.Channel: Text("饭桶碰了一下石壁。旧磨坊的泄水渠通往坡背，水轮停转后，这里还能走小船。");break;
            }
            reader.Prepare(picture);
            Choice("收起",()=>Show(false));
            Choice("返回标题",()=>SceneManager.LoadScene("Expedition"));
        }
        private void Landing()
        {
            if(!raised)
            {
                Layer("Shade");Layer("Lamp");
                Text("红披肩的哨兵摊开掌心，只有一枚铜钱。\n“我不过河。劳驾，把我的影子送回家。”\n他还坐在岸上，影子已经上了船。");
                Choice("提灯看看",()=>StartCoroutine(Lift()));return;
            }
            var lamp=Layer("Lamp");lamp.style.top=Length.Percent(-16);
            if(fare==Fare.None)
            {
                Layer("Thumb");Text("灯一高，影子便退回主人的脚下。船舷下却还扣着一只黑手，拇指轻轻一顶，像在推刀出鞘。");
                Text("“磨坊那扇窗底下。每天清早，我给我女儿做一只兔子。”修桥工也在岸边等你解缆。");
                Choice("放低灯，送影去磨坊 · 半天",()=>Depart(Fare.Shadow));
                Choice("先载修桥工去铁链桥 · 半天",()=>Depart(Fare.Workers));
                Choice("把船拖上浅滩查船底 · 半天",()=>Depart(Fare.Hull));
            }
            else if(!returned)
            {
                Text("你沿岸走了回来。船还系在首航停靠的地方。");
            Choice("回到停船处",()=>Visit(fare==Fare.Shadow?Site.Mill:fare==Fare.Workers?Site.Bridge:Site.Beach));
            }
            else
            {
                Text(fare switch{
                    Fare.Workers=>"工头把三枚工钱放在缆桩上。哨兵还坐在原处，看着下游的窗。",
                    Fare.Hull=>"修桥工搭了别人的船。哨兵挪出半张长凳，让你坐下。掌心的铜钱已经捏热了。",
                    _=>told?"女孩顺岸跑来，挨着父亲坐下。他试着弯起手指，这次耳朵对了。":"红披肩还在等。听说女儿认出了那只兔子，他才肯把铜钱放下。"
                });
                Text("告示写着：第八天腾空磨坊，第二十四天起闸泄洪。");
                if(Day<8)Choice("在渡口行船到第 8 天清晨",()=>{halfDays=14;Render();});
                else Choice("留守渡口到第 24 天清晨",()=>{halfDays=46;Render();});
                Choice("重试首航",()=>SceneManager.LoadScene("Crossing"));
            }
        }
        private IEnumerator Lift()
        {
            busy=true;menu.Clear();menu.Add(picture);picture.Clear();Text("你慢慢提起船灯。");
            var shade=Layer("Shade");var lamp=Layer("Lamp");var hand=Layer("Hand");hand.style.opacity=0;
            for(float t=0;t<1;t+=Time.deltaTime/.95f)
            {
                float k=Mathf.SmoothStep(0,1,t);
                lamp.style.top=Length.Percent(-16*k);
                shade.style.translate=new Translate(Length.Percent(-25*k),Length.Percent(-7*k));
                shade.style.opacity=1-k; hand.style.opacity=Mathf.Clamp01((k-.52f)*3);
                yield return null;
            }
            yield return new WaitForSeconds(.25f);Paint(hand,"Thumb");
            yield return new WaitForSeconds(.45f);raised=true;busy=false;Render();
        }
        private void Depart(Fare job)
        {
            if(fare!=Fare.None || !raised)return;
            fare=job;
            StartCoroutine(Sail(job==Fare.Shadow?Site.Mill:job==Fare.Workers?Site.Bridge:Site.Beach));
        }
        private void ReturnBoat()
        {
            if(returned)return;
            returned=true;StartCoroutine(Sail(Site.Landing));
        }
        private IEnumerator Sail(Site destination)
        {
            busy=true;Show(false); walker.style.display=DisplayStyle.None;
            var from=boatAt;var to=Berths[(int)destination];
            for(float t=0;t<1;t+=Time.deltaTime/.95f)
            {
                var p=Vector2.Lerp(from,to,t);
                p.y=RiverY(p.x)+Mathf.Lerp(from.y-RiverY(from.x),to.y-RiverY(to.x),t);
                Locate(boat,p);yield return null;
            }
            halfDays++;site=destination;boatAt=to;walkerAt=Landmarks[(int)site];
            walker.style.display=DisplayStyle.Flex;busy=false;Render();Show(true);
        }
        private void Mill()
        {
            if(Day>=8){Text(savedWindow?"旧窗已经搬到坡上。女孩掰着两根手指，等那只瘸耳朵。":"窗框拆走了。灰泥上只剩比墙白一圈的方印。");return;}
            if(fare!=Fare.Shadow){Text("女孩在打包，窗还支着。她不时朝水上看一眼。下游的船要从北岸解缆。");return;}
            Layer(rabbitFixed?"BentEar":"Rabbit");
            if(!rabbitFixed)
            {
                Text("影子爬上窗，竖起两根手指。女孩伸手按了按：“这才是瘸耳朵。你每回都忘。”");
                Choice("再做一次兔子",()=>StartCoroutine(Rabbit()));return;
            }
            Text("这一次她笑了。手影留在墙上，女孩把收好的包裹又放下。");
            if(!told&&!silent)
            {
                Choice("告诉她，父亲在上游等",()=>{told=true;Render();});
                Choice("只替哨兵捎一句早安",()=>{silent=true;Render();});
            }
            if(!channel)Choice("帮她卸下门板",()=>{channel=true;Render();});
            if(channel&&!savedWindow){Text("门板后是一段泄水槽，绕到水轮背后。窗框也快拆了。");Choice("陪她把窗搬上坡 · 半天",()=>{savedWindow=true;halfDays++;Render();});}
            if(savedWindow)Text("窗已安在坡上的新屋，旧水道也标在了地图上。");
            if(!returned)Choice("撑回渡口 · 半天",ReturnBoat);
        }
        private IEnumerator Rabbit()
        {
            busy=true;var shadow=picture[picture.childCount-1];
            shadow.style.opacity=0;yield return new WaitForSeconds(.18f);
            Paint(shadow,"BentEar");shadow.style.opacity=1;yield return new WaitForSeconds(.4f);
            rabbitFixed=true;busy=false;Render();
        }
        private void Bridge()
        {
            Text(fare==Fare.Workers?"第一截铁链露出水面。链上的水滴往下落，水底那一排手却跟着铁链往上抬。工头塞给你三枚铜钱，嘱你上岸别数那些手。":"桥头钉着春汛告示。第二十四天永久起闸。工头带人趴在桥边，听水底的链响。");
            if(fare==Fare.Workers&&!returned)Choice("撑回渡口 · 半天",ReturnBoat);
        }
        private void Beach()
        {
            Text(fare==Fare.Hull?"船翻过来，黑手松开了。你刮去河泥，看见歪歪斜斜的刻字：“若我上船，把灯提高。”那个向左弯的末笔，是你的字。":"浅滩上有一道旧拖痕。在这里翻船底，得先把渡船拖上岸。");
            if(fare==Fare.Hull&&!returned)Choice("撑回渡口 · 半天",ReturnBoat);
        }
        private void FloodPage()
        {
            Text("第二十四天清晨，铁链桥起闸。平日系船的缆桩沉进水里，水一直漫到磨坊的窗台。");
            Text(savedWindow?"坡上的新窗亮起来。一只弯着耳朵的兔子，完完整整地落在墙上。":"山坡上有人开了灯，旧窗的位置只剩一圈水纹。");
            if(savedWindow)Layer("BentEar");
            Choice("重试首航",()=>SceneManager.LoadScene("Crossing"));
        }
    }
}

