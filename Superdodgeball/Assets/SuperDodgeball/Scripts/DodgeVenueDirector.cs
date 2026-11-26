using System.Collections.Generic;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeVenue { Tour, Warehouse, Flume, Gym }

    // Positions and cues derive from the networked round clock. Only the host resolves impacts.
    [DefaultExecutionOrder(-200)]
    public sealed partial class DodgeVenueDirector : MonoBehaviour
    {
        const float Travel=3.85f, Warning=1.65f, RailX=14.1f, JetEnd=7.5f;
        static readonly float[] Lanes={-12,4,-4,12,-8,8,-12,12,-4,4,-8,8};
        readonly Transform[] loads=new Transform[Lanes.Length], tracks=new Transform[Lanes.Length];
        readonly int[] struck=new int[Lanes.Length];
        readonly GameObject[] sets=new GameObject[4];
        readonly List<Renderer> campus=new();
        readonly List<Mesh> meshes=new();
        Material ink;
        DodgeApp app;
        DodgeVenue venue;
        int epoch=-1, lastSignal=-1;
        float lastAge=-1000, displayAge;
        public string Cue {get;private set;}
        public int RollerHits {get;private set;}
        public int VisibleLoads {get;private set;}
        public DodgeVenue Displayed=>venue;
        public static string Title(DodgeVenue v)=>v switch
        {
            DodgeVenue.Warehouse=>"工厂仓库",DodgeVenue.Flume=>"导流槽",DodgeVenue.Gym=>"大学体育馆",_=>"三场轮换"
        };

        internal void Initialize(DodgeApp owner)
        {
            app=owner;
            ink=new Material(Resources.Load<Shader>("Court/Neighbourhood"));
            foreach(var r in app.Stadium.GetComponentsInChildren<MeshRenderer>(true))
                if(r.name=="Painted maple surface" || r.name.EndsWith("end wall standards") || r.name.EndsWith("athletics banner") ||
                   r.name=="Banner hanging rail" || r.name=="Supporters pennants and barrier fastenings")campus.Add(r);
        }

        float Age=>app.Room.duration*.5f-app.TimeLeft;
        static DodgeVenueDirector Live=>DodgeApp.Active && DodgeApp.Instance.MatchRunning?DodgeApp.Instance.Venue:null;
        public static bool InUpdraft
        {
            get {var v=Live;return v && v.venue==DodgeVenue.Flume && v.Age>=0 && v.Age<JetEnd;}
        }
        public static float WindHeight(Vector3 position)
        {
            if(!InUpdraft)return 0;
            float t=Live.Age;
            float envelope=Mathf.SmoothStep(0,1,t/1.15f)*(1-Mathf.SmoothStep(0,1,(t-5.0f)/2.15f));
            return envelope*(5.35f+.42f*Mathf.Sin(t*2.2f+position.x*.23f+position.z*.14f));
        }
        public static void ApplyWind(Vector3 position,ref float verticalSpeed)
        {
            if(!InUpdraft)return;
            float target=WindHeight(position);
            if(Live.Age<.9f && position.y>target+.35f){verticalSpeed=Mathf.Max(verticalSpeed,0);return;}
            verticalSpeed=Mathf.Clamp((target+.05f-position.y)*8f,-5.3f,10f);
        }

        void Update()
        {
            if(!app || !app.Initialized)return;
            var wanted=DodgeApp.Active && app.Room.leg>0?app.CurrentVenue:DodgeVenue.Gym;
            if(wanted!=venue || app.Room.round!=epoch)
            {
                if(sets[(int)venue])sets[(int)venue].SetActive(false);
                venue=wanted;epoch=app.Room.round;RollerHits=0;lastSignal=-1;lastAge=-1000;
                System.Array.Clear(struck,0,struck.Length);
                foreach(var r in campus)r.enabled=venue==DodgeVenue.Gym;
                if(venue!=DodgeVenue.Gym)
                {
                    if(!sets[(int)venue])BuildVenue();
                    sets[(int)venue].SetActive(true);
                }
            }
            bool live=app.MatchRunning && DodgeApp.Active;
            float now=live?Age:-1000;
            if(app.IsClient && live && Mathf.Abs(displayAge-now)<.30f)
                now=Mathf.Clamp(displayAge+Time.deltaTime,now-.06f,now+.045f);
            displayAge=now;
            Cue=null;VisibleLoads=0;
            if(venue==DodgeVenue.Warehouse)WarehouseEvent(now,live);
            if(venue==DodgeVenue.Flume)FlumeEvent(now);
            lastAge=now;
        }

        static float Release(int i)=>i/4*3.10f+(i%4)*.23f;
        static int Direction(int i)=>(i+i/4)%2==0?1:-1;
        static float Radius(int i)=>i%3==0?1.06f:i%3==1?.90f:1.0f;
        static float X(int i,float time)=>Mathf.Lerp(-RailX,RailX,Mathf.Clamp01(time/Travel))*Direction(i);

        void WarehouseEvent(float now,bool live)
        {
            if(now>=-Warning && now<Release(Lanes.Length-1)+Travel)
                Cue=now<0?"落料预警 · 离开箭头车道":"零件过道 · 横移避让";
            for(int i=0;i<Lanes.Length;i++)
            {
                float t=now-Release(i);
                bool warning=t>=-Warning && t<Travel;
                tracks[i].gameObject.SetActive(warning);
                if(warning)tracks[i].localScale=new Vector3(1,1,t<0?.82f+.18f*Mathf.Sin(t*14):.68f);
                bool rolling=t>=0 && t<Travel;
                loads[i].gameObject.SetActive(rolling);
                if(rolling)
                {
                    VisibleLoads++;
                    float entry=Mathf.Clamp01(t/.40f);
                    loads[i].localPosition=new Vector3(X(i,t),Radius(i)+1.0f*(1-entry)*(1-entry),Lanes[i]);
                    loads[i].localRotation=Quaternion.Euler(0,0,-Direction(i)*t*(2*RailX/Travel)/Radius(i)*Mathf.Rad2Deg);
                }
                if(!live || !app.IsHost || !app.SimulationRunning || t<0 || lastAge>=Release(i)+Travel)continue;
                // Sweep the full distance travelled since the preceding authoritative frame.
                float oldX=X(i,lastAge-Release(i)),x=X(i,t);
                for(int slot=0;slot<app.Actors.Length;slot++)
                {
                    var actor=app.Actors[slot];
                    if((struck[i]&(1<<slot))!=0 || !actor.isActiveAndEnabled || actor.IsStunned || DodgeReferee.Restricted(actor))continue;
                    var p=actor.transform.position;
                    if(Mathf.Abs(p.z-Lanes[i])>1.06f || p.y>Radius(i)*2f+.12f ||
                       p.x<Mathf.Min(x,oldX)-Radius(i)-.26f || p.x>Mathf.Max(x,oldX)+Radius(i)+.26f)continue;
                    struck[i]|=1<<slot;RollerHits++;
                    actor.Puppet.Hit(p+Vector3.up*.7f,Vector3.right*Direction(i)*4f+Vector3.up,3.4f,2.2f);
                    DodgeGame.Instance.EmitFoam(p,Vector3.up,7,1.15f);
                }
            }
            if(now>=-Warning && now<8f)
            {
                int wave=now<1.45f?0:now<4.55f?1:2;
                if(lastSignal!=wave){lastSignal=wave;DodgeGame.Instance.PlayTone(355+wave*55,.23f,.09f);}
            }
        }
        public Vector3 AvoidRollers(Vector3 p,Vector3 direction)
        {
            if(venue!=DodgeVenue.Warehouse || !app.MatchRunning)return direction;
            for(int i=0;i<Lanes.Length;i++)
            {
                float t=Age-Release(i), dz=p.z-Lanes[i];
                if(t< -Warning || t>=Travel || Mathf.Abs(dz)>1.65f)continue;
                float arrival=(p.x+Direction(i)*RailX)*Direction(i)*Travel/(2*RailX)-t;
                if(arrival<-.16f || arrival>1.15f)continue;
                return new Vector3(direction.x*.22f,0,Mathf.Abs(dz)<.08f?-Mathf.Sign(Lanes[i]):Mathf.Sign(dz)).normalized;
            }
            return direction;
        }
        void FlumeEvent(float now)
        {
            bool pre=now>=-2.5f && now<0, wind=now>=0 && now<JetEnd;
            if(pre)Cue="导流槽预热 · 准备升空";
            if(wind)Cue=now<5?"上升气流 · 空中可投球":"气流减弱 · 准备落地";
            if((pre || wind) && lastSignal!=(wind?1:0))
            {
                lastSignal=wind?1:0;DodgeGame.Instance.PlayTone(wind?115:455,wind?.75f:.28f,.12f);
            }
            DrawJets(now,pre,wind);
        }
        void OnDestroy()
        {
            foreach(var mesh in meshes)if(mesh)Destroy(mesh);
            if(ink)Destroy(ink);
        }
    }
}
