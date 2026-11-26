using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace SuperDodgeball
{
    public enum DodgeEffect { Banana, Decoy, Tea, Bomb, Burst, VestBurst, Thunderstrike, Refill, RocketBurst, HornWave }
    public sealed partial class DodgePartyEffects : MonoBehaviour
    {
        public static DodgePartyEffects Instance { get; private set; }
        public sealed class Effect
        {
            public int id,owner;public DodgeEffect kind;public Vector3 position,velocity;public float left,total;public Transform visual;internal Transform flash;public TextMesh label;internal DodgeCostumeScatter scatter;
        }
        readonly Dictionary<int,Effect> effects=new Dictionary<int,Effect>();
        readonly List<int> remove=new List<int>();int sequence;float clock;
        public int Explosions {get;private set;}
        public int ActiveCount=>effects.Count;
        public IEnumerable<Effect> Active=>effects.Values;
        readonly RaycastHit[] hits=new RaycastHit[64];
        void Awake(){Instance=this;}
        public void ResetEffects(){ClearWeapons();foreach(var e in effects.Values)if(e.visual)Destroy(e.visual.gameObject);effects.Clear();Explosions=0;clock=0;}
        public Effect Spawn(DodgeEffect kind,Vector3 point,Vector3 velocity,DodgeActor source,float seconds)
        {
            if(effects.Count>=48){int first=0;foreach(var key in effects.Keys){first=key;break;}Destroy(effects[first].visual.gameObject);effects.Remove(first);}
            var e=new Effect{id=++sequence,kind=kind,position=point,velocity=velocity,left=seconds,total=seconds,owner=DodgeApp.Instance.SeatOf(source)};
            Create(e);effects.Add(e.id,e);return e;
        }
        void Create(Effect e)
        {
            var root=new GameObject("Party effect "+e.kind);e.visual=root.transform;e.visual.position=e.position;
            if (e.kind == DodgeEffect.Refill)
            {
                DodgeWeaponArt.Model(e.visual,DodgeWeaponArt.Prop.FoamAmmo).localScale=Vector3.one*.42f;
            }
            else if(e.kind==DodgeEffect.HornWave)
            {
                DodgeFanGifts.Ring(e.visual,DodgePartyVisuals.Mint,1);
                DodgeGame.Instance.PlayTone(315f,.22f,.12f);
                DodgeGame.Instance.PlayTone(405f,.29f,.09f);
            }
            else if (e.kind == DodgeEffect.Thunderstrike) DodgeStorm.Impact(e.visual,e.velocity,e.total);
            else if(e.kind==DodgeEffect.Bomb){var model=DodgePartyVisuals.Weapon(e.visual,DodgeWeapon.Bomb);model.localScale=Vector3.one*.39f;e.label=model.GetComponentInChildren<TextMesh>();}
            else if(e.kind==DodgeEffect.Burst || e.kind==DodgeEffect.VestBurst || e.kind==DodgeEffect.RocketBurst)
            {
                DodgeWeaponVfx.Emit(e.visual,false,e.kind==DodgeEffect.VestBurst);
                e.flash=DodgeWeaponArt.Model(e.visual,DodgeWeaponArt.Prop.Flash);
                if(e.kind==DodgeEffect.VestBurst)
                {
                    e.scatter=e.visual.gameObject.AddComponent<DodgeCostumeScatter>();
                    e.scatter.Build(DodgeApp.Instance.Room.seats[e.owner].team);
                }
                var ring=e.visual.gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=48;
                ring.sharedMaterial=DodgeGame.Instance.trailMaterial;ring.startWidth=ring.endWidth=1f;
                for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*.5f,.05f,Mathf.Sin(a)*.5f));}
            }
            else if(e.kind==DodgeEffect.Decoy)
            {
                var a=DodgeApp.Instance.Actors[Mathf.Max(0,e.owner)];var c=DodgeApp.Instance.Room.seats[Mathf.Max(0,e.owner)].team==0?DodgePartyVisuals.Mint:new Color(.98f,.65f,.47f);
                DodgePartyVisuals.Part(e.visual,PrimitiveType.Capsule,Vector3.up,new Vector3(.75f,.85f,.65f),c);
                DodgePartyVisuals.Part(e.visual,PrimitiveType.Sphere,Vector3.up*1.8f,Vector3.one*.75f,c);
                for(int s=-1;s<=1;s+=2){DodgePartyVisuals.Part(e.visual,PrimitiveType.Capsule,new Vector3(s*.55f,1.15f,0),new Vector3(.23f,.55f,.23f),c);DodgePartyVisuals.Part(e.visual,PrimitiveType.Capsule,new Vector3(s*.24f,.4f,0),new Vector3(.3f,.5f,.3f),c);}
                e.visual.rotation=a.transform.rotation;
            }
            else
            {
                DodgePartyVisuals.Item(e.visual,e.kind==DodgeEffect.Banana?DodgeItem.Banana:DodgeItem.Tea);
                if(e.kind==DodgeEffect.Banana && e.owner<0)
                    DodgeFanGifts.Ring(e.visual,DodgeFanGifts.Colour(DodgeGiftKind.Peel,default,default),1.05f);
            }
        }
        public static bool LineClear(Vector3 from,Vector3 to)
        {
            foreach(var hit in Physics.RaycastAll(from,to-from,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore))
                if(hit.collider.GetComponent<DodgeSurface>())return false;
            return true;
        }
        public static bool Strike(DodgeActor source,DodgeActor target,Vector3 point,Vector3 impulse,float launch=0,float carry=3f)
        {
            var app=DodgeApp.Instance;
            if(!app.CanHurt(source,target) || source==target || target.IsStunned)return false;
            if(!target.ReceiveHit(point,source && source.isPlayer,impulse,launch,carry))return false;
            app.CreditHit(source,target);var bot=target.GetComponent<DodgeBot>();if(bot)bot.RememberAttacker(source);return true;
        }
        public void Melee(DodgeActor source,DodgeWeapon weapon,Vector3 aim,float power)
        {
            Vector3 direction=Vector3.ProjectOnPlane(aim-source.transform.position,Vector3.up).normalized;
            if(direction.sqrMagnitude<.1f)direction=source.transform.forward;
            Vector3 origin=source.transform.position+Vector3.up*1.15f;
            float range=weapon==DodgeWeapon.Hammer?2.65f:2.35f;
            var seen=new HashSet<DodgeActor>();
            var seenSpectators=new HashSet<DodgeSpectator>();
            foreach(var hit in Physics.OverlapSphere(origin+direction*1.1f,1.5f,~0,QueryTriggerInteraction.Collide))
            {
                var fan=hit.GetComponent<DodgeSpectatorHitbox>();
                if(fan && !seenSpectators.Contains(fan.spectator))
                {
                    Vector3 contact=hit.ClosestPoint(origin);
                    if(Vector3.Distance(contact,origin)<range && Vector3.Dot((contact-origin).normalized,direction)>.1f && LineClear(origin,contact))
                    {seenSpectators.Add(fan.spectator);fan.spectator.Hit(contact,direction*5+Vector3.up*(weapon==DodgeWeapon.Bat?4:1),source);}
                }
                var box=hit.GetComponent<DodgeHitbox>();if(!box || seen.Contains(box.actor))continue;
                Vector3 p=hit.ClosestPoint(origin);
                if(Vector3.Distance(p,origin)>range || Vector3.Dot((p-origin).normalized,direction)<.1f || !LineClear(origin,p))continue;
                seen.Add(box.actor);
                if(Strike(source,box.actor,p,direction*(weapon==DodgeWeapon.Bat?7:4)+Vector3.up*2,weapon==DodgeWeapon.Bat?5:0))
                {if(weapon==DodgeWeapon.Hammer)box.actor.Kit.Flatten();DodgeGame.Instance.EmitFoam(p,direction,12,1.2f);}
            }
            foreach(var e in effects.Values)
                if(e.kind==DodgeEffect.Bomb && Vector3.Distance(origin,e.position)<range && Vector3.Dot((e.position-origin).normalized,direction)>.1f && LineClear(origin,e.position))
                {e.velocity=direction*18+Vector3.up*5;e.owner=DodgeApp.Instance.SeatOf(source);}
            DodgeGame.Instance.PlayTone(weapon==DodgeWeapon.Hammer?85:240,.14f,.1f);
        }
        public bool Intercept(Vector3 from,Vector3 to,DodgeActor source)
        {
            foreach(var e in effects.Values)
            {
                if(e.kind!=DodgeEffect.Decoy || e.left<1.1f || (source && e.owner>=0 && DodgeApp.Instance.SameTeam(source,DodgeApp.Instance.Actors[e.owner])))continue;
                Vector3 p=e.position+Vector3.up;
                Vector3 line=to-from;float t=line.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(p-from,line)/line.sqrMagnitude);
                if(Vector3.Distance(p,from+line*t)<.9f && LineClear(from,p))
                {e.left=1f;e.velocity=new Vector3(4,5,2);DodgeGame.Instance.EmitFoam(p,Vector3.up,7,1);return true;}
            }
            return false;
        }
        public Vector3 DecoyTarget(DodgeActor seeker,Vector3 fallback)
        {
            float distance=Vector3.Distance(seeker.transform.position,fallback);
            foreach(var e in effects.Values)
                if(e.kind==DodgeEffect.Decoy && e.left>1.1f && e.owner>=0 && !DodgeApp.Instance.SameTeam(seeker,DodgeApp.Instance.Actors[e.owner])
                    && Vector3.Distance(seeker.transform.position,e.position)<distance && LineClear(seeker.transform.position+Vector3.up,e.position+Vector3.up))
                    return e.position+Vector3.up;
            return fallback;
        }
        public void Write(BinaryWriter w)
        {
            WriteWeapons(w);w.Write(Explosions);w.Write((byte)effects.Count);
            foreach(var e in effects.Values){w.Write(e.id);w.Write((byte)e.kind);w.Write((sbyte)e.owner);DodgeNet.WriteVector(w,e.position);DodgeNet.WriteVector(w,e.velocity);w.Write(e.left);w.Write(e.total);}
        }
        public void Read(BinaryReader r)
        {
            ReadWeapons(r);
            int blasts=r.ReadInt32();if(blasts>Explosions)DodgeGame.Instance.PlayTone(75,.17f,.12f);Explosions=blasts;
            int n=r.ReadByte();if(n>64)throw new InvalidDataException("Too many effects");
            var seen=new HashSet<int>();
            for(int i=0;i<n;i++)
            {
                int id=r.ReadInt32();var kind=(DodgeEffect)r.ReadByte();int owner=r.ReadSByte();Vector3 p=DodgeNet.ReadVector(r),v=DodgeNet.ReadVector(r);float left=r.ReadSingle(),total=r.ReadSingle();
                Effect e;if(!effects.TryGetValue(id,out e)){e=new Effect{id=id,kind=kind,owner=owner,position=p,velocity=v,left=left,total=total};Create(e);effects.Add(id,e);}
                e.position=p;e.velocity=v;e.left=left;e.total=total;e.owner=owner;seen.Add(id);
            }
            remove.Clear();foreach(var id in effects.Keys)if(!seen.Contains(id))remove.Add(id);Remove();
        }
        void Remove(){foreach(var id in remove){if(effects[id].visual)Destroy(effects[id].visual.gameObject);effects.Remove(id);}}
        void Update()
        {
            if(!DodgeApp.Active)return;
            if(DodgeGame.SimulationActive && DodgeApp.Instance.Page==DodgePage.Playing && !DodgeGame.IsReplica)Tick(Time.deltaTime);
            foreach(var e in effects.Values)
            {
                e.visual.position=e.position;
                if(e.kind==DodgeEffect.HornWave)
                {
                    float t=1-Mathf.Clamp01(e.left/e.total);
                    e.visual.localScale=Vector3.one*Mathf.Lerp(.3f,5,t);
                    var ring=e.visual.GetComponent<LineRenderer>();var c=DodgePartyVisuals.Mint;c.a=1-t;
                    ring.startColor=ring.endColor=c;
                }
                if(e.kind==DodgeEffect.Refill && e.owner>=0)
                {
                    float t=1-Mathf.Clamp01(e.left/e.total);
                    var hand=DodgeApp.Instance.Actors[e.owner].HandPosition;
                    e.visual.position=Vector3.Lerp(e.position,hand,t)+Vector3.up*(4*t*(1-t)*1.4f);
                }
                if(e.kind==DodgeEffect.Decoy)
                {e.visual.localScale=Vector3.one*(e.left<1?e.left:1);e.visual.rotation*=Quaternion.Euler(0,0,e.left<1?Time.deltaTime*650:0);}
                if(e.kind==DodgeEffect.Bomb)
                {e.visual.localScale=Vector3.one*(1+Mathf.Sin(e.left*(e.left<1?30:12))*.08f);e.label.text=Mathf.CeilToInt(e.left).ToString("00");e.visual.rotation=Quaternion.Euler(0,DodgeGame.Instance.view.Yaw,0);}
                if(e.kind==DodgeEffect.Burst || e.kind==DodgeEffect.VestBurst || e.kind==DodgeEffect.RocketBurst)
                {
                    float age=e.total-e.left;
                    e.flash.gameObject.SetActive(age<.26f);
                    if(age<.26f)e.flash.localScale=Vector3.one*(e.scatter?1.75f:1.22f)*Mathf.Sin(Mathf.PI*Mathf.Clamp01(.09f+age/.285f));
                    var ring=e.visual.GetComponent<LineRenderer>();ring.enabled=age<.4f;
                    float radius=Mathf.Lerp(.18f,BlastRadius(e.scatter?DodgeWeapon.Vest:e.kind==DodgeEffect.RocketBurst?DodgeWeapon.Rocket:DodgeWeapon.Bomb),Mathf.SmoothStep(0,1,age/.4f));
                    if(ring.enabled)
                    {
                        float t=Mathf.Clamp01(age/.4f);
                        var chalk=Color.Lerp(new Color(1f,.73f,.32f),new Color(.98f,.89f,.70f),t);
                        chalk.a=1f-Mathf.SmoothStep(0,1,t);
                        ring.startColor=ring.endColor=chalk;
                        ring.widthMultiplier=Mathf.Lerp(.16f,.045f,t);
                        for(int i=0;i<ring.positionCount;i++)
                        {
                            float angle=i*Mathf.PI*2/ring.positionCount;
                            ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,.05f,Mathf.Sin(angle)*radius));
                        }
                    }
                    if(e.scatter)e.scatter.Draw(age,e.left);
                }
            }
        }
        void Tick(float dt)
        {
            clock+=dt;remove.Clear();var additions=new List<Effect>();
            foreach(var e in effects.Values)
            {
                e.left-=dt;
                if(e.kind==DodgeEffect.Bomb)
                {
                    Vector3 next=e.position+e.velocity*dt;
                    if(DodgeBall.Trace(e.position,next,.3f,null,hits,out RaycastHit hit,out DodgeActor ignored,false,false))
                    {next=hit.point+hit.normal*.32f;e.velocity=Vector3.Reflect(e.velocity,hit.normal)*.48f;}
                    e.position=next;e.velocity+=Vector3.down*9.5f*dt;
                    if(e.position.y<.32f){e.position.y=.32f;e.velocity.y=Mathf.Abs(e.velocity.y)*.4f;e.velocity.x*=.98f;e.velocity.z*=.98f;}
                    if(e.left<=0)
                    {
                        Vector3 blast=e.position;
                        DamageBlast(blast+Vector3.up*.12f,e.owner>=0?DodgeApp.Instance.Actors[e.owner]:null,DodgeWeapon.Bomb,5f,4f,null,null);
                        additions.Add(new Effect{id=++sequence,kind=DodgeEffect.Burst,position=blast,left=1.48f,total=1.48f,owner=e.owner});
                    }
                }
                else if(e.kind==DodgeEffect.Banana && e.left<e.total-.6f)
                {
                    if(DodgeApp.Instance.Referee.SlipGuard(e.position))e.left=0;
                    else foreach(var actor in DodgeApp.Instance.Actors)
                        if(actor.gameObject.activeSelf && !actor.IsStunned && !DodgeReferee.Restricted(actor) && actor.transform.position.y<.5f && Vector3.Distance(Vector3.ProjectOnPlane(actor.transform.position-e.position,Vector3.up),Vector3.zero)<.65f)
                        {actor.ReceiveHit(actor.Puppet.BodyCenter,false,actor.transform.forward*6+Vector3.up*2,2);e.left=0;break;}
                }
                else if(e.kind==DodgeEffect.Decoy && e.left<1f){e.position+=e.velocity*dt;e.velocity+=Vector3.down*5*dt;}
                else if(e.kind==DodgeEffect.Tea)e.position=Vector3.Lerp(e.position,new Vector3(-10.4f*DodgeCourt.Scale,3.2f,0),dt*5);
                if(e.left<=0)remove.Add(e.id);
            }
            Remove();foreach(var e in additions){Create(e);effects.Add(e.id,e);}
        }
        void OnDestroy(){ResetEffects();if(Instance==this){Instance=null;DodgeWeaponArt.Dispose();DodgeWeaponVfx.Dispose();DodgeStorm.Dispose();}}
    }
}
