Shader "Custom/BouquetFlat"
{
    Properties
    {
        _LineWidth ("Line Width", Range(0.0005, 0.02)) = 0.0021
        _OutlineDistance ("Outline Reference Distance", Range(0.5, 12)) = 3.9
        _OutlineFloor ("Outline Thin Limit", Range(0.2, 1)) = 0.66
        _OutlineCeiling ("Outline Thick Limit", Range(1, 2)) = 1.22
        _OutlineWobble ("Outline Wobble", Range(0, 0.6)) = 0.22
        _InkRecess ("Ink Recess", Range(0, 0.02)) = 0.0007
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "BouquetInk"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.5
            #define BOUQUET_INK_PASS 1
            #include "BouquetFlatCore.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "BouquetFill"
            Tags { "LightMode" = "UniversalForward" }
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.5
            #include "BouquetFlatCore.hlsl"
            ENDHLSL
        }
    }
}
