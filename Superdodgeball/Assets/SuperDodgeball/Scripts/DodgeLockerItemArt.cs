using UnityEngine;

namespace SuperDodgeball
{
    internal static partial class DodgeWeaponArt
    {
        // Cross-sections are drawn around the pill's long axis. A scalloped end
        // replaces the intact cap at bite contact.
        static void Pill(CourtSketchMesh m,bool bitten)
        {
            const int n=20;
            float[] xs={-.58f,-.53f,-.43f,-.32f,-.025f,0,.03f,.32f,.43f,.53f,.58f};
            float[] rs={.02f,.15f,.235f,.258f,.258f,.264f,.258f,.258f,.235f,.15f,.02f};
            Vector3[] prev=new Vector3[n];
            int last=bitten?7:xs.Length-1;
            for(int i=0;i<=last;i++)
            {
                var ring=new Vector3[n];
                for(int j=0;j<n;j++)
                {
                    float theta=j*2*Mathf.PI/n;
                    float x=xs[i];
                    if(bitten && i==last)x=.26f+.048f*Mathf.Cos(theta*5)-.03f*Mathf.Sin(theta);
                    ring[j]=new Vector3(x,rs[i]*Mathf.Cos(theta),rs[i]*Mathf.Sin(theta));
                }
                if(i>0)for(int j=0;j<n;j++)
                {
                    int k=(j+1)%n;
                    m.Face(i<5?0xe9a637:i<7?0x704b48:0xffece0,prev[j],prev[k],ring[k],ring[j]);
                }
                if(i==last || i==0)for(int j=0;j<n;j++)
                {
                    int k=(j+1)%n;
                    var mid=new Vector3(bitten && i==last?.16f:xs[i],0,0);
                    m.Face(i==0?0xe9a637:0xf5bf83,mid,ring[i==0?k:j],ring[i==0?j:k]);
                }
                prev=ring;
            }
            for(int side=-1;side<=1;side+=2)
            {
                var z=.259f*side;
                m.Face(Ivory,new Vector3(-.26f,.12f,z),new Vector3(-.36f,-.015f,z),new Vector3(-.278f,-.015f,z),
                    new Vector3(-.30f,-.12f,z),new Vector3(-.15f,.042f,z),new Vector3(-.237f,.042f,z));
            }
        }
        static void Injector(CourtSketchMesh m)
        {
            Barrel(m,Sea,-.24f,.16f,.102f,0);
            Barrel(m,Ivory,-.27f,-.20f,.126f,0);Barrel(m,Ivory,.14f,.21f,.115f,0);
            Barrel(m,Gold,.21f,.30f,.064f,0);Barrel(m,Metal,.30f,.39f,.028f,0);
            Barrel(m,Metal,-.40f,-.26f,.035f,0);Barrel(m,Rose,-.44f,-.38f,.115f,0);
            for(int i=0;i<4;i++)m.Box(Ink,new Vector3(.10f,0,-.12f+.07f*i),new Vector3(.014f,.065f,.021f));
        }
    }
}
