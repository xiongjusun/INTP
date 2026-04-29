Shader "Unlit/DreamGrassSimple"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.2, 0.4, 0.25, 1)
        _TipColor ("Tip Color", Color) = (0.5, 0.8, 0.5, 1)
        _FogColor ("Fog/Background", Color) = (0.85, 0.9, 0.95, 1)

        [Header(Grass Settings)]
        _GrassCount ("Grass Blades", Range(50, 500)) = 200
        _GrassHeight ("Height", Range(0.1, 1)) = 0.6

        [Header(Wind)]
        _WindSpeed ("Wind Speed", Range(0, 2)) = 0.5
        _WindStrength ("Wind Strength", Range(0, 0.3)) = 0.1

        [Header(Dream Effect)]
        _Haze ("Haze", Range(0, 1)) = 0.4
        _Glow ("Glow", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _BaseColor, _TipColor, _FogColor;
            float _GrassCount, _GrassHeight;
            float _WindSpeed, _WindStrength;
            float _Haze, _Glow;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise1D(float p)
            {
                float i = floor(p);
                float f = frac(p);
                return lerp(rand(float2(i, 0)), rand(float2(i + 1.0, 0)), smoothstep(0.0, 1.0, f));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float grassAtPos(float2 pos, float time)
            {
                float result = 0.0;

                for (float i = 0.0; i < 3.0; i++)
                {
                    float2 samplePos = pos * (_GrassCount + i * 30.0);
                    float2 cellId = floor(samplePos);
                    float2 cellUV = frac(samplePos);

                    float randVal = rand(cellId + i * 100.0);

                    float windPhase = randVal * 6.28 + time * _WindSpeed + cellId.x * 0.1;
                    float wind = sin(windPhase) * _WindStrength;

                    float bladeX = 0.5 + wind * (1.0 - cellUV.y) * _GrassHeight;
                    float bladeWidth = 0.15 * (1.0 - cellUV.y * 0.8);

                    float blade = smoothstep(bladeX - bladeWidth, bladeX, cellUV.x) *
                                  smoothstep(bladeX + bladeWidth, bladeX, cellUV.x);

                    float heightMask = smoothstep(_GrassHeight * randVal, _GrassHeight * randVal * 0.7, cellUV.y);

                    result = max(result, blade * heightMask * randVal);
                }

                return result;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;

                float grass = grassAtPos(uv, time);
                float grassFar = grassAtPos(uv * 0.5, time * 1.2);

                float grassMask = max(grass, grassFar * 0.5);

                float heightFade = smoothstep(0.0, 0.3, uv.y);
                float fogBlend = 1.0 - grassMask * heightFade;

                float3 color = lerp(_BaseColor.rgb, _TipColor.rgb, heightFade);
                color = lerp(_FogColor.rgb, color, grassMask);

                float backlight = pow(grassMask, 2.0) * 0.3;
                color += float3(0.1, 0.15, 0.1) * backlight;

                float hazeNoise = noise1D(uv.x * 50.0 + time * 0.5) * noise1D(uv.y * 30.0 - time * 0.3);
                color = lerp(color, _FogColor.rgb * 1.1, hazeNoise * _Haze * 0.3);

                float glow = pow(grassMask, 3.0) * _Glow;
                color += float3(0.05, 0.08, 0.05) * glow;

                float vignette = 1.0 - length(uv - 0.5) * 0.8;
                color *= vignette;

                float alpha = max(grassMask * 0.9, fogBlend * 0.2);

                return float4(color, alpha);
            }
            ENDCG
        }
    }
}
