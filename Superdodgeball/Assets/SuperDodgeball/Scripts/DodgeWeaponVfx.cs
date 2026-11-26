using UnityEngine;

namespace SuperDodgeball
{
    // Cosmetic particles share a painted puff. No particle collision or rigid bodies.
    internal sealed class DodgeWeaponVfx : MonoBehaviour
    {
        static Texture2D puff;
        static Material ink;
        ParticleSystem systemFx;
        Transform rocket;
        bool following;
        internal static Material PuffMaterial()
        {
            if(ink)return ink;
            const int n=64;
            puff=new Texture2D(n,n,TextureFormat.RGBA32,true){name="Chalk puff",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1;
                float angle=Mathf.Atan2(v,u);
                float rim=.76f+.084f*Mathf.Sin(5*angle+.4f)+.065f*Mathf.Cos(3*angle)+.025f*Mathf.Sin(8*angle);
                float distance=Mathf.Sqrt(u*u+v*v);
                float alpha=Mathf.SmoothStep(0,1,(rim-distance)*19f);
                byte shade=(byte)Mathf.Lerp(181,255,Mathf.SmoothStep(0,1,.60f+v*.83f-u*.28f));
                pixels[y*n+x]=new Color32(shade,shade,shade,(byte)(255*alpha));
            }
            puff.SetPixels32(pixels);puff.Apply(true,true);
            ink=new Material(DodgeGame.Instance.trailMaterial){name="Gym chalk particle"};
            ink.SetTexture("_BaseMap",puff);return ink;
        }
        internal static void Dispose()
        {
            if(ink)Destroy(ink);if(puff)Destroy(puff);
        }
        internal static void Emit(Transform parent,bool exhaust,bool big=false)
        {
            var go=new GameObject(exhaust?"Rocket exhaust":"Chalk and sparks");
            go.transform.SetParent(parent,false);
            var fx=go.AddComponent<DodgeWeaponVfx>();
            if(exhaust)
            {
                fx.following=true;fx.rocket=parent;
                go.transform.SetParent(DodgePartyEffects.Instance.transform,true);
            }
            var ps=fx.systemFx=go.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;
            main.playOnAwake=false;main.loop=exhaust;main.duration=1;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
            main.maxParticles=exhaust?72:96;
            main.startLifetime=new ParticleSystem.MinMaxCurve(exhaust?.29f:.44f,exhaust?.64f:1.32f);
            main.startSize=new ParticleSystem.MinMaxCurve(exhaust?.18f:.52f,exhaust?.44f:big?1.31f:1.06f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(exhaust?.20f:1.25f,exhaust?.75f:big?5.8f:4.8f);
            main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=exhaust?.065f:.55f;
            if(!exhaust){shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.rotation=new Vector3(-90,0,0);}
            if(!exhaust)
            {
                var lift=ps.velocityOverLifetime;
                lift.enabled=true;
                lift.space=ParticleSystemSimulationSpace.World;
                lift.y=new ParticleSystem.MinMaxCurve(1.15f);
            }
            var emit=ps.emission;emit.rateOverTime=exhaust?14:0;emit.rateOverDistance=exhaust?5.2f:0;
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.43f,1,1.55f));
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.85f,.45f),0),
                new GradientColorKey(exhaust?new Color(1,.91f,.74f):new Color(1,.56f,.29f),.21f),
                new GradientColorKey(new Color(.77f,.79f,.79f),.58f),new GradientColorKey(new Color(.46f,.56f,.60f),1)},
                new[]{new GradientAlphaKey(.96f,0),new GradientAlphaKey(.89f,.48f),new GradientAlphaKey(0,1)});
            var colors=ps.colorOverLifetime;colors.enabled=true;colors.color=gradient;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=PuffMaterial();
            ps.Play();if(!exhaust)ps.Emit(big?84:64);
        }
        void LateUpdate()
        {
            if(!following)return;
            if(rocket){transform.position=rocket.position-rocket.forward*.24f;return;}
            following=false;
            var main=systemFx.main;main.stopAction=ParticleSystemStopAction.Destroy;
            systemFx.Stop(true,ParticleSystemStopBehavior.StopEmitting);
        }
        void Update()
        {
            if(!systemFx || !DodgeApp.Active)return;
            if(!DodgeGame.SimulationActive && systemFx.isPlaying)systemFx.Pause();
            else if(DodgeGame.SimulationActive && systemFx.isPaused)systemFx.Play();
        }
    }

    internal sealed class DodgeCostumeScatter : MonoBehaviour
    {
        readonly Transform[] pieces=new Transform[5];
        internal void Build(int team)
        {
            for(int i=0;i<pieces.Length;i++)
            {
                var prop=i==0?(team==0?DodgeWeaponArt.Prop.MintHead:DodgeWeaponArt.Prop.PeachHead):
                    i<3?DodgeWeaponArt.Prop.Mitt:DodgeWeaponArt.Prop.Shoe;
                pieces[i]=DodgeWeaponArt.Model(transform,prop);
            }
        }
        internal void Draw(float age,float remaining)
        {
            for(int i=0;i<pieces.Length;i++)
            {
                float angle=i*2.39996f+.5f;
                float t=Mathf.Clamp01(age/(.62f+i*.058f));
                float travel=(1-(1-t)*(1-t))*(1.25f+i*.24f);
                var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                float floor=.13f-transform.position.y;
                float y=Mathf.Lerp(.16f,floor,t)+(i==0?1.4f:1f)*4*t*(1-t);
                pieces[i].localPosition=radial*travel+Vector3.up*y;
                pieces[i].localRotation=Quaternion.Euler(250*t,angle*57+t*130,i*35+t*190);
                pieces[i].localScale=Vector3.one*Mathf.SmoothStep(0,1,remaining/.4f);
            }
        }
    }
}
