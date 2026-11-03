Shader "Custom/VisionCircleOverlay"
{
    Properties
    {
        _DarkColor ("Dark Color", Color) = (0, 0, 0, 0.88)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.25
        _Softness ("Softness", Float) = 0.03
        _Aspect ("Aspect", Float) = 1.777

        // 경계 노이즈 조절 값
        _NoisePower ("Noise Power", Float) = 0.14
        _NoiseSpeed ("Noise Speed", Float) = 22
        _NoiseScale ("Noise Scale", Float) = 100
        _NoiseWidth ("Noise Width", Float) = 0.09
        _TimeValue ("Time Value", Float) = 0
    }

    SubShader
    {
        Tags
        {
            // 투명 오버레이로 렌더링
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off

        // 알파값으로 어두운 영역 합성
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _DarkColor;
            float4 _Center;
            float _Radius;
            float _Softness;
            float _Aspect;

            float _NoisePower;
            float _NoiseSpeed;
            float _NoiseScale;
            float _NoiseWidth;
            float _TimeValue;

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

            // 랜덤 노이즈 값 생성
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float staticNoise(float2 uv, float t, float scale)
            {
                float2 p1 = floor((uv + float2(t * 0.07, -t * 0.05)) * scale);
                float2 p2 = floor((uv * 1.8 + float2(-t * 0.04, t * 0.09)) * scale * 0.65);
                float2 p3 = floor((uv * 3.2 + float2(t * 0.12, t * 0.03)) * scale * 0.35);

                float n1 = random(p1);
                float n2 = random(p2 + 19.13);
                float n3 = random(p3 + 73.71);

                float n = n1 * 0.5 + n2 * 0.35 + n3 * 0.15;

                n = smoothstep(0.2, 1.0, n);

                return n;
            }

            v2f vert(appdata v)
            {
                v2f o;

                // 오브젝트 좌표를 화면 좌표로 변환
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 현재 픽셀과 시야 중심의 거리 계산
                float2 diff = i.uv - _Center.xy;

                diff.x *= _Aspect;

                float dist = length(diff);

                // 시간에 따라 노이즈 움직임
                float t = _TimeValue * _NoiseSpeed;

                float nA = staticNoise(i.uv, t, _NoiseScale);
                float nB = staticNoise(i.uv + float2(3.1, 7.3), -t * 0.8, _NoiseScale * 0.55);

                float noise = lerp(nA, nB, 0.45);

                float flicker = 0.92 + 0.08 * sin(_TimeValue * 25.0);
                noise *= flicker;

                float ring = 1.0 - smoothstep(0.0, _NoiseWidth, abs(dist - _Radius));
                float ringBoost = 1.0 - smoothstep(0.0, _NoiseWidth * 0.6, abs(dist - _Radius));
                float band = saturate(ring * 0.7 + ringBoost * 0.8);
                float jaggedNoise = noise * 2.0 - 1.0;
                float edgeJitter = jaggedNoise * _NoisePower * band;
                float radiusCompensation = _NoisePower * 0.35 * band;

                float noisyRadius = _Radius + edgeJitter + radiusCompensation;

                float alpha = smoothstep(noisyRadius, noisyRadius + _Softness, dist);

                fixed4 col = _DarkColor;
                col.a *= alpha;

                return col;
            }
            ENDCG
        }
    }
}
