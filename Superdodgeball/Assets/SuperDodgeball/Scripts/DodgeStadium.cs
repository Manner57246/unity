using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace SuperDodgeball
{
    [ExecuteAlways, DefaultExecutionOrder(220)]
    public sealed class DodgeStadium : MonoBehaviour
    {
        [Serializable] public struct Paint {public Renderer renderer; public Color color;}
        [SerializeField] List<Paint> paints=new List<Paint>();
        [SerializeField] List<Transform> displayFaces=new List<Transform>();
        [SerializeField] Material surface,unlit;
        sealed class Display
        {
            public Transform face;
            public TextMesh blue,peach,clock,status,countdown;
            public GameObject scoreGroup;
        }
        readonly List<Display> displays=new List<Display>();
        DodgeApp app;
        public int ScreenCount=>displayFaces.Count;
        public string DisplayClock {get;private set;}="--:--";
        public string DisplayStatus {get;private set;}="等待入场";
        public int BlueScore {get;private set;}
        public int PeachScore {get;private set;}
        public int CountdownNumber {get;private set;}
        public bool ScoresMatch=>app && BlueScore==app.TeamScore(0) && PeachScore==app.TeamScore(1);
        public bool CountdownVisible=>displays.Count>0 && displays[0].countdown.gameObject.activeSelf;
        static readonly Color Ink=new Color(.13f,.22f,.27f),Cream=new Color(.93f,.9f,.8f),Mint=new Color(.35f,.76f,.7f),Peach=new Color(.96f,.58f,.4f);
        void OnEnable(){ApplyPaint();}
        void ApplyPaint()
        {
            foreach(var paint in paints)if(paint.renderer)DodgeWardrobe.Tint(paint.renderer,paint.color);
        }
        Transform Part(string name,Transform parent,Vector3 p,Vector3 size,Color color,bool solid=false,bool glowing=false,PrimitiveType shape=PrimitiveType.Cube)
        {
            var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
            var renderer=g.GetComponent<Renderer>();renderer.sharedMaterial=glowing && unlit?unlit:surface;
            paints.Add(new Paint{renderer=renderer,color=color});DodgeWardrobe.Tint(renderer,color);
            if(solid)g.AddComponent<DodgeSurface>();
            else {var c=g.GetComponent<Collider>();c.enabled=false;if(Application.isPlaying)Destroy(c);else DestroyImmediate(c);}
            return g.transform;
        }
        void TintExisting(Renderer renderer,Color color)
        {paints.Add(new Paint{renderer=renderer,color=color});DodgeWardrobe.Tint(renderer,color);}
        public void BuildGeometry(DodgeGame game,Material screenMaterial)
        {
            surface=game.popMaterial;unlit=screenMaterial;
            var root=transform;
            Part("Gym concourse floor",root,new Vector3(0,-.16f,0),new Vector3(44.8f,.2f,42.8f),new Color(.77f,.82f,.79f),true);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Indoor side wall",root,new Vector3(side*22.4f,7.7f,0),new Vector3(.55f,15.6f,43.2f),Cream,true);
                Part("Team colour lower wall",root,new Vector3(side*22.08f,1.5f,0),new Vector3(.09f,3f,42.2f),side<0?Mint:Peach);
                Part("Wall upper band",root,new Vector3(side*22.04f,5.1f,0),new Vector3(.12f,.28f,42.2f),Ink);
                for(int i=0;i<6;i++)
                {
                    float z=-17.5f+i*7;
                    Part("Indoor pilaster",root,new Vector3(side*21.85f,7.4f,z),new Vector3(.55f,14.8f,.75f),new Color(.62f,.73f,.73f));
                    Part("Clerestory window frame",root,new Vector3(side*22.01f,10.1f,z+2.5f),new Vector3(.14f,3.4f,3.5f),Ink);
                    Part("Clerestory soft glass",root,new Vector3(side*21.91f,10.1f,z+2.5f),new Vector3(.04f,3.02f,3.1f),new Color(.59f,.78f,.8f),false,true);
                    Part("Window mullion",root,new Vector3(side*21.83f,10.1f,z+2.5f),new Vector3(.07f,3.1f,.08f),Cream);
                }
                float zEnd=side*21.35f;
                // The team doorway remains clear, aligned with the existing rest-room entrance.
                for(int xSide=-1;xSide<=1;xSide+=2)
                    Part("End wall beside doorway",root,new Vector3(xSide*12.4f,2.15f,zEnd),new Vector3(19.9f,4.3f,.55f),Cream,true);
                Part("Indoor end wall upper",root,new Vector3(0,9.8f,zEnd),new Vector3(44.7f,11.4f,.55f),Cream,true);
                Part("End wall team band",root,new Vector3(0,4.45f,zEnd-side*.33f),new Vector3(44,.32f,.08f),side<0?Mint:Peach);
                // Enclose the existing changing / upgrade rooms without changing their usable space.
                Part("Rest wing roof",root,new Vector3(0,7.8f,side*26.45f),new Vector3(13.1f,.45f,10.1f),Cream,true);
                for(int xSide=-1;xSide<=1;xSide+=2)
                    Part("Rest wing upper side wall",root,new Vector3(xSide*6.35f,4.0f,side*26.45f),new Vector3(.3f,8,10.1f),Cream,true);
                Part("Rest wing rear wall",root,new Vector3(0,4,side*31.5f),new Vector3(13,8,.35f),Cream,true);
                BuildFace(root,new Vector3(0,8.0f,side*20.95f),side<0?180:0,1.04f,"End wall screen");
            }
            Part("Indoor roof",root,new Vector3(0,15.9f,0),new Vector3(45.5f,.65f,43.8f),new Color(.66f,.75f,.75f),true);
            for(int i=0;i<7;i++)
            {
                float z=-18+i*6;
                Part("Roof cross beam",root,new Vector3(0,15.2f,z),new Vector3(44.6f,.6f,.32f),Ink);
                for(int side=-1;side<=1;side+=2)
                {
                    var brace=Part("Roof angled brace",root,new Vector3(side*17.9f,14.3f,z),new Vector3(9f,.22f,.25f),Ink);
                    brace.localRotation=Quaternion.Euler(0,0,side*12);
                    Part("Warm strip light housing",root,new Vector3(side*9,14.78f,z),new Vector3(6.6f,.22f,.65f),Ink);
                    Part("Warm strip light diffuser",root,new Vector3(side*9,14.62f,z),new Vector3(6.1f,.08f,.48f),new Color(1,.96f,.82f),false,true);
                }
            }
            for(int z=-12;z<=12;z+=12)for(int side=-1;side<=1;side+=2)
            {
                var lightObject=new GameObject("Indoor court downlight",typeof(Light));lightObject.transform.SetParent(root,false);lightObject.transform.localPosition=new Vector3(side*8,13.9f,z);lightObject.transform.localRotation=Quaternion.Euler(90,0,0);
                var light=lightObject.GetComponent<Light>();light.type=LightType.Spot;light.spotAngle=100;light.range=25;light.intensity=5f;light.color=new Color(1,.93f,.82f);light.shadows=LightShadows.None;
            }
            // Suspend four displays above the centre without covering any playable floor.
            for(int side=-1;side<=1;side+=2)
            {
                Part("Scoreboard suspension",root,new Vector3(side*1.7f,13.1f,0),new Vector3(.09f,4,.09f),Ink);
                BuildFace(root,new Vector3(0,8.9f,side*2.9f),side<0?0:180,.55f,"Centre end screen");
                BuildFace(root,new Vector3(side*3.8f,8.9f,0),side<0?90:-90,.44f,"Centre sideline screen");
            }
            Part("Central scoreboard housing",root,new Vector3(0,8.9f,0),new Vector3(7.35f,4.25f,5.7f),Ink,true);
            Part("Scoreboard crown",root,new Vector3(0,11.05f,0),new Vector3(8.1f,.35f,6.25f),Ink);
            Part("Scoreboard underside",root,new Vector3(0,6.7f,0),new Vector3(7.9f,.2f,6.05f),Ink);
            var arena=GameObject.Find("01 - Playground");
            if(arena)
            {
                foreach(var renderer in arena.GetComponentsInChildren<Renderer>(true))
                {
                    if(renderer.name=="Court floor")TintExisting(renderer,new Color(.8f,.66f,.45f));
                    if(renderer.name=="Court panel")TintExisting(renderer,new Color(.87f,.75f,.55f));
                    if(renderer.name=="Side bumper" || renderer.name=="Side bumper stripe")TintExisting(renderer,renderer.transform.position.x<0?Mint:Peach);
                }
            }
            for(int i=-13;i<=13;i++)
                Part("Wood floor seam",root,new Vector3(i*.98f,.007f,0),new Vector3(.032f,.003f,34.8f),new Color(.74f,.61f,.42f));
            ApplyEnvironment();
        }
        void BuildFace(Transform parent,Vector3 p,float yaw,float scale,string name)
        {
            var face=new GameObject(name).transform;face.SetParent(parent,false);face.localPosition=p;face.localRotation=Quaternion.Euler(0,yaw,0);face.localScale=Vector3.one*scale;
            Part("Screen frame",face,Vector3.zero,new Vector3(14.4f,6.5f,.5f),Ink,true);
            Part("Screen glass",face,new Vector3(0,0,-.27f),new Vector3(13.8f,5.98f,.035f),new Color(.025f,.045f,.065f),false,true);
            Part("Mint screen rim",face,new Vector3(-6.98f,0,-.3f),new Vector3(.12f,5.9f,.04f),Mint,false,true);
            Part("Peach screen rim",face,new Vector3(6.98f,0,-.3f),new Vector3(.12f,5.9f,.04f),Peach,false,true);
            displayFaces.Add(face);
        }
        public void ApplyEnvironment()
        {
            var arena=GameObject.Find("01 - Playground");
            if(arena)foreach(var child in arena.GetComponentsInChildren<Transform>(true))
            {
                switch(child.name)
                {
                    case "Soft garden hill":case "Cotton cloud":case "Back billboard":case "Billboard left accent":case "Game title":case "Game subtitle":case "Billboard support":case "Floodlight":child.gameObject.SetActive(false);break;
                }
            }
            var sun=GameObject.Find("Sun");
            if(sun){var light=sun.GetComponent<Light>();light.intensity=.65f;light.shadows=LightShadows.None;light.color=new Color(1,.95f,.87f);}
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.73f,.78f,.8f);RenderSettings.ambientEquatorColor=new Color(.65f,.7f,.72f);RenderSettings.ambientGroundColor=new Color(.47f,.49f,.48f);
            RenderSettings.fog=false;
            ApplyPaint();
        }
        TextMesh Label(string name,Transform parent,Vector3 p,string text,float size,Color color)
            =>app.Clubhouse.WorldLabel(name,parent,p,text,size,color);
        public void Initialize(DodgeApp owner)
        {
            app=owner;ApplyEnvironment();
            if(displays.Count>0)return;
            foreach(var face in displayFaces)
            {
                Label("Scoreboard brand",face,new Vector3(0,2.48f,-.34f),"SUPER DODGEBALL",.085f,new Color(.76f,.83f,.84f));
                Label("Mint team name",face,new Vector3(-3.5f,1.55f,-.34f),"薄荷队",.1f,Mint);
                Label("Peach team name",face,new Vector3(3.5f,1.55f,-.34f),"蜜桃队",.1f,Peach);
                var group=new GameObject("Live team scores");group.transform.SetParent(face,false);
                var display=new Display{face=face,scoreGroup=group};
                display.blue=Label("Mint score",group.transform,new Vector3(-3.5f,-.3f,-.36f),"0",.32f,Color.white);
                display.peach=Label("Peach score",group.transform,new Vector3(3.5f,-.3f,-.36f),"0",.32f,Color.white);
                Label("Score separator",group.transform,new Vector3(0,-.3f,-.36f),":",.22f,new Color(.58f,.68f,.72f));
                display.clock=Label("Match time",face,new Vector3(0,-2.55f,-.36f),"03:00",.09f,new Color(1,.86f,.48f));
                display.status=Label("Scoreboard state",face,new Vector3(0,-1.85f,-.36f),"等待入场",.065f,new Color(.7f,.79f,.79f));
                display.countdown=Label("Opening countdown",face,new Vector3(0,.1f,-.4f),"3",.43f,new Color(1,.86f,.48f));
                displays.Add(display);
            }
            DrawScreens();
        }
        void LateUpdate(){if(Application.isPlaying && app && app.Initialized)DrawScreens();}
        public void DrawScreens()
        {
            if(!app)return;
            BlueScore=app.TeamScore(0);PeachScore=app.TeamScore(1);
            bool counting=app.Page==DodgePage.Countdown;
            CountdownNumber=counting?Mathf.Clamp(Mathf.CeilToInt(app.Countdown),1,3):0;
            int seconds=Mathf.Max(0,Mathf.CeilToInt(app.TimeLeft));
            DisplayClock=app.Page==DodgePage.Home || app.Page==DodgePage.Join || app.Page==DodgePage.Lobby?"--:--":(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");
            DisplayStatus=counting?"准备开球":app.Page==DodgePage.Staging?"全员进入球场后开始":app.Page==DodgePage.Playing?(app.TimeLeft>app.Room.duration-.8f?"GO!":"比赛进行中"):app.Page==DodgePage.Results?app.WinnerText:"等待入场";
            foreach(var display in displays)
            {
                display.scoreGroup.SetActive(!counting);display.countdown.gameObject.SetActive(counting);
                display.clock.gameObject.SetActive(!counting);
                display.status.transform.localPosition=new Vector3(0,counting?-2.3f:-1.85f,-.36f);
                display.blue.text=BlueScore.ToString();display.peach.text=PeachScore.ToString();display.clock.text=DisplayClock;display.status.text=DisplayStatus;
                display.countdown.text=CountdownNumber.ToString();
                float pulse=1f+.055f*Mathf.Sin(Mathf.Repeat(app.Countdown,1)*Mathf.PI);
                display.countdown.transform.localScale=Vector3.one*pulse;
            }
        }
    }
}
