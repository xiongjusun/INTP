Shader "Unlit/DreamCoreGrass URP"
{
    Properties
    {
        [Header(Base Colors)]
        _GrassBase ("Grass Base", Color) = (0.15, 0.35, 0.2, 1)
        _GrassTip ("Grass Tip", Color) = (0.4, 0.7, 0.5, 1)
        _FogColor ("Fog Color", Color) = (0.6, 0.8, 0.9, 1)

        [Header(Dream Effect)]
        _HazeIntensity ("Haze Intensity", Range(0, 1)) = 0.6
        _GlowStrength ("Dream Glow", Range(0, 1)) = 0.4
        _Desaturation ("Dream Desaturate", Range(0, 1)) = 0.2

        [Header(Wind)]
        _WindSpeed ("Wind Speed", Range(0, 1)) = 0.3
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.1
        _WindDirX ("Wind Dir X", Range(-1, 1)) = 0.5
        _WindDirY ("Wind Dir Y", Range(-1, 1)) = 0.2

        [Header(Grass Pattern)]
        _GrassDensity ("Grass Density", Range(10, 100)) = 40
        _GrassHeight ("Grass Height", Range(0.05, 0.5)) = 0.2

        [Header(Backlight)]
        _BacklightColor ("Backlight Color", Color) = (0.8, 0.95, 1.0, 1)
        _BacklightIntensity ("Backlight Intensity", Range(0, 2)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GrassBase;
                float4 _GrassTip;
                float4 _FogColor;
                float _HazeIntensity;
                float _GlowStrength;
                float _Desaturation;
                float _WindSpeed;
                float _WindStrength;
                float _WindDirX;
                float _WindDirY;
                float _GrassDensity;
                float _GrassHeight;
                float4 _BacklightColor;
                float _BacklightIntensity;
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
                float3 worldPos     : TEXCOORD1;
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(hash(i), hash(i + float2(1.0, 0.0)), u.x),
                    lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), u.x),
                    u.y
                );
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            float grassBlade(float2 uv, float2 id, float time)
            {
                float h = hash(id);

                float2 windDir = normalize(float2(_WindDirX, _WindDirY));
                float windWave = sin(time * _WindSpeed + h * 6.28 + id.x * 0.1 + id.y * 0.1);
                float windWave2 = sin(time * _WindSpeed * 0.7 + h * 3.14 + id.y * 0.05);

                float combinedWave = windWave * 0.7 + windWave2 * 0.3;

                float sway = combinedWave * _WindStrength * (1.0 - uv.y);

                float bladeWidth = 0.08 * (1.0 - uv.y * 0.7);
                float center = 0.5 + sway + (h - 0.5) * 0.15;

                float blade = smoothstep(center - bladeWidth, center, uv.x) *
                              smoothstep(center + bladeWidth, center, uv.x);

                float heightMask = smoothstep(_GrassHeight * h, _GrassHeight * h * 0.8, uv.y);

                return blade * heightMask;
            }

            float grassLayer(float2 uv, float time, float scale)
            {
                float2 scaledUV = uv * scale;
                float2 id = floor(scaledUV);
                float2 localUV = frac(scaledUV);

                float blade = grassBlade(localUV, id, time);

                float neighbor = 0.0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(float(x), float(y));
                        float2 neighborID = id + offset;
                        float2 neighborLocal = localUV - offset;

                        float h = hash(neighborID);
                        float nBlade = grassBlade(neighborLocal, neighborID, time);
                        neighbor = max(neighbor, nBlade * h);
                    }
                }

                return max(blade, neighbor * 0.6);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClipPos(input.positionOS);
                output.uv = input.uv;
                output.worldPos = mul(unity_ObjectToWorld, input.positionOS).xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                float grass1 = grassLayer(uv, time, _GrassDensity);
                float grass2 = grassLayer(uv + float2(0.1, 0.2), time * 1.1, _GrassDensity * 0.8);
                float grass3 = grassLayer(uv + float2(0.05, 0.1), time * 0.9, _GrassDensity * 1.2);

                float grassMask = max(max(grass1, grass2 * 0.7), grass3 * 0.5);

                float heightGradient = fbm(uv * 5.0 + time * 0.1);
                half3 grassColor = lerp(_GrassBase.rgb, _GrassTip.rgb, heightGradient);

                float fogNoise = fbm(uv * 3.0 + time * 0.05);
                half3 fog = lerp(_FogColor.rgb, _FogColor.rgb * 1.2, fogNoise);

                half3 color = lerp(fog, grassColor, grassMask);

                float backlight = grass1 * 0.8 + grass2 * 0.5 + grass3 * 0.3;
                float backlightMask = pow(1.0 - uv.y, 2.0) * 0.5;
                color += _BacklightColor.rgb * backlight * _BacklightIntensity * backlightMask;

                float haze = fbm(uv * 8.0 + time * 0.02) * _HazeIntensity;
                color = lerp(color, fog, haze * 0.3);

                float dreamGlow = pow(grassMask, 2.0) * _GlowStrength;
                color += half3(0.1, 0.15, 0.1) * dreamGlow;

                float luma = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(color, half3(luma, luma, luma), _Desaturation);

                float vignette = 1.0 - length(uv - 0.5) * 0.5;
                color *= vignette;

                float alpha = max(grassMask * 0.8, fogNoise * 0.3);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
