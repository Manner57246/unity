using System;
using System.Collections;
using Rouge.Core;
using UnityEngine;

namespace Rouge.Unity
{
    internal sealed class EnemyActor : MonoBehaviour
    {
        private enum Pose { Idle, Breathe, Windup, Reach, Reverse, Follow, Guard, Hurt, Fall }
        private readonly Sprite[] poses=new Sprite[9];
        private SpriteRenderer body, player;
        private SwordImpact particles;
        private AudioSource voice;
        private AudioClip swing, contact, thunder;
        private LineRenderer tether;

        internal void Bind(Battle battle,SpriteRenderer fighter,SwordImpact impact,string appearance=null)
        {
            body=GetComponent<SpriteRenderer>(); player=fighter; particles=impact;
            var sheet=Resources.Load<Texture2D>(appearance ?? "Enemies/"+battle.Enemy);
            int width=sheet.width/poses.Length;
            for(int i=0;i<poses.Length;i++)
                poses[i]=Sprite.Create(sheet,new Rect(i*width,0,width,sheet.height),new Vector2(.55f,.5f),20,0,SpriteMeshType.FullRect);
            body.flipX=false;
            voice=gameObject.AddComponent<AudioSource>(); voice.playOnAwake=false;
            swing=Resources.Load<AudioClip>("BladeFX/bladeDraw");
            contact=Resources.Load<AudioClip>("BladeFX/ironImpact");
            thunder=Resources.Load<AudioClip>("Spells/Lightning");
            tether=new GameObject("Enemy spell trace").AddComponent<LineRenderer>();
            tether.transform.SetParent(transform,false); tether.sharedMaterial=body.sharedMaterial;
            tether.sortingOrder=6; tether.positionCount=12; tether.enabled=false;
            Idle(battle);
        }

        private void Set(Pose pose) => body.sprite=poses[(int)pose];
        internal void Idle(Battle battle)
        {
            Set(battle.Frozen ? Pose.Hurt : battle.Armor>0 ? Pose.Guard :
                Mathf.Repeat(Time.unscaledTime,.8f)<.46f ? Pose.Idle : Pose.Breathe);
            body.color=battle.Frozen ? new Color(.57f,.87f,1f) : Color.white;
        }
        internal void Hurt() => Set(Pose.Hurt);
        internal IEnumerator Fall()
        {
            Set(Pose.Fall);
            var rest=transform.rotation;
            for(float t=0;t<1;t+=Time.deltaTime/.32f)
            {
                transform.rotation=rest*Quaternion.Euler(0,0,-28*t);
                body.color=new Color(.76f,.71f,.72f,1-t);
                yield return null;
            }
            body.enabled=false;
        }

        internal IEnumerator Act(Battle battle,Action resolve)
        {
            var action=battle.Intent;
            var home=transform.position;
            var foe=player.transform.position;
            var direction=(foe-home).normalized;
            bool heavy=action.Move==EnemyMove.Crush;
            bool magic=action.Move==EnemyMove.Bolt || action.Move==EnemyMove.GroundSpark;
            bool ranged=magic || action.Move==EnemyMove.Bind;
            bool miss=battle.Airborne && !action.HitsAir;
            int wound=action.Wound(battle.Guard,battle.Airborne,battle.Frozen);
            if(battle.Frozen || action.Move==EnemyMove.Brace)
            {
                Set(battle.Frozen ? Pose.Hurt : Pose.Guard);
                body.color=battle.Frozen ? new Color(.53f,.85f,1) : new Color(1,.90f,.68f);
                yield return new WaitForSeconds(.26f);
                resolve();
            }
            else
            {
                Set(Pose.Windup);
                float windup=heavy ? .38f : ranged ? .30f : .18f;
                var ready=home-direction*(heavy?.49f:.24f);
                yield return Move(home,ready,windup,0);
                var reach=ranged ? ready : foe-direction*1.25f;
                for(int hit=0;hit<action.Hits;hit++)
                {
                    Set(hit==0 ? Pose.Reach : Pose.Reverse);
                    voice.PlayOneShot(magic ? thunder : swing,.68f);
                    yield return Move(transform.position,reach,heavy?.14f:.09f,heavy?.86f:action.Move==EnemyMove.Feint?-.40f:0);
                    if(ranged) Trace(reach,foe,magic);
                    if(!miss)
                    {
                        particles.Strike(foe-direction*.18f);
                        voice.PlayOneShot(contact,heavy?.92f:.65f);
                        player.color=wound>0 ? new Color(1,.70f,.67f) : new Color(.72f,.87f,1);
                    }
                    else player.transform.position=foe+Vector3.forward*.58f;
                    if(hit==action.Hits-1) resolve();
                    yield return new WaitForSeconds(heavy?.12f:.08f);
                    tether.enabled=false;
                    if(hit+1<action.Hits) yield return Move(reach,reach-direction*.60f,.07f,0);
                }
                Set(Pose.Follow);
                yield return Move(transform.position,home,.20f,0);
            }
            player.transform.position=foe;
            player.color=Color.white;
            body.color=Color.white;
            Set(Pose.Idle);
        }

        private IEnumerator Move(Vector3 from,Vector3 to,float duration,float arc)
        {
            for(float t=0;t<1;t+=Time.deltaTime/duration)
            {
                transform.position=Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,t))+Vector3.forward*(arc*Mathf.Sin(t*Mathf.PI));
                yield return null;
            }
            transform.position=to;
        }
        private void Trace(Vector3 from,Vector3 to,bool lightning)
        {
            var tint=lightning ? new Color(.61f,.92f,1) : new Color(.95f,.68f,.34f);
            tether.startColor=tether.endColor=tint;
            tether.startWidth=lightning?.10f:.08f; tether.endWidth=.04f;
            for(int i=0;i<tether.positionCount;i++)
            {
                float t=(float)i/(tether.positionCount-1);
                float offset=lightning ? Mathf.Sin(i*2.4f)*.26f : -.38f;
                tether.SetPosition(i,Vector3.Lerp(from,to,t)+Vector3.forward*(Mathf.Sin(t*Mathf.PI)*offset));
            }
            tether.enabled=true;
        }
        private void OnDestroy()
        {
            foreach(var pose in poses) if(pose!=null) Destroy(pose);
        }
    }
}
