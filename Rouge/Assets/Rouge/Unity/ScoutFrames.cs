using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Unity
{
    internal sealed class ScoutFrames : IDisposable
    {
        [Serializable] private struct Box { public int x,y,w,h; }
        [Serializable] private struct Frame { public Box frame; public int duration; }
        [Serializable] internal struct Clip { public string name; public int from,to; }
        [Serializable] private struct Meta { public Clip[] frameTags; }
        [Serializable] private class Sheet { public Frame[] frames; public Meta meta; }
        private readonly Dictionary<string,Clip> clips = new();
        private readonly Sprite[] art;
        private readonly float[] seconds;
        internal ScoutFrames(string texturePath = "WingedScout/Scout")
        {
            var tex=Resources.Load<Texture2D>(texturePath);
            var sheet=JsonUtility.FromJson<Sheet>(Resources.Load<TextAsset>("WingedScout/Timing").text);
            art=new Sprite[sheet.frames.Length];
            seconds=new float[art.Length];
            for(int i=0;i<art.Length;i++)
            {
                var box=sheet.frames[i].frame;
                var rect=new Rect(box.x,tex.height-box.y-box.h,box.w,box.h);
                art[i]=Sprite.Create(tex,rect,new Vector2(.5f,.5f),box.h/3.2f,0,SpriteMeshType.FullRect);
                art[i].name="WingedScout_"+i;
                seconds[i]=sheet.frames[i].duration*.001f;
            }
            foreach(var clip in sheet.meta.frameTags) clips.Add(clip.name,clip);
        }
        internal Clip Get(string name)=>clips[name];
        internal Sprite FrameAt(int index)=>art[index];
        internal float Duration(int index)=>seconds[index];
        internal Sprite Loop(string name,float time)
        {
            var clip=clips[name];
            float duration=0;
            for(int i=clip.from;i<=clip.to;i++)duration+=seconds[i];
            float phase=Mathf.Repeat(time,duration);
            for(int i=clip.from;i<clip.to;i++){phase-=seconds[i]; if(phase<0)return art[i];}
            return art[clip.to];
        }
        public void Dispose(){foreach(var sprite in art)UnityEngine.Object.Destroy(sprite);}
    }
}
