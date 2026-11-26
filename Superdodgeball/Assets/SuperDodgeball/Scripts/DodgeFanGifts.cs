using UnityEngine;

namespace SuperDodgeball
{
    internal static partial class DodgeWeaponArt
    {
        static void FanPeel(CourtSketchMesh m)
        {
            m.Stick(0xeaba42,new Vector3(0,.14f,0),new Vector3(.025f,.38f,.015f),.12f);
            m.Box(Brown,new Vector3(.025f,.39f,.015f),new Vector3(.11f,.06f,.11f));
            for(int i=0;i<3;i++)
            {
                m.Push(Vector3.zero,i*120+12);
                Vector3 a=new(0,.21f,0),b=new(0,.05f,.32f),c=new(.05f,.085f,.59f);
                Vector3 w=Vector3.right*.105f;
                m.Face(0xffdf68,a-w*.64f,b-w,b+w,a+w*.64f);
                m.Face(0xffec9a,b-w,c,b+w);
                m.Face(0xe7ab36,a+w*.64f,b+w,b-w,a-w*.64f);
                m.Face(0xe7ab36,b+w,c,b-w);
                m.Pop();
            }
        }

        static void FanTea(CourtSketchMesh m)
        {
            m.Drum(0xd7955d,new Vector3(0,.19f,0),new Vector3(.49f,.56f,.49f),14);
            m.Drum(0xedbd8d,new Vector3(0,.32f,0),new Vector3(.51f,.28f,.51f),14);
            for(int i=0;i<10;i++)
            {
                float a=i*2*Mathf.PI/10;
                m.Plum(0x644b45,new Vector3(.228f*Mathf.Cos(a),.018f+.064f*(i%2),.228f*Mathf.Sin(a)),Vector3.one*.079f,8);
            }
            m.Drum(Ivory,new Vector3(0,.49f,0),new Vector3(.58f,.083f,.58f),16);
            m.Stick(Rose,new Vector3(.05f,.51f,0),new Vector3(.18f,.87f,.035f),.067f);
        }

        static void FanShoes(CourtSketchMesh m)
        {
            for(int side=-1;side<=1;side+=2)
            {
                m.Push(new Vector3(side*.205f,0,side*.07f),side*9);
                m.Box(Blue,new Vector3(0,-.17f,.015f),new Vector3(.28f,.069f,.48f),.4f);
                for(int i=0;i<21;i++)
                {
                    Vector3 Turn(int j){float t=j/21f;return new Vector3(.091f*Mathf.Cos(t*6*Mathf.PI),-.13f+t*.22f,.11f*Mathf.Sin(t*6*Mathf.PI));}
                    m.Stick(Gold,Turn(i),Turn(i+1),.042f);
                }
                m.Plum(Ivory,new Vector3(0,.10f,.045f),new Vector3(.32f,.12f,.58f),12);
                m.Plum(Rose,new Vector3(0,.17f,.04f),new Vector3(.30f,.19f,.55f),12);
                m.Plum(Rose,new Vector3(0,.23f,-.10f),new Vector3(.28f,.18f,.26f),10);
                m.Box(Ivory,new Vector3(0,.26f,.043f),new Vector3(.16f,.037f,.043f));
                m.Pop();
            }
        }

        static void FanDummy(CourtSketchMesh m)
        {
            m.Drum(Blue,new Vector3(0,.01f,0),new Vector3(.55f,.12f,.42f));
            m.Plum(Sea,new Vector3(0,.34f,0),new Vector3(.47f,.61f,.38f),10);
            m.Plum(Ivory,new Vector3(0,.78f,0),new Vector3(.40f,.40f,.38f),10);
            m.Drum(Rose,new Vector3(0,.83f,0),new Vector3(.41f,.059f,.39f),12);
            for(int s=-1;s<=1;s+=2)
            {
                m.Stick(Sea,new Vector3(s*.19f,.48f,0),new Vector3(s*.38f,.56f,0),.14f);
                m.Plum(Ink,new Vector3(s*.072f,.77f,-.165f),Vector3.one*.041f,7);
            }
            m.Box(Ivory,new Vector3(0,.39f,-.187f),new Vector3(.12f,.18f,.015f));
        }

        static void FanAmmo(CourtSketchMesh m)
        {
            // Three exposed foam balls in a narrow woven carrier.
            foreach (var p in new[] { new Vector3(-.26f,-.11f,0),new Vector3(.26f,-.11f,0),new Vector3(0,.30f,0) })
            {
                m.Plum(0xf79137,p,new Vector3(.58f,.56f,.56f),14);
                m.Ring(0xe6662c,p,.285f,.275f,.025f,20);
            }
            m.Push(Vector3.zero,Quaternion.Euler(90,0,0));
            m.Ring(Blue,new Vector3(0,0,.04f),.50f,.49f,.08f,18);
            m.Pop();
            m.Box(Blue,new Vector3(0,-.02f,-.27f),new Vector3(.16f,.72f,.07f));
            m.Box(Ivory,new Vector3(0,.05f,-.32f),new Vector3(.20f,.13f,.05f));
        }

        static void FanCherries(CourtSketchMesh m)
        {
            for(int side=-1;side<=1;side+=2)
            {
                var fruit=new Vector3(side*.24f,-.13f,side*.09f);
                m.Plum(side<0?0xb72846:0xdd3f49,fruit,new Vector3(.60f,.59f,.57f),14);
                m.Plum(0xffb4a0,fruit+new Vector3(-.115f,.105f,-.20f),new Vector3(.105f,.12f,.06f),8);
                m.Stick(0x46724c,fruit+Vector3.up*.24f,new Vector3(side*.15f,.42f,0),.038f);
                m.Stick(0x466445,new Vector3(side*.15f,.42f,0),new Vector3(.03f,.62f,.02f),.041f);
            }
            m.Push(new Vector3(.17f,.58f,0),Quaternion.Euler(12,12,-22));
            m.Plum(0x82aa59,Vector3.zero,new Vector3(.35f,.095f,.18f),8);m.Pop();
        }

