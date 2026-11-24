using UnityEngine;

namespace Rouge.Unity
{
    internal sealed class SwordImpact
    {
        private readonly ParticleSystem dust, sparks, gusts, flash;

        internal SwordImpact(Transform owner, Material material)
        {
            dust = Emitter(owner, material, "dust", 1);
            gusts = Emitter(owner, material, "gust", 5);
            sparks = Emitter(owner, material, "spark", 8);
            flash = Emitter(owner, material, "flash", 9);
        }

        private static ParticleSystem Emitter(Transform owner, Material material, string texture, int order)
        {
            var node = new GameObject("Blade FX " + texture);
            node.transform.SetParent(owner, false);
            var system = node.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", Resources.Load<Texture2D>("BladeFX/" + texture));
            renderer.SetPropertyBlock(block);
            var sizes = system.sizeOverLifetime; sizes.enabled = true;
            sizes.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0,.8f),new Keyframe(.15f,1),new Keyframe(.65f,.85f),new Keyframe(1,.16f)));
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.95f,.45f),new GradientAlphaKey(0,1)});
            fade.color = gradient;
            system.Play();
            return system;
        }

        private static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity,
            float size, float life, Color color, float angle)
        {
            system.Emit(new ParticleSystem.EmitParams {
                position=position, velocity=velocity, startSize=size, startLifetime=life,
                startColor=color, rotation=angle
            },1);
        }

        internal void Lift(Vector3 center)
        {
            for(int i=0;i<6;i++)
            {
                int sign=(i%2==0) ? -1 : 1;
                int row=i/2;
                Vector3 offset=new Vector3(sign*(.54f+row*.1f),0,.62f-row*.28f);
                Emit(gusts,center+offset,new Vector3(sign*(1.2f+row*.35f),0,-1.6f),
                    .55f+row*.08f,.36f+row*.055f,new Color(.76f,.81f,.81f,.65f),sign*(22+row*14));
            }
        }

        internal void KickDust(Vector3 feet)
        {
            for(int i=0;i<10;i++)
            {
                float a=Mathf.PI+(i%7-3)*.24f;
                var velocity=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(1.1f+(i%4)*.65f);
                Emit(dust,feet+velocity*.10f,velocity,.39f+(i%5)*.095f,
                    .39f+(i%5)*.078f,new Color(.76f,.76f,.74f,.85f),i*31);
            }
        }

        internal void Strike(Vector3 contact)
        {
            Emit(flash,contact,Vector3.zero,.79f,.085f,Color.white,-18);
            for(int i=0;i<11;i++)
            {
                float a=.15f+(i%7-3)*.24f+(i>7 ? 2.95f : 0f);
                var direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Emit(sparks,contact+direction*.14f,direction*(2.5f+i%6),
                    .16f+(i%4)*.09f,.14f+(i%5)*.045f,
                    Color.Lerp(new Color(.93f,.74f,.58f),Color.white,(i%5)/4f),-a*Mathf.Rad2Deg);
            }
        }
    }
}
