Shader "Unlit/DreamcoreBackgroundURP"
{
    Properties
    {
        [Header(Noise Layers)]
        _NoiseScale1("Large Noise Scale", Range(0.5, 8)) = 2.5
        _NoiseScale2("Medium Noise Scale", Range(5, 30)) = 12.0
        _NoiseScale3("Small Noise Scale", Range(20, 100)) = 50.0
        _GrainIntensity("Grain Intensity", Range(0, 1)) = 0.3

        [Header(Drift Effect)]
        _DriftStrength("Drift Strength", Range(0, 0.5)) = 0.1
        _DriftSpeed("Drift Speed", Range(0, 2)) = 0.3

        [Header(Distortion)]
        _DistortionStrength("Distortion Strength", Range(0, 0.1)) = 0.02
        _WaveFrequency("Wave Frequency", Range(0.1, 5)) = 1.5

        [Header(Colors)]
        _Color1("Deep Void", Color) = (0.02, 0.02, 0.08, 1)
        _Color2("Ethereal Purple", Color) = (0.3, 0.1, 0.4, 1)
        _Color3("Sickly Green", Color) = (0.2, 0.5, 0.3, 1)
        _Color4("Faded Pink", Color) = (0.6, 0.3, 0.4, 1)
        _Color5("Sickly Yellow", Color) = (0.7, 0.6, 0.2, 1)

        [Header(Chromatic)]
        _ChromaticStrength("Chromatic Strength", Range(0, 0.02)) = 0.008

        [Header(Animation)]
        _Speed1("Layer 1 Speed", Range(0, 1)) = 0.1
        _Speed2("Layer 2 Speed", Range(0, 1)) = 0.2
        _Speed3("Layer 3 Speed", Range(0, 3)) = 1.5

        [Header(Blend)]
        _BlendMode("Blend Mode", Float) = 0
        _BlendStrength("Blend Strength", Range(0, 1)) = 0.6

        [Header(Post Effects)]
        _Contrast("Contrast", Range(0.5, 2)) = 1.3
        _Brightness("Brightness", Range(0.5, 2)) = 1.0
        _Vignette("Vignette", Range(0, 2)) = 1.2
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
                float _GrainIntensity;
                float _DriftStrength;
                float _DriftSpeed;
                float _DistortionStrength;
                float _WaveFrequency;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float4 _Color5;
                float _ChromaticStrength;
                float _Speed1;
                float _Speed2;
                float _Speed3;
                float _BlendMode;
                float _BlendStrength;
                float _Contrast;
                float _Brightness;
                float _Vignette;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.13);
                p3 = p3 + dot(p3, p3.yzx + 3.333);
                return frac((p3.x + p3.y) * p3.z);
            }

            float hash2(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 = p3 + dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float grainNoise(float2 p, float t)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float ts = t * 10.0;
                float tc = t * 13.0;
                float tp = t * 17.0;
                float2 s1 = float2(sin(ts), cos(ts)) * 100.0;
                float2 s2 = float2(sin(tc), cos(tp)) * 200.0;

                float2 g1 = i + s1;
                float2 g2 = i + float2(1.0, 0.0) + s1;
                float2 g3 = i + float2(0.0, 1.0) + s2;
                float2 g4 = i + float2(1.0, 1.0) + s2;

                return lerp(
                    lerp(hash(g1), hash(g2), f.x),
                    lerp(hash(g3), hash(g4), f.x),
                    f.y
                );
            }

            float fbm(float2 st)
            {
                float value = 0.0;
                float amp = 0.5;
                float freq = 1.0;

                for (int i = 0; i < 6; i++)
                {
                    value = value + amp * noise(st * freq);
                    amp = amp * 0.5;
                    freq = freq * 2.0;
                    st = st * 2.0 + float2(100.0, 100.0);
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
                        float fx = float(x);
                        float fy = float(y);
                        float2 neighbor = float2(fx, fy);
                        float2 ip = i_st + neighbor;
                        float h1 = hash(ip);
                        float h2 = hash2(ip);
                        float2 randP = float2(h1, h2);
                        float sinArg = _TimeParameters.y * 0.5 + 6.2831 * h1;
                        float sinArg2 = _TimeParameters.y * 0.5 + 6.2831 * h2;
                        randP.x = 0.5 + 0.5 * sin(sinArg);
                        randP.y = 0.5 + 0.5 * sin(sinArg2);
                        float2 diff = neighbor + randP - f_st;
                        float dist = length(diff);
                        m_dist = min(m_dist, dist);
                    }
                }
                return m_dist;
            }

            float flowNoise(float2 p, float t)
            {
                float2 flow = float2(
                    sin(t * 0.3 + p.x * 2.0) * 0.5,
                    cos(t * 0.2 + p.y * 2.0) * 0.5
                );

                float2 p1 = p + flow * _DriftStrength + float2(t * _DriftSpeed, t * _DriftSpeed);
                float2 p2 = p * 1.5 - flow * _DriftStrength * 0.5 + float2(t * _DriftSpeed * 0.7, t * _DriftSpeed * 0.7);

                float n1 = noise(p1);
                float n2 = noise(p2);

                return (n1 + n2) * 0.5;
            }

            float3 dreamcoreGradient(float t, float4 c1, float4 c2, float4 c3, float4 c4, float4 c5)
            {
                t = saturate(t);

                float3 result;
                if (t < 0.2)
                    result = lerp(c1.rgb, c2.rgb, t / 0.2);
                else if (t < 0.4)
                    result = lerp(c2.rgb, c3.rgb, (t - 0.2) / 0.2);
                else if (t < 0.6)
                    result = lerp(c3.rgb, c4.rgb, (t - 0.4) / 0.2);
                else if (t < 0.8)
                    result = lerp(c4.rgb, c5.rgb, (t - 0.6) / 0.2);
                else
                    result = lerp(c5.rgb, c1.rgb * 0.5, (t - 0.8) / 0.2);

                return result;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                float2 uv = input.uv;
                float time = _TimeParameters.y;

                float distort = noise(uv * _WaveFrequency + float2(time * 0.1, time * 0.1)) * _DistortionStrength;
                float2 distortedUV;
                distortedUV.x = uv.x + sin(uv.y * 10.0 + time) * distort;
                distortedUV.y = uv.y + cos(uv.x * 10.0 + time * 1.3) * distort;

                float2 uv1 = distortedUV * _NoiseScale1 + float2(time * _Speed1, -time * _Speed1 * 0.3);
                float n1 = fbm(uv1);

                float2 uv2 = distortedUV * _NoiseScale2 + float2(-time * _Speed2 * 0.7, time * _Speed2);
                float n2 = voronoi(uv2);
                n2 = pow(n2, 1.5);

                float2 uv3 = distortedUV * _NoiseScale3 + float2(time * _Speed3 * 0.1, -time * _Speed3 * 0.15);
                float n3 = grainNoise(uv3, time);

                float grain = noise(uv * 500.0 + float2(time * 5.0, time * 5.0)) * _GrainIntensity;

                float2 uvFlow = distortedUV * 3.0 + float2(time * 0.2, time * 0.2);
                float flow = flowNoise(uvFlow, time);

                float finalNoise;
                if (_BlendMode < 0.5)
                {
                    finalNoise = (n1 * 0.4 + n2 * _BlendStrength + flow * 0.3) / (1.0 + _BlendStrength);
                }
                else
                {
                    finalNoise = n1 * (n2 * _BlendStrength + (1.0 - _BlendStrength)) * flow;
                }

                finalNoise = finalNoise + grain + n3 * 0.1;
                finalNoise = (finalNoise - 0.5) * _Contrast + 0.5;
                finalNoise = finalNoise * _Brightness;

                float3 color = dreamcoreGradient(finalNoise, _Color1, _Color2, _Color3, _Color4, _Color5);

                float2 uvCenter = float2(0.5, 0.5);
                float2 dir = normalize(uv - uvCenter) * _ChromaticStrength;
                float offset = sin(time * 0.5) * 0.001;
                float r = noise(uv + dir * (1.0 + offset));
                float g = noise(uv);
                float b = noise(uv - dir * (1.0 - offset));
                float3 chrom = float3(r, g, b);
                color = lerp(color, color * chrom * 1.5, 0.3);

                float glow = pow(finalNoise, 4.0) * 0.3;
                color = color + glow * _Color4.rgb * 0.5;

                float3 coloredGrain;
                coloredGrain.x = noise(uv * 800.0 + float2(time * 3.0, time * 3.0));
                coloredGrain.y = noise(uv * 800.0 + float2(time * 3.0 + 100.0, time * 3.0 + 100.0));
                coloredGrain.z = noise(uv * 800.0 + float2(time * 3.0 + 200.0, time * 3.0 + 200.0));
                color = color + coloredGrain * grain * 2.0;

                float2 vigUV = uv - uvCenter;
                float vig = 1.0 - dot(vigUV, vigUV) * _Vignette;
                vig = saturate(vig);
                color = color * vig;

                float lum = dot(color, float3(0.299h, 0.587h, 0.114h));
                color = lerp(float3(lum, lum, lum), color, 0.8h);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
