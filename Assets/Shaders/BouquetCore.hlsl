#ifndef BOUQUET_CORE_INCLUDED
#define BOUQUET_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define TAU 6.28318530718

#define MAX_STEMS 30
#define STEM_SEGMENTS 5
#define LAYER_COUNT 3
#define PALETTE_COUNT 4
#define PALETTE_SLOTS 8

#define SLOT_BACKGROUND 0
#define SLOT_INK 1
#define SLOT_STEM 2
#define SLOT_LEAF 3
#define SLOT_BLOOM_A 4
#define SLOT_BLOOM_B 5
#define SLOT_BLOOM_C 6
#define SLOT_RIBBON 7

#define SPECIES_ROSE 0
#define SPECIES_ANEMONE 1
#define SPECIES_DAHLIA 2
#define SPECIES_LAVENDER 3
#define SPECIES_GYPSOPHILA 4
#define SPECIES_EUCALYPTUS 5
#define SPECIES_FERN 6

#define LAVENDER_BEADS 15
#define GYPSOPHILA_ARMS 9
#define EUCALYPTUS_PAIRS 4
#define FERN_LEAFLETS 6
#define ANEMONE_STAMENS 9
#define RIBBON_TAILS 2

#define STEM_CULL_MARGIN 0.08
#define HEAD_CULL_SCALE 2.0
#define CONTROL_POINT_REACH 0.55
#define HIGHLIGHT_STEP 0.06
#define DOME_FALLOFF 0.17
#define BUNDLE_SPREAD 0.62

CBUFFER_START(UnityPerMaterial)
    float _Palette;
    float _StemCount;
    float _Spread;
    float _SpreadJitter;
    float _StemLength;
    float _StemWidth;
    float _Curve;
    float _Scale;
    float _LineWidth;
    float _Seed;
    float _BindHeight;
    float _CutLength;
    float _HeadScale;
    float _RibbonWidth;
    float _TailLength;
CBUFFER_END

static const float3 PALETTE_DATA[PALETTE_COUNT * PALETTE_SLOTS] =
{
    float3(0.961, 0.969, 0.976), float3(0.114, 0.118, 0.114),
    float3(0.659, 0.706, 0.659), float3(0.561, 0.627, 0.561),
    float3(0.784, 0.157, 0.220), float3(0.863, 0.682, 0.235),
    float3(0.282, 0.157, 0.408), float3(0.902, 0.882, 0.831),

    float3(0.961, 0.969, 0.976), float3(0.165, 0.149, 0.125),
    float3(0.659, 0.706, 0.659), float3(0.533, 0.659, 0.596),
    float3(0.910, 0.847, 0.784), float3(0.471, 0.031, 0.157),
    float3(0.847, 0.816, 0.745), float3(0.910, 0.875, 0.816),

    float3(0.961, 0.969, 0.976), float3(0.106, 0.118, 0.110),
    float3(0.290, 0.341, 0.290), float3(0.408, 0.533, 0.533),
    float3(0.910, 0.659, 0.596), float3(0.596, 0.157, 0.220),
    float3(0.847, 0.659, 0.659), float3(0.914, 0.843, 0.808),

    float3(0.961, 0.969, 0.976), float3(0.078, 0.094, 0.082),
    float3(0.235, 0.282, 0.235), float3(0.353, 0.455, 0.439),
    float3(0.878, 0.627, 0.588), float3(0.478, 0.125, 0.188),
    float3(0.753, 0.549, 0.549), float3(0.898, 0.812, 0.776)
};

static const float LAYER_SPREAD[LAYER_COUNT] = { 1.10, 0.95, 0.78 };
static const float LAYER_LENGTH[LAYER_COUNT] = { 1.22, 0.98, 0.72 };
static const float LAYER_HEAD[LAYER_COUNT] = { 0.78, 1.00, 1.22 };

static const int LAYER_SPECIES[LAYER_COUNT * 4] =
{
    SPECIES_GYPSOPHILA, SPECIES_LAVENDER, SPECIES_FERN, SPECIES_EUCALYPTUS,
    SPECIES_EUCALYPTUS, SPECIES_DAHLIA, SPECIES_ROSE, SPECIES_ANEMONE,
    SPECIES_ROSE, SPECIES_ANEMONE, SPECIES_DAHLIA, SPECIES_ROSE
};

