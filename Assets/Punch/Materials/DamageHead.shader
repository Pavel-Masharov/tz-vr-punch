Shader "Custom/HeadDamageShader" {
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _DamageMask ("Damage Mask", 2D) = "black" {}
        _DamageColor ("Damage Color", Color) = (0.8, 0.2, 0.3, 0.8)
        _DeformationMask ("Deformation Mask", 2D) = "black" {}
        _DeformationStrength ("Deformation Strength", Range(0, 0.1)) = 0.05
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _DamageMask;
            sampler2D _DeformationMask;
            float4 _DamageColor;
            float _DeformationStrength;
            
            v2f vert (appdata v) {
                v2f o;
                
                // Получаем деформацию из текстуры
                float deformation = tex2Dlod(_DeformationMask, float4(v.uv, 0, 0)).r;
                
                // Смещаем вершину вдоль нормали (внутрь сферы)
                float4 deformedVertex = v.vertex + float4(v.normal * -deformation * _DeformationStrength, 0);
                
                o.vertex = UnityObjectToClipPos(deformedVertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // Основная текстура
                fixed4 mainColor = tex2D(_MainTex, i.uv);
                
                // Маска повреждений
                fixed mask = tex2D(_DamageMask, i.uv).r;
                
                // Смешиваем: основная текстура + повреждения
                fixed4 result = mainColor + (_DamageColor * mask);
                
                return result;
            }
            ENDCG
        }
    }
}