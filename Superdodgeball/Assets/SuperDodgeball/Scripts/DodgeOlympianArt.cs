using UnityEngine;

namespace SuperDodgeball
{
    internal static partial class DodgeWeaponArt
    {
        const int Linen = 0xfff0cf, Fold = 0xc6d4db, Indigo = 0x405a84;
        const int Leaf = 0xffcb53, Gleam = 0xfff3a8, Storm = 0x616585;

        // Six corners make the silhouette legible edge to edge; the raised facets survive a turn in the hand.
        static void Thunderbolt(CourtSketchMesh m)
        {
            Vector2[] shape={new(.38f,.85f),new(-.33f,-.05f),new(-.04f,-.05f),
                new(-.34f,-.87f),new(.39f,.18f),new(.09f,.18f)};
            var front=new Vector3[6];var back=new Vector3[6];
            for(int i=0;i<shape.Length;i++)
            {
                float depth=i==0 || i==3?.016f:i==2 || i==5?.096f:.054f;
                front[i]=(Vector3)shape[i]+Vector3.forward*depth;
                back[i]=(Vector3)shape[i]-Vector3.forward*depth;
            }
            for(int i=0;i<4;i++)
            {
                int a=i,b=i+1;int colour=i%2==0?Gleam:Leaf;
                m.Face(colour,front[5],front[a],front[b]);m.Face(colour,back[b],back[a],back[5]);
            }
            for(int i=0;i<6;i++)
            {
                int next=(i+1)%6;
                m.Face(Gold,front[i],back[i],back[next],front[next]);
            }
        }

        static void Thundercloud(CourtSketchMesh m)
        {
            m.Plum(Storm,new Vector3(0,-.06f,0),new Vector3(1.26f,.52f,.77f),12);
            for (int i=0;i<7;i++)
            {
                float a = i*2.39996f;
                m.Plum(i%2==0?0xc4cfed:0x9eafd0,new Vector3(.43f*Mathf.Cos(a),.03f+i%3*.052f,.23f*Mathf.Sin(a)),
                    new Vector3(.55f,.43f+i%3*.043f,.47f),10);
            }
            m.Push(new Vector3(0,.1f,.36f),Quaternion.Euler(0,0,-11));
            Thunderbolt(m); m.Pop();
        }

        static void Laurel(CourtSketchMesh m)
        {
            // Hair at the back remains visible in the normal over-the-shoulder camera.
            for (int i=0;i<12;i++)
            {
                float a=(52f+i*23)*Mathf.Deg2Rad;
                m.Plum(i%3==0?Fold:Linen,new Vector3(.32f*Mathf.Sin(a),.08f-.14f*Mathf.Max(0,-Mathf.Cos(a)),.31f*Mathf.Cos(a)),
                    new Vector3(.215f,.28f,.21f),9);
            }
            m.Plum(Linen,new Vector3(0,.20f,-.04f),new Vector3(.73f,.35f,.65f),13);
            for (int side=-1;side<=1;side+=2)
            {
                for (int i=0;i<7;i++)
                {
                    float a=side*(26f+i*22f)*Mathf.Deg2Rad;
                    var p=new Vector3(.39f*Mathf.Sin(a),.19f+i*.006f,.335f*Mathf.Cos(a));
                    m.Push(p,Quaternion.Euler(0,a*Mathf.Rad2Deg,side*(40f+i*2)));
                    m.Plum(i%2==0?Leaf:Gold,Vector3.zero,new Vector3(.087f,.21f,.074f),7);m.Pop();
                }
                m.Push(new Vector3(side*.15f,.103f,.334f),Quaternion.Euler(0,0,side*14));
                m.Plum(Linen,Vector3.zero,new Vector3(.216f,.076f,.096f));m.Pop();
                m.Plum(Linen,new Vector3(side*.21f,-.14f,.29f),new Vector3(.22f,.24f,.18f),9);
            }
            for (int i=-2;i<=2;i++)
                m.Plum(i%2==0?Linen:0xe3e3d4,new Vector3(i*.092f,-.22f+.030f*Mathf.Abs(i),.316f-.020f*Mathf.Abs(i)),
                    new Vector3(.20f,.41f-.075f*Mathf.Abs(i),.20f),9);
        }

        // Wrap the back as well as the chest. The hem steps down from the pinned shoulder.
        static void Toga(CourtSketchMesh m)
        {
            const int n=28;
            Vector3 Cloth(int i,float level,float extra=0)
            {
                float a=i*2*Mathf.PI/n,x=Mathf.Cos(a),z=Mathf.Sin(a);
                float ripple=.024f*Mathf.Cos(7*a);
                float rx=Mathf.Lerp(.424f,.385f,level)+ripple+extra;
                float rz=Mathf.Lerp(.38f,.345f,level)+ripple+extra;
                return new Vector3(rx*x,Mathf.Lerp(-.44f+ripple*1.2f,.39f-.18f*x,level),rz*z);
            }
            for(int i=0;i<n;i++)
            {
                void Band(float low,float high,int colour,float extra)
                {
                    var a=Cloth(i,low,extra);var b=Cloth(i,high,extra);
                    var c=Cloth(i+1,high,extra);var d=Cloth(i+1,low,extra);
                    m.Face(colour,a,b,c,d);m.Face(colour,d,c,b,a);
                }
                Band(0,1,i%4==0?0xd6d9cd:Linen,0);
                Band(.045f,.106f,Gold,.005f);
                Band(.59f,.81f,Indigo,.010f);
                Band(.78f,.81f,Leaf,.015f);
            }
            m.Plum(Leaf,new Vector3(-.28f,.40f,.23f),Vector3.one*.19f,10);
        }
    }
}
