using System.Collections.Generic;
using UnityEngine;

namespace SuperDodgeball
{
    // A shared mesh for each prop keeps a whole volley or supply shelf inexpensive.
    internal static partial class DodgeWeaponArt
    {
        internal enum Prop { Cannon, Bat, Launcher, Rocket, Timer, Vest, Coil, Cradle, Flash, Pill, BittenPill, Injector, MintHead, PeachHead, Mitt, Shoe, Thunderbolt, Thundercloud, Laurel, Toga, Soap, FoamAmmo, Cherries, AirHorn, Peel, BubbleTea, SpringShoes, TrainingDummy }
        const int Ink=0x293a43, Blue=0x396c79, Sea=0x70aca5, Ivory=0xffe5af,
            Gold=0xefae49, Red=0xd84e43, Rose=0xf27863, Metal=0x92aab0, Brown=0x92603c;
        static readonly Dictionary<Prop,Mesh> meshes=new();
        static Material paint;
        internal static Transform Model(Transform parent, Prop prop)
        {
            if(!paint)paint=new Material(Resources.Load<Shader>("Court/Neighbourhood"));
            if(!meshes.TryGetValue(prop,out var mesh) || !mesh)
            {
                var m=new CourtSketchMesh();
                switch(prop)
                {
                    case Prop.Thunderbolt: Thunderbolt(m);break;
                    case Prop.Thundercloud: Thundercloud(m);break;
                    case Prop.Laurel: Laurel(m);break;
                    case Prop.Toga: Toga(m);break;
                    case Prop.Cannon: Cannon(m);break;
                    case Prop.Bat: Bat(m);break;
                    case Prop.Launcher: Launcher(m);break;
                    case Prop.Rocket: Rocket(m);break;
                    case Prop.Timer: Timer(m);break;
                    case Prop.Vest: Vest(m);break;
                    case Prop.Coil: Coil(m);break;
                    case Prop.Cradle: Cradle(m);break;
                    case Prop.Peel: FanPeel(m);break;
                    case Prop.BubbleTea: FanTea(m);break;
                    case Prop.SpringShoes: FanShoes(m);break;
                    case Prop.TrainingDummy: FanDummy(m);break;
                    case Prop.FoamAmmo: FanAmmo(m);break;
                    case Prop.Cherries: FanCherries(m);break;
                    case Prop.AirHorn: FanHorn(m);break;
                    case Prop.Soap: Soap(m);break;
                    case Prop.Pill: Pill(m,false);break;
                    case Prop.BittenPill: Pill(m,true);break;
                    case Prop.Injector: Injector(m);break;
                    case Prop.Flash:
                        for(int i=0;i<11;i++)
                        {
                            float height=-.8f+1.65f*i/10f;
                            var direction=new Vector3(Mathf.Cos(i*2.4f),height,Mathf.Sin(i*2.4f)).normalized;
                            m.Push(direction*(.47f+i%3*.10f),Quaternion.FromToRotation(Vector3.up,direction));
                            m.Plum(i%3==0?Ivory:Gold,Vector3.zero,new Vector3(.12f,.61f+i%3*.13f,.13f),6);
                            m.Pop();
                        }
                        break;
                    case Prop.MintHead: case Prop.PeachHead:
                        m.Plum(prop==Prop.MintHead?Sea:Rose,Vector3.zero,new Vector3(.51f,.45f,.44f),14);
                        m.Drum(Gold,new Vector3(0,.14f,0),new Vector3(.47f,.07f,.43f));break;
                    case Prop.Mitt: m.Plum(Ivory,Vector3.zero,new Vector3(.26f,.19f,.2f));break;
                    case Prop.Shoe:
                        m.Plum(Red,Vector3.zero,new Vector3(.19f,.18f,.36f));
                        m.Box(Ivory,Vector3.down*.075f,new Vector3(.20f,.05f,.36f));break;
                }
                var first=m.Bake(prop.ToString(),parent,paint);
                meshes[prop]=first.GetComponent<MeshFilter>().sharedMesh;
                return first.transform;
            }
            var go=new GameObject(prop.ToString(),typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=paint;
            return go.transform;
        }
        internal static void Dispose()
        {
            foreach(var mesh in meshes.Values)if(mesh)Object.Destroy(mesh);
            meshes.Clear();if(paint)Object.Destroy(paint);paint=null;
        }
        static void Tube(CourtSketchMesh m,int c,Vector3 a,Vector3 b,float radius)
        {
            m.Push((a+b)*.5f,Quaternion.FromToRotation(Vector3.up,(b-a).normalized));
            m.Drum(c,Vector3.zero,new Vector3(radius*2,(b-a).magnitude,radius*2),20);m.Pop();
        }
        static void Barrel(CourtSketchMesh m,int c,float z1,float z2,float r,float y=.12f)
            =>Tube(m,c,new Vector3(0,y,z1),new Vector3(0,y,z2),r);
        static void Cannon(CourtSketchMesh m)
        {
            Barrel(m,Ink,-.40f,.62f,.26f);Barrel(m,Blue,-.36f,.54f,.28f);
            Barrel(m,Gold,-.25f,-.14f,.30f);Barrel(m,Gold,.42f,.64f,.32f);
            Barrel(m,Ink,.633f,.654f,.246f);Barrel(m,Red,.649f,.66f,.137f);
            m.Box(Brown,new Vector3(0,-.15f,-.09f),new Vector3(.20f,.35f,.52f));
            for(int i=0;i<5;i++)m.Box(Ivory,new Vector3(.266f,.135f,-.08f+i*.086f),new Vector3(.025f,.13f,.039f));
            Tube(m,Ink,new Vector3(-.32f,-.07f,-.2f),new Vector3(.32f,-.07f,-.2f),.135f);
        }
        static void Bat(CourtSketchMesh m)
        {
            m.Plum(Gold,new Vector3(0,.74f,0),new Vector3(.155f,.68f,.155f),16);
            Tube(m,Brown,new Vector3(0,.02f,0),new Vector3(0,.61f,0),.043f);
            Tube(m,Blue,new Vector3(0,-.09f,0),new Vector3(0,.21f,0),.049f);
            for(int i=0;i<4;i++)m.Drum(Sea,new Vector3(0,-.05f+i*.063f,0),new Vector3(.103f,.021f,.103f));
            m.Drum(Gold,new Vector3(0,-.1f,0),new Vector3(.14f,.055f,.14f));
            m.Drum(Red,new Vector3(0,.81f,0),new Vector3(.16f,.04f,.16f));
        }
        static void Launcher(CourtSketchMesh m)
        {
            Barrel(m,Blue,-.56f,.64f,.19f);Barrel(m,Ink,-.55f,-.41f,.22f);
            Barrel(m,Sea,-.26f,-.07f,.208f);Barrel(m,Gold,.47f,.67f,.235f);
            Barrel(m,Ink,.662f,.685f,.168f);Barrel(m,Rose,.68f,.71f,.095f);
            m.Box(Ink,new Vector3(0,-.12f,-.19f),new Vector3(.14f,.34f,.18f));
            m.Box(Brown,new Vector3(0,-.09f,-.43f),new Vector3(.28f,.14f,.30f));
            m.Box(Ink,new Vector3(0,.355f,.10f),new Vector3(.13f,.13f,.28f));
            m.Box(Sea,new Vector3(0,.357f,-.047f),new Vector3(.094f,.09f,.015f));
            for(int i=0;i<3;i++)m.Box(Ivory,new Vector3(.19f,.14f,.08f*i+.13f),new Vector3(.015f,.1f,.031f));
        }
        static void Rocket(CourtSketchMesh m)
        {
            Barrel(m,Ivory,-.25f,.16f,.122f,0);Barrel(m,Red,.12f,.24f,.122f,0);
            m.Plum(Rose,new Vector3(0,0,.235f),new Vector3(.228f,.228f,.28f),16);
            Barrel(m,Ink,-.27f,-.22f,.096f,0);
            for(int i=0;i<4;i++)
            {
                m.Push(Vector3.zero,Quaternion.Euler(0,0,i*90));
                m.Face(Sea,new Vector3(0,.095f,-.05f),new Vector3(0,.224f,-.26f),new Vector3(0,.096f,-.21f));m.Pop();
            }
        }
        static void Timer(CourtSketchMesh m)
        {
            for(int i=-1;i<=1;i++)
            {
                var p=new Vector3(i*.17f,i==0?.055f:0,0);
                Tube(m,i==0?Rose:Red,p+Vector3.back*.28f,p+Vector3.forward*.28f,.11f);
                Tube(m,Gold,p+Vector3.forward*.265f,p+Vector3.forward*.30f,.088f);
            }
            for(int i=-1;i<=1;i+=2)
                m.Box(Ink,new Vector3(0,0,i*.16f),new Vector3(.55f,.23f,.065f));
            m.Box(Gold,new Vector3(0,.07f,-.28f),new Vector3(.34f,.23f,.07f));
            m.Box(Ink,new Vector3(0,.07f,-.32f),new Vector3(.29f,.164f,.023f));
        }
        static void Vest(CourtSketchMesh m)
        {
            m.Box(Gold,new Vector3(0,.02f,0),new Vector3(.76f,.54f,.55f),.45f);
            for(int s=-1;s<=1;s+=2)
            {
                m.Box(Red,new Vector3(s*.235f,.27f,0),new Vector3(.13f,.22f,.46f));
                m.Box(Ivory,new Vector3(s*.235f,.30f,-.233f),new Vector3(.13f,.065f,.04f));
            }
            m.Box(Ink,new Vector3(0,-.13f,0),new Vector3(.78f,.085f,.57f));
            // Oversized wind-up duck faces forward; its matching tail and key read from behind.
            m.Plum(Gold,new Vector3(0,.27f,.32f),new Vector3(.37f,.37f,.34f),14);
            m.Plum(Rose,new Vector3(0,.21f,.47f),new Vector3(.28f,.09f,.16f));
            for(int s=-1;s<=1;s+=2)m.Plum(Ink,new Vector3(s*.109f,.30f,.434f),Vector3.one*.045f);
            m.Box(Red,new Vector3(0,.05f,-.31f),new Vector3(.42f,.24f,.10f));
            for(int s=-1;s<=1;s+=2)
                m.Plum(Ivory,new Vector3(s*.15f,.10f,-.39f),new Vector3(.23f,.14f,.08f));
        }
        static void Coil(CourtSketchMesh m)
        {
            const float outer=.39f,inner=.155f,half=.26f;
            const int n=36;
            Vector3 P(float x,float r,float a)=>new Vector3(x,r*Mathf.Sin(a),r*Mathf.Cos(a));
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
                m.Face(Metal,P(-half,outer,a),P(half,outer,a),P(half,outer,b),P(-half,outer,b));
                m.Face(Ink,P(-half,inner,a),P(-half,inner,b),P(half,inner,b),P(half,inner,a));
                for(int s=-1;s<=1;s+=2)
                {
                    m.Face(Blue,P(s*half,inner,a),P(s*half,outer,a),P(s*half,outer,b),P(s*half,inner,b));
                    for(int j=0;j<3;j++)
                    {
                        float r=inner+.055f+j*.073f+(float)i/n*.05f;
                        m.Face(Metal,P(s*(half+.003f),r,a),P(s*(half+.003f),r+.011f,a),P(s*(half+.003f),r+.012f,b),P(s*(half+.003f),r+.001f,b));
                    }
                    m.Face(Gold,P(s*.14f,outer+.003f,a),P(s*.20f,outer+.003f,a),P(s*.20f,outer+.003f,b),P(s*.14f,outer+.003f,b));
                }
            }
        }
        static void Cradle(CourtSketchMesh m)
        {
            m.Box(Ink,new Vector3(0,.105f,0),new Vector3(1.45f,.21f,.94f));
            for(int s=-1;s<=1;s+=2)
                m.Box(Gold,new Vector3(0,.25f,s*.28f),new Vector3(1.35f,.20f,.18f));
        }
    }
}
