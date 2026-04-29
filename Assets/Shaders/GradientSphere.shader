Shader "Unlit/GradientSphere"
{
    Properties
    {
        [Header(Color Gradient)]
        _Color1 ("Deep Blue", Color) = (0.05, 0.1, 0.3, 1)
        _Color2 ("Cyan Green", Color) = (0.1, 0.5, 0.4, 1)
        _Color3 ("Yellow", Color) = (0.9, 0.8, 0.2, 1)
        _Color4 ("Orange", Color) = (0.9, 0.4, 0.1, 1)

        [Header(Animation)]
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 0.3
        _FlowDirection ("Flow Direction", Vector) = (1, 0.5, 0, 0)

        [Header(Sphere)]
        _SphereRadius ("Radius", Range(0, 1)) = 0.4
        _SphereSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.1

        [Header(Extra)]
        _Contrast ("Contrast", Range(0.5, 2)) = 1.2
        _Brightness ("Brightness", Range(0.5, 2)) = 1.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color1, _Color2, _Color3, _Color4;
            float _FlowSpeed;
            float4 _FlowDirection;
            float _SphereRadius;
            float _SphereSoftness;
            float _Contrast, _Brightness;

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

            float3 applyLoopingGradient(float t)
            {
                t = fmod(t, 1.0);
                if (t < 0) t += 1.0;

                float4 c1 = _Color1;
                float4 c2 = _Color2;
                float4 c3 = _Color3;
                float4 c4 = _Color4;

                float3 result;

                if (t < 0.25)
                {
                    float localT = t / 0.25;
                    localT = smoothstep(0, 1, localT);
                    result = lerp(c1.rgb, c2.rgb, localT);
                }
                else if (t < 0.5)
                {
                    float localT = (t - 0.25) / 0.25;
                    localT = smoothstep(0, 1, localT);
                    result = lerp(c2.rgb, c3.rgb, localT);
                }
                else if (t < 0.75)
                {
                    float localT = (t - 0.5) / 0.25;
                    localT = smoothstep(0, 1, localT);
                    result = lerp(c3.rgb, c4.rgb, localT);
                }
                else
                {
                    float localT = (t - 0.75) / 0.25;
                    localT = smoothstep(0, 1, localT);
                    result = lerp(c4.rgb, c1.rgb, localT);
                }

                return result;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float2 center = float2(0.5, 0.5);
                float dist = length(uv - center);

                float sphereMask = 1.0 - smoothstep(_SphereRadius - _SphereSoftness, _SphereRadius, dist);

                float2 flowDir = normalize(float2(_FlowDirection.x, _FlowDirection.y));
                float flowOffset = _Time.y * _FlowSpeed;

                float gradientPos = dot(uv - center, flowDir) + flowOffset;

                float3 color = applyLoopingGradient(gradientPos);

                color = (color - 0.5) * _Contrast + 0.5;
                color *= _Brightness;

                float glow = pow(1.0 - dist / _SphereRadius, 2.0) * 0.3;
                color += glow;

                float alpha = sphereMask;

                return float4(color, alpha);
            }
            ENDCG
        }
    }
}
