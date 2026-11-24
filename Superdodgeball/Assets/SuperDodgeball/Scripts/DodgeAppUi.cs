using System;
using System.Linq;
using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeApp
    {
        Font uiFont;
        Texture2D panelTexture, buttonTexture, hoverTexture, accentTexture, fieldTexture;
        GUIStyle titleStyle, headingStyle, labelStyle, smallStyle, buttonStyle, primaryStyle, fieldStyle, cardStyle;
        static readonly Color Ink = new Color(.17f,.25f,.3f);
        static readonly Color Muted = new Color(.4f,.49f,.51f);
        static readonly Color Cream = new Color(1f,.975f,.91f);
        static readonly Color Mint = new Color(.51f,.84f,.75f);
        bool uiReady;

        void EnsureUi()
        {
            if (uiReady) return;
            uiReady = true;
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Microsoft YaHei UI", "Noto Sans CJK SC", "Arial" }, 24);
            panelTexture = Rounded(new Color(1f,.98f,.93f,.98f), 22);
            buttonTexture = Rounded(new Color(.87f,.93f,.89f), 14);
            hoverTexture = Rounded(new Color(.75f,.88f,.82f), 14);
            accentTexture = Rounded(new Color(.99f,.72f,.41f), 14);
            fieldTexture = Rounded(new Color(.93f,.94f,.89f), 12);
            titleStyle = TextStyle(58, FontStyle.Bold, Ink);
            headingStyle = TextStyle(29, FontStyle.Bold, Ink);
            labelStyle = TextStyle(20, FontStyle.Normal, Ink);
            smallStyle = TextStyle(16, FontStyle.Normal, Muted);
            buttonStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, border = new RectOffset(16,16,16,16), padding = new RectOffset(12,12,8,8) };
            buttonStyle.normal.background = buttonTexture; buttonStyle.hover.background = hoverTexture;
            buttonStyle.active.background = hoverTexture; buttonStyle.focused.background = hoverTexture;
            buttonStyle.hover.textColor = buttonStyle.active.textColor = buttonStyle.focused.textColor = Ink;
            primaryStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
            primaryStyle.normal.background = accentTexture;
            fieldStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft, border = new RectOffset(12,12,12,12), padding = new RectOffset(15,15,9,9) };
            fieldStyle.normal.background = fieldTexture; fieldStyle.focused.background = buttonTexture;
            fieldStyle.focused.textColor = Ink;
            cardStyle = new GUIStyle { border = new RectOffset(24,24,24,24) };
            cardStyle.normal.background = panelTexture;
        }
        GUIStyle TextStyle(int size, FontStyle weight, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = size, fontStyle = weight, wordWrap = true, richText = false };
            style.normal.textColor = color; return style;
        }
        static Texture2D Rounded(Color color, float radius)
        {
            const int size = 64;
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - .5f, x + .5f - (size - radius));
                float dy = Mathf.Max(radius - y - .5f, y + .5f - (size - radius));
                float d = new Vector2(Mathf.Max(0f,dx),Mathf.Max(0f,dy)).magnitude;
                var c = color; c.a *= Mathf.Clamp01(radius - d + .5f); pixels[y*size+x] = c;
            }
            texture.SetPixels(pixels); texture.Apply(false,true); return texture;
        }
        void OnGUI()
        {
            if (!initialized || BypassForChecks) return;
            bool overlay = Paused || settingsOpen || helpOpen;
            if (Walkable && !overlay) return;
            EnsureUi();
            int oldDepth = GUI.depth; GUI.depth = -20;
            var oldMatrix = GUI.matrix; var oldColor = GUI.color;
            GUI.color = new Color(.13f,.22f,.25f, Page == DodgePage.Countdown ? .18f : .42f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color = Color.white;
            float scale = Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280f*scale)/2f,(Screen.height-720f*scale)/2f,0),Quaternion.identity,Vector3.one*scale);
            if (settingsOpen) SettingsPage();
            else if (helpOpen) HelpPage();
            else if (Paused) PausePage();
            else switch (Page)
            {
                case DodgePage.Home: HomePage(); break;
                case DodgePage.Join: JoinPage(); break;
                case DodgePage.Results: ResultsPage(); break;
            }
            GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.depth = oldDepth;
        }
        void Card(float x,float y,float w,float h) => GUI.Box(new Rect(x,y,w,h),GUIContent.none,cardStyle);
        void Label(string text,float x,float y,float w,float h,GUIStyle style = null) => GUI.Label(new Rect(x,y,w,h),text,style ?? labelStyle);
        bool Button(string text,float x,float y,float w,float h,bool primary = false) => GUI.Button(new Rect(x,y,w,h),text,primary ? primaryStyle : buttonStyle);
        string Field(string value,float x,float y,float w,float h,int length=32) => GUI.TextField(new Rect(x,y,w,h),value,length,fieldStyle);
        void Notice(float x,float y,float w) { if (!string.IsNullOrEmpty(Message)) Label(Message,x,y,w,54,smallStyle); }
        void Footer(string text) => Label(text,64,674,1160,28,smallStyle);

        void HomePage()
        {
            Card(72,92,490,535);
            Label("SUPER\nDODGEBALL",110,120,440,151,titleStyle);
            Label("软糖躲避球派对",113,284,410,44,headingStyle);
            if(Button("开始游戏",112,365,410,62,true))BeginParty();
            if(Button("设置",112,445,410,55))settingsOpen=true;
            if(Button("退出游戏",112,518,410,55))Application.Quit();
            Notice(622,540,550);
        }
        void JoinPage()
        {
            Card(330,95,620,524);
            Label("一起进场",372,130,520,50,headingStyle);
            Label("输入房主分享的地址。双方需处于可互通的网络。",372,192,520,52,smallStyle);
            Label("你的名字",372,265,160,36);
            PlayerName = Field(PlayerName,543,257,360,48,16);
            Label("房主地址",372,329,160,36);
            JoinAddress = Field(JoinAddress,543,321,360,48,128);
            Label("端口",372,393,160,36);
            PortText = Field(PortText,543,385,170,48,5);
            GUI.enabled = !Connecting;
            if (Button("加入房间",372,461,330,56,true)) JoinRoom();
            GUI.enabled = true;
            if (Button(Connecting ? "取消" : "返回大厅",718,461,185,56)) CancelJoin();
            Notice(372,542,530);
            Footer("房主在大厅等候时才能加入。此版本使用地址直连，Steam 邀请将在后续接入。");
        }
        void ResultsPage()
        {
            Card(235,62,810,594);
            Label("派对结束",277,90,730,45,headingStyle);
            Label(WinnerText+"   "+TeamScore(0)+" : "+TeamScore(1),277,149,730,65,headingStyle);
            Label("玩家",305,230,370,34,smallStyle);
            Label("命中得分",738,230,120,34,smallStyle); Label("被击中",901,230,110,34,smallStyle);
            var ranked=Room.seats.Select((seat,index)=>new {seat,index}).Where(x=>x.seat.kind!=DodgeSeatKind.Empty).OrderByDescending(x=>x.seat.score).ToArray();
            for(int i=0;i<ranked.Length;i++)
            {
                var row=ranked[i]; float y=278+i*48;
                Label((i+1)+"   "+row.seat.name+(row.seat.team==0?" · 薄荷":" · 蜜桃")+(row.index==LocalSlot?"（你）":""),288,y,420,38);
                Label(row.seat.score.ToString(),785,y,100,38); Label(row.seat.hits.ToString(),938,y,80,38);
            }
            if(IsHost) { if(Button("返回大厅 · 再来一局",277,553,468,59,true)) BackToLobby(); }
            else Label("等房主返回大厅，准备下一局。",277,558,470,55,smallStyle);
            if(Button("离开房间",766,553,234,59)) ReturnHome("");
            Footer("胜负之外，记住刚才那个离谱的姿势。");
        }
        void PausePage()
        {
            Card(428,116,424,484);
            Label(Online ? "稍歇一下" : "已暂停",472,150,350,48,headingStyle);
            Label(Page==DodgePage.Lobby?"走两侧选队 · 用球砸大屏选规则":Online?"其他人仍在场内活动。":"准备好了就继续。",472,212,340,56,smallStyle);
            if(Button("回到场内",472,287,336,56,true)) { Paused=false; game.CaptureInput(); }
            if(Button("设置",472,358,162,48)) settingsOpen=true;
            if(Button("怎么玩",646,358,162,48)) helpOpen=true;
            if(Page!=DodgePage.Lobby && Button(IsHost ? "返回大厅" : "离开房间",472,431,336,50)) { if(IsHost) BackToLobby(); else ReturnHome(""); }
            if(Button("回主菜单",472,500,336,48)) ReturnHome("");
        }
        void SettingsPage()
        {
            Card(330,40,620,640); Label("舒服地玩",373,64,500,48,headingStyle);
            Label("音量",373,219,180,36);
            volume=GUI.HorizontalSlider(new Rect(559,233,322,24),volume,0f,1f);
            Label("鼠标灵敏度",373,288,180,36);
            sensitivity=GUI.HorizontalSlider(new Rect(559,303,322,24),sensitivity,.035f,.22f);
            Label("显示模式",373,367,180,36);
            if(Button(fullscreen?"无边框全屏":"窗口",559,358,322,47)) fullscreen=!fullscreen;
            Label("画面质量",373,435,180,36);
            if(Button(QualitySettings.names[Mathf.Clamp(quality,0,QualitySettings.names.Length-1)],559,425,322,47)) quality=(quality+1)%QualitySettings.names.Length;
            Label("你的名字",373,492,165,36);
            PlayerName=Field(PlayerName,559,484,322,44,16);
            Label("房间端口",373,550,165,36);
            PortText=Field(PortText,559,542,170,44,5);
            if(Button("保存并返回",373,605,510,48,true))
            {
                PlayerName=CleanName(PlayerName);PlayerPrefs.SetString("MVP.PlayerName",PlayerName);
                if(IsHost && Page==DodgePage.Lobby){Room.seats[0].name=PlayerName;Room.name=PlayerName+"的派对";Net.BroadcastLobby();}
                ApplySettings(true); settingsOpen=false;
            }
        }
        void HelpPage()
        {
            Card(188,66,904,588); Label("把身体当成你的招牌动作",230,99,820,48,headingStyle);
            string[,] controls = { {"移动 / 镜头","WASD / 鼠标"},{"瞄准蓄力 / 投球","按住右键 / 点击左键"},{"向左 / 向右侧弯","Q / E"},{"L 型弯腰 / O 型卷身","按住 Z / X"},{"飞扑","空格"},{"普通球 / 已有特殊武器","1 / 2"},{"使用携带道具","F"},{"更衣间内切换装扮","C / 用球砸衣柜"},{"暂停 / 返回","Esc"} };
            for(int i=0;i<controls.GetLength(0);i++) {Label(controls[i,0],233,170+i*36,400,32);Label(controls[i,1],692,170+i*36,350,32,smallStyle);}
            Label("大厅：走两侧选队，房主用球砸大屏选择规则与出发。休息间：投球领道具、选技能；全员走入本方半场后倒计时。比赛：命中对手为本队得分；用球先砸中礼盒领走道具。",233,505,815,64,smallStyle);
            if(Button("明白了",799,582,247,49,true)) helpOpen=false;
        }
        void DestroyUi()
        {
            foreach(var texture in new[]{panelTexture,buttonTexture,hoverTexture,accentTexture,fieldTexture}) if(texture) Destroy(texture);
            if(uiFont) Destroy(uiFont);
        }
    }
}
