using UnityEngine;
using UnityEngine.Rendering;

namespace SuperDodgeball
{
    public sealed partial class DodgeHud
    {
        const float HitDuration = .82f;
        readonly float[] hitTimeLeft = new float[5];
        readonly Transform[] hitVolumes = new Transform[5];
        readonly Quaternion[] hitOrientation = new Quaternion[5];
        readonly float[] hitSide = new float[5];
        readonly MeshRenderer[] cloudRenderers = new MeshRenderer[5];
        MaterialPropertyBlock cloudProperties;
        Material cloudMaterial;
        Vector3 cloudDimensions;
        static readonly int AgeProperty = Shader.PropertyToID("_Age");
        static readonly int SeedProperty = Shader.PropertyToID("_Seed");

        void PrepareHitClouds()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var density = Resources.Load<Texture3D>("Court/HitCloudVolume");
            var shader = Resources.Load<Shader>("Court/HitCloud");
            var boundsMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (!density || !shader || !boundsMesh) return;
            cloudDimensions = new Vector3(density.width,density.height,density.depth) * .022f;
            cloudMaterial = new Material(shader) { name = "HIT · illuminated cloud" };
            cloudMaterial.SetTexture("_CloudVolume",density);
            cloudMaterial.SetVector("_Dimensions",cloudDimensions);
            cloudProperties = new MaterialPropertyBlock();
            for (int slot=0; slot<hitVolumes.Length; slot++)
            {
                // Only the ray's entry and exit use this box. The renderer integrates cloud density inside it.
                var volume = new GameObject("HIT cloud " + slot, typeof(MeshFilter), typeof(MeshRenderer));
                volume.transform.SetParent(transform,false);
                volume.GetComponent<MeshFilter>().sharedMesh = boundsMesh;
                var surface = volume.GetComponent<MeshRenderer>();
                surface.sharedMaterial = cloudMaterial;
                surface.shadowCastingMode = ShadowCastingMode.Off;
                surface.receiveShadows = false;
                surface.lightProbeUsage = LightProbeUsage.Off;
                surface.reflectionProbeUsage = ReflectionProbeUsage.Off;
                hitVolumes[slot] = volume.transform;
                cloudRenderers[slot] = surface;
                volume.SetActive(false);
            }
        }

        internal void ShowHit(int target)
        {
            hitTimeLeft[target] = HitDuration;
            if (!matchCamera) return;
            hitOrientation[target] = matchCamera.transform.rotation;
            var position = DodgeApp.Instance.Actors[target].Puppet.BodyCenter;
            float x = matchCamera.WorldToViewportPoint(position).x;
            hitSide[target] = x < .18f || (x >= .5f && x <= .82f) ? 1f : -1f;
        }

        internal bool HasHit(int target) => hitTimeLeft[target] > 0f;

        internal void ClearHits()
        {
            System.Array.Clear(hitTimeLeft,0,hitTimeLeft.Length);
            foreach (var volume in hitVolumes)
                if (volume) volume.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var app = DodgeApp.Instance;
            bool playing = DodgeApp.Active && app.MatchRunning;
            bool visible = playing && app.ShowReticle && matchCamera;
            float dt = playing && DodgeGame.SimulationActive ? Time.deltaTime : 0f;
            for (int slot=0; slot<hitTimeLeft.Length; slot++)
            {
                hitTimeLeft[slot] = Mathf.Max(0f,hitTimeLeft[slot]-dt);
                var volume = hitVolumes[slot];
                if (!volume) continue;
                bool show = visible && HasHit(slot) && app.Actors[slot].isActiveAndEnabled;
                if (show) PoseCloud(slot, app.Actors[slot].Puppet.BodyCenter);
                if (volume.gameObject.activeSelf != show) volume.gameObject.SetActive(show);
            }
        }

        void PoseCloud(int slot, Vector3 center)
        {
            float age = 1f-hitTimeLeft[slot]/HitDuration;
            float pop = age < .16f ? Mathf.SmoothStep(.55f,1.14f,age/.16f)
                                   : Mathf.SmoothStep(1.14f,1f,(age-.16f)/.22f);
            var orientation = hitOrientation[slot];
            float side = hitSide[slot];
            float depth = Mathf.Max(.1f,Vector3.Dot(center-matchCamera.transform.position,matchCamera.transform.forward));
            float viewHeight = 2f*depth*Mathf.Tan(matchCamera.fieldOfView*Mathf.Deg2Rad*.5f);
            float unitsPerPixel = viewHeight*Mathf.Min(1f/720f,matchCamera.aspect/960f);
            float width = Mathf.Lerp(98f,80f,Mathf.InverseLerp(5f,25f,depth))*unitsPerPixel;
            float size = Mathf.Clamp(width/2.55f,.26f,1.22f);
            var volume = hitVolumes[slot];
            var position = center + Vector3.up*(1.18f+.50f*age)
                + orientation*new Vector3(side*(.55f+1.40f*size),0,-.23f);
            float turn = Mathf.SmoothStep(-30f,18f,age/.61f);
            volume.SetPositionAndRotation(position,orientation*Quaternion.Euler(-9f+5f*age,side*turn,side*(6f-4f*age)));
            volume.localScale = cloudDimensions * (size*pop);
            cloudProperties.SetFloat(AgeProperty,age);
            cloudProperties.SetFloat(SeedProperty,slot*1.87f);
            cloudRenderers[slot].SetPropertyBlock(cloudProperties);
        }
    }
}
