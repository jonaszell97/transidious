Shader "Unlit/Vertex Color" {
 SubShader {
     Tags { "RenderType"="Opaque" }
     LOD 200
 CGPROGRAM
 struct Input {
     fixed4 color : COLOR;
 };
 void surf (Input IN, inout SurfaceOutput o) {
     o.Albedo = IN.color;
 }
 ENDCG
 }
 Fallback "VertexLit"
 }