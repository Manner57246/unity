Shader "SuperDodgeball/Neighbourhood"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct MeshVertex { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct Raster { float4 position : SV_POSITION; half3 colour : COLOR0; };
            Raster vert(MeshVertex v)
            {
                Raster o;
                o.position = UnityObjectToClipPos(v.vertex);
                float3 n = UnityObjectToWorldNormal(v.normal);
                float diffuse = saturate(dot(n, normalize(float3(-0.48, 0.83, -0.32))));
                half3 c = v.color.rgb * lerp(half3(.75,.78,.84), half3(1.055,1.035,1), diffuse);
                #ifndef UNITY_COLORSPACE_GAMMA
                    c = GammaToLinearSpace(c);
                #endif
                o.colour = c;
                return o;
            }
            half4 frag(Raster i) : SV_Target { return half4(i.colour,1); }
            ENDCG
        }
    }
}
