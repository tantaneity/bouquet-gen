Shader "Custom/Bouquet"
{
    Properties
    {
        _Palette ("Palette", Range(0, 3)) = 2

        _StemCount ("Stem Count", Range(3, 30)) = 28
        _Spread ("Cone Half Angle", Range(5, 70)) = 27
        _SpreadJitter ("Azimuth Jitter", Range(0, 1)) = 0.35
        _StemLength ("Stem Length", Range(0.2, 1.6)) = 0.82
        _StemWidth ("Stem Half Width", Range(0.002, 0.04)) = 0.0095
        _Curve ("Outward Curve", Range(0, 1)) = 0.72
        _Seed ("Seed", Range(0, 64)) = 7

        _Yaw ("Yaw", Range(0, 360)) = 0
        _Pitch ("Pitch", Range(-40, 40)) = 8
        _Perspective ("Perspective", Range(0, 0.8)) = 0.30

        _Scale ("Composition Scale", Range(0.4, 2.5)) = 1.0
        _LineWidth ("Line Width", Range(0.001, 0.02)) = 0.0075

        _BindHeight ("Binding Height", Range(-0.9, 0.2)) = -0.40
        _CutLength ("Cut Stem Length", Range(0.05, 0.8)) = 0.31
        _HeadScale ("Head Scale", Range(0.02, 0.3)) = 0.135
        _HeadTilt ("Head Tilt", Range(0, 1)) = 0.35

        _RibbonWidth ("Ribbon Width", Range(0.02, 0.3)) = 0.105
        _TailLength ("Ribbon Tail Length", Range(0, 0.6)) = 0.36
        _KnotAngle ("Knot Angle", Range(0, 360)) = 200
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
