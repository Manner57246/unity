using UnityEngine;

namespace SuperDodgeball
{
    internal static class NeighbourhoodScenery
    {
        internal const int Ink=0x344459, Cream=0xffefc9, Blue=0x297fb0, Red=0xd5504c;
        const int Timber=0xa16d54, Rail=0xc1916d, Paper=0xfff0cf;
        static Vector3 V(float x,float y,float z)=>new(x,y,z);
        static Vector2 P(float x,float z)=>new(x,z);

        internal static void Build(CourtSketchMesh m,bool pier)
        {
            int outer=pier?0x77b7ad:0x79a677;
            int edge=pier?0xe6bd80:0x99ba83;
            int field=pier?0xd9ac75:0x78a979;
            m.Box(outer,V(0,-.35f,0),V(100,.28f,100));
            m.Ground(edge,-.11f,P(-16,-11),P(12,-12),P(15,-7),P(15,6),P(11,10),P(-13,11),P(-16,5));
            m.Ground(field,0,P(-11.8f,-5.8f),P(-10.65f,-7.15f),P(9,-7.15f),P(11.7f,-5.7f),
                P(11.7f,5.65f),P(9.9f,7.2f),P(-10.7f,7.2f),P(-11.8f,5.8f));
            if(!pier)
            {
                // One canopy shadow crosses the back line; a second clips the near corner.
                m.Ground(0x649878,.007f,P(-11.65f,1.8f),P(-10.72f,1.69f),P(-9.80f,2.15f),
                    P(-9.12f,3.03f),P(-8.17f,3.12f),P(-7.36f,3.78f),P(-6.90f,4.83f),
                    P(-6.05f,5.32f),P(-5.46f,6.28f),P(-5.36f,7.18f),P(-10.65f,7.18f),P(-11.65f,5.8f));
                m.Ground(0x6ba078,.008f,P(7.4f,-7.13f),P(8.2f,-6.14f),P(9.25f,-5.93f),
                    P(9.65f,-4.94f),P(10.68f,-4.29f),P(11.68f,-4.14f),P(11.68f,-5.67f),P(9,-7.13f));
            }
            // Wear accumulates along the margins; the central throwing lane stays quiet.
            var rng=new System.Random(pier?37:51);
            for(int i=0;i<76;i++)
            {
                float x=(float)rng.NextDouble()*21-10.5f;
                float z=(i%2==0?1:-1)*(5.4f+(float)rng.NextDouble()*1.55f);
                float w=.06f+(float)rng.NextDouble()*.28f;
                m.Ground(pier?0xcf9f6d:0x73a581,.012f,P(x-w,z),P(x,z+.07f),P(x+w,z+.08f),P(x+.05f,z-.02f));
            }
            Vector2[] bounds={P(-11,-5.55f),P(-9.98f,-6.55f),P(9,-6.55f),P(11,-5.45f),P(11,5.35f),P(9.65f,6.6f),P(-10,6.6f),P(-11,5.5f)};
            for(int i=0;i<bounds.Length;i++) Chalk(m,bounds[i],bounds[(i+1)%bounds.Length],.105f);
            for(int i=0;i<11;i++) Chalk(m,P(0,-6.5f+i*1.25f),P(0,-5.84f+i*1.25f),.075f);
            m.Ring(0xf1e4bc,V(0,.024f,0),1.35f,1.35f,.075f);
            if(pier) Harbour(m); else WashYard(m);
            Chair(m);
        }

