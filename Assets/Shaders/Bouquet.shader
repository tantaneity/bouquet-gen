Shader "Custom/Bouquet"
{
    Properties
    {
        _Palette ("Palette", Range(0, 3)) = 2

        _StemCount ("Stem Count", Range(3, 30)) = 24
        _Spread ("Fan Half Angle", Range(5, 60)) = 29
        _SpreadJitter ("Fan Jitter", Range(0, 0.4)) = 0.07
        _StemLength ("Stem Length", Range(0.2, 1.6)) = 0.95
        _StemWidth ("Stem Half Width", Range(0.002, 0.04)) = 0.0095
        _Curve ("Outward Curve", Range(0, 1)) = 0.72
        _Seed ("Seed", Range(0, 64)) = 7

        _Scale ("Composition Scale", Range(0.4, 2.5)) = 1.0
        _LineWidth ("Line Width", Range(0.001, 0.02)) = 0.0075

        _BindHeight ("Binding Height", Range(-0.9, 0.2)) = -0.34
        _CutLength ("Cut Stem Length", Range(0.05, 0.8)) = 0.31
        _HeadScale ("Head Scale", Range(0.02, 0.3)) = 0.105

        _RibbonWidth ("Ribbon Width", Range(0.02, 0.3)) = 0.085
        _TailLength ("Ribbon Tail Length", Range(0, 0.6)) = 0.36
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "Bouquet"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.5
            #include "BouquetCore.hlsl"
            ENDHLSL
        }
    }
}
