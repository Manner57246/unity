using UnityEngine;
using static SuperDodgeball.NeighbourhoodScenery;

namespace SuperDodgeball
{
    internal sealed class NeighbourhoodPlayer
    {
        public readonly Transform Root;
        readonly Transform body, head, leftArm, rightArm;
        public Vector3 Hand => rightArm.TransformPoint(V(.22f,-.72f,-.28f));
        readonly int style;
        readonly Transform[] boots=new Transform[2], thighs=new Transform[2];
        readonly bool seated;
        static Vector3 V(float x,float y,float z)=>new(x,y,z);
        static Transform Joint(Transform parent,string name,Vector3 point)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=point;return t;
        }

        public NeighbourhoodPlayer(Transform parent,Material material,int id)
        {
            seated=id==4; style=id;
            int kit=id<2?Blue:id<4?Red:Cream;
            int dark=id<2?0x2e5679:id<4?0x853f4a:Ink;
            int skin=new[]{0xb97456,0xf4be8e,0xe8a77d,0x985e4e,0xdeaa83}[id];
            int hair=new[]{0x4f4046,0xab674b,0x665045,0x403d48,0x96978e}[id];
            Root=Joint(parent,"Player "+id,Vector3.zero);
            if(!seated)
            {
                var foot=new CourtSketchMesh();
                foot.Drum(0x536e66,V(.15f,.035f,.06f),V(1.33f,.018f,.78f),14);
                foot.Ring(id<2?0x9de0eb:0xf3a793,V(0,.058f,0),.78f,.58f,.095f,28);
                foot.Bake("Footprint",Root,material);
            }
            body=Joint(Root,"Hips",V(0,.57f,0));
            var mesh=new CourtSketchMesh();
            mesh.Push(V(0,-.57f,0));
            // Broad shoulders, visible nose, separated legs; the hats identify players at game distance.
            mesh.Box(dark,V(0,.57f,0),V(.91f,.42f,.65f),.32f);
            mesh.Box(kit,V(0,1.0f,.01f),V(1.09f,.75f,.74f),.41f);
            mesh.Box(Cream,V(0,.77f,.01f),V(1.08f,.105f,.74f),.38f);
            if(seated)
            {
                for(int i=-1;i<=1;i++)mesh.Box(Ink,V(i*.29f,1.05f,-.37f),V(.13f,.5f,.07f));
            }
            else
            {
                mesh.Box(Cream,V(-.22f,1.12f,-.364f),V(.17f,.19f,.05f));
                mesh.Box(Cream,V(.22f,1.09f,.371f),V(.14f,.28f,.045f));
            }
            mesh.Pop();
            mesh.Bake("Jersey",body,material);
            head=Joint(body,"Head turn",V(0,.99f,-.065f));
            mesh=new CourtSketchMesh();mesh.Push(V(0,-1.56f,0));
            mesh.Plum(skin,V(0,1.61f,-.045f),V(1.06f,.86f,.88f),11);
            mesh.Plum(hair,V(0,1.81f,.07f),V(1.07f,.6f,.86f),11);
            mesh.Plum(skin,V(0,1.60f,-.47f),V(.29f,.22f,.31f),8);
            for(int side=-1;side<=1;side+=2)
            {
                mesh.Plum(skin,V(side*.51f,1.58f,0),V(.23f,.28f,.22f),8);
                mesh.Box(Ink,V(side*.22f,1.70f,-.44f),V(.105f,.12f,.075f));
            }
            if(id==0 || seated)
            {
                mesh.Plum(seated?Ink:0x3b7591,V(0,1.99f,.015f),V(1.12f,.44f,.95f),10);
                mesh.Box(seated?Ink:Cream,V(.07f,1.91f,-.46f),V(.88f,.11f,.58f),.48f);
            }
            if(id==1)
            {
                mesh.Plum(hair,V(0,2.09f,.23f),V(.65f,.63f,.60f),9);
                mesh.Drum(0xecd798,V(0,1.92f,0),V(1.05f,.14f,.94f),11);
            }
            if(id==2)
            {
                mesh.Drum(Cream,V(0,1.94f,0),V(1.14f,.18f,.94f),11);
                for(int i=0;i<2;i++)mesh.Stick(Red,V(.43f,1.95f,.05f),V(.85f,1.75f+i*.19f,.20f+i*.12f),.15f);
                mesh.Plum(0x725049,V(.01f,1.47f,-.45f),V(.52f,.15f,.16f),8);
            }
            if(id==3)
            {
                for(int i=0;i<5;i++)
                {
                    float a=i*Mathf.PI*2/5;
                    mesh.Plum(hair,V(Mathf.Cos(a)*.34f,1.98f,Mathf.Sin(a)*.27f),V(.57f,.48f,.54f),8);
                }
                mesh.Box(0xeab875,V(0,1.86f,-.42f),V(.85f,.10f,.12f));
            }
            mesh.Pop();mesh.Bake("Face and hair",head,material);
            Transform Arm(int side)
            {
                var t=Joint(body,side<0?"Left sleeve":"Right sleeve",V(side*.48f,.68f,0));
                var arm=new CourtSketchMesh();
                arm.Stick(kit,Vector3.zero,V(side*.16f,-.24f,-.03f),.35f);
                arm.Stick(skin,V(side*.16f,-.22f,-.03f),V(side*.19f,-.51f,-.19f),.236f);
                arm.Plum(skin,V(side*.19f,-.54f,-.22f),V(.33f,.33f,.36f));
                arm.Bake("Hand",t,material);return t;
            }
            leftArm=Arm(-1);rightArm=Arm(1);
            for(int i=0;i<2;i++)
            {
                var leg=Joint(Root,"Shoe",V(i==0?-.31f:.31f,.56f,.02f));
                var shoe=new CourtSketchMesh();
                shoe.Box(skin,V(0,-.18f,0),V(.30f,.37f,.32f));
                shoe.Box(Cream,V(0,-.30f,0),V(.32f,.17f,.34f));
                shoe.Box(dark,V(0,-.45f,-.083f),V(.42f,.24f,.61f),.39f);
                shoe.Box(0xe7d8b7,V(0,-.52f,-.11f),V(.43f,.08f,.62f),.39f);
                shoe.Bake("Foot",leg,material); boots[i]=leg;
                var limb=new CourtSketchMesh();
                limb.Plum(dark,V(0,.5f,0),V(.31f,1.05f,.33f),8);
                thighs[i]=limb.Bake("Shorts leg",Root,material).transform;
            }
        }
        // Hand-keyed phases. Feet stay grounded through the coil; the head follows the hips late.
        public void Pose(float phase,float speed,float coil,float snap,float gather,float catchHit)
        {
            float step=Mathf.Sin(phase)*speed;
            if(seated)
            {
                body.localPosition=V(0,.57f,0);head.localRotation=Quaternion.Euler(9,6*Mathf.Sin(phase*.2f),-5);
                leftArm.localRotation=Quaternion.Euler(55,0,-20);rightArm.localRotation=Quaternion.Euler(61,0,23);
                foreach(var boot in boots)boot.localRotation=Quaternion.Euler(48,0,0);
                for(int i=0;i<2;i++)LinkLeg(i);
                return;
            }
            float stance=(style%2==0?1:-1), breath=Mathf.Sin(phase*.43f);
            float crouch=.055f+.18f*coil+.12f*gather+.20f*catchHit;
            body.localPosition=V(stance*.075f-.20f*coil+.15f*snap,.58f-crouch+Mathf.Abs(step)*.14f,
                -.10f+.27f*coil-.49f*snap+.26f*catchHit);
            float build=style==1?.91f:style==3?1.14f:1;
            body.localScale=V(build+.09f*coil+.14f*catchHit,(1-.13f*coil+.16f*snap-.22f*catchHit)/Mathf.Sqrt(build),1+.10f*coil);
            body.localRotation=Quaternion.Euler(-10-breath*2+coil*26-snap*38+catchHit*29,-11*stance-56*coil+47*snap,
                stance*(7+breath*2)-step*7+19*coil-22*snap);
            head.localRotation=Quaternion.Euler(4+coil*9+snap*15,15*stance+30*coil-23*snap,stance*-7-8*coil+snap*13);
            leftArm.localRotation=Quaternion.Euler(24+style*7+step*23+coil*62-snap*42+gather*63,coil*-19,-26-22*coil-16*gather);
            rightArm.localRotation=Quaternion.Euler(29-style*4-step*26-coil*107+snap*95+gather*57,coil*12-snap*30,30+coil*47+snap*19+gather*17);
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                var foot=boots[i];
                foot.localPosition=V(side*(.30f+.11f*coil+.17f*snap),.56f+Mathf.Max(0,side*step)*.20f+
                    (i==1?.22f*snap:0),side*(.13f+.19f*coil+(i==0?.54f:.34f)*snap)+side*step*.31f);
                foot.localRotation=Quaternion.Euler(side*step*30+(i==1?-39:11)*snap,side*10,-side*(7+coil*9));
                LinkLeg(i);
            }
        }
        void LinkLeg(int i)
        {
            Vector3 hip=body.TransformPoint(V(i==0?-.24f:.24f,.04f,0));
            Vector3 knee=boots[i].TransformPoint(V(0,-.13f,0));
            thighs[i].position=knee;
            thighs[i].rotation=Quaternion.FromToRotation(Vector3.up,(hip-knee).normalized);
            thighs[i].localScale=V(1,(hip-knee).magnitude,1);
        }
    }
}
