Shader "SuperDodgeball/Referee Half Warning"
{
    Properties { _Strength ("Warning strength", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float _Strength;
            CBUFFER_END
            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.uv=i.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float2 d=min(i.uv,1-i.uv)*float2(26,16);
                float edge=1-smoothstep(.04,.13,min(d.x,d.y));
                float tooth=1-smoothstep(.08,.14,abs(d.y-.36+abs(frac(i.uv.x*13)-.5)*.73));
                float ink=max(edge,tooth);
                return half4(lerp(half3(.88,.06,.075),half3(1,.21,.115),ink),_Strength*lerp(.39,.89,ink));
            }
            ENDHLSL
        }
    }
}