// how far a head keeps climbing past the stem tip, in head sizes
static const float SPECIES_REACH[7] = { 0.0, 0.0, 0.0, 2.9, 1.9, 2.6, 2.8 };
static const float SPECIES_SIZE[7] = { 1.0, 0.92, 0.95, 1.0, 1.5, 1.0, 1.0 };

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

struct Stem
{
    float2 anchor;
    float2 control;
    float2 tip;
    float2 tangent;
    float2 cutEnd;
    float3 bloom;
    float headSize;
    float roll;
    int species;
};

float BouquetHash(uint seed)
{
    seed = (seed << 13u) ^ seed;
    seed = seed * (seed * seed * 15731u + 789221u) + 1376312589u;
    return float(seed & 0x7fffffffu) / float(0x7fffffff);
}

float StemHash(int index, int channel)
{
    return BouquetHash(uint(index * 7 + channel * 131 + int(_Seed) * 977 + 11));
}

float2 Rot(float2 p, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float3 PaletteColor(int slot)
{
    float position = clamp(_Palette, 0.0, PALETTE_COUNT - 1.0);
    int low = (int)floor(position);
    int high = min(low + 1, PALETTE_COUNT - 1);
    return lerp(PALETTE_DATA[low * PALETTE_SLOTS + slot], PALETTE_DATA[high * PALETTE_SLOTS + slot], frac(position));
}

float SdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
    return length(pa - ba * h);
}

float SdCurve(float2 p, float2 a, float2 b, float2 c)
{
    float distance = 1e9;
    float2 previous = a;

    [unroll]
    for (int i = 1; i <= STEM_SEGMENTS; i++)
    {
        float t = i / (float)STEM_SEGMENTS;
        float2 current = lerp(lerp(a, b, t), lerp(b, c, t), t);
        distance = min(distance, SdSegment(p, previous, current));
        previous = current;
    }

    return distance;
}

