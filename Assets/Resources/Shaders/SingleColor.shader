﻿Shader "Unlit/Single Color" {
    Properties {
        _Color ("Color", Color) = (1,1,1)
    }
     
    SubShader {
        Color [_Color]
        ZWrite Off

        Pass {}
    }
}