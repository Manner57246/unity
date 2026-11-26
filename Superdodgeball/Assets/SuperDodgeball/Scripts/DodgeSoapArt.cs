using UnityEngine;

namespace SuperDodgeball
{
    internal static partial class DodgeWeaponArt
    {
        // Profiles for a moulded oval bar: a rounded underside, narrow edge and softly bevelled face.
        static void Soap(CourtSketchMesh m)
        {
            const int count = 32;
            var rim = new Vector2[count];
            for (int i=0;i<count;i++)
            {
                float a = i*Mathf.PI*2/count;
                float c=Mathf.Cos(a), s=Mathf.Sin(a);
                rim[i] = new Vector2(.57f*Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.65f),
                                     .33f*Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.65f));
            }
            var profile = new Vector2[] {new(.75f,-.145f),new(.91f,-.116f),new(.99f,-.063f),
                new(1f,.016f),new(.96f,.086f),new(.84f,.133f),new(.70f,.15f)};
            int[] dyes = {0x5bbcaf,0x6ac8b8,0x7ed7c5,0x95e2ce,0xa9ead6,0xb8efdc};
            Vector3 Point(int j,int i) => new(rim[i].x*profile[j].x,profile[j].y,rim[i].y*profile[j].x);
            for(int i=0;i<count;i++)
            {
                int next=(i+1)%count, top=profile.Length-1;
                for(int j=0;j<top;j++) m.Face(dyes[j],Point(j,i),Point(j+1,i),Point(j+1,next),Point(j,next));
                m.Face(0xc1f2df,Vector3.up*.15f,Point(top,next),Point(top,i));
                m.Face(dyes[0],Vector3.down*.145f,Point(0,i),Point(0,next));
            }
            // A shallow stamped word on the broad face distinguishes the bar from a mint or a ball.
            Stamp(m,-.298f,new(1,1),new(.22f,1),new(0,.80f),new(.16f,.61f),new(.85f,.43f),new(1,.22f),new(.78f,0),new(0,0));
            Stamp(m,-.147f,new(.22f,1),new(.78f,1),new(1,.77f),new(1,.23f),new(.78f,0),new(.22f,0),new(0,.23f),new(0,.77f),new(.22f,1));
            Stamp(m,.007f,new(0,0),new(.48f,1),new(1,0));
            Stamp(m,.007f,new(.22f,.39f),new(.78f,.39f));
            Stamp(m,.162f,new(0,0),new(0,1),new(.76f,1),new(1,.78f),new(.94f,.59f),new(.73f,.48f),new(0,.48f));
            Sud(m,new Vector3(.405f,.146f,.113f),.19f);
            Sud(m,new Vector3(.324f,.154f,.181f),.13f);
            Sud(m,new Vector3(.442f,.256f,.155f),.092f);
        }

        static void Stamp(CourtSketchMesh m,float x,params Vector2[] path)
        {
            for(int i=1;i<path.Length;i++)
            {
                Vector2 a = new(x+path[i-1].x*.115f,(path[i-1].y-.5f)*.188f);
                Vector2 b = new(x+path[i].x*.115f,(path[i].y-.5f)*.188f);
                Vector2 along = (b-a).normalized*.0105f;
                Vector2 side = new(-along.y,along.x);
                m.Ground(0x62ada3,.151f,a-along+side,a-along-side,b+along-side,b+along+side);
            }
        }

        static void Sud(CourtSketchMesh m,Vector3 p,float diameter)
        {
            m.Plum(0xcceee7,p,Vector3.one*diameter,12);
            m.Plum(0xffffef,p+new Vector3(-.18f,.30f,-.30f)*diameter,Vector3.one*diameter*.29f,8);
        }
    }
}
