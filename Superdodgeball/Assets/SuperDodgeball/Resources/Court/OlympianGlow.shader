Shader "SuperDodgeball/Olympian Corona"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha One
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Vertex { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Pixel { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Pixel vert(Vertex v)
            {
                Pixel o;
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.color=v.color; o.uv=v.uv; return o;
            }
            half4 frag(Pixel i):SV_Target
            {
                half rim=saturate(1-abs(i.uv.y*2-1));
                return half4(i.color.rgb*1.4h,i.color.a*rim*rim);
            }
            ENDHLSL
        }
    }
}
