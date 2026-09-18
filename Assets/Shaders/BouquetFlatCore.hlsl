#ifndef BOUQUET_FLAT_CORE_INCLUDED
#define BOUQUET_FLAT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define KIND_CARD 0.5
#define KIND_STEM 1.5
#define PROBE_STEP 0.002

CBUFFER_START(UnityPerMaterial)
    float4 _InkColor;
    float _LineWidth;
CBUFFER_END

struct Attributes
{
    float4 positionOS : POSITION;
    float3 expandOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    float2 kind : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 color : COLOR;
};

// a world direction turned into a unit direction in pixel space, so the widening
// below is measured in pixels no matter how far away the vertex sits
float2 ScreenDirection(float3 positionWS, float3 directionWS, float4 clip)
{
    float4 shifted = TransformWorldToHClip(positionWS + directionWS * PROBE_STEP);
    float2 delta = shifted.xy / max(shifted.w, 1e-5) - clip.xy / max(clip.w, 1e-5);
    delta *= float2(_ScreenParams.x, _ScreenParams.y);

    float length2 = dot(delta, delta);
    return (length2 > 1e-12) ? delta * rsqrt(length2) : float2(0.0, 0.0);
}

Varyings Vertex(Attributes input)
{
    float kind = input.kind.x;
    float width = input.kind.y;

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 expandWS = float3(0.0, 0.0, 0.0);

    if (kind < KIND_CARD)
    {
        expandWS = TransformObjectToWorldDir(input.expandOS, false);
    }
    else if (kind < KIND_STEM)
    {
        float3 tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz, false);
        float3 toCamera = normalize(GetCameraPositionWS() - positionWS);
        float3 side = cross(tangentWS, toCamera);
        float sideLength = length(side);
        expandWS = (sideLength > 1e-5) ? (side / sideLength) * input.tangentOS.w : float3(0.0, 0.0, 0.0);
    }
    else
    {
        float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
        float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
        float3 offset = right * input.expandOS.x + up * input.expandOS.y;
        positionWS += offset;
        expandWS = offset;
    }

    float4 clip = TransformWorldToHClip(positionWS);

#ifdef BOUQUET_INK_PASS
    width += _LineWidth;
#endif

    if (width > 0.0 && dot(expandWS, expandWS) > 1e-12)
    {
        float2 direction = ScreenDirection(positionWS, expandWS, clip);
        float pixels = width * _ScreenParams.y;
        clip.xy += direction * pixels * float2(2.0 / _ScreenParams.x, 2.0 / _ScreenParams.y) * clip.w;
    }

    Varyings output;
    output.positionCS = clip;
#ifdef BOUQUET_INK_PASS
    output.color = _InkColor;
#else
    output.color = input.color;
#endif
    return output;
}

float4 Fragment(Varyings input) : SV_Target
{
    return float4(input.color.rgb, 1.0);
}

#endif
