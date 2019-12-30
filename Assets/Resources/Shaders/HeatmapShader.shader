// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Transidious/Heatmap" {
    Properties {
        _HeatTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1)
    }
    SubShader {
        Tags {"Queue"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha // Alpha blend

        Pass {
            CGPROGRAM
            #pragma vertex vert             
            #pragma fragment frag

            struct vertInput {
                float4 pos : POSITION;
            };  

            struct vertOutput {
                float4 pos : POSITION;
                fixed2 worldPos : TEXCOORD1;
            };

            vertOutput vert(vertInput input) {
                vertOutput o;
                o.pos = UnityObjectToClipPos(input.pos);
                o.worldPos = mul(unity_ObjectToWorld, input.pos).xy;

                return o;
            }

            uniform int _Points_Length = 0;
            uniform float4 _Properties [100];    // (x, y) = position, z = radius, w = intensity
            
            sampler2D _HeatTex;
            fixed4 _Color;

            half4 frag(vertOutput output) : COLOR {
                // Loops over all the points
                half h = 0;
                for (int i = 0; i < _Points_Length; i ++)
                {
                    // Calculates the contribution of each point
                    half di = distance(output.worldPos, _Properties[i].xy);

                    half ri = _Properties[i].z;
                    half hi = 1 - saturate(di / ri);

                    h += hi * _Properties[i].w;
                }

                // Converts (0-1) according to the heat texture
                h = saturate(h);
                half4 color = tex2D(_HeatTex, fixed2(h, 0.5));
                return half4(color.r * _Color.r, color.g * _Color.g, color.b * _Color.b, color.a);
            }
            ENDCG
        }
    } 
    Fallback "Diffuse"
}