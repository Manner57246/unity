using System.IO;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeItem { None, Soap, Banana, Decoy, Tea, SpringShoes }
    public enum DodgeSkill { RubberWaist, DiveRelay, CounterThrow, QuickPitch, GuiltySprint, GoodBehaviour }
    public sealed class DodgePartyKit : MonoBehaviour
    {
        public DodgeItem Item { get; private set; }
        public int Skills { get; private set; }
        public int Points { get; private set; }
        public int Experience { get; private set; }
        public float SpringReady { get; private set; }
        public float Flat { get; private set; }
        public float FlatAmount {get;private set;}
        public float CounterReady { get; private set; }
        public float RelayReady { get; private set; }
        public float Swing { get; private set; }
        public DodgeWeapon SwingWeapon { get; private set; }
        public Vector3 SwingAim { get; private set; }
        public float WindupScale=>Has(DodgeSkill.QuickPitch)?.72f:1f;
        public float LeanScale=>Has(DodgeSkill.RubberWaist)?1.18f:1f;
        public static readonly string[] SkillNames={"橡皮腰","飞扑接力","回马枪","快递投手","做贼心虚","装乖高手"};
        public static readonly string[] SkillDetails={"侧弯更深 · 恢复更快","飞扑躲过近球后恢复更快","飞扑落地后下一球快速出手","普通投球出手更快","裁判回头时撤退加速","守规矩更快消退警告"};
        public static readonly string[] ItemNames={"空手","泡泡皂","香蕉皮","假人替身","裁判奶茶","弹簧鞋"};
        DodgeActor actor;
        TextMesh label;
        Transform pocket,shoes;
        float relayCooldown;
        void Awake(){actor=GetComponent<DodgeActor>();}
        public bool Has(DodgeSkill skill)=>DodgeApp.Active && (Skills&(1<<(int)skill))!=0;
        public void ResetKit(bool inMatch)
        {
            StarterClaimed=false;Item=DodgeItem.None;Skills=Experience=0;Points=inMatch?1:0;
            FlatAmount=0;SpringReady=Flat=CounterReady=RelayReady=Swing=relayCooldown=0;
        }
        public bool StarterClaimed {get;private set;}
        public bool ClaimStarter(DodgeItem item){if(StarterClaimed)return false;StarterClaimed=true;Grant(item);return true;}
        public void Grant(DodgeItem item){Item=item;}
        public bool Upgrade(DodgeSkill skill)
        {
            if(DodgeGame.IsReplica || Points<=0 || (Skills&(1<<(int)skill))!=0)return false;
            Skills|=1<<(int)skill;Points--;DodgeGame.Instance.EmitFoam(transform.position,Vector3.up,10,1.3f);return true;
        }
        public void Earn(int amount=1)
        {
            if(!DodgeApp.Active || DodgeApp.Instance.Page!=DodgePage.Playing || DodgeGame.IsReplica)return;
            int before=Experience/3;Experience+=amount;Points=Mathf.Min(6,Points+Experience/3-before);
        }
        public bool Use(Vector3 aim)
        {
            if(!DodgeApp.Active || DodgeGame.IsReplica || DodgeApp.Instance.Page!=DodgePage.Playing || !DodgeGame.SimulationActive
                || actor.IsStunned || DodgeReferee.Restricted(actor) || Item==DodgeItem.None)return false;
            var item=Item;
            if(item==DodgeItem.Soap)actor.CrowdStatus.Wash();
            else if(item==DodgeItem.SpringShoes)SpringReady=20f;
            else if(item==DodgeItem.Tea)
            {
                if(!DodgeApp.Instance.Referee.OfferTea())return false;
                DodgePartyEffects.Instance.Spawn(DodgeEffect.Tea,transform.position+Vector3.up,Vector3.zero,actor,1.1f);
            }
            else if(item==DodgeItem.Decoy)
                DodgePartyEffects.Instance.Spawn(DodgeEffect.Decoy,transform.position+transform.forward*.85f,Vector3.zero,actor,7f);
            else
            {
                var target=transform.position+Vector3.ClampMagnitude(Vector3.ProjectOnPlane(aim-transform.position,Vector3.up),6);
                target.x=Mathf.Clamp(target.x,-DodgeCourt.PlayWidth,DodgeCourt.PlayWidth);target.z=Mathf.Clamp(target.z,-DodgeCourt.PlayDepth,DodgeCourt.PlayDepth);target.y=.055f;
                DodgePartyEffects.Instance.Spawn(DodgeEffect.Banana,target,Vector3.zero,actor,12f);
            }
            Item=DodgeItem.None;DodgeGame.Instance.PlayTone(620,.1f,.07f);return true;
        }
        public bool TakeSpring(){bool result=SpringReady>0;SpringReady=0;return result;}
        public void Land(){if(Has(DodgeSkill.CounterThrow))CounterReady=2.2f;}
        public void Dodged(bool dive)
        {
            if(relayCooldown>0)return;
            relayCooldown=2.5f;Earn();
            if(dive && Has(DodgeSkill.DiveRelay))RelayReady=2.2f;
        }
        public bool ConsumeRelay(){bool result=RelayReady>0;RelayReady=0;return result;}
        public float ReleaseDelay(DodgeWeapon weapon)
        {
            if(weapon==DodgeWeapon.Hammer)return .42f;
            if(weapon==DodgeWeapon.Bat)return .23f;
            if(weapon==DodgeWeapon.Standard)return .22f*WindupScale*(CounterReady>0?.55f:1f);
            return .22f;
        }
        public void BeginSwing(DodgeWeapon weapon,Vector3 aim)
        {SwingWeapon=weapon;SwingAim=aim;Swing=weapon==DodgeWeapon.Hammer?.85f:.5f;}
        public void ConsumeCounter(){CounterReady=0;}
        public void Flatten(){Flat=3f;}
        public float RetreatSpeed(Vector3 direction)
        {
            if(!Has(DodgeSkill.GuiltySprint) || !DodgeApp.Instance.Referee.MustRetreat(actor))return 1f;
            int team=DodgeApp.Instance.Room.seats[DodgeApp.Instance.SeatOf(actor)].team;
            return direction.z*(team==0?-1:1)>.1f?1.4f:1f;
        }
        void Update()
        {
            if(!DodgeApp.Active || !DodgeApp.Instance.Initialized)return;
            if(!DodgeGame.IsReplica && DodgeGame.SimulationActive)
            {
                float dt=Time.deltaTime;SpringReady=Mathf.Max(0,SpringReady-dt);Flat=Mathf.Max(0,Flat-dt);
                CounterReady=Mathf.Max(0,CounterReady-dt);RelayReady=Mathf.Max(0,RelayReady-dt);
                Swing=Mathf.Max(0,Swing-dt);relayCooldown=Mathf.Max(0,relayCooldown-dt);
            }
            if(DodgeGame.SimulationActive)FlatAmount=Mathf.MoveTowards(FlatAmount,Flat>0?1:0,Time.deltaTime*5);
            Draw();
        }
        void Draw()
        {
            var app=DodgeApp.Instance;bool active=app.Page==DodgePage.Playing || app.Page==DodgePage.Staging;
            bool inRest=DodgeCourt.InRest(transform.position);
            if(!label)label=app.Clubhouse.WorldLabel("Loadout in world",transform,Vector3.up*2.8f,"",.045f,new Color(.2f,.28f,.3f));
            label.text=!active?"":inRest && actor.isPlayer?"升级点 "+Points+" · "+(Item==DodgeItem.None?"投球选牌":ItemNames[(int)Item]+" · F")
                :actor.isPlayer && Item!=DodgeItem.None?ItemNames[(int)Item]+" · F":"";
            label.transform.rotation=DodgeGame.Instance.view.transform.rotation;
            if(!pocket)pocket=DodgePartyVisuals.Item(transform,Item==DodgeItem.None?DodgeItem.Soap:Item);
            if(pocket.gameObject.name!="Item "+Item){Destroy(pocket.gameObject);pocket=DodgePartyVisuals.Item(transform,Item);}
            pocket.gameObject.SetActive(active && Item!=DodgeItem.None);pocket.localPosition=new Vector3(-.48f,.8f,-.12f);pocket.localScale=Vector3.one*.45f;
            if(!shoes)
            {
                shoes=new GameObject("Spring shoe preview").transform;shoes.SetParent(transform,false);
                for(int s=-1;s<=1;s+=2)DodgePartyVisuals.Part(shoes,PrimitiveType.Cylinder,new Vector3(s*.25f,.16f,0),new Vector3(.24f,.15f,.24f),new Color(.9f,.75f,.3f));
            }
            shoes.gameObject.SetActive(active && SpringReady>0);
        }
        public void Write(BinaryWriter w)
        {w.Write((byte)Item);w.Write((byte)Skills);w.Write((byte)Points);w.Write(Experience);w.Write(SpringReady);w.Write(Flat);w.Write(CounterReady);w.Write(Swing);w.Write((byte)SwingWeapon);DodgeNet.WriteVector(w,SwingAim);}
        public void Read(BinaryReader r)
        {Item=(DodgeItem)r.ReadByte();Skills=r.ReadByte();Points=r.ReadByte();Experience=r.ReadInt32();SpringReady=r.ReadSingle();Flat=r.ReadSingle();CounterReady=r.ReadSingle();Swing=r.ReadSingle();SwingWeapon=(DodgeWeapon)r.ReadByte();SwingAim=DodgeNet.ReadVector(r);}
    }
}
