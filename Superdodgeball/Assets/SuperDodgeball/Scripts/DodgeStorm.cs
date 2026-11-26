using UnityEngine;
using UnityEngine.Rendering;

namespace SuperDodgeball
{
    // Three short-lived, local layers: silhouette and forks, drifting vapour, hot sparks.
    // Gameplay and hit detection never depend on individual particles.
    internal sealed class DodgeStorm : MonoBehaviour
    {
        internal static readonly Color Gold = new Color(1f,.79f,.24f);
        static readonly Color Core = new Color(1f,.99f,.84f);
        static readonly Color Blue = new Color(.42f,.70f,1f);
        static readonly Color Cloud = new Color(.61f,.66f,.82f,.70f);
        LineRenderer[] outlines, cores;
        LineRenderer halo;
        static Material glow;
        static AudioClip strikeSound;
        internal static void Dispose()
        { if(glow) Destroy(glow); }
        ParticleSystem vapour, sparks;
        float clock, duration, nextPuff;
        Vector3 normal;
        bool impact, flying;
        const int Nodes=9;

        internal static DodgeStorm Around(Transform parent,bool shocked)
        {
            var go=new GameObject(shocked?"Static on struck player":"Olympian charge");
            go.transform.SetParent(parent,false);
            var fx=go.AddComponent<DodgeStorm>();
            fx.Build(shocked?3:6);
            return fx;
        }
        internal static void Trail(Transform projectile)
        {
            var wake = new GameObject("Forked lightning wake");
            wake.transform.SetParent(projectile,false);
            var storm = wake.AddComponent<DodgeStorm>();
            storm.flying = true;
            storm.Build(2);
        }
        internal static void Impact(Transform root,Vector3 normal,float duration)
        {
            var fx=root.gameObject.AddComponent<DodgeStorm>();
            fx.normal=normal.sqrMagnitude>.001f?normal.normalized:Vector3.up;
            fx.duration=duration; fx.impact=true;
            fx.Build(7);
            float scale=Mathf.Lerp(.8f,1.32f,Mathf.InverseLerp(.76f,.98f,duration));
            Vector3 point=root.position+fx.normal*.08f;
            var rotation=Quaternion.FromToRotation(Vector3.up,fx.normal);
            for(int i=0;i<42;i++)
            {
                float angle=i*2.39996f;
                var side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float phase=(i%9)/8f;
                fx.Particle(fx.sparks,point,rotation*(side*(2.3f+phase*5f)+Vector3.up*(1f+i%5))*scale,
                    i%3==0?Blue:Gold,(.055f+.075f*phase)*scale,.22f+phase*.39f);
                if(i%3!=1)
                    fx.Particle(fx.vapour,point+rotation*side*(.3f+phase*1.50f),rotation*(side*(2.3f+phase)+Vector3.up*.76f)*scale,
                        i%2==0?Cloud:new Color(.83f,.85f,.94f,.75f),(.55f+.44f*phase)*scale,.60f+.29f*phase);
            }
            fx.DrawImpact();
            if(!strikeSound)strikeSound=Resources.Load<AudioClip>("Court/OlympianStrike");
            if(strikeSound)
            {
                var audio=root.gameObject.AddComponent<AudioSource>();
                audio.spatialBlend=.36f;audio.minDistance=4f;audio.maxDistance=35f;
                audio.PlayOneShot(strikeSound,.68f);
            }
        }
        void Build(int count)
        {
            outlines=new LineRenderer[count]; cores=new LineRenderer[count];
            for(int i=0;i<count;i++)
            {
                outlines[i]=Line("Electric edge",0);
                cores[i]=Line("Light core",1);
            }
            if(!glow)glow=new Material(Resources.Load<Shader>("Court/OlympianGlow"));
            halo=Line("Feathered corona",-2);halo.sharedMaterial=glow;
            vapour=Particles("Storm mist",false);sparks=Particles("Charged flecks",true);
        }
        LineRenderer Line(string name,int order)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var line=child.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=Nodes;
            line.sharedMaterial=DodgeGame.Instance.trailMaterial;line.sortingOrder=order+2;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.numCornerVertices=1;
            line.enabled=false;
            return line;
        }
        ParticleSystem Particles(string name,bool hot)
        {
            var node=new GameObject(name);node.transform.SetParent(transform,false);
            var ps=node.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=hot?52:34;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=hot?.24f:-.03f;
            var emission=ps.emission;emission.enabled=false;
            var shape=ps.shape;shape.enabled=false;
            var colour=ps.colorOverLifetime;colour.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(.78f,.28f),new GradientAlphaKey(0,1)});
            colour.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,hot?1:.50f,1,hot?.20f:1.7f));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=DodgeWeaponVfx.PuffMaterial();
            if(hot){renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=.055f;renderer.lengthScale=1.65f;}
            renderer.sortingOrder=hot?2:0;ps.Play();
            return ps;
        }
        void Particle(ParticleSystem ps,Vector3 p,Vector3 v,Color ink,float size,float life)
        {
            var parcel=new ParticleSystem.EmitParams {position=p,velocity=v,startColor=ink,startSize=size,startLifetime=life,
                rotation=clock*83f+p.x*39f+p.z*11f};
            ps.Emit(parcel,1);
        }
        void OnDisable()
        {
            if(vapour)vapour.Clear();
            if(sparks)sparks.Clear();
            if(outlines!=null)for(int i=0;i<outlines.Length;i++){outlines[i].enabled=false;cores[i].enabled=false;}
            if(halo)halo.enabled=false;
            nextPuff=clock;
        }
        void Update()
        {
            if(!DodgeGame.SimulationActive)
            {if(vapour.isPlaying){vapour.Pause();sparks.Pause();}return;}
            if(!vapour.isPlaying){vapour.Play();sparks.Play();}
            clock+=Time.deltaTime;
            if(impact)DrawImpact();
            if(flying)
            {
                Vector3 p=transform.position, back=-transform.forward, side=transform.right;
                Fork(0,p+back*.20f,p+back*1.55f+side*.10f,.070f,.14f,Gold,.95f);
                Fork(1,p+back*.42f,p+back*.95f-side*.32f,.041f,.085f,Blue,.90f);
            }
        }
        void Style(int index,float width,Color rim,float alpha)
        {
            var outer=outlines[index];var inner=cores[index];
            outer.enabled=inner.enabled=width>.0005f && alpha>.01f;
            if(index==0)
            {
                halo.enabled=outer.enabled;
                halo.startWidth=width*5;halo.endWidth=width*2.6f;
                var bloom=rim;bloom.a=alpha*.72f;halo.startColor=halo.endColor=bloom;
            }
            outer.startWidth=width*1.9f;outer.endWidth=width*.8f;
            inner.startWidth=width*.82f;inner.endWidth=width*.40f;
            rim.a=alpha;var core=Core;core.a=alpha;
            outer.startColor=outer.endColor=rim;inner.startColor=inner.endColor=core;
        }
        void Node(int index,int j,Vector3 p)
        {outlines[index].SetPosition(j,p);cores[index].SetPosition(j,p);if(index==0)halo.SetPosition(j,p);}
        void Fork(int index,Vector3 from,Vector3 to,float width,float jitter,Color rim,float opacity)
        {
            Style(index,width,rim,opacity);
            if(!cores[index].enabled)return;
            var axis=(to-from).normalized;var cross=Vector3.Cross(axis,Mathf.Abs(axis.y)<.8f?Vector3.up:Vector3.forward).normalized;
            var other=Vector3.Cross(cross,axis);
            float frame=Mathf.Floor(clock*12f);
            for(int j=0;j<Nodes;j++)
            {
                float t=j/(Nodes-1f);float id=frame*.77f+j*7.91f+index*13.6f;
                var offset=(cross*Mathf.Sin(id*3.17f)+other*Mathf.Cos(id*2.71f))*jitter*Mathf.Sin(Mathf.PI*t);
                Node(index,j,Vector3.Lerp(from,to,t)+offset);
            }
        }
        internal void Arrival(Vector3 feet)
        {
            clock=nextPuff=0;
            for(int i=0;i<24;i++)
            {
                float a=i*2.39996f;var side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                float level=i%8/7f;
                Particle(sparks,feet+side*(.38f+.16f*level)+Vector3.up*(.22f+1.75f*level),
                    Vector3.Cross(side,Vector3.up)*1.5f+Vector3.up*(1.1f+1.7f*level),
                    i%3==0?Blue:Gold,.065f+.065f*level,.29f+.24f*level);
                if(i%3==0)Particle(vapour,feet+side*.38f+Vector3.up*.14f,side*.9f+Vector3.up*.66f,Cloud,.51f,.59f);
            }
        }
        internal void PoseCharge(DodgeActor actor,float amount)
        {
            Vector3 feet=actor.transform.position, right=actor.transform.right, hand=actor.HandPosition;
            float arrival=1-Mathf.SmoothStep(0,1,clock/.42f);
            float t=Mathf.Max(arrival*.82f, Mathf.SmoothStep(0,1,amount));
            for(int i=0;i<3;i++)
            {
                float a=clock*(1.7f+.2f*i)+i*2.0944f;
                var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 from=feet+radial*(.65f+.43f*t)+Vector3.up*(.04f+.17f*i);
                Vector3 to=i==0?hand:feet+Quaternion.Euler(0,48f,0)*radial*(.42f+.32f*t)+Vector3.up*(.52f+.25f*i);
                Fork(i,from,to,.025f+.029f*t,.055f+.09f*t,i==0?Gold:Blue,t*(.65f+.27f*Mathf.Sin(a*3)));
            }
            float angle=clock*2.9f;
            Vector3 ring=right*Mathf.Cos(angle)+actor.transform.forward*Mathf.Sin(angle);
            Fork(3,hand+ring*(.30f+.18f*t),hand+Vector3.up*.40f,.035f,.10f,Gold,t);
            Vector3 ground=new Vector3(feet.x,.085f,feet.z);
            // Broken induction rings travel around the launch point; gaps leave the court lines legible.
            float height=Mathf.Clamp01(feet.y/DodgeZeus.MaxHeight);
            for(int k=0;k<2;k++)
            {
                Style(4+k,.041f+.021f*t,k==0?Blue:Gold,height*t*.8f);
                if(!cores[4+k].enabled)continue;
                for(int j=0;j<Nodes;j++)
                {
                    float phase=clock*2.8f+k*Mathf.PI+j*.196f;
                    float radius=.65f+.80f*height+.07f*Mathf.Sin(phase*3f);
                    Node(4+k,j,ground+new Vector3(Mathf.Cos(phase)*radius,.015f,Mathf.Sin(phase)*radius));
                }
            }
            if(!DodgeGame.SimulationActive || clock<nextPuff)return;
            nextPuff=clock+Mathf.Lerp(.26f,.065f,t);
            float spin=clock*9.1f;var side=new Vector3(Mathf.Cos(spin),0,Mathf.Sin(spin));
            var tangent=Vector3.Cross(side,Vector3.up);
            Particle(vapour,feet+side*(.38f+.35f*t)+Vector3.up*.12f,
                tangent*(.22f+.42f*t)+Vector3.down*.16f,Cloud,.24f+.45f*t,.65f+.27f*t);
            if(height>.2f)
                Particle(vapour,ground+side*(.63f+.37f*t),tangent*.76f+Vector3.up*(.49f+t),
                    new Color(.82f,.85f,.95f,.48f),.4f+t*.33f,.84f);
            if(t>.12f)
                Particle(sparks,hand+side*.15f,side*(.4f+t)+Vector3.up*(.8f+t),Gold,.055f+.04f*t,.29f);
        }
        internal void PoseShock(Vector3 p,float left)
        {
            for(int i=0;i<3;i++)
            {
                float a=clock*7+i*2.09f;var r=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.52f;
                Fork(i,p+r+Vector3.up*.70f,p-r*.78f-Vector3.up*(.20f+i*.12f),.051f,.17f,
                    i==1?Gold:Blue,Mathf.Clamp01(left*5));
            }
        }
        void DrawImpact()
        {
            var p=transform.position+normal*.07f;
            var rotation=Quaternion.FromToRotation(Vector3.up,normal);
            float strength=Mathf.InverseLerp(.76f,.98f,duration);
            float reach=DodgePartyEffects.BlastRadius(DodgeWeapon.Thunderbolt,strength);
            float life=clock;float fade=1-Mathf.SmoothStep(0,1,life/.53f);
            var top=p+Vector3.up*(2.8f+2.1f*strength)+new Vector3(-.5f,0,.22f);
            Fork(0,top,p,.10f+.12f*strength,.23f+.12f*strength,Gold,fade);
            for(int i=1;i<6;i++)
            {
                float a=i*2.39996f;var side=rotation*new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                if(i<=2)
                    Fork(i,Vector3.Lerp(p,top,.31f+.14f*i),p+side*(.7f+.26f*i),.066f,.17f,Blue,fade);
                else
                    Fork(i,p, p+side*(reach*Mathf.Lerp(.23f,.90f,Mathf.Clamp01(life/.32f))),.06f,.10f,Gold,fade);
            }
            Style(6,.046f,Blue,.76f*(1-Mathf.Clamp01(life/.47f)));
            for(int j=0;j<Nodes;j++)
            {
                float a=2*Mathf.PI*j/(Nodes-1);
                Node(6,j,p+rotation*new Vector3(Mathf.Cos(a),.025f,Mathf.Sin(a))*Mathf.Lerp(.13f,reach,Mathf.SmoothStep(0,1,life/.43f)));
            }
        }
    }
}
