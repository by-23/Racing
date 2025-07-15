Shader "Custom/ForceField"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 2.0
        _FresnelScale ("Fresnel Scale", Range(0.0, 5.0)) = 1.0
        _Alpha ("Alpha", Range(0.0, 1.0)) = 0.5
        _RimPower ("Rim Power", Range(0.1, 10.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0.0, 5.0)) = 1.0
        _PulseIntensity ("Pulse Intensity", Range(0.0, 2.0)) = 0.3
        _NoiseScale ("Noise Scale", Range(0.1, 10.0)) = 1.0
        _NoiseSpeed ("Noise Speed", Range(0.0, 2.0)) = 0.5
        _Distortion ("Distortion", Range(0.0, 0.1)) = 0.02
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 200
        
        Pass
        {
            Name "ForceField"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _FresnelPower;
            float _FresnelScale;
            float _Alpha;
            float _RimPower;
            float _RimIntensity;
            float _PulseSpeed;
            float _PulseIntensity;
            float _NoiseScale;
            float _NoiseSpeed;
            float _Distortion;
            
            // Простая функция шума
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Улучшенная функция шума с интерполяцией
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Нормализация векторов
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                
                // Эффект Френеля
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelScale;
                
                // Rim lighting эффект
                float rim = 1.0 - saturate(dot(normal, viewDir));
                rim = pow(rim, _RimPower) * _RimIntensity;
                
                // Пульсирующий эффект
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseIntensity + 1.0;
                
                // Анимированный шум для дополнительного эффекта
                float2 noiseUV = i.uv * _NoiseScale + _Time.y * _NoiseSpeed;
                float noiseValue = smoothNoise(noiseUV);
                
                // Искажение UV для более динамичного эффекта
                float2 distortedUV = i.uv + sin(_Time.y + i.uv.x * 10.0) * _Distortion;
                
                // Текстура (если нужна)
                fixed4 tex = tex2D(_MainTex, distortedUV);
                
                // Комбинирование эффектов
                float intensity = fresnel + rim * pulse + noiseValue * 0.2;
                
                // Основной цвет с эффектами
                fixed4 col = _Color * intensity;
                col.rgb += rim * _Color.rgb * pulse;
                
                // Прозрачность основана на эффекте Френеля и настройке альфа
                col.a = (fresnel + rim * 0.5) * _Alpha * pulse;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
} 