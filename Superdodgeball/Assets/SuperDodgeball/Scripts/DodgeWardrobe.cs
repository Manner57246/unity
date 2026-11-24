using UnityEngine;

namespace SuperDodgeball
{
    // Small working sample of the wardrobe contract: rigid accessories use sockets;
    // flexible garments sample exactly the body's physical deformation curve.
    public sealed class DodgeWardrobe : MonoBehaviour
    {
        DodgeRagdoll puppet;
        DodgeTube shirt, waistband;
        GameObject cap;
        public int Style { get; private set; }
        public bool GarmentVisible => shirt && shirt.gameObject.activeSelf;
        public Transform Hat => cap ? cap.transform : null;

        public void Initialize(DodgeRagdoll owner, Material material)
        {
            puppet = owner;
            shirt = Tube("Sleeveless jersey - same deformation", material, new Color(1f, 0.87f, 0.66f));
            waistband = Tube("Shorts waistband - same deformation", material, new Color(0.29f, 0.34f, 0.52f));
            cap = new GameObject("Modular baseball cap");
            cap.transform.SetParent(puppet.HeadSocket, false);
            Part("Cap crown", cap.transform, new Vector3(0f, -0.03f, 0f), new Vector3(0.72f, 0.25f, 0.65f), material, new Color(0.94f, 0.62f, 0.4f));
            Part("Cap brim", cap.transform, new Vector3(0f, -0.09f, 0.27f), new Vector3(0.78f, 0.055f, 0.53f), material, new Color(1f, 0.8f, 0.49f));
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

        static void Part(string label, Transform parent, Vector3 position, Vector3 scale, Material material, Color color)
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
        }

        public void SetStyle(int style)
        {
            Style = ((style % 3) + 3) % 3;
            if (cap) cap.SetActive(Style > 0);
            if (shirt) shirt.gameObject.SetActive(Style == 2);
            if (waistband) waistband.gameObject.SetActive(Style == 2);
        }

        public void Draw()
        {
            if (Style != 2 || puppet.BodyPath == null) return;
            shirt.Draw(puppet.BodyPath, puppet.BodyRadii, puppet.transform.forward, 0.86f, 0.2f, 0.94f, 0.018f);
            waistband.Draw(puppet.BodyPath, puppet.BodyRadii, puppet.transform.forward, 0.86f, 0.04f, 0.2f, 0.022f);
        }
    }
}
