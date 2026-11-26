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
            if(kind==DodgeWeapon.Hammer)
            {
                Part(root,PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(.2f,1f,.2f),Mint);
                Part(root,PrimitiveType.Capsule,new Vector3(0,2.1f,0),new Vector3(2.8f,.8f,1.3f),Pink).localRotation=Quaternion.Euler(0,0,90);
                Part(root,PrimitiveType.Sphere,new Vector3(0,2.1f,-.55f),new Vector3(1f,1f,.35f),Cream);
            }
            else
            {
                var prop=kind switch {
                    DodgeWeapon.Burst=>DodgeWeaponArt.Prop.Cannon,DodgeWeapon.Bat=>DodgeWeaponArt.Prop.Bat,
                    DodgeWeapon.Rocket=>DodgeWeaponArt.Prop.Launcher,DodgeWeapon.Bomb=>DodgeWeaponArt.Prop.Timer,
                    DodgeWeapon.Thunderbolt=>DodgeWeaponArt.Prop.Thunderbolt,
                    DodgeWeapon.Vest=>DodgeWeaponArt.Prop.Vest,DodgeWeapon.Coil=>DodgeWeaponArt.Prop.Coil,
                    _=>throw new System.ArgumentOutOfRangeException(nameof(kind))};
                var art=DodgeWeaponArt.Model(root,prop);art.localScale=Vector3.one/.34f;
                if(kind==DodgeWeapon.Bomb)
                    DodgeApp.Instance.Clubhouse.WorldLabel("Fuse display",art,new Vector3(0,.074f,-.341f),"03",.029f,new Color(.7f,1,.72f));
            }
            return root;
        }
        public static Transform Item(Transform parent,DodgeItem kind)
        {
            var root=new GameObject("Item "+kind).transform;root.SetParent(parent,false);
            if(kind==DodgeItem.Soap)
                DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.Soap);
            else if(kind==DodgeItem.ThunderPill)
            {
                DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.Pill);
                DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.BittenPill).gameObject.SetActive(false);
            }
            else if(kind==DodgeItem.SprintShot)
                DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.Injector).localRotation=Quaternion.Euler(-15,65,0);
            else if(kind==DodgeItem.Cherries || kind==DodgeItem.AirHorn)
                DodgeWeaponArt.Model(root,kind==DodgeItem.Cherries?DodgeWeaponArt.Prop.Cherries:DodgeWeaponArt.Prop.AirHorn);
            else if(kind==DodgeItem.Banana) DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.Peel);
            else if(kind==DodgeItem.Tea) DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.BubbleTea);
            else if(kind==DodgeItem.Decoy) DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.TrainingDummy);
            else if(kind==DodgeItem.SpringShoes) DodgeWeaponArt.Model(root,DodgeWeaponArt.Prop.SpringShoes);
            else
            {Part(root,PrimitiveType.Sphere,Vector3.up*.2f,new Vector3(.7f,.3f,.45f),Mint);Part(root,PrimitiveType.Sphere,new Vector3(.2f,.43f,0),Vector3.one*.2f,Color.white);}
            return root;
        }
    }
}
