using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Unity
{
    internal static class RiverGrid
    {
        internal static int ToCell(Vector2 p)=>Mathf.Clamp((int)p.x/8,0,79)+80*Mathf.Clamp((int)p.y/8,0,49);
        internal static Vector2 FromCell(int cell)=>new Vector2(cell%80*8+4,cell/80*8+4);
        internal static float RiverY(float x)=>188+54*Mathf.Sin(x/112-.4f)+.09f*(x-260);
        internal static Stack<Vector2> Path(Vector2 from, Vector2 end, Func<int,bool> walkable)
        {
            int start=ToCell(from), goal=ToCell(end);
            var prev=new int[4000];Array.Fill(prev,-1);prev[start]=start;
            var queue=new Queue<int>();queue.Enqueue(start);
            while(queue.Count>0 && prev[goal]<0)
            {
                int c=queue.Dequeue();
                void Step(int n){if(prev[n]!=-1||!walkable(n))return;prev[n]=c;queue.Enqueue(n);}
                if(c%80>0)Step(c-1);if(c%80<79)Step(c+1);
                if(c>=80)Step(c-80);if(c<3920)Step(c+80);
            }
            if(prev[goal]<0)return null;
            var path=new Stack<Vector2>();path.Push(end);
            for(int n=goal;n!=start;n=prev[n])path.Push(FromCell(n));
            return path;
        }
    }
}
