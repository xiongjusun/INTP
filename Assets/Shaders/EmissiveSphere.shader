Shader "Custom/EmissiveSphere"
{
    Properties
    {
        [Header(Base Color Settings)]
        _BaseColor ("Base Color", Color) = (0.2, 0.2, 0.2, 1)
        
        [Header(Emission Settings)]
        [Toggle(_EMISSION)] _UseEmission ("Enable Emission", Float) = 1
        _EmissionColor1 ("Emission Color 1", Color) = (1, 0.5, 0, 1)
        _EmissionColor2 ("Emission Color 2", Color) = (0, 0.5, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 2.0
        _EmissionSpeed ("Color Transition Speed", Range(0.1, 5)) = 1.0
        
        [Header(Runtime Adjustable Properties)]
        [HDR] _EmissionColor1Runtime ("Runtime Color 1 (HDR)", Color) = (1, 0.5, 0, 1)
        [HDR] _EmissionColor2Runtime ("Runtime Color 2 (HDR)", Color) = (0, 0.5, 1, 1)
        _EmissionIntensityRuntime ("Runtime Emission Intensity", Range(0, 10)) = 2.0
        
        [Header(Surface Settings)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        _Metallic ("Metallic", Range(0, 1)) = 0.1
        
        [Header(Performance)]
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows ("Receive Shadows", Float) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            // 性能优化：使用multi-compile变体来减少变体数量
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            
            // 实例化支持 - 关键性能优化！
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            // 获取主光源信息
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // ============================================
            // 纹理和采样器定义
            // ============================================
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            // ============================================
            // 实例化属性块 - GPU Instancing支持
            // ============================================
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor1;
                float4 _EmissionColor2;
                float _EmissionIntensity;
                float _EmissionSpeed;
                
                // 运行时可调整属性
                float4 _EmissionColor1Runtime;
                float4 _EmissionColor2Runtime;
                float _EmissionIntensityRuntime;
                
                float _Smoothness;
                float _Metallic;
            CBUFFER_END
            
            // ============================================
            // 结构体定义
            // ============================================
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
                float3 viewDirWS    : TEXCOORD3;
                float4 emission     : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // ============================================
            // 函数前置声明
            // ============================================
            float4 CalculateEmission();

            // ============================================
            // 顶点着色器
            // ============================================
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // 实例化支持
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // 顶点位置变换
                const float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                
                // 法线变换
                const float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalWS = normalWS;
                
                // 视角方向
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                
                // 纹理坐标
                output.uv = input.texcoord;
                
                // 计算自发光
                output.emission = CalculateEmission();
                
                return output;
            }
            
            // ============================================
            // 自发光计算函数
            // ============================================
            float4 CalculateEmission()
            {
            #ifdef _EMISSION
                // 使用运行时值或编辑器值
                const float4 color1 = _EmissionColor1Runtime.a > 0 ? _EmissionColor1Runtime : _EmissionColor1;
                const float4 color2 = _EmissionColor2Runtime.a > 0 ? _EmissionColor2Runtime : _EmissionColor2;
                
                // 使用sin函数创建平滑的颜色渐变
                // 0到1之间平滑过渡
                const float t = (sin(_Time.y * _EmissionSpeed) + 1.0) * 0.5;
                
                // 插值混合两种颜色
                const float4 emissionColor = lerp(color1, color2, t);
                
                // 应用强度并使用HDR格式存储
                return emissionColor * _EmissionIntensityRuntime;
            #else
                return float4(0, 0, 0, 1);
            #endif
            }
            
            // ============================================
            // 片元着色器
            // ============================================
            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half3 finalColor = _BaseColor.rgb + input.emission.rgb;
                return half4(finalColor, 1.0h);
            }
            
            ENDHLSL
        }
        
        // 阴影Pass
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        
        // 深度 Prepass
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        
        // 深度雾气Pass (可选)
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
