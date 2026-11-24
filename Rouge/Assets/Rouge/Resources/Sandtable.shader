Shader "Rouge/Sandtable"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct InVertex { float4 position : POSITION; half4 color : COLOR; };
            struct OutVertex { float4 position : SV_POSITION; half4 color : COLOR; };
            OutVertex Vert(InVertex v)
            {
                OutVertex o;
                o.position = TransformObjectToHClip(v.position.xyz);
                o.color = v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = SRGBToLinear(o.color.rgb);
                #endif
                return o;
            }
            half4 Frag(OutVertex i) : SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
