#ifndef BOUQUET_FLAT_CORE_INCLUDED
#define BOUQUET_FLAT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define KIND_CARD 0.5
#define KIND_STEM 1.5
#define PROBE_STEP 0.002
#define EDGE_ON_FLOOR 0.22
#define EDGE_ON_RANGE 0.45
#define EDGE_ON_MIN_COSINE 0.12
#define SHADE_SURFACE 0.5
#define SHADE_SPHERE 1.5
#define SHADE_TUBE 2.5
#define TUBE_ROUNDNESS 1.3

CBUFFER_START(UnityPerMaterial)
    float _LineWidth;
    float _OutlineDistance;
    float _OutlineFloor;
    float _OutlineCeiling;
    float _OutlineWobble;
    float _InkRecess;
    float4 _LightView;
    float4 _ShadowTint;
    float _ShadeThreshold;
    float _ShadeSoftness;
    float _ShadeStrength;
CBUFFER_END

struct Attributes
{
    float4 positionOS : POSITION;
    float3 expandOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    float4 stroke : TEXCOORD0;
    float4 ink : TEXCOORD1;
    float3 facing : TEXCOORD2;
    float4 shading : TEXCOORD3;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 color : COLOR;
    float4 normalWS : TEXCOORD0;
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

// breaks the line off a perfectly even machine width without going sketchy
float StrokeWobble(float3 positionOS)
{
    float noise = frac(sin(dot(positionOS, float3(12.9898, 78.233, 37.719))) * 43758.5453);
    return 1.0 + (noise - 0.5) * _OutlineWobble;
}

// how squarely a card faces the eye. strokes and billboards carry no facing and
// count as face on
float FacingCosine(float3 facingOS, float3 eyeDirection)
{
    if (dot(facingOS, facingOS) < 0.25)
    {
        return 1.0;
    }

    float3 facingWS = normalize(TransformObjectToWorldDir(facingOS, false));
    return abs(dot(facingWS, eyeDirection));
}

// w is 1 where the element has a surface to light and 0 where it stays flat
float4 ShadingNormal(float4 shading, float3 expandWS, float3 eyeDirection)
{
    if (shading.w < SHADE_SURFACE)
    {
        return float4(eyeDirection, 0.0);
    }

    if (shading.w < SHADE_SPHERE)
    {
        float3 normalWS = normalize(TransformObjectToWorldDir(shading.xyz, false));
        return float4(dot(normalWS, eyeDirection) < 0.0 ? -normalWS : normalWS, 1.0);
    }

    if (shading.w < SHADE_TUBE)
    {
        float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
        float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
        float2 offset = shading.xy;
        return float4(right * offset.x + up * offset.y + eyeDirection * sqrt(saturate(1.0 - dot(offset, offset))), 1.0);
    }

    return float4(normalize(expandWS * TUBE_ROUNDNESS + eyeDirection), 1.0);
}

Varyings Vertex(Attributes input)
{
    float kind = input.stroke.x;
    float baseWidth = input.stroke.y;
    float outlineWeight = input.stroke.z;
    float depthBias = input.stroke.w;

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
        // a billboard carries its shape as an offset on the camera plane and its
        // outline direction separately, because the rim normal of a petal is not
        // the radial direction from the flower centre
        float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
        float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
        positionWS += right * input.expandOS.x + up * input.expandOS.y;
        expandWS = right * input.tangentOS.x + up * input.tangentOS.y;
    }

    // pulling toward the eye is what keeps stacked cards in order and puts a vein
    // on top of the leaf it belongs to, without a second depth trick per species
    float3 toEye = GetCameraPositionWS() - positionWS;
    float eyeDistance = max(length(toEye), 1e-4);
    float3 eyeDirection = toEye / eyeDistance;
    positionWS += eyeDirection * depthBias;

    float width = baseWidth;
#ifdef BOUQUET_INK_PASS
    // a card turned edge on shrinks to a sliver narrower than its own outline, and
    // a cupped flower is full of them: without the fade every fold fills with ink
    float facingCosine = FacingCosine(input.facing, eyeDirection);
    float attenuation = clamp(_OutlineDistance / eyeDistance, _OutlineFloor, _OutlineCeiling);
    width += _LineWidth * outlineWeight * attenuation * StrokeWobble(input.positionOS.xyz)
           * lerp(EDGE_ON_FLOOR, 1.0, saturate(facingCosine / EDGE_ON_RANGE));

    // the ink is widened on screen but keeps its depth, so on a steep card the
    // widened rim climbs in front of the fill. seating it back by the line's own
    // width times the slope keeps the fill on top; the fixed recess covers the
    // face on case, where the two planes would otherwise fight into stipple
    float lineWorld = width * eyeDistance * 2.0 / abs(UNITY_MATRIX_P._m11);
    float slope = sqrt(saturate(1.0 - facingCosine * facingCosine)) / max(facingCosine, EDGE_ON_MIN_COSINE);
    positionWS -= eyeDirection * (_InkRecess + lineWorld * slope);
#endif

    float4 clip = TransformWorldToHClip(positionWS);

    if (width > 0.0 && dot(expandWS, expandWS) > 1e-12)
    {
        float2 direction = ScreenDirection(positionWS, expandWS, clip);
        float pixels = width * _ScreenParams.y;
        clip.xy += direction * pixels * float2(2.0 / _ScreenParams.x, 2.0 / _ScreenParams.y) * clip.w;
    }

    Varyings output;
    output.positionCS = clip;
#ifdef BOUQUET_INK_PASS
    output.color = input.ink;
    output.normalWS = float4(eyeDirection, 0.0);
#else
    output.color = input.color;
    output.normalWS = ShadingNormal(input.shading, expandWS, eyeDirection);
#endif
    return output;
}

// two tones, lit from over the viewer's shoulder so every side of the turntable
// gets the same key: flat fills stay flat, but each surface now has a lit and a
// shaded side and that is what reads as volume
float4 Fragment(Varyings input) : SV_Target
{
    float3 colour = input.color.rgb;
#ifndef BOUQUET_INK_PASS
    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
    float3 back = UNITY_MATRIX_I_V._m02_m12_m22;
    float3 light = normalize(right * _LightView.x + up * _LightView.y + back * _LightView.z);

    float lit = dot(normalize(input.normalWS.xyz), light);
    float band = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, lit);
    float3 shaded = lerp(colour * _ShadowTint.rgb, colour, band);
    colour = lerp(colour, shaded, _ShadeStrength * saturate(input.normalWS.w));
#endif
    return float4(colour, 1.0);
}

#endif
