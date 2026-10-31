Shader "Naknak/Flashlight Free Sight Overlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.25
        _Softness ("Softness", Float) = 0.03
        _Aspect ("Aspect", Float) = 1.777
        _DarkAlpha ("Dark Alpha", Range(0, 1)) = 1
        _ShadowAlpha ("Shadow Alpha", Range(0, 1)) = 1
        _FlashlightVisible ("Flashlight Visible", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            sampler2D _MainTex;
            float4 _Center;
            float _Radius;
            float _Softness;
            float _Aspect;
            float _DarkAlpha;
            float _ShadowAlpha;
            float _FlashlightVisible;
            int _OccluderCount;
            float4 _OccluderRects[32];
            float4 _ShadowDirections[32];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            bool InsideRect(float2 uv, float4 rect)
            {
                return uv.x >= rect.x && uv.x <= rect.z && uv.y >= rect.y && uv.y <= rect.w;
            }

            bool InsideShadow(float2 uv, float4 rect, float2 dir)
            {
                if (InsideRect(uv, rect))
                    return false;

                if (dir.x > 0.5)
                    return uv.x > rect.z && uv.y >= rect.y && uv.y <= rect.w;

                if (dir.x < -0.5)
                    return uv.x < rect.x && uv.y >= rect.y && uv.y <= rect.w;

                if (dir.y > 0.5)
                    return uv.y > rect.w && uv.x >= rect.x && uv.x <= rect.z;

                if (dir.y < -0.5)
                    return uv.y < rect.y && uv.x >= rect.x && uv.x <= rect.z;

                return false;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 diff = i.uv - _Center.xy;
                diff.x *= _Aspect;
                float dist = length(diff);
                float outsideVision = smoothstep(_Radius, _Radius + _Softness, dist);
                outsideVision = lerp(1.0, outsideVision, _FlashlightVisible);
                float alpha = _DarkAlpha * outsideVision;

                if (outsideVision < 0.999)
                {
                    for (int index = 0; index < 32; index++)
                    {
                        if (index >= _OccluderCount)
                            break;

                        if (InsideShadow(i.uv, _OccluderRects[index], _ShadowDirections[index].xy))
                            alpha = max(alpha, _ShadowAlpha);
                    }
                }

                return fixed4(0, 0, 0, saturate(alpha));
            }
            ENDCG
        }
    }
}
