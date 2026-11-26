Shader "SuperDodgeball/Hit cloud volume"
{
    Properties
    {
        _CloudVolume ("Density, light, and turbulence", 3D) = "white" {}
        _Age ("Normalized age", Float) = 0
        _Seed ("Billow variation", Float) = 0
        _Dimensions ("Volume proportions", Vector) = (3.168,1.496,.968,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE3D(_CloudVolume);
            SAMPLER(sampler_CloudVolume);
            CBUFFER_START(UnityPerMaterial)
                float4 _Dimensions;
                float _Age, _Seed;
            CBUFFER_END

            struct Pixel { float4 positionCS : SV_POSITION; float3 entry : TEXCOORD0; };
            Pixel Vert(float4 positionOS : POSITION)
            {
                Pixel o;
                o.positionCS = TransformObjectToHClip(positionOS.xyz);
                o.entry = positionOS.xyz;
                return o;
            }

            half4 Frag(Pixel i) : SV_Target
            {
                float3 dir = normalize(i.entry - TransformWorldToObject(GetCameraPositionWS()));
                float3 safeDir = (step(0,dir)*2-1)*max(abs(dir),.00001);
                float3 travel = (.5*sign(safeDir)-i.entry)/safeDir;
                float end = min(travel.x,min(travel.y,travel.z));
                float3 entryWS = TransformObjectToWorld(i.entry);
                float3 rayWS = TransformObjectToWorldDir(dir,false);
                float sceneZ = LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS)),_ZBufferParams);
                float eyeRate = -mul((float3x3)UNITY_MATRIX_V,rayWS).z;
                end = min(end,(sceneZ + TransformWorldToView(entryWS).z) / max(eyeRate,.00001));
                if (end <= 0) discard;

                const int steps = 36;
                float stride = end/steps;
                float opticalStep = stride*length(dir*_Dimensions.xyz)*20;
                float3 p = i.entry + .5 + dir*(stride*.5);
                float3 advance = dir*stride;
                float breakup = smoothstep(.54,.99,_Age);
                float fade = 1-smoothstep(.77,1,_Age);
                half4 cloud = 0;
                [loop] for (int n=0; n<steps; n++,p+=advance)
                {
                    float3 uvw = p;
                    // Late in the burst, different pockets of air lift the letter strokes apart.
                    uvw.y -= breakup*(.052+.060*sin(p.x*21+_Seed+p.z*8));
                    uvw.x += breakup*.017*sin(p.y*17+p.z*7+_Seed);
                    half4 field = SAMPLE_TEXTURE3D_LOD(_CloudVolume,sampler_CloudVolume,uvw,0);
                    float density = max(0,field.r - breakup*(.42+.76*field.b));
                    float alpha = 1-exp(-density*opticalStep*fade);
                    half light = (.17h+.83h*field.a)*field.g;
                    half3 base = lerp(half3(.115,.22,.28),half3(.53,.66,.70),smoothstep(.035,.40,light));
                    half3 lit = lerp(base,half3(1.13,1.02,.85),smoothstep(.31,.79,light));
                    cloud.rgb += (1-cloud.a)*alpha*lit;
                    cloud.a += (1-cloud.a)*alpha;
                    if (cloud.a>.992h) break;
                }
                return cloud;
            }
            ENDHLSL
        }
    }
}
