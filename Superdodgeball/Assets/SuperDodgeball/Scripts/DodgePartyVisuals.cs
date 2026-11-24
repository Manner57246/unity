using UnityEngine;
namespace SuperDodgeball
{
    public static class DodgePartyVisuals
    {
        public static readonly Color Cream=new Color(1f,.94f,.75f), Mint=new Color(.44f,.85f,.74f), Pink=new Color(.98f,.48f,.66f), Dark=new Color(.22f,.25f,.34f);
        public static Transform Part(Transform parent,PrimitiveType shape,Vector3 p,Vector3 scale,Color color)
        {
            var g=GameObject.CreatePrimitive(shape);g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
            var c=g.GetComponent<Collider>();c.enabled=false;Object.Destroy(c);
            var r=g.GetComponent<Renderer>();r.sharedMaterial=DodgeGame.Instance.popMaterial;DodgeWardrobe.Tint(r,color);return g.transform;
        }
        public static Transform Weapon(Transform parent,DodgeWeapon kind)
        {
            var root=new GameObject("Weapon "+kind).transform;root.SetParent(parent,false);
            if(kind==DodgeWeapon.Bat)
            {
                Part(root,PrimitiveType.Capsule,new Vector3(0,1.25f,0),new Vector3(.5f,2f,.5f),Cream);
                Part(root,PrimitiveType.Cylinder,new Vector3(0,-.1f,0),new Vector3(.2f,.7f,.2f),Pink);
            }
            else if(kind==DodgeWeapon.Hammer)
            {
                Part(root,PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(.2f,1f,.2f),Mint);
                Part(root,PrimitiveType.Capsule,new Vector3(0,2.1f,0),new Vector3(2.8f,.8f,1.3f),Pink).localRotation=Quaternion.Euler(0,0,90);
                Part(root,PrimitiveType.Sphere,new Vector3(0,2.1f,-.55f),new Vector3(1f,1f,.35f),Cream);
            }
            else if(kind==DodgeWeapon.Glove)
            {
                Part(root,PrimitiveType.Cylinder,Vector3.zero,new Vector3(1.5f,1.4f,1.5f),Mint).localRotation=Quaternion.Euler(90,0,0);
                Part(root,PrimitiveType.Cube,new Vector3(0,-.65f,-.3f),new Vector3(.4f,1f,.4f),Dark);
                Glove(root,new Vector3(0,0,1.4f),1.5f);
            }
            else if(kind==DodgeWeapon.Bomb)
            {
                Part(root,PrimitiveType.Sphere,Vector3.zero,Vector3.one*1.8f,Dark);
                Part(root,PrimitiveType.Cylinder,new Vector3(0,.98f,0),new Vector3(.2f,.35f,.2f),Cream);
                Part(root,PrimitiveType.Sphere,new Vector3(0,1.35f,0),Vector3.one*.35f,new Color(1,.45f,.18f));
            }
            return root;
        }
        public static void Glove(Transform root,Vector3 position,float size)
        {
            Part(root,PrimitiveType.Sphere,position,new Vector3(size,size*.85f,size*1.1f),Pink);
            Part(root,PrimitiveType.Sphere,position+new Vector3(size*.38f,-size*.2f,-size*.1f),Vector3.one*size*.52f,Pink);
            Part(root,PrimitiveType.Cylinder,position+Vector3.back*size*.45f,new Vector3(size*.65f,size*.2f,size*.65f),Cream).localRotation=Quaternion.Euler(90,0,0);
        }
        public static Transform Item(Transform parent,DodgeItem kind)
        {
            var root=new GameObject("Item "+kind).transform;root.SetParent(parent,false);
            if(kind==DodgeItem.Banana)
            {
                for(int i=0;i<3;i++)
                {float angle=i*120*Mathf.Deg2Rad;Part(root,PrimitiveType.Capsule,new Vector3(Mathf.Cos(angle)*.24f,.12f,Mathf.Sin(angle)*.24f),new Vector3(.16f,.4f,.16f),new Color(1,.84f,.23f)).localRotation=Quaternion.Euler(Mathf.Sin(angle)*65,0,-Mathf.Cos(angle)*65);}
            }
            else if(kind==DodgeItem.Tea)
            {
                Part(root,PrimitiveType.Cylinder,Vector3.up*.25f,new Vector3(.45f,.3f,.45f),Cream);
                Part(root,PrimitiveType.Cylinder,new Vector3(.08f,.7f,0),new Vector3(.045f,.25f,.045f),Pink);
                for(int i=0;i<4;i++)Part(root,PrimitiveType.Sphere,new Vector3((i%2-.5f)*.17f,.1f,(i/2-.5f)*.17f),Vector3.one*.09f,Dark);
            }
            else if(kind==DodgeItem.Decoy)
            {
                Part(root,PrimitiveType.Capsule,Vector3.up*.38f,new Vector3(.45f,.5f,.4f),Mint);
                Part(root,PrimitiveType.Sphere,Vector3.up*.85f,Vector3.one*.4f,Cream);
            }
            else if(kind==DodgeItem.SpringShoes)
            {
                for(int s=-1;s<=1;s+=2){Part(root,PrimitiveType.Cube,new Vector3(s*.2f,.1f,0),new Vector3(.27f,.15f,.5f),Pink);for(int i=0;i<3;i++)Part(root,PrimitiveType.Cylinder,new Vector3(s*.2f,-.1f-i*.08f,0),new Vector3(.22f,.015f,.22f),Cream);}
            }
            else
            {Part(root,PrimitiveType.Sphere,Vector3.up*.2f,new Vector3(.7f,.3f,.45f),Mint);Part(root,PrimitiveType.Sphere,new Vector3(.2f,.43f,0),Vector3.one*.2f,Color.white);}
            return root;
        }
    }
}
