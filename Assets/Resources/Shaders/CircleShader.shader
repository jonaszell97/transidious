
Shader "Unlit/Circle"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _InnerColor ("Color", Color) = (1,1,1,1)
        _Thickness ("Thickness", float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        ZWrite Off
        Cull Off
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
         
            #include "UnityCG.cginc"
 
            // Quality level
            // 2 == high quality
            // 1 == medium quality
            // 0 == low quality
            #define QUALITY_LEVEL 1
 
            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
 
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
         
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord - 0.5;
                return o;
            }
 
            fixed4 _Color;
            fixed4 _InnerColor;
            float _Thickness;
 
            fixed4 frag (v2f i) : SV_Target
            {
                float dist = length(i.uv);
            #if QUALITY_LEVEL == 2
                // length derivative, 1.5 pixel smoothstep edge
                float pwidth = length(float2(ddx(dist), ddy(dist)));
                float alpha = smoothstep(0.5, 0.5 - pwidth * 1.5, dist);
            #elif QUALITY_LEVEL == 1
                // fwidth, 1.5 pixel smoothstep edge
                float pwidth = fwidth(dist);
                float alpha = smoothstep(0.5, 0.5 - pwidth * 1.5, dist);
            #else // Low
                // fwidth, 1 pixel linear edge
                float pwidth = fwidth(dist);
                float alpha = saturate((0.5 - dist) / pwidth);
            #endif
            
                if (dist < (0.5 - _Thickness)) {
                    // FIXME
                    return fixed4(_InnerColor.rgb, _Color.a * alpha);
                }
 
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}

/*
Shader "Custom/Circle" {
      Properties {
          _Color("Color", Color) = (1,0,0,0)
          _InnerColor("InnerColor", Color) = (1,1,1,1)
          _Thickness("Thickness", float) = 0.15
          _Radius("Radius", float) = 0.4
          _Dropoff("Dropoff", Range(0.00, 4)) = 0.1
      }
      SubShader {
          Pass {
              Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
              Blend SrcAlpha OneMinusSrcAlpha // Alpha blending
              CGPROGRAM
             
              #pragma vertex vert
              #pragma fragment frag
              #include "UnityCG.cginc"
             
             
             fixed4 _Color; // low precision type is usually enough for colors
             fixed4 _InnerColor; // low precision type is usually enough for colors
             float _Thickness;
             float _Radius;
             float _Dropoff;
             
              struct fragmentInput {
                  float4 pos : SV_POSITION;
                  float2 uv : TEXTCOORD0;
              };
  
              fragmentInput vert (appdata_base v)
              {
                  fragmentInput o;
  
                  o.pos = UnityObjectToClipPos (v.vertex);
                  o.uv = v.texcoord.xy - fixed2(0.5,0.5);
  
                  return o;
              }
  
              // r = radius
              // d = distance
              // t = thickness
              // p = % thickness used for dropoff
              float antialias(float r, float d, float t, float p) {
                 if( d < (r - 0.5 * t)) {
                    return pow( d - r + 0.5*t,2)/ pow(p*t, 2);
                 }
                 else if ( d > (r + 0.5*t)) {
                     return - pow( d - r - 0.5*t,2)/ pow(p*t, 2) + 1.0; 
                 }
                 else {
                     return 1.0;
                 }
              }
              
              fixed4 frag(fragmentInput i) : SV_Target {
                 float distance = sqrt(pow(i.uv.x, 2) + pow(i.uv.y,2));
                 
                 fixed4 color;
                 if (distance < (_Radius - 0.5 * _Thickness)) {
                    color = _InnerColor;
                 }
                 else {
                    color = _Color;
                 }
                 
                 return fixed4(color.r, color.g, color.b, color.a * antialias(_Radius, distance, _Thickness, _Dropoff));
              }
              
              
              ENDCG
          }
      }
  }*/