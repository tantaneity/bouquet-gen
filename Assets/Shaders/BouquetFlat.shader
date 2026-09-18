Shader "Custom/BouquetFlat"
{
    Properties
    {
        _InkColor ("Ink Color", Color) = (0.106, 0.118, 0.110, 1)
        _LineWidth ("Line Width", Range(0.0005, 0.02)) = 0.0032
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