        static void FanHorn(CourtSketchMesh m)
        {
            m.Drum(0x36958f,new Vector3(-.20f,-.12f,0),new Vector3(.29f,.62f,.29f),14);
            m.Drum(Ivory,new Vector3(-.20f,-.15f,0),new Vector3(.30f,.13f,.30f),14);
            m.Drum(Blue,new Vector3(-.20f,.20f,0),new Vector3(.25f,.12f,.25f),14);
            m.Stick(Rose,new Vector3(-.31f,.29f,0),new Vector3(.14f,.29f,0),.16f);
            m.Push(new Vector3(.10f,.29f,0),Quaternion.Euler(0,0,-90));
            const int count=16;
            for(int i=0;i<count;i++)
            {
                float a=2*Mathf.PI*i/count,b=2*Mathf.PI*(i+1)/count;
                Vector3 u=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),v=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                Vector3 end=Vector3.up*.39f;
                m.Face(Rose,u*.08f,end+u*.30f,end+v*.30f,v*.08f);
                m.Face(0xb04542,v*.065f,end+v*.26f,end+u*.26f,u*.065f);
                m.Face(Ivory,end+u*.30f,end+u*.26f,end+v*.26f,end+v*.30f);
            }
            m.Pop();
        }
    }

    internal static class DodgeFanGifts
    {
        internal static readonly DodgeItem[] Gags={DodgeItem.Decoy,DodgeItem.Tea,DodgeItem.SpringShoes,DodgeItem.Cherries,DodgeItem.AirHorn};
        internal static Color Colour(DodgeGiftKind kind,DodgeWeapon weapon,DodgeItem item)
        {
            if(kind==DodgeGiftKind.Ammo)return new Color(1,.56f,.16f);
            if(kind==DodgeGiftKind.Zeus)return DodgeStorm.Gold;
            if(kind==DodgeGiftKind.Peel)return new Color(1,.86f,.28f);
            if(kind==DodgeGiftKind.Soap || item==DodgeItem.Soap)return DodgeCrowd.SoapColor;
            if(kind==DodgeGiftKind.Skill)return DodgePartyVisuals.Cream;
            if(item==DodgeItem.Cherries)return new Color(.98f,.32f,.41f);
            if(item==DodgeItem.AirHorn || item==DodgeItem.Decoy)return DodgePartyVisuals.Mint;
            if(kind==DodgeGiftKind.Item)return DodgePartyVisuals.Pink;
            return weapon==DodgeWeapon.Bowling?DodgeArsenal.BowlingColor:DodgeArsenal.BurstColor;
        }

        internal static Transform Model(Transform parent,DodgeGiftKind kind,DodgeWeapon weapon,DodgeItem item,DodgeSkill skill)
        {
            var root=new GameObject(kind==DodgeGiftKind.Item?item.ToString():kind.ToString()).transform;
            root.SetParent(parent,false);
            if(kind==DodgeGiftKind.Ammo) DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.FoamAmmo);
            else if(kind==DodgeGiftKind.Zeus) DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.Thundercloud);
            else if(kind==DodgeGiftKind.Peel) DodgePartyVisuals.Item(root,DodgeItem.Banana);
            else if(kind==DodgeGiftKind.Soap || item==DodgeItem.Soap)
            {
                var bar=DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.Soap);
                bar.localRotation=Quaternion.Euler(-36,0,8);bar.localScale=Vector3.one*1.10f;
            }
            else if(kind==DodgeGiftKind.Item) DodgePartyVisuals.Item(root,item).localScale=Vector3.one*1.2f;
            else if(kind==DodgeGiftKind.Skill)
            {
                DodgePartyVisuals.Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(.78f,.88f,.075f),DodgePartyVisuals.Cream);
                DodgePartyVisuals.Part(root,PrimitiveType.Cube,Vector3.up*.18f,new Vector3(.4f,.31f,.084f),DodgePartyVisuals.Mint);
                if(DodgeApp.Active)
                    for(int side=-1;side<=1;side+=2)
                        DodgeApp.Instance.Clubhouse.WorldLabel("技能名",root,new Vector3(0,-.14f,side*.05f),DodgePartyKit.SkillNames[(int)skill],.035f,DodgePartyVisuals.Dark,side<0?0:180);
            }
            else if(weapon==DodgeWeapon.Bowling)
            {
                var ball=DodgePartyVisuals.Part(root,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.65f,DodgeArsenal.BowlingColor);
                DodgeArsenal.AddFingerHoles(ball,DodgeGame.Instance.popMaterial);
            }
            else if(weapon!=DodgeWeapon.Standard) DodgePartyVisuals.Weapon(root,weapon).localScale=Vector3.one*.3f;
            else DodgePartyVisuals.Part(root,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.48f,DodgeArsenal.BurstColor);
            return root;
        }

        internal static LineRenderer Ring(Transform parent,Color colour,float radius)
        {
            var ring=parent.gameObject.AddComponent<LineRenderer>();
            ring.sharedMaterial=DodgeGame.Instance.trailMaterial;ring.useWorldSpace=false;ring.loop=true;
            ring.startColor=ring.endColor=colour;ring.widthMultiplier=.075f;ring.positionCount=48;
            for(int i=0;i<ring.positionCount;i++)
            {
                float a=2*Mathf.PI*i/ring.positionCount;
                ring.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));
            }
            return ring;
        }
    }
}
