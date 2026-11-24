using System.Collections;
using System.Collections.Generic;
using Rouge.Core;
using UnityEngine;

namespace Rouge.Unity
{
    internal sealed class SpellCast : MonoBehaviour
    {
        private const int FrameCount = 10;
        private readonly Dictionary<Element,Sprite[]> frames = new();
        private readonly Dictionary<Element,AudioClip> sounds = new();
        private readonly Dictionary<string,Sprite[]> impacts = new();
        private SpriteRenderer ink;
        private SpriteRenderer burst;
        internal Camera View { private get; set; }
        private AudioSource voice;

        internal IEnumerator Play(CardPlay play, SpriteRenderer caster, SpriteRenderer target,System.Action hurt)
        {
            if(ink==null)
            {
                ink=gameObject.AddComponent<SpriteRenderer>();
                ink.sharedMaterial=caster.sharedMaterial;
                ink.sortingOrder=10;
                burst=new GameObject("Spell impact").AddComponent<SpriteRenderer>();
                burst.transform.SetParent(transform,false);
                burst.sharedMaterial=caster.sharedMaterial;
                burst.sortingOrder=11;
                burst.enabled=false;
                voice=gameObject.AddComponent<AudioSource>();
                voice.playOnAwake=false;
                voice.spatialBlend=0;
            }
            var element=play.Card.School;
            if(!frames.TryGetValue(element,out var clip))
            {
                string path="Spells/"+element;
                var sheet=Resources.Load<Texture2D>(path);
                int width=sheet.width/FrameCount;
                clip=new Sprite[FrameCount];
                for(int i=0;i<clip.Length;i++)
                    clip[i]=Sprite.Create(sheet,new Rect(i*width,0,width,sheet.height),new Vector2(.5f,.5f),20,0,SpriteMeshType.FullRect);
                frames.Add(element,clip);
                sounds.Add(element,Resources.Load<AudioClip>(path));
            }
            var delta=target.transform.position-caster.transform.position;
            bool shield=element==Element.Earth && play.Reaction==null;
            var anchor=shield ? caster.transform.position : target.transform.position;
            // The projectile travels from column 24 to column 134 on a 160-pixel canvas.
            transform.SetPositionAndRotation(anchor-delta*(54f/110f),caster.transform.rotation);
            float scale=delta.magnitude/(110f/20f);
            transform.localScale=new Vector3(scale,scale*(play.Reaction!=null ? 1.20f : 1f),1);
            voice.PlayOneShot(sounds[element],play.Reaction!=null ? 1f : .8f);
            var receiver=shield ? caster : target;
            var original=receiver.color;
            bool combo=play.Reaction!=null;
            string impactName=play.Reaction?.Freeze==true ? "Freeze" :
                combo && element==Element.Lightning ? "Conduct" : "Burst";
            if(!impacts.TryGetValue(impactName,out var impactFrames))
            {
                var strip=Resources.Load<Texture2D>("Spells/"+impactName+"Impact");
                int side=strip.height;
                impactFrames=new Sprite[strip.width/side];
                for(int n=0;n<impactFrames.Length;n++)
                    impactFrames[n]=Sprite.Create(strip,new Rect(n*side,0,side,side),new Vector2(.5f,.44f),26,0,SpriteMeshType.FullRect);
                impacts.Add(impactName,impactFrames);
            }
            var tint=element switch {
                Element.Fire=>new Color(1,.55f,.21f), Element.Earth=>new Color(1,.85f,.52f),
                Element.Lightning=>new Color(.88f,.79f,1), Element.Wind=>new Color(.62f,1,.80f),
                _=>new Color(.64f,.94f,1)
            };
            float lens=View.orthographicSize;
            Vector3 origin=View.transform.position;
            ink.enabled=true;
            for(int i=0;i<6;i++)
            {
                ink.sprite=clip[i];
                View.orthographicSize=Mathf.Lerp(lens,lens*(combo?.945f:.975f),i/5f);
                yield return new WaitForSeconds(.048f);
            }
            if(!shield)hurt();
            receiver.color=tint;
            burst.transform.position=receiver.transform.position;
            burst.transform.localScale=Vector3.one*(combo?1.23f:.82f);
            burst.enabled=true;
            for(int i=0;i<impactFrames.Length;i++)
            {
                float t=(float)i/(impactFrames.Length-1);
                ink.enabled=i<6;
                ink.sprite=clip[Mathf.Min(6+i/2,clip.Length-1)];
                burst.sprite=impactFrames[i];
                burst.color=new Color(tint.r,tint.g,tint.b,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,t)));
                View.transform.position=origin+(Vector3.right*Mathf.Sin(i*2.3f)+Vector3.forward*Mathf.Cos(i*2f)*.45f)
                    *((combo?.14f:.055f)*(1-t)*(1-t));
                View.orthographicSize=lens*(1-(combo?.065f:.028f)*(1-t));
                yield return new WaitForSeconds(combo?.048f:.031f);
            }
            View.transform.position=origin;
            View.orthographicSize=lens;
            burst.enabled=false;
            ink.enabled=false;
            receiver.color=original;
        }

        private void OnDestroy()
        {
            foreach(var clip in frames.Values)
                foreach(var sprite in clip) Destroy(sprite);
            foreach(var clip in impacts.Values)
                foreach(var sprite in clip) Destroy(sprite);
        }
    }
}