float SdRoundBox(float2 p, float2 halfSize, float radius)
{
    float2 q = abs(p) - halfSize + radius;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

float SdEllipseApprox(float2 p, float2 radii)
{
    float2 safe = max(radii, 1e-5);
    float2 normalized = p / safe;
    return (length(normalized) - 1.0) * min(safe.x, safe.y);
}

float SdPetalRing(float2 p, float petals, float radius, float aspect, float phase)
{
    float sector = TAU / petals;
    float angle = atan2(p.y, p.x) - phase;
    float index = round(angle / sector);
    float2 local = Rot(p, -(index * sector + phase));
    float2 radii = float2(radius * 0.5, radius * 0.5 * aspect);
    return SdEllipseApprox(local - float2(radii.x, 0.0), radii);
}

float3 Lighten(float3 color, float amount)
{
    return lerp(color, 1.0, amount);
}

void Paint(inout float3 color, float distance, float3 fill, float3 ink, float lineWidth, float antialias)
{
    float inside = 1.0 - smoothstep(-antialias, antialias, distance);
    color = lerp(color, fill, inside);

    float stroke = 1.0 - smoothstep(-antialias, antialias, abs(distance) - lineWidth);
    color = lerp(color, ink, stroke);
}

Stem BuildStem(int index, int count, float2 bind)
{
    int perLayer = max(count / LAYER_COUNT, 1);
    int layer = min(index / perLayer, LAYER_COUNT - 1);
    int layerStart = layer * perLayer;
    int layerSize = (layer == LAYER_COUNT - 1) ? (count - layerStart) : perLayer;
    int local = index - layerStart;

    int pairCount = max((layerSize + 1) / 2, 1);
    int pair = local / 2;
    float side = (local % 2 == 0) ? -1.0 : 1.0;
    float across = side * (1.0 - (pair + 0.35) / pairCount);

    float jitter = (StemHash(index, 0) - 0.5) * _SpreadJitter;
    float angle = across * radians(_Spread) * LAYER_SPREAD[layer] + jitter;
    float dome = 1.0 - DOME_FALLOFF * abs(across);

    Stem stem;
    stem.species = LAYER_SPECIES[layer * 4 + (int)(StemHash(index, 4) * 3.999)];
    stem.headSize = _HeadScale * LAYER_HEAD[layer] * SPECIES_SIZE[stem.species] * (0.78 + 0.44 * StemHash(index, 5));
    stem.roll = StemHash(index, 6) * TAU;

    float silhouette = _StemLength * LAYER_LENGTH[layer] * dome * (0.78 + 0.44 * StemHash(index, 1));
    float reach = max(silhouette - SPECIES_REACH[stem.species] * stem.headSize, _StemLength * 0.25);

    float2 outward = float2(sin(angle), cos(angle));
    float2 mid = normalize(lerp(outward, float2(0.0, 1.0), _Curve));

    // stems enter the tie spread across its width, they do not meet at a point
    float2 anchor = bind + float2(across * _RibbonWidth * BUNDLE_SPREAD, 0.0);

    stem.anchor = anchor;
    stem.control = anchor + mid * reach * CONTROL_POINT_REACH;
    stem.tip = anchor + outward * reach;
    stem.tangent = normalize(stem.tip - stem.control);

    float cutAngle = -angle * 0.85 + (StemHash(index, 2) - 0.5) * 0.35;
    float cutLength = _CutLength * (0.55 + 0.45 * StemHash(index, 3));
    stem.cutEnd = anchor + float2(sin(cutAngle), -cos(cutAngle)) * cutLength;

    float pick = StemHash(index, 7);
    int bloomSlot = SLOT_BLOOM_A + (int)(pick * 2.999);
    stem.bloom = PaletteColor(bloomSlot);

    return stem;
}

void DrawRose(inout float3 color, float2 local, float size, float3 bloom, float3 ink, float lineWidth, float antialias, float roll)
{
    Paint(color, SdPetalRing(local, 9.0, size, 1.05, roll), bloom, ink, lineWidth, antialias);

    float2 middle = local - float2(size * 0.09, size * 0.06);
    Paint(color, SdPetalRing(middle, 7.0, size * 0.74, 1.10, roll + 0.42), Lighten(bloom, HIGHLIGHT_STEP), ink, lineWidth, antialias);

    float2 inner = local - float2(-size * 0.05, size * 0.12);
    Paint(color, SdPetalRing(inner, 5.0, size * 0.44, 1.15, roll + 1.05), Lighten(bloom, HIGHLIGHT_STEP * 2.0), ink, lineWidth, antialias);
}

void DrawAnemone(inout float3 color, float2 local, float size, float3 bloom, float3 ink, float lineWidth, float antialias, float roll)
{
    Paint(color, SdPetalRing(local, 6.0, size, 1.12, roll), bloom, ink, lineWidth, antialias);

    float disc = length(local) - size * 0.30;
    Paint(color, disc, ink, ink, lineWidth, antialias);

    float sector = TAU / ANEMONE_STAMENS;
    float angle = atan2(local.y, local.x) - roll;
    float2 stamen = Rot(local, -(round(angle / sector) * sector + roll));
    float speck = length(stamen - float2(size * 0.37, 0.0)) - size * 0.035;
    Paint(color, speck, ink, ink, lineWidth * 0.6, antialias);
}

void DrawDahlia(inout float3 color, float2 local, float size, float3 bloom, float3 ink, float lineWidth, float antialias, float roll)
{
    Paint(color, SdPetalRing(local, 12.0, size, 0.42, roll), bloom, ink, lineWidth, antialias);
    Paint(color, SdPetalRing(local, 10.0, size * 0.76, 0.46, roll + 0.26), Lighten(bloom, HIGHLIGHT_STEP), ink, lineWidth, antialias);
    Paint(color, SdPetalRing(local, 8.0, size * 0.50, 0.52, roll + 0.58), Lighten(bloom, HIGHLIGHT_STEP * 2.0), ink, lineWidth, antialias);
    Paint(color, length(local) - size * 0.07, Lighten(bloom, HIGHLIGHT_STEP * 3.0), ink, lineWidth, antialias);
}

void DrawLavender(inout float3 color, float2 local, float size, float3 bloom, float3 ink, float lineWidth, float antialias)
{
    float spikeLength = size * 2.9;
    float beadRadius = size * 0.155;

    Paint(color, SdSegment(local, float2(0.0, 0.0), float2(0.0, spikeLength * 0.35)) - lineWidth * 0.8, bloom, ink, lineWidth * 0.8, antialias);

    [unroll]
    for (int i = 0; i < LAVENDER_BEADS; i++)
    {
        float t = i / (float)(LAVENDER_BEADS - 1);
        float side = (i % 2 == 0) ? -1.0 : 1.0;
        float2 center = float2(side * beadRadius * 0.62, spikeLength * (0.28 + 0.72 * t));
        float radius = beadRadius * (1.0 - 0.5 * t);
        Paint(color, length(local - center) - radius, bloom, ink, lineWidth * 0.8, antialias);
    }
}

void DrawGypsophila(inout float3 color, float2 local, float size, float3 background, float3 ink, float lineWidth, float antialias, float roll)
{
    float reach = size * 1.9;

    [unroll]
    for (int i = 0; i < GYPSOPHILA_ARMS; i++)
    {
        float t = i / (float)(GYPSOPHILA_ARMS - 1) - 0.5;
        float angle = t * 1.9 + sin(roll + i * 1.7) * 0.14;
        float armLength = reach * (0.55 + 0.45 * frac(sin(roll + i * 4.1) * 43.7));
        float2 tip = float2(sin(angle), cos(angle)) * armLength;

        Paint(color, SdSegment(local, float2(0.0, 0.0), tip) - lineWidth * 0.3, ink, ink, lineWidth * 0.3, antialias);
        Paint(color, length(local - tip) - size * 0.105, background, ink, lineWidth * 0.7, antialias);
        Paint(color, length(local - tip - float2(size * 0.17, size * 0.09)) - size * 0.08, background, ink, lineWidth * 0.7, antialias);
        Paint(color, length(local - tip + float2(size * 0.16, -size * 0.13)) - size * 0.08, background, ink, lineWidth * 0.7, antialias);
    }
}

void DrawEucalyptus(inout float3 color, float2 local, float size, float3 leaf, float3 ink, float lineWidth, float antialias)
{
    float spineLength = size * 2.6;
    Paint(color, SdSegment(local, float2(0.0, 0.0), float2(0.0, spineLength)) - lineWidth * 0.5, leaf, ink, lineWidth * 0.5, antialias);

    [unroll]
    for (int i = 0; i < EUCALYPTUS_PAIRS; i++)
    {
        float t = (i + 0.35) / EUCALYPTUS_PAIRS;
        float radius = size * 0.46 * (1.0 - 0.34 * t);
        float2 spine = float2(0.0, spineLength * t);
        Paint(color, SdEllipseApprox(local - spine - float2(radius * 0.86, 0.0), float2(radius, radius * 0.86)), leaf, ink, lineWidth, antialias);
        Paint(color, SdEllipseApprox(local - spine + float2(radius * 0.86, 0.0), float2(radius, radius * 0.86)), leaf, ink, lineWidth, antialias);
    }
}

void DrawFern(inout float3 color, float2 local, float size, float3 leaf, float3 ink, float lineWidth, float antialias)
{
    float spineLength = size * 2.8;
    Paint(color, SdSegment(local, float2(0.0, 0.0), float2(0.0, spineLength)) - lineWidth * 0.6, leaf, ink, lineWidth * 0.6, antialias);

    [unroll]
    for (int i = 0; i < FERN_LEAFLETS; i++)
    {
        float t = (i + 0.6) / FERN_LEAFLETS;
        float2 spine = float2(0.0, spineLength * t);
        float leafletLength = size * 0.95 * (1.0 - 0.52 * t);
        float2 radii = float2(leafletLength, size * 0.28);

        float2 right = Rot(local - spine, -0.62) - float2(leafletLength, 0.0);
        Paint(color, SdEllipseApprox(right, radii), leaf, ink, lineWidth * 0.9, antialias);

        float2 left = Rot(local - spine, -(TAU * 0.5 + 0.62)) - float2(leafletLength, 0.0);
        Paint(color, SdEllipseApprox(left, radii), leaf, ink, lineWidth * 0.9, antialias);
    }
}

void DrawHead(inout float3 color, Stem stem, float2 p, float3 ink, float3 leaf, float3 background, float lineWidth, float antialias)
{
    float2 offset = p - stem.tip;
    if (dot(offset, offset) > stem.headSize * stem.headSize * HEAD_CULL_SCALE * HEAD_CULL_SCALE * 4.0)
    {
        return;
    }

    float2 right = float2(stem.tangent.y, -stem.tangent.x);
    float2 aligned = float2(dot(offset, right), dot(offset, stem.tangent));

    if (stem.species == SPECIES_ROSE)
    {
        DrawRose(color, offset, stem.headSize, stem.bloom, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_ANEMONE)
    {
        DrawAnemone(color, offset, stem.headSize, stem.bloom, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_DAHLIA)
    {
        DrawDahlia(color, offset, stem.headSize, stem.bloom, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_LAVENDER)
    {
        DrawLavender(color, aligned, stem.headSize, stem.bloom, ink, lineWidth, antialias);
    }
    else if (stem.species == SPECIES_GYPSOPHILA)
    {
        DrawGypsophila(color, aligned, stem.headSize, background, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_EUCALYPTUS)
    {
        DrawEucalyptus(color, aligned, stem.headSize, leaf, ink, lineWidth, antialias);
    }
    else
    {
        DrawFern(color, aligned, stem.headSize, leaf, ink, lineWidth, antialias);
    }
}

void DrawRibbon(inout float3 color, float2 p, float2 bind, float3 ribbon, float3 ink, float lineWidth, float antialias)
{
    [unroll]
    for (int i = 0; i < RIBBON_TAILS; i++)
    {
        float sway = (i == 0) ? -1.0 : 0.45;
        float2 start = bind + float2(-_RibbonWidth * (0.18 + 0.30 * i), -_RibbonWidth * 0.30);
        float2 control = start + float2(sway * _TailLength * 0.30, -_TailLength * 0.55);
        float2 end = start + float2(sway * _TailLength * 0.16 - _TailLength * 0.10, -_TailLength);
        Paint(color, SdCurve(p, start, control, end) - _RibbonWidth * 0.26, ribbon, ink, lineWidth, antialias);
    }

    float2 band = Rot(p - bind, radians(6.0));
    Paint(color, SdRoundBox(band, float2(_RibbonWidth, _RibbonWidth * 0.42), _RibbonWidth * 0.10), ribbon, ink, lineWidth, antialias);

    float2 knot = Rot(p - bind - float2(-_RibbonWidth * 0.72, _RibbonWidth * 0.34), radians(-58.0));
    Paint(color, SdRoundBox(knot, float2(_RibbonWidth * 0.40, _RibbonWidth * 0.24), _RibbonWidth * 0.10), ribbon, ink, lineWidth, antialias);
}

float4 Fragment(Varyings input) : SV_Target
{
    float2 screen = (input.uv - 0.5) * 2.0;
    float2 p = screen / _Scale;

    float antialias = length(fwidth(p)) * 0.6;
    float lineWidth = _LineWidth / _Scale;
    float stemWidth = _StemWidth / _Scale;

    float3 background = PaletteColor(SLOT_BACKGROUND);
    float3 ink = PaletteColor(SLOT_INK);
    float3 stemColor = PaletteColor(SLOT_STEM);
    float3 leaf = PaletteColor(SLOT_LEAF);
    float3 ribbon = PaletteColor(SLOT_RIBBON);

    float3 color = background;
    float2 bind = float2(0.0, _BindHeight);
    int count = clamp((int)_StemCount, LAYER_COUNT, MAX_STEMS);

    for (int cut = 0; cut < MAX_STEMS; cut++)
    {
        if (cut >= count)
        {
            break;
        }

        Stem stem = BuildStem(cut, count, bind);
        Paint(color, SdSegment(p, bind, stem.cutEnd) - stemWidth, stemColor, ink, lineWidth, antialias);
    }

    for (int i = 0; i < MAX_STEMS; i++)
    {
        if (i >= count)
        {
            break;
        }

        Stem stem = BuildStem(i, count, bind);

        float2 low = min(min(stem.anchor, stem.control), stem.tip) - STEM_CULL_MARGIN;
        float2 high = max(max(stem.anchor, stem.control), stem.tip) + STEM_CULL_MARGIN;
        if (all(p > low) && all(p < high))
        {
            Paint(color, SdCurve(p, stem.anchor, stem.control, stem.tip) - stemWidth, stemColor, ink, lineWidth, antialias);
        }

        DrawHead(color, stem, p, ink, leaf, background, lineWidth, antialias);
    }

    DrawRibbon(color, p, bind, ribbon, ink, lineWidth, antialias);

    return float4(color, 1.0);
}

Varyings Vertex(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

#endif
