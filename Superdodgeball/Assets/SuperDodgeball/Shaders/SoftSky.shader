Shader "SuperDodgeball/SoftSky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.32, 0.65, 0.88, 1)
        _HorizonColor ("Horizon", Color) = (0.98, 0.93, 0.82, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float height = normalize(input.direction).y;
                float blend = smoothstep(-0.3, 0.45, height);
                return half4(lerp(_HorizonColor.rgb, _TopColor.rgb, blend), 1);
            }
            ENDHLSL
        }
    }
}
