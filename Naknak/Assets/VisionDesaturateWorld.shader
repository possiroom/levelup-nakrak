Shader "Custom/VisionDesaturateWorld"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _VisionCenter ("Vision Center", Vector) = (0, 0, 0, 0)
        _Radius ("Radius", Float) = 2.1
        _Softness ("Softness", Float) = 0.8
        _OutsideSaturation ("Outside Saturation", Range(0, 1)) = 0.08
        _OutsideDarkness ("Outside Darkness", Range(0, 1)) = 0.45
        _NoisePower ("Noise Power", Float) = 0.35
        _NoiseScale ("Noise Scale", Float) = 18
        _TimeValue ("Time Value", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
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
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _VisionCenter;
            float _Radius;
            float _Softness;
            float _OutsideSaturation;
            float _OutsideDarkness;
            float _NoisePower;
            float _NoiseScale;
            float _TimeValue;

            struct vision_v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            vision_v2f vert(appdata_t v)
            {
                vision_v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 position = UnityFlipSprite(v.vertex, _Flip);
                o.vertex = UnityObjectToClipPos(position);
                o.uv = v.texcoord;
                o.color = v.color * _Color * _RendererColor;
                o.worldPos = mul(unity_ObjectToWorld, position).xyz;
                return o;
            }

            fixed4 frag(vision_v2f i) : SV_Target
            {
                fixed4 col = SampleSpriteTexture(i.uv) * i.color;

                float dist = distance(i.worldPos.xy, _VisionCenter.xy);
                float noise = (hash21(i.worldPos.xy * _NoiseScale + _TimeValue) * 2.0 - 1.0) * _NoisePower;
                float outside = smoothstep(_Radius, _Radius + _Softness, dist + noise);

                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float3 desaturated = lerp(float3(gray, gray, gray), col.rgb, _OutsideSaturation);
                desaturated *= 1.0 - _OutsideDarkness;

                col.rgb = lerp(col.rgb, desaturated, outside);
                return col;
            }
            ENDCG
        }
    }
}
