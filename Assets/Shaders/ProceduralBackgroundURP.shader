Shader "Unlit/ProceduralBackground URP"
{
    Properties
    {
        [Header(Noise Settings)]
        _NoiseScale1 ("Large Noise Scale", Range(0.5, 10)) = 3.0
        _NoiseScale2 ("Small Noise Scale", Range(5, 50)) = 15.0
        _NoiseScale3 ("Fog Noise Scale", Range(0.1, 3)) = 0.5

        [Header(Blend Mode)]
        _BlendMode ("Blend Mode", Int) = 0
        _BlendStrength ("Blend Strength", Range(0, 1)) = 0.7

        [Header(Splash Effect)]
        _PowerValue ("Power (Splash Intensity)", Range(1, 8)) = 2.5
        _SplashThreshold ("Splash Threshold", Range(0, 1)) = 0.3

        [Header(Colors)]
        _Color1 ("Deep Blue", Color) = (0.05, 0.1, 0.3, 1)
        _Color2 ("Cyan Green", Color) = (0.1, 0.5, 0.4, 1)
        _Color3 ("Yellow", Color) = (0.9, 0.8, 0.2, 1)
        _Color4 ("Orange", Color) = (0.9, 0.4, 0.1, 1)
        _Color5 ("Dark Base", Color) = (0.02, 0.05, 0.1, 1)

        [Header(Animation)]
        _Speed1 ("Large Noise Speed", Range(0, 2)) = 0.15
        _Speed2 ("Small Noise Speed", Range(0, 2)) = 0.3
        _Speed3 ("Fog Speed", Range(0, 0.5)) = 0.05

        [Header(Fog Layer)]
        _FogOpacity ("Fog Opacity", Range(0, 1)) = 0.4

        [Header(Extra)]
        _Contrast ("Contrast", Range(0.5, 2)) = 1.2
        _Brightness ("Brightness", Range(0.5, 2)) = 1.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _NoiseScale1;
                float _NoiseScale2;
                float _NoiseScale3;
                int _BlendMode;
                float _BlendStrength;
                float _PowerValue;
                float _SplashThreshold;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float4 _Color5;
                float _Speed1;
                float _Speed2;
                float _Speed3;
                float _FogOpacity;
                float _Contrast;
                float _Brightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float2 random2(float2 st)
            {
                st = float2(dot(st, float2(127.1, 311.7)), dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }

            float gradientNoise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(dot(random2(i), f),
                         dot(random2(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                    lerp(dot(random2(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                         dot(random2(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x),
                    u.y
                );
            }

            float fbm(float2 st)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                float2 shift = float2(100.0, 100.0);

                for (int i = 0; i < 5; i++)
                {
                    value += amplitude * gradientNoise(st * frequency);
                    st = st * 2.0 + shift;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }

            float voronoi(float2 st)
            {
                float2 i_st = floor(st);
                float2 f_st = frac(st);

                float m_dist = 1.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float2 randP = random2(i_st + neighbor);
                        randP = 0.5 + 0.5 * sin(_TimeParameters.y * 0.5 + 6.2831 * randP);
                        float2 diff = neighbor + randP - f_st;
                        float dist = length(diff);
                        m_dist = min(m_dist, dist);
                    }
                }
                return m_dist;
            }

            float3 layeredNoise(float2 uv, float time)
            {
                float2 uv1 = uv * _NoiseScale1 + float2(time * _Speed1, -time * _Speed1 * 0.3);
                float noise1 = fbm(uv1) * 0.5 + 0.5;

                float2 uv2 = uv * _NoiseScale2 + float2(-time * _Speed2 * 0.7, time * _Speed2);
                float noise2 = voronoi(uv2);

                float2 uv3 = uv * _NoiseScale3 + float2(time * _Speed3, -time * _Speed3 * 0.5);
                float noise3 = fbm(uv3) * 0.5 + 0.5;

                return float3(noise1, noise2, noise3);
            }

            float applySplash(float noiseValue)
            {
                float powered = pow(noiseValue, _PowerValue);
                powered = smoothstep(_SplashThreshold, _SplashThreshold + 0.5, powered);
                return powered;
            }

            float4 applyGradient(float t)
            {
                t = saturate(t);

                float4 c1 = _Color1;
                float4 c2 = _Color2;
                float4 c3 = _Color3;
                float4 c4 = _Color4;
                float4 c5 = _Color5;

                float4 result;
                if (t < 0.25)
                    result = lerp(c5, c1, t / 0.25);
                else if (t < 0.5)
                    result = lerp(c1, c2, (t - 0.25) / 0.25);
                else if (t < 0.75)
                    result = lerp(c2, c3, (t - 0.5) / 0.25);
                else
                    result = lerp(c3, c4, (t - 0.75) / 0.25);

                return result;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float3 noises = layeredNoise(uv, _TimeParameters.y);

                float noise1 = noises.x;
                float noise2 = noises.y;
                float noise3 = noises.z;

                noise1 = applySplash(noise1);
                noise2 = applySplash(noise2);

                float finalNoise;
                if (_BlendMode == 0)
                {
                    finalNoise = (noise1 + noise2 * _BlendStrength) / (1.0 + _BlendStrength);
                }
                else
                {
                    finalNoise = noise1 * (noise2 * _BlendStrength + (1.0 - _BlendStrength));
                }

                finalNoise = lerp(finalNoise, noise3, _FogOpacity * 0.5);
                finalNoise = (finalNoise - 0.5) * _Contrast + 0.5;
                finalNoise *= _Brightness;

                half4 color = applyGradient(finalNoise);

                float glow = pow(finalNoise, 3.0) * 0.2;
                color.rgb += glow;

                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
