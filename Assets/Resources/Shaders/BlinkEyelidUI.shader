Shader "INTP/UI/BlinkEyelid"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (0,0,0,1)
        _CloseAmount("Close Amount", Range(0,1)) = 0
        _EdgeSoftness("Edge Softness", Range(0,0.2)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _CloseAmount;
            float _EdgeSoftness;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord);

                // _CloseAmount=0: fully open; _CloseAmount=1: fully closed.
                float halfOpen = (1.0 - saturate(_CloseAmount)) * 0.5;
                float distanceFromCenter = abs(IN.texcoord.y - 0.5);
                float softness = max(_EdgeSoftness, 1e-5);
                float visible = 1.0 - smoothstep(halfOpen, halfOpen + softness, distanceFromCenter);
                float eyelidMask = 1.0 - visible;

                fixed4 result = texColor * IN.color;
                result.a *= eyelidMask;
                return result;
            }
            ENDCG
        }
    }
}