        static void Chalk(CourtSketchMesh m,Vector2 a,Vector2 b,float w)
        {
            Vector2 d=(b-a).normalized, n=new(-d.y*w*.5f,d.x*w*.5f);
            m.Ground(0xf4e7c9,.025f,a-n,a+n,b+n,b-n);
        }
        static void Shadow(CourtSketchMesh m,float x,float z,float w,float d,int c)
            =>m.Drum(c,V(x,.035f,z),V(w,.015f,d),13);
        static void Fence(CourtSketchMesh m,float x,float z,float length,float yaw)
        {
            m.Push(V(x,0,z),yaw);
            int n=Mathf.CeilToInt(length/2.3f);
            for(int i=0;i<=n;i++)
                m.Box(Timber,V(i*length/n,.49f,0),V(.21f,1.0f+(i%3)*.05f,.26f));
            m.Box(Rail,V(length/2,.72f,0),V(length,.15f,.13f));
            m.Box(Rail,V(length/2,.32f,0),V(length,.12f,.13f)); m.Pop();
        }
        static void Tree(CourtSketchMesh m,float x,float z,float s,int leaf)
        {
            Shadow(m,x+.52f*s,z+.35f*s,3.25f*s,1.68f*s,0x629674);
            m.Push(V(x,0,z));
            m.Stick(0x865e55,V(-.12f*s,0,0),V(.1f*s,2.85f*s,0),.49f*s);
            m.Stick(0x906354,V(0,1.3f*s,0),V(-1.15f*s,2.6f*s,.1f),.26f*s);
            m.Plum(leaf,V(-.58f*s,2.95f*s,.09f),V(3.0f*s,2.15f*s,2.78f*s),9,1.1f);
            m.Plum(leaf,V(.86f*s,3.35f*s,.1f*s),V(2.72f*s,2.49f*s,2.83f*s),9,2.3f);
            m.Plum(leaf,V(-.14f*s,3.92f*s,.15f*s),V(2.9f*s,2.12f*s,2.52f*s),10,3.1f); m.Pop();
        }
        static void WashYard(CourtSketchMesh m)
        {
            Fence(m,-12.65f,-5.6f,12.8f,-90); Fence(m,12.55f,-6.3f,12.6f,-90);
            Fence(m,-10.9f,7.65f,5.2f,0); Fence(m,2.2f,7.65f,9.5f,0);
            Fence(m,-11.1f,-7.5f,5.0f,0); Fence(m,3.2f,-7.5f,7.5f,0);
            Tree(m,-13.7f,5.3f,1.22f,0x82b778); Tree(m,12.9f,7.7f,1.22f,0x8fbb7d);
            Tree(m,-15.7f,-5.8f,1.28f,0x82b87c); Tree(m,14,-8.0f,1.15f,0x91bd84);
            m.Push(V(-8.3f,0,8.85f),-6);
            m.Box(0xd59b72,V(0,1.05f,0),V(4.4f,2.1f,2.25f));
            m.Box(0x477c75,V(-.72f,.92f,-1.14f),V(1.18f,1.81f,.11f));
            m.Box(0x759e91,V(.82f,1.22f,-1.16f),V(1.35f,.91f,.1f));
            m.Push(V(0,2.32f,0),Quaternion.Euler(-9,0,0));
            m.Box(0x976456,Vector3.zero,V(4.98f,.29f,2.85f));
            for(int i=0;i<7;i++) m.Box(0xad7864,V(-2.12f+i*.71f,.14f,0),V(.07f,.065f,2.70f));m.Pop();m.Pop();
            // A missing sock and a referee-striped shirt tell a story in silhouette.
            m.Push(V(-12.2f,0,-3.2f),-91); Clothesline(m,6.6f); m.Pop();
            m.Push(V(5.25f,0,8.08f),-3); Clothesline(m,5.15f); m.Pop();
            Tub(m,12.7f,2.2f); Tub(m,-12.9f,-3.55f);
            for(int i=0;i<32;i++)
            {
                float x=(i%2==0?-1:1)*(11.85f+(i%5)*.35f), z=-7+(i*19%143)*.1f;
                m.Face(0x568a65,V(x,.01f,z),V(x+.13f,.32f,z+.05f),V(x+.19f,.01f,z+.02f));
                if(i%4==0) m.Plum(0xf0dfaa,V(x+.19f,.34f,z),V(.25f,.14f,.24f),6);
            }
        }
        static void Clothesline(CourtSketchMesh m,float length)
        {
            for(int i=0;i<2;i++)
                m.Stick(0x967858,V(i*length,0,0),V(i*length,2.25f,0),.15f);
            Vector3 split=V(length*.52f,1.88f,0);
            m.Stick(Ink,V(0,2.19f,0),split,.044f);
            m.Stick(Ink,split,V(length,2.19f,0),.044f);
            for(int i=0;i<4;i++)
            {
                float x=length*(.16f+.205f*i), y=1.94f+Mathf.Abs(x/length-.5f)*.58f;
                int c=i==1?0xd9bb7e:i==3?0x80aaa4:0xe8e1c6;
                m.Push(V(x,y,0),Quaternion.Euler(6+i*3,0,(i%2==0?1:-1)*6));
                if(i==3)
                {
                    m.Box(c,V(0,-.29f,0),V(.27f,.57f,.08f)); m.Box(c,V(-.09f,-.53f,-.02f),V(.43f,.19f,.12f));
                }
                else
                {
                    m.Box(c,V(0,-.38f,0),V(.65f,.86f,.095f));
                    m.Box(c,V(0,-.13f,0),V(.97f,.29f,.105f));
                    if(i==0)for(int j=0;j<3;j++)m.Box(0x6f827e,V(-.23f+j*.23f,-.4f,-.056f),V(.085f,.7f,.018f));
                }
                m.Box(0xb18b57,V(.17f,.04f,0),V(.06f,.14f,.09f));m.Pop();
            }
        }
        static void Tub(CourtSketchMesh m,float x,float z)
        {
            m.Push(V(x,0,z),-18);
            m.Drum(0x749a9c,V(0,.38f,0),V(1.4f,.7f,1.1f)); m.Drum(0xa6bdb0,V(0,.72f,0),V(1.46f,.12f,1.16f));
            m.Plum(0xe9e2c7,V(-.12f,.79f,.05f),V(.98f,.32f,.67f));m.Pop();
        }
        static void Harbour(CourtSketchMesh m)
        {
            for(int i=0;i<34;i++)
            {
                float x=(i%2==0?-1:1)*(15.3f+i%5*.68f),z=-12f+i*17%26;
                m.Ground(0xb6d9c8,-.18f,P(x,z),P(x+.3f,z+.12f),P(x+1.05f,z+.08f),P(x+.65f,z));
            }
            m.Push(V(-13.2f,0,.5f),-4);
            for(int i=0;i<21;i++)m.Box(i%4==0?0xbb8f72:0xca9d7e,V(0,.12f,-6.8f+i*.67f),V(2.62f,.27f,.62f));
            for(int i=0;i<4;i++)
            {
                float z=-6.5f+i*4.25f;
                m.Drum(0x907459,V(-1.19f,.6f,z),V(.33f,1.26f,.34f),8);
                if(i<3)m.Stick(0x988167,V(-1.19f,.99f,z),V(-1.19f,.99f,z+4.25f),.065f);
            }
            m.Pop();
            Boat(m,13.5f,2.8f,12); Boat(m,-9.2f,8.8f,76);
            for(int i=0;i<4;i++)
            {
                float x=i<2?10.8f+i*.9f:-12.5f+(i-2)*.8f, z=i<2?6.9f:-5.7f;
                m.Push(V(x,0,z),i*17);m.Box(0x9e7c62,V(0,.38f,0),V(1.05f,.76f,.91f));
                for(int j=0;j<3;j++)m.Box(0xc29a72,V(-.34f+j*.32f,.77f,0),V(.24f,.06f,.83f));m.Pop();
            }
            m.Push(V(6.9f,0,8.6f),-13);
            m.Stick(0x927762,V(0,0,0),V(0,2.25f,0),.15f);
            m.Plum(0xc88b7d,V(0,2.37f,0),V(3.1f,.83f,2.9f),9);
            m.Box(0xeece9e,V(.2f,.49f,-.24f),V(1.25f,.14f,.59f));
            m.Stick(Ink,V(-.3f,0,-.48f),V(.53f,.48f,-.15f),.09f);m.Pop();
            for(int i=0;i<13;i++)
            {
                float x=i%3==0?-12.2f+i*.18f:9.7f+i*.4f,z=i%3==0?6.6f:-7.6f+.28f*(i%4);
                m.Plum(i%2==0?0x9a9f88:0xa8a88b,V(x,.2f,z),V(.8f+i%3*.29f,.59f,.76f),7,i+.1f);
            }
        }
        static void Boat(CourtSketchMesh m,float x,float z,float yaw)
        {
            m.Push(V(x,0,z),yaw);
            m.Plum(0x587f82,V(0,.43f,0),V(1.95f,1.15f,4.18f),10);
            m.Plum(0xe9d4a9,V(0,.82f,0),V(1.95f,.54f,4.13f),10);
            m.Plum(0x749c9a,V(0,1.02f,0),V(1.62f,.15f,3.44f),10);
            for(int i=-1;i<=1;i++)m.Box(0xc69972,V(0,1.12f,i*.88f),V(1.43f,.14f,.25f));
            m.Stick(0x96745e,V(-.6f,1.25f,-1.5f),V(.58f,1.3f,1.64f),.094f);
            m.Pop();
        }
        static void Chair(CourtSketchMesh m)
        {
            m.Push(V(.2f,0,7.48f),-8);
            for(int i=-1;i<=1;i+=2)
            {
                m.Stick(0x697d79,V(i*.7f,0,-.48f),V(i*.52f,1.65f,.24f),.13f);
                m.Stick(0x697d79,V(i*.65f,0,.67f),V(i*.55f,1.65f,-.35f),.13f);
            }
            m.Box(0xc5966d,V(0,1.48f,0),V(1.57f,.23f,1.29f));
            m.Box(0xbc8a65,V(0,1.95f,.54f),V(1.4f,1.02f,.15f));m.Pop();
        }
        internal static void Newspaper(CourtSketchMesh m)
        {
            for(int i=-1;i<=1;i+=2)
            {
                m.Push(V(i*.51f,0,0),Quaternion.Euler(-21,i*13,i*-5));
                m.Box(Paper,Vector3.zero,V(1.09f,1.03f,.065f));
                m.Box(Ink,V(0,.33f,-.046f),V(.84f,.1f,.018f));
                if(i==-1)m.Box(0x9aaaa1,V(-.16f,.02f,-.047f),V(.44f,.37f,.018f));
                for(int j=0;j<4;j++)m.Box(0xb5b5a3,V(i==-1?.1f:0,-.32f+.1f*j,-.049f),V(i==-1?.48f:.77f,.03f,.017f));
                m.Pop();
            }
        }
    }
}
