Shader "Unlit/DreamCoreGrass"
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
        }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _GrassBase, _GrassTip, _FogColor;
            float _HazeIntensity, _GlowStrength, _Desaturation;
            float _WindSpeed, _WindStrength;
            float _WindDirX, _WindDirY;
            float _GrassDensity, _GrassHeight;
            float4 _BacklightColor;
            float _BacklightIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;

                float grass1 = grassLayer(uv, time, _GrassDensity);
                float grass2 = grassLayer(uv + float2(0.1, 0.2), time * 1.1, _GrassDensity * 0.8);
                float grass3 = grassLayer(uv + float2(0.05, 0.1), time * 0.9, _GrassDensity * 1.2);

                float grassMask = max(max(grass1, grass2 * 0.7), grass3 * 0.5);

                float heightGradient = fbm(uv * 5.0 + time * 0.1);
                float3 grassColor = lerp(_GrassBase.rgb, _GrassTip.rgb, heightGradient);

                float3 fogNoise = fbm(uv * 3.0 + time * 0.05);
                float3 fog = lerp(_FogColor.rgb, _FogColor.rgb * 1.2, fogNoise);

                float3 color = lerp(fog, grassColor, grassMask);

                float backlight = grass1 * 0.8 + grass2 * 0.5 + grass3 * 0.3;
                float backlightMask = pow(1.0 - uv.y, 2.0) * 0.5;
                color += _BacklightColor.rgb * backlight * _BacklightIntensity * backlightMask;

                float haze = fbm(uv * 8.0 + time * 0.02) * _HazeIntensity;
                color = lerp(color, fog, haze * 0.3);

                float dreamGlow = pow(grassMask, 2.0) * _GlowStrength;
                color += float3(0.1, 0.15, 0.1) * dreamGlow;

                float luma = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(color, float3(luma, luma, luma), _Desaturation);

                float vignette = 1.0 - length(uv - 0.5) * 0.5;
                color *= vignette;

                float alpha = max(grassMask * 0.8, fogNoise * 0.3);

                return float4(color, alpha);
            }
            ENDCG
        }
    }
}
