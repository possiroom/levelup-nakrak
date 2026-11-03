Shader "Custom/VisionCircleOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Range(0.0, 1.0)) = 0.2
        _Softness ("Softness", Range(0.0, 0.2)) = 0.02
        _Aspect ("Aspect", Float) = 1.0
        _NoisePower ("Noise Power", Range(0.0, 1.0)) = 0.0
        _NoiseSpeed ("Noise Speed", Float) = 20.0
        _NoiseScale ("Noise Scale", Float) = 80.0
        _NoiseWidth ("Noise Width", Range(0.0, 0.2)) = 0.06
        _TimeValue ("Time Value", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
            float _NoisePower;
            float _NoiseSpeed;
            float _NoiseScale;
            float _NoiseWidth;
            float _TimeValue;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = _Center.xy;
                float2 delta = uv - center;
                delta.x *= _Aspect;
                float dist = length(delta);
                float radius = _Radius;
                float edge = smoothstep(radius, radius + _Softness, dist);
                float alpha = 1.0 - edge;
                if (_NoisePower > 0.0)
                {
                    float noise = sin((uv.x + _TimeValue * 0.01 * _NoiseSpeed) * _NoiseScale) * cos((uv.y + _TimeValue * 0.01 * _NoiseSpeed) * _NoiseScale);
                    alpha *= 1.0 - saturate(_NoisePower * 0.5 + noise * _NoisePower * 0.5);
                }
                return fixed4(0,0,0, saturate(alpha));
            }
            ENDCG
        }
    }
}
