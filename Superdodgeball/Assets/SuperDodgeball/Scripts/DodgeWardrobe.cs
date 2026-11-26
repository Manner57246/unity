using UnityEngine;

namespace SuperDodgeball
{
    // Small working sample of the wardrobe contract: rigid accessories use sockets;
    // flexible garments sample exactly the body's physical deformation curve.
    public sealed partial class DodgeWardrobe : MonoBehaviour
    {
        DodgeRagdoll puppet;
        DodgeTube shirt, waistband;
        Transform faceFit, headRig;
        GameObject cap;
        public int Style { get; private set; }
        public bool GarmentVisible => shirt && shirt.gameObject.activeSelf;
        public Transform Hat => cap ? cap.transform : null;

        public void Initialize(DodgeRagdoll owner, Material material)
        {
            puppet = owner;
            shirt = Tube("Sleeveless jersey - same deformation", material, new Color(1f, 0.87f, 0.66f));
            waistband = Tube("Shorts waistband - same deformation", material, new Color(0.29f, 0.34f, 0.52f));
            headRig = owner.GetComponent<DodgeActor>().head;
            faceFit = new GameObject("Uniform head fit").transform;
            faceFit.SetParent(puppet.HeadSocket.parent, false);
            cap = new GameObject("Modular baseball cap");
            cap.transform.SetParent(faceFit, false);
            cap.transform.localPosition = puppet.HeadSocket.localPosition;
            Part("Cap crown", cap.transform, new Vector3(0f, -0.03f, 0f), new Vector3(0.72f, 0.25f, 0.65f), material, new Color(0.94f, 0.62f, 0.4f));
            Part("Cap brim", cap.transform, new Vector3(0f, -0.09f, 0.27f), new Vector3(0.78f, 0.055f, 0.53f), material, new Color(1f, 0.8f, 0.49f));
            // Mount the face on the physical head so the expression follows every dodge and ragdoll.
            var face = faceFit;
            var ink = new Color(.075f, .13f, .17f);
            InitializeTrackKit(face,material);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Eye", face, new Vector3(side * .145f, .026f, .356f), new Vector3(.105f, .135f, .069f), material, ink);
                Part("Eye glint", face, new Vector3(side * .145f - .013f, .053f, .385f), Vector3.one * .032f, material, Color.white);
            }
            Part("Crooked game face", face, new Vector3(.031f, -.11f, .353f), new Vector3(.126f, .032f, .048f), material, ink).localRotation=Quaternion.Euler(0,0,9);
            SetStyle(0);
        }

        DodgeTube Tube(string label, Material material, Color color)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var tube = go.AddComponent<DodgeTube>();
            tube.Initialize(material);
            Tint(tube.Surface, color);
            return tube;
        }

        public static void Tint(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }

        static Transform Part(string label, Transform parent, Vector3 position, Vector3 scale, Material material, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            Tint(renderer, color);
            return part.transform;
        }

        internal bool Olympian;
        public void SetStyle(int style)
        {
            Style = ((style % 3) + 3) % 3;
            if (cap) cap.SetActive(!Olympian && Style > 0);
            if (shirt) shirt.gameObject.SetActive(!Olympian);
            if (sweatband) sweatband.SetActive(!Olympian && Style==0);
            if (hairMesh) hairMesh.SetActive(!Olympian);
            if (waistband) waistband.gameObject.SetActive(!Olympian);
        }

        public void Draw()
        {
            var size = headRig.lossyScale;
            faceFit.localScale = new Vector3(size.x / .85f, size.y / .70f, size.z / .77f);
            if (Olympian || puppet.BodyPath == null) return;
            shirt.Draw(puppet.BodyPath, puppet.BodyRadii, puppet.transform.forward, 0.91f, 0.11f, 0.98f, 0.030f*puppet.transform.localScale.x);
            waistband.Draw(puppet.BodyPath, puppet.BodyRadii, puppet.transform.forward, 0.92f, 0.03f, 0.15f, 0.035f*puppet.transform.localScale.x);
        }
    }
}
