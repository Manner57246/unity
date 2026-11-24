using UnityEngine;
namespace SuperDodgeball
{
    public static class DodgeCourt
    {
        public const float Scale=1.25f;
        public const float HalfWidth=11f*Scale, HalfDepth=14f*Scale;
        public const float PlayWidth=10.3f*Scale, PlayDepth=12.6f*Scale;
        public const float RoomOffset=14.5f*(Scale-1f);
        public static float RestZ(float oldDistance)=>oldDistance+RoomOffset;
        public static bool InRest(Vector3 point)=>Mathf.Abs(point.x)<6f && Mathf.Abs(point.z)>RestZ(17) && Mathf.Abs(point.z)<RestZ(27);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyLayout()
        {
            var arena=GameObject.Find("01 - Playground");
            if(arena)arena.transform.localScale=new Vector3(Scale,1f,Scale);
        }
    }
}
