using System.Collections;
using System.Collections.Generic;
using Rouge.Core;
using UnityEngine;

namespace Rouge.Unity
{
    internal sealed class ExpeditionFoley : MonoBehaviour
    {
        private readonly Dictionary<string,AudioClip> clips=new();
        private AudioSource steps, cards, actions;
        private bool leftFoot;

        private void Awake()
        {
            foreach(var clip in Resources.LoadAll<AudioClip>("Foley")) clips.Add(clip.name,clip);
            steps=Channel("Footsteps");
            cards=Channel("Dealing");
            actions=Channel("Card and item sounds");
        }

        private AudioSource Channel(string label)
        {
            var child=new GameObject(label);
            child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;
            source.spatialBlend=0;
            return source;
        }

        internal void Step(RouteMap map,int cell,Talent talent)
        {
            var ground=map.Terrain[cell];
            int x=cell%map.Terrain.Width, y=cell/map.Terrain.Width;
            bool bridge=map.Region==WorldAtlas.Highland && y==12 &&
                map.Terrain[map.Terrain.Cell(x,13)]==TerrainKind.Cliff;
            string surface=bridge ? "Bridge" : ground switch {
                TerrainKind.Lake or TerrainKind.Cliff when talent==Talent.Wings => "Wing",
                TerrainKind.Lake => "Water",
                TerrainKind.Cliff => "Stone",
                TerrainKind.Forest => "Leaves",
                _ when map.Region==WorldAtlas.Highland || map.Region==WorldAtlas.Mine || map.Region==WorldAtlas.Forge => "Stone",
                _ => "Soil"
            };
            leftFoot=!leftFoot;
            steps.pitch=(leftFoot?.96f:1.04f)+.015f*Mathf.Sin(cell);
            steps.PlayOneShot(clips[surface],leftFoot?.49f:.54f);
        }

        internal void Draw(int count)
        {
            if(count>0) StartCoroutine(Deal(count));
        }

        private IEnumerator Deal(int count)
        {
            for(int i=0;i<count;i++)
            {
                cards.PlayOneShot(clips["Draw"],.42f);
                if(i+1<count)yield return new WaitForSecondsRealtime(.055f);
            }
        }

        internal void PlayCard(CardType type)
        {
            string cue=type switch { CardType.Defense=>"Guard",CardType.Skill=>"Skill",_=>"Lay" };
            actions.PlayOneShot(clips[cue],.55f);
        }
        internal void PlayCue(AudioClip clip,float volume) => actions.PlayOneShot(clip,volume);
        internal void Coins() => actions.PlayOneShot(clips["Coin"],.50f);
        internal void Dice() => actions.PlayOneShot(clips["Dice"],.58f);
    }
}
