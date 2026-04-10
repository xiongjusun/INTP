// Copyright (c) 2026 Dreamcore XR Labs

Shader "Gsplat/Standard"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            ZWrite Off
            Blend One OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require compute
            #pragma use_dxc
            #pragma multi_compile SH_BANDS_0 SH_BANDS_1 SH_BANDS_2 SH_BANDS_3

            #include "UnityCG.cginc"
            #include "Gsplat.hlsl"
            #include "GsplatEffects.hlsl"
            #include "GsplatRelighting.hlsl"

            bool _GammaToLinear;
            int _SplatCount;
            int _SplatInstanceSize;
            int _SHDegree;
            float4x4 _MATRIX_M;
            StructuredBuffer<uint> _OrderBuffer;
            StructuredBuffer<float3> _PositionBuffer;
            StructuredBuffer<float3> _ScaleBuffer;
            StructuredBuffer<float4> _RotationBuffer;
            StructuredBuffer<float4> _ColorBuffer;

            #ifndef SH_BANDS_0
            StructuredBuffer<float3> _SHBuffer;
            #endif
            int _EffectType;
            float _EffectIntensity;
            float _EffectTime ;
            float3 _EffectWindDir;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _waveSpeed;
            float _blendScale;
            float _lightWaveAmplitude;
            float _lightWaveFrequency;
            float _lightWaveSpeed; 
            float _GlitterDensity =0.3f;
            float _DissolveDriftSpeed = 0.3f;
            float _burnDuration = 2.0f;
            struct appdata
            {
                float4 vertex : POSITION;
                #if !defined(UNITY_INSTANCING_ENABLED) && !defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && !defined(UNITY_STEREO_INSTANCING_ENABLED)
                uint instanceID : SV_InstanceID;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            bool InitSource(appdata v, out SplatSource source)
            {
                #if !defined(UNITY_INSTANCING_ENABLED) && !defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && !defined(UNITY_STEREO_INSTANCING_ENABLED)
                source.order = v.instanceID * _SplatInstanceSize + asuint(v.vertex.z);
                #else
                source.order = unity_InstanceID * _SplatInstanceSize + asuint(v.vertex.z);
                #endif

                if (source.order >= _SplatCount)
                    return false;

                source.id = _OrderBuffer[source.order];
                source.cornerUV = float2(v.vertex.x, v.vertex.y);
                return true;
            }

            bool InitCenter(float3 modelCenter, out SplatCenter center)
            {
                float4x4 modelView = mul(UNITY_MATRIX_V, _MATRIX_M);
                float4 centerView = mul(modelView, float4(modelCenter, 1.0));
                if (centerView.z > 0.0)
                {
                    return false;
                }
                float4 centerProj = mul(UNITY_MATRIX_P, centerView);
                centerProj.z = clamp(centerProj.z, -abs(centerProj.w), abs(centerProj.w));
                center.view = centerView.xyz / centerView.w;
                center.proj = centerProj;
                center.projMat00 = UNITY_MATRIX_P[0][0];
                center.modelView = modelView;
                return true;
            }

            // sample covariance vectors
            SplatCovariance ReadCovariance(SplatSource source)
            {
                float4 quat = _RotationBuffer[source.id];
                float3 scale = _ScaleBuffer[source.id];
                return CalcCovariance(quat, scale);
            }

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color: COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float3 p)
            {
                // Simple hash function for deterministic noise
                return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                SplatSource source;
                if (!InitSource(v, source))
                {
                    o.vertex = discardVec;
                    return o;
                }

                float3 modelCenter = _PositionBuffer[source.id];

                float3 localCenter = modelCenter;
                float3 localScales = _ScaleBuffer[source.id];
                float4 localColor  = _ColorBuffer[source.id];
                localColor.rgb = localColor.rgb * SH_C0 + 0.5;

                ApplyGsplatEffect(localCenter, localScales, localColor, 
                                  _EffectType, _EffectTime, _EffectIntensity, _EffectWindDir,
                                  _WaveAmplitude, _WaveFrequency,_waveSpeed, _blendScale,
                                  _lightWaveAmplitude, _lightWaveFrequency, _lightWaveSpeed, 
                                  _GlitterDensity, _DissolveDriftSpeed, _burnDuration);

                ApplyRelighting(localCenter, localColor, _MATRIX_M);


                SplatCenter modifiedCenter;
                if (!InitCenter(localCenter, modifiedCenter))
                {
                    o.vertex = discardVec;
                    return o;
                }       
                
                SplatCovariance cov = CalcCovariance(_RotationBuffer[source.id], localScales); //ReadCovariance(source);
                SplatCorner corner;
                if (!InitCorner(source, cov, modifiedCenter, corner))
                {
                    o.vertex = discardVec;
                    return o;
                }
                ClipCorner(corner, localColor.w);
                
                // Lighting SH
                #ifndef SH_BANDS_0
                float3 dir = normalize(mul(modifiedCenter.view, (float3x3)modifiedCenter.modelView));
                float3 sh[SH_COEFFS];
                for (int i = 0; i < SH_COEFFS; i++)
                    sh[i] = _SHBuffer[source.id * SH_COEFFS + i];
                localColor.rgb += EvalSH(sh, dir, _SHDegree);
                #endif

                o.vertex = modifiedCenter.proj + float4(corner.offset.x, _ProjectionParams.x * corner.offset.y, 0, 0);
                o.color = float4(max(localColor.rgb, float3(0, 0, 0)), localColor.a);
                o.uv = corner.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float A = dot(i.uv, i.uv);
                if (A > 1.0) discard;
                float alpha = exp(-A * 4.0) * i.color.a;
                if (alpha < 1.0 / 255.0) discard;
                if (_GammaToLinear)
                    return float4(GammaToLinearSpace(i.color.rgb) * alpha, alpha);
                return float4(i.color.rgb * alpha, alpha);
            }
            ENDHLSL


        }
    }
}