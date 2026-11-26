using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeWeapon { Standard, Burst, Bowling, Bat, Hammer, Rocket, Bomb, Vest, Coil, Thunderbolt }

    // Match-local equipment. One charge purchases a whole attack, including a burst.
    public sealed class DodgeArsenal : MonoBehaviour
    {
        public DodgeWeapon Weapon { get; private set; }
        public int Charges { get; private set; }
        public bool SpecialSelected { get; private set; } = true;
        public DodgeWeapon Selected => SpecialSelected && Charges > 0 ? Weapon : DodgeWeapon.Standard;
        public static readonly Color BowlingColor = new Color(0.31f, 0.24f, 0.56f);
        public static readonly Color BurstColor = new Color(1f, 0.65f, 0.29f);
        public static int Capacity(DodgeWeapon kind) => kind == DodgeWeapon.Bowling ? 2 : 1;
        public bool CanRefill => Weapon != DodgeWeapon.Standard && !DodgeCombat.IsMelee(Weapon) && Charges < Capacity(Weapon);
        public bool Refill()
        {
            if (!CanRefill) return false;
            Charges++; SpecialSelected = true; return true;
        }
        public bool WearingVest => Weapon == DodgeWeapon.Vest && Charges > 0;
        public static DodgeWeapon RandomSupply() => (DodgeWeapon)Random.Range((int)DodgeWeapon.Burst,(int)DodgeWeapon.Vest+1);
        public static readonly string[] Names={"普通球","五连发礼炮","升天保龄球","棒球棍","充气大锤","毒刺火箭筒","定时炸弹","尖叫鸭爆炸背心","五吨钢卷","宙斯雷枪"};
        Material ordinaryHeldMaterial;
        DodgeActor actor;
        void Awake() { actor=GetComponent<DodgeActor>(); }

        public void Equip(DodgeWeapon weapon, int charges = 3)
        {
            Weapon = weapon;
            Charges = weapon == DodgeWeapon.Standard ? 0 : weapon==DodgeWeapon.Vest || weapon==DodgeWeapon.Coil ? 1 : Mathf.Max(1, charges);
            SpecialSelected = true;
        }

        public void SelectSpecial(bool selected) => SpecialSelected = selected;
        public void ApplyReplica(DodgeWeapon weapon, int charges, bool selected)
        {
            Weapon = weapon; Charges = Mathf.Clamp(charges, 0, 3); SpecialSelected = selected;
        }
        public DodgeWeapon ConsumeAttack()
        {
            if (actor.Zeus.Transformed) return DodgeWeapon.Thunderbolt;
            var result = Selected;
            if (result == DodgeWeapon.Vest) return DodgeWeapon.Standard;
            if (result != DodgeWeapon.Standard && !DodgeCombat.IsMelee(result)) Charges--;
            return result;
        }
        public void ResetEquipment() => Equip(DodgeWeapon.Standard);

        public void DrawHeld(Transform held, Material material, DodgeWeapon weapon)
        {
            bool model = weapon == DodgeWeapon.Burst || (weapon >= DodgeWeapon.Bat && weapon != DodgeWeapon.Vest);
            for(int i=0;i<held.childCount;i++)if(held.GetChild(i).name.StartsWith("Weapon "))held.GetChild(i).gameObject.SetActive(held.GetChild(i).name=="Weapon "+weapon);
            if(model)
            {
                var prop=held.Find("Weapon "+weapon);
                if(!prop)prop=DodgePartyVisuals.Weapon(held,weapon);
                if(weapon==DodgeWeapon.Bat || DodgeCombat.IsLauncher(weapon))
                    DodgeCombat.PoseHeld(actor,prop,weapon);
            }
            bool bowling = weapon == DodgeWeapon.Bowling;
            bool burst = weapon == DodgeWeapon.Burst;
            held.localScale *= bowling ? 1.55f : 1f;
            var renderer = held.GetComponent<Renderer>();
            if (renderer)
            {
                renderer.enabled=!model;
                if (!ordinaryHeldMaterial) ordinaryHeldMaterial = renderer.sharedMaterial;
                renderer.sharedMaterial = bowling || burst ? material : ordinaryHeldMaterial;
                if (bowling || burst) DodgeWardrobe.Tint(renderer, bowling ? BowlingColor : BurstColor);
                else renderer.SetPropertyBlock(null);
            }
            var holes = held.Find("Bowling finger holes");
            if (!holes && bowling) holes = AddFingerHoles(held, material);
            if (holes) holes.gameObject.SetActive(bowling);
        }

        public static Transform AddFingerHoles(Transform ball, Material material)
        {
            var root = new GameObject("Bowling finger holes").transform;
            root.SetParent(ball, false);
            foreach (var p in new[] { new Vector3(-0.1f, 0.12f, 0.46f), new Vector3(0.1f, 0.12f, 0.46f), new Vector3(0f, -0.08f, 0.49f) })
            {
                var hole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hole.name = "Finger well";
                hole.transform.SetParent(root, false);
                hole.transform.localPosition = p;
                hole.transform.localScale = new Vector3(0.14f, 0.14f, 0.035f);
                var collider = hole.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                var renderer = hole.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                DodgeWardrobe.Tint(renderer, new Color(0.07f, 0.065f, 0.13f));
            }
            return root;
        }
    }
}
