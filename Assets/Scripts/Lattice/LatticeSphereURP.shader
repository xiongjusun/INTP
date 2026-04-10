Shader "Custom/LatticeSphereURP"
{
    Properties
    {
        _DarkColor ("Dark Color", Color) = (0.08,0.08,0.12,1)
        _BrightColor ("Bright Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,5)) = 1.5
        _Radius ("Radius", Float) = 2
        _MaxHeight ("Max Height", Float) = 1.5
        _Falloff ("Falloff", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DarkColor;
                float4 _BrightColor;
                float _EmissionStrength;
                float _Radius;
                float _MaxHeight;
                float _Falloff;
                int _ParticleCount;
            CBUFFER_END

            float4 _ParticlePositions[32];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float bright01 : TEXCOORD1;
            };

            float ComputeHeight(float3 worldCenter)
            {
                int particleCount = min((int)_ParticleCount, 32);
                float best = 0.0;

                [loop]
                for (int i = 0; i < 32; i++)
                {
                    if (i >= particleCount)
                        break;

                    float2 d = worldCenter.xz - _ParticlePositions[i].xz;
                    float dist = length(d);

                    if (dist > _Radius)
                        continue;

                    float t = saturate(1.0 - dist / _Radius);
                    float h = pow(t, _Falloff) * _MaxHeight;
                    best = max(best, h);
                }

                return best;
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                Varyings OUT;

                VertexPositionInputs centerInputs = GetVertexPositionInputs(float3(0, 0, 0));
                float3 centerWS = centerInputs.positionWS;
                float h = ComputeHeight(centerWS);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS.y += h;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.bright01 = saturate(h / max(_MaxHeight, 0.0001));

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 lightDir = normalize(float3(0.35, 1.0, 0.25));
                float NdotL = saturate(dot(normalize(IN.normalWS), lightDir));

                float3 baseColor = lerp(_DarkColor.rgb, _BrightColor.rgb, IN.bright01);
                float3 lit = baseColor * (0.25 + 0.75 * NdotL);
                float3 emission = baseColor * (_EmissionStrength * IN.bright01);

                return half4(lit + emission, 1.0);
            }
            ENDHLSL
        }
    }
}