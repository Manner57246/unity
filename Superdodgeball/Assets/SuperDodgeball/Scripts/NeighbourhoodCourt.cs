using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static SuperDodgeball.NeighbourhoodScenery;

namespace SuperDodgeball
{
    // Optional local art study. It is entered from Home, before a network room exists.
    public sealed class NeighbourhoodCourt : MonoBehaviour
    {
        readonly List<GameObject> pausedRoots = new();
        readonly NeighbourhoodPlayer[] players=new NeighbourhoodPlayer[4];
        readonly Vector3[] homes={new(-6,0,-2.45f),new(-3.85f,0,3.6f),new(5.05f,0,1.75f),new(7.1f,0,-3.1f)};
        readonly float[] thrown={9,9,9,9}, received={9,9,9,9};
        readonly GameObject[] venues=new GameObject[2];
        Transform ball, trail, shadow, paper, marker;
        NeighbourhoodPlayer official;
        Camera eye;
        Material paint;
        Font font;
        GUIStyle caption, legend;
        bool pier, manual, flying, delivering, returning;
        int holder,receiver,returns;
        float clock,hold,progress,duration;
        Vector3 start,end;
        static readonly int[] Rally={2,1,3,0};
        public static void Open()
        {
            if(FindFirstObjectByType<NeighbourhoodCourt>())return;
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();
            var court=new GameObject("Neighbourhood court study").AddComponent<NeighbourhoodCourt>();
            foreach(var root in roots) if(root.activeSelf) {court.pausedRoots.Add(root);root.SetActive(false);}
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            court.Build();
        }
        void Close()
        {
            gameObject.SetActive(false);
            foreach(var root in pausedRoots)if(root)root.SetActive(true);
            Destroy(gameObject);
        }
        static Vector3 V(float x,float y,float z)=>new(x,y,z);
        Transform Make(CourtSketchMesh shape,string label)=>shape.Bake(label,transform,paint).transform;
        void Build()
        {
            paint=new Material(Resources.Load<Shader>("Court/Neighbourhood"));
            for(int i=0;i<2;i++)
            {
                var m=new CourtSketchMesh();NeighbourhoodScenery.Build(m,i==1);
                venues[i]=Make(m,i==1?"Low tide":"Wash day").gameObject;
            }
            for(int i=0;i<4;i++)
            {
                players[i]=new NeighbourhoodPlayer(transform,paint,i);players[i].Root.localPosition=homes[i];
                players[i].Root.rotation=Quaternion.LookRotation(i<2?Vector3.left:Vector3.right);
            }
            official=new NeighbourhoodPlayer(transform,paint,4);official.Root.localPosition=V(.2f,1.47f,7.49f);
            var news=new CourtSketchMesh();Newspaper(news);paper=Make(news,"Sports pages");
            var b=new CourtSketchMesh();
            b.Plum(0xff5629,Vector3.zero,V(.70f,.70f,.70f),14);
            b.Drum(0xffefb7,V(0,.045f,0),V(.708f,.13f,.708f),16);
            b.Plum(0xffb85e,V(-.105f,.211f,-.105f),V(.24f,.105f,.25f),9);
            ball=Make(b,"Match ball");
            var streak=new CourtSketchMesh();
            streak.Face(0xffae51,V(0,0,0),V(.19f,0,1),V(-.19f,0,1));
            streak.Face(0xffe4a0,V(0,.015f,.16f),V(.085f,.015f,1),V(-.085f,.015f,1));trail=Make(streak,"Ball flight");
            var sh=new CourtSketchMesh();sh.Drum(0x666f60,Vector3.zero,V(.61f,.014f,.41f));shadow=Make(sh,"Ball ground contact");
            var pointer=new CourtSketchMesh();pointer.Ground(0xfff0c4,0,new(-.22f,0),new(.22f,0),new(0,-.28f));marker=Make(pointer,"Local player marker");
            var cam=new GameObject("Match camera",typeof(Camera));cam.transform.SetParent(transform,false);
            eye=cam.GetComponent<Camera>();eye.orthographic=true;eye.clearFlags=CameraClearFlags.SolidColor;
            eye.backgroundColor=new Color(.45f,.64f,.57f);eye.nearClipPlane=.1f;eye.farClipPlane=110;
            eye.transform.rotation=Quaternion.Euler(46,0,0);
            eye.transform.position=V(0,.50f,.15f)-eye.transform.forward*40;
            cam.AddComponent<AudioListener>();font=DodgeApp.CreateUiFont(20);
            SetVenue(false);
        }
        void SetVenue(bool dock)
        {
            pier=dock;venues[0].SetActive(!pier);venues[1].SetActive(pier);
        }
        Vector3 Hand(int i)=>players[i].Hand;
        void Release(int target,Vector3 point)
        {
            receiver=target;end=point;progress=0;
            delivering=true;thrown[holder]=0;
        }
        Vector3 MouseAim()
        {
            if(Mouse.current==null)return homes[2];
            var ray=eye.ScreenPointToRay(Mouse.current.position.ReadValue());
            var ground=new Plane(Vector3.up,Vector3.up);
            if(!ground.Raycast(ray,out float distance))return homes[2];
            var p=ray.GetPoint(distance);
            return V(Mathf.Clamp(p.x,-10.4f,10.4f),1.1f,Mathf.Clamp(p.z,-5.9f,5.9f));
        }
        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.08f);clock+=dt;
            bool justReleased=false;
            eye.orthographicSize=Mathf.Max(8.5f,15.1f/Mathf.Max(.1f,eye.aspect));
            var k=Keyboard.current;var mouse=Mouse.current;
            if(k!=null && k.escapeKey.wasPressedThisFrame){Close();return;}
            if(k!=null && k.tabKey.wasPressedThisFrame)SetVenue(!pier);
            Vector3 walk=Vector3.zero;
            if(k!=null)walk=V((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)).normalized;
            bool fire=(mouse!=null && mouse.leftButton.wasPressedThisFrame)||(k!=null && k.spaceKey.wasPressedThisFrame);
            manual|=walk.sqrMagnitude>.01f || fire;
            for(int i=0;i<4;i++)
            {
                Transform t=players[i].Root;
                Vector3 old=t.position, desired;
                if(i==0 && manual)
                {
                    desired=old+walk*dt*5.5f;
                    desired.x=Mathf.Clamp(desired.x,-10,10);desired.z=Mathf.Clamp(desired.z,-5.8f,5.8f);
                }
                else
                {
                    var mark=homes[i];
                    if(flying && i==receiver)
                    {
                        var reach=start-mark;reach.y=0;
                        mark+=reach.normalized*.62f;
                    }
                    desired=Vector3.MoveTowards(old,mark,dt*(i==receiver?2.8f:1.2f));
                }
                t.position=desired;
                int nextTarget=manual?0:Rally[returns%Rally.Length];
                if(nextTarget==i)nextTarget=i==0?2:0;
                var target=i==holder?players[nextTarget].Root.position:players[holder].Root.position;
                if(i==0 && manual)target=MouseAim();
                var look=target-desired;look.y=0;
                if(!(manual && i==0))look.z-=Mathf.Abs(look.x)*.40f;
                if(look.sqrMagnitude>.02f)t.rotation=Quaternion.Slerp(t.rotation,Quaternion.LookRotation(-look),1-Mathf.Exp(-9*dt));
                thrown[i]+=dt;received[i]+=dt;
                float sweep=Mathf.SmoothStep(0,1,Mathf.Clamp01(thrown[i]/.12f));
                float snap=sweep*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((thrown[i]-.21f)/.42f)));
                float catchHit=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((received[i]-.06f)/.24f));
                float coil=!flying && i==holder?Mathf.SmoothStep(0,1,Mathf.Clamp01((hold-.16f)/.37f)):0;
                if(delivering && i==holder)coil*=1-sweep;
                float gather=flying && receiver==i?Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.51f)*3)):0;
                float phase=Mathf.Floor(clock*18)/18f*(i==0 && manual?11:7)+i;
                players[i].Pose(phase,Mathf.Clamp01((desired-old).magnitude/Mathf.Max(dt,.001f)*.5f),coil,snap,gather,catchHit);
            }
            marker.position=players[0].Root.position+V(0,.065f,-.86f);
            if(!flying)
            {
                hold+=dt;ball.position=Hand(holder);
                if(delivering)
                {
                    if(thrown[holder]>=.105f)
                    {
                        delivering=false;flying=true;justReleased=true;start=Hand(holder);
                        duration=Mathf.Clamp(Vector3.Distance(start,end)/12.5f,.32f,1.32f);
                    }
                }
                else if(holder==0 && manual)
                {
                    if(fire)
                    {
                        var aim=MouseAim();
                        int closest=-1;float nearest=1.85f;
                        for(int i=1;i<4;i++)
                        {
                            var delta=players[i].Root.position-aim;delta.y=0;
                            if(delta.magnitude<nearest){nearest=delta.magnitude;closest=i;}
                        }
                        Release(closest,closest>=0?Hand(closest):aim);
                    }
                }
                else if(hold>.67f)
                {
                    int next=manual?0:Rally[returns%Rally.Length];
                    if(next==holder)next=holder==0?2:0;
                    Release(next,Hand(next));
                }
            }
            if(flying && dt>0 && !justReleased)
            {
                progress=Mathf.Min(1,progress+dt/duration);
                if(receiver>=0)end=Hand(receiver);
                var point=Vector3.Lerp(start,end,progress)+Vector3.up*(4*progress*(1-progress)*(returning?.65f:1.22f));
                var forward=end-start;forward.y=0;
                float length=Mathf.Min(progress*forward.magnitude,1.65f);
                if(forward.sqrMagnitude>.0001f)
                {
                    trail.rotation=Quaternion.LookRotation(forward);
                    trail.localScale=V(1,1,length);
                    trail.position=point-forward.normalized*length;
                }
                ball.position=point;ball.Rotate(180*dt,0,140*dt,Space.World);
                if(progress>=1)
                {
                    if(receiver<0)
                    {
                        // The ball bounces back from a missed practice throw.
                        start=point;receiver=0;end=Hand(0);progress=0;returning=true;
                        duration=Mathf.Clamp(Vector3.Distance(start,end)/11,.3f,1.3f);
                    }
                    else
                    {
                        holder=receiver;received[holder]=0;
                        flying=false;returning=false;hold=0;returns++;
                    }
                }
            }
            trail.gameObject.SetActive(flying && progress>0);
            shadow.position=V(ball.position.x,.07f,ball.position.z);shadow.localScale=Vector3.one*Mathf.Clamp(1.15f-ball.position.y*.1f,.65f,1);
            float peek=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Sin(clock*.90f)-.48f)*3));
            official.Pose(clock*3,0,0,0,0,0);
            paper.position=official.Root.position+V(0,1.69f-peek*.81f,-.59f);
            paper.rotation=Quaternion.Euler(Mathf.Sin(clock*2)*3+peek*19,8*Mathf.Sin(clock*.8f),-4+peek*10);
        }
        void OnGUI()
        {
            if(caption==null)
            {
                caption=new GUIStyle(GUI.skin.label){font=font,fontSize=20,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft};
                legend=new GUIStyle(caption){fontSize=15,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleCenter};
                caption.normal.textColor=legend.normal.textColor=new Color(1,.96f,.84f);
            }
            var original=GUI.matrix;var colour=GUI.color;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            void Panel(Rect r){GUI.color=new Color(.13f,.22f,.24f,.89f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
            Panel(new Rect(18,16,151,39));GUI.Label(new Rect(32,15,135,40),pier?"退潮码头":"晾衣院球场",caption);
            Panel(new Rect((width-476)/2,height-42,476,30));
            GUI.Label(new Rect((width-476)/2,height-43,476,31),"WASD 移动    ·    点击瞄准投球    ·    Tab 换场    ·    Esc 返回",legend);
            GUI.matrix=original;GUI.color=colour;
        }
        void OnDestroy()
        {
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh)Destroy(filter.sharedMesh);
            if(paint)Destroy(paint);
        }
    }
}
