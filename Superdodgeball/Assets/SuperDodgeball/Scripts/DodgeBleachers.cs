using System.Collections.Generic;
using UnityEngine;
namespace SuperDodgeball
{
    public static class DodgeBleachers
    {
        public const int Rows=4,Columns=12,Count=Rows*Columns*2;
        static readonly Color Navy=new Color(.2f,.29f,.37f),Cream=new Color(.91f,.9f,.82f),Mint=new Color(.28f,.62f,.58f),Peach=new Color(.83f,.38f,.27f);
        static Transform Part(Transform parent,PrimitiveType shape,string name,Vector3 p,Vector3 size,Color color,DodgeGame game,bool solid=false)
        {
            var part=DodgeCrowd.Part(parent,shape,name,p,size,color,game);
            if(solid){part.gameObject.AddComponent<BoxCollider>();part.gameObject.AddComponent<DodgeSurface>();}
            return part;
        }
        public static void HideOldSteps()
        {
            var arena=GameObject.Find("01 - Playground");if(!arena)return;
            foreach(var node in arena.GetComponentsInChildren<Transform>(true))if(node.name=="Bleacher step")node.gameObject.SetActive(false);
        }
        public static DodgeCrowd.Seat[] Build(DodgeCrowd crowd,DodgeGame game)
        {
            HideOldSteps();var list=new List<DodgeCrowd.Seat>();
            Color[] people={new Color(.94f,.6f,.66f),new Color(.66f,.62f,.86f),DodgeCrowd.SoapColor,new Color(.98f,.78f,.4f),new Color(.4f,.76f,.89f)};
            for(int side=-1;side<=1;side+=2)
            {
                Color colour=side<0?Mint:Peach;
                var bank=new GameObject(side<0?"Home grandstand":"Away grandstand").transform;bank.SetParent(crowd.transform,false);
                for(int row=0;row<Rows;row++)
                {
                    float x=16.05f+row*1.3f,height=.4f+row*.85f;
                    for(int block=-1;block<=1;block+=2)
                    {
                        Part(bank,PrimitiveType.Cube,"Tiered seating platform",new Vector3(side*x,height*.5f,block*7.2f),new Vector3(1.3f,height,11.8f),Cream,game,true);
                        Part(bank,PrimitiveType.Cube,"Tier colour stripe",new Vector3(side*(x-.66f),height-.12f,block*7.2f),new Vector3(.035f,.16f,11.7f),colour,game);
                    }
                    for(int column=0;column<Columns;column++)
                    {
                        float z=column<6?-10.35f+column*1.65f:2.1f+(column-6)*1.65f;
                        var chair=new GameObject("Stadium seat "+(side<0?"H":"A")+(char)('A'+row)+(column+1).ToString("00")).transform;
                        chair.SetParent(bank,false);chair.localPosition=new Vector3(side*x,height,z);chair.localRotation=Quaternion.Euler(0,-side*90,0);
                        Part(chair,PrimitiveType.Cube,"Individual seat cushion",new Vector3(0,.61f,0),new Vector3(.86f,.14f,.67f),colour,game,true);
                        var back=Part(chair,PrimitiveType.Cube,"Individual seat back",new Vector3(0,1.02f,-.36f),new Vector3(.9f,.88f,.14f),colour,game,true);back.localRotation=Quaternion.Euler(-8,0,0);
                        Part(chair,PrimitiveType.Cylinder,"Seat pedestal",new Vector3(0,.3f,0),new Vector3(.18f,.29f,.18f),Navy,game);
                        for(int s=-1;s<=1;s+=2)
                        {
                            Part(chair,PrimitiveType.Cube,"Armrest",new Vector3(s*.51f,.82f,.03f),new Vector3(.09f,.09f,.58f),Navy,game);
                            Part(chair,PrimitiveType.Cylinder,"Cup holder rim",new Vector3(s*.51f,.85f,.34f),new Vector3(.19f,.04f,.19f),Navy,game);
                            Part(chair,PrimitiveType.Cylinder,"Cup holder well",new Vector3(s*.51f,.895f,.34f),new Vector3(.13f,.005f,.13f),new Color(.1f,.15f,.2f),game);
                        }
                        // Spectator root stays fixed at the seat; its six physical bones can leave it.
                        var root=new GameObject("Cheering bean "+(list.Count+1)).transform;root.SetParent(crowd.transform,false);root.SetPositionAndRotation(chair.position,chair.rotation);
                        var body=new GameObject("Squishy torso").transform;body.SetParent(root,false);body.localPosition=Vector3.up*.98f;
                        Color c=people[(row*2+column+(side>0?1:0))%people.Length];
                        Part(body,PrimitiveType.Sphere,"Jelly shirt",Vector3.zero,new Vector3(.62f,.65f,.5f),c,game);
                        Part(body,PrimitiveType.Capsule,"Jelly neck",Vector3.up*.33f,new Vector3(.18f,.1f,.18f),c,game);
                        var head=new GameObject("Spectator head").transform;head.SetParent(body,false);head.localPosition=Vector3.up*.58f;
                        Part(head,PrimitiveType.Sphere,"Head",Vector3.zero,Vector3.one*.54f,c,game);
                        var arm=new GameObject("Throwing and waving arm").transform;arm.SetParent(body,false);arm.localPosition=new Vector3(.36f,.1f,0);arm.localRotation=Quaternion.Euler(-15,0,-25);
                        Part(arm,PrimitiveType.Capsule,"Arm",Vector3.up*.22f,new Vector3(.18f,.28f,.18f),c,game);
                        var hand=new GameObject("Throw origin").transform;hand.SetParent(arm,false);hand.localPosition=Vector3.up*.5f;
                        var other=new GameObject("Other arm pivot").transform;other.SetParent(body,false);other.localPosition=new Vector3(-.36f,.1f,0);other.localRotation=Quaternion.Euler(-50,0,-10);
                        Part(other,PrimitiveType.Capsule,"Other arm",Vector3.down*.22f,new Vector3(.18f,.28f,.18f),c,game);
                        var legs=new Transform[2];
                        for(int s=-1;s<=1;s+=2)
                        {
                            var leg=new GameObject(s<0?"Left seated leg":"Right seated leg").transform;leg.SetParent(body,false);leg.localPosition=new Vector3(s*.18f,-.35f,.08f);legs[s<0?0:1]=leg;
                            var thigh=Part(leg,PrimitiveType.Capsule,"Seated thigh",new Vector3(0,0,.18f),new Vector3(.21f,.23f,.21f),c,game);thigh.localRotation=Quaternion.Euler(90,0,0);
                            Part(leg,PrimitiveType.Capsule,"Seated shin",new Vector3(0,-.18f,.4f),new Vector3(.18f,.22f,.18f),c,game);
                            Part(leg,PrimitiveType.Sphere,"Seated shoe",new Vector3(0,-.4f,.49f),new Vector3(.24f,.22f,.4f),Cream,game);
                        }
                        if(list.Count%3==0)Part(head,PrimitiveType.Sphere,"Marshmallow hat",Vector3.up*.27f,new Vector3(.61f,.13f,.58f),people[(column+2)%people.Length],game);
                        root.gameObject.AddComponent<DodgeSpectator>();
                        list.Add(new DodgeCrowd.Seat{body=body,head=head,arm=arm,hand=hand,otherArm=other,leftLeg=legs[0],rightLeg=legs[1],phase=list.Count*1.79f});
                    }
                }
                Part(bank,PrimitiveType.Cube,"Upper aisle landing",new Vector3(side*20.25f,1.53f,0),new Vector3(.8f,3.06f,31.8f),Cream,game,true);
                for(int aisle=0;aisle<3;aisle++)
                {
                    float z=aisle==0?-14.1f:aisle==1?0:14.1f;
                    for(int i=0;i<18;i++)
                    {
                        float height=(i+1)*.17f,x=15.45f+i*.26f;
                        Part(bank,PrimitiveType.Cube,"Concrete aisle stair",new Vector3(side*x,height*.5f,z),new Vector3(.27f,height,aisle==1?2.3f:1.65f),new Color(.67f,.73f,.73f),game,true);
                        Part(bank,PrimitiveType.Cube,"Stair safety nosing",new Vector3(side*(x-.11f),height+.012f,z),new Vector3(.045f,.02f,aisle==1?2.27f:1.6f),new Color(.99f,.84f,.36f),game);
                    }
                    for(int i=0;i<5;i++)
                    {
                        float x=15.65f+i*1.05f,y=.8f+i*.66f;
                        Part(bank,PrimitiveType.Cylinder,"Aisle handrail post",new Vector3(side*x,y,z+(aisle==1?1.3f:.92f)),new Vector3(.07f,.4f,.07f),Navy,game);
                        if(i<4)
                        {
                            var rail=Part(bank,PrimitiveType.Cylinder,"Sloping aisle handrail",new Vector3(side*(x+.525f),y+.73f,z+(aisle==1?1.3f:.92f)),new Vector3(.085f,.63f,.085f),Navy,game);
                            rail.localRotation=Quaternion.FromToRotation(Vector3.up,new Vector3(side*1.05f,.66f,0));
                        }
                    }
                }
                for(int block=0;block<2;block++)
                {
                    int number=(side<0?100:200)+block+1;
                    var sign=new GameObject("Section sign "+number).transform;sign.SetParent(crowd.transform,false);sign.localPosition=new Vector3(side*21.5f,5.1f,block==0?-7:7);sign.localRotation=Quaternion.Euler(0,side*90,0);
                    Part(sign,PrimitiveType.Cube,"Section sign panel",Vector3.zero,new Vector3(4.5f,1.1f,.15f),Navy,game);
                }
            }
            return list.ToArray();
        }
        public static void Labels(DodgeCrowd crowd,DodgeClubhouse club)
        {
            foreach(var node in crowd.GetComponentsInChildren<Transform>())if(node.name.StartsWith("Section sign ") && int.TryParse(node.name.Substring(13),out int section))
            {
                if(node.Find("Section label"))continue;
                club.WorldLabel("Section label",node,new Vector3(0,.16f,-.09f),"SECTION "+node.name.Substring(13),.055f,Color.white);
                club.WorldLabel("Row label",node,new Vector3(0,-.27f,-.09f),"ROWS A - D",.03f,new Color(.76f,.86f,.82f));
            }
        }
    }
}
