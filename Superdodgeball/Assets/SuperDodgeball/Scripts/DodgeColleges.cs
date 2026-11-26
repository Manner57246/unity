using UnityEngine;

namespace SuperDodgeball
{
    internal static class DodgeColleges
    {
        internal const string Cape="海岬大学", Ridge="赤岭大学";
        internal static string Name(int team)=>team==0?Cape:Ridge;
        internal static readonly Color Sea=new Color32(31,108,105,255), Brick=new Color32(163,61,42,255),
            Navy=new Color32(31,52,64,255), Cream=new Color32(249,230,185,255), Gold=new Color32(221,169,76,255);
        static readonly Color32[] Complexions={new(222,160,111,255),new(175,113,79,255),
            new(243,188,143,255),new(156,102,76,255),new(209,150,113,255)};
        internal static Color Skin(int seat)=>Complexions[Mathf.Clamp(seat,0,4)];
        internal static Color Colour(int team)=>team==0?Sea:team==1?Brick:Navy;
    }
}
