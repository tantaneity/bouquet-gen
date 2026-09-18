#ifndef BOUQUET_CORE_INCLUDED
#define BOUQUET_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define TAU 6.28318530718

#define MAX_STEMS 30
#define STEM_SEGMENTS 5
#define RING_COUNT 3
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

#define CONTROL_POINT_REACH 0.55
#define HIGHLIGHT_STEP 0.06
#define BUNDLE_SPREAD 0.80
#define HEAD_CULL_SCALE 2.1
#define BACKGROUND_DEPTH 1e6

// sub parts of one element step toward the viewer so they stack in the order drawn
#define DEPTH_LAYER 0.0006

#define MIN_BLOOM_FORESHORTEN 0.42
#define MIN_SPRIG_FORESHORTEN 0.26

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
    float _HeadTilt;
    float _RibbonWidth;
    float _TailLength;
    float _KnotAngle;
    float _Yaw;
    float _Pitch;
    float _Perspective;
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

// rings run outside in: outer stems lean furthest and carry the tall sprigs
static const float RING_TILT[RING_COUNT] = { 1.00, 0.66, 0.32 };
static const float RING_LENGTH[RING_COUNT] = { 1.12, 0.86, 0.60 };
static const float RING_HEAD[RING_COUNT] = { 0.84, 1.00, 1.18 };
static const float RING_ANCHOR[RING_COUNT] = { 1.00, 0.62, 0.26 };
static const float RING_PHASE[RING_COUNT] = { 0.0, 0.7, 1.9 };

static const int RING_SPECIES[RING_COUNT * 4] =
{
    SPECIES_GYPSOPHILA, SPECIES_LAVENDER, SPECIES_FERN, SPECIES_EUCALYPTUS,
    SPECIES_ROSE, SPECIES_DAHLIA, SPECIES_ANEMONE, SPECIES_EUCALYPTUS,
    SPECIES_ROSE, SPECIES_ANEMONE, SPECIES_DAHLIA, SPECIES_ROSE
};

// how far a head keeps climbing past the stem tip, in head sizes
static const float SPECIES_REACH[7] = { 0.0, 0.0, 0.0, 2.9, 1.9, 2.6, 2.8 };
static const float SPECIES_SIZE[7] = { 1.0, 0.92, 0.95, 1.0, 1.5, 1.0, 1.0 };

// tall thin sprigs rise through the middle instead of flying out with their ring
static const float SPECIES_TILT[7] = { 1.0, 1.0, 1.0, 0.84, 0.78, 1.06, 1.00 };
static const float SPECIES_LENGTH[7] = { 1.0, 1.0, 1.0, 1.22, 1.26, 1.06, 1.08 };

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

// painter's order without a sort: every element carries its own depth
struct Canvas
{
    float3 color;
    float depth;
};

struct Stem
{
    float2 anchor;
    float2 control;
    float2 tip;
    float2 cutEnd;
    float2 heading;
    float anchorDepth;
    float tipDepth;
    float cutDepth;
    float headSize;
    float foreshorten;
    float roll;
    float3 bloom;
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

float3 Orient(float3 v)
{
    float yaw = radians(_Yaw);
    float pitch = radians(_Pitch);

    float sy = sin(yaw);
    float cy = cos(yaw);
    v = float3(cy * v.x + sy * v.z, v.y, cy * v.z - sy * v.x);

    float sp = sin(pitch);
    float cp = cos(pitch);
    return float3(v.x, cp * v.y - sp * v.z, cp * v.z + sp * v.y);
}

// z grows toward the viewer, so nearer means a larger z and a smaller depth
float ViewScale(float3 v)
{
    return 1.0 / max(1.0 - v.z * _Perspective, 0.25);
}

float2 Project(float3 v)
{
    return v.xy * ViewScale(v);
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

// distance in x, curve parameter of the closest point in y:
// the parameter is what lets a stem carry a depth that varies along its length
float2 SdCurveAt(float2 p, float2 a, float2 b, float2 c)
{
    float best = 1e9;
    float bestParameter = 0.0;
    float2 previous = a;

    [unroll]
    for (int i = 1; i <= STEM_SEGMENTS; i++)
    {
        float t = i / (float)STEM_SEGMENTS;
        float2 current = lerp(lerp(a, b, t), lerp(b, c, t), t);

        float2 pa = p - previous;
        float2 ba = current - previous;
        float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
        float distance = length(pa - ba * h);

        if (distance < best)
        {
            best = distance;
            bestParameter = (i - 1 + h) / STEM_SEGMENTS;
        }

        previous = current;
    }

    return float2(best, bestParameter);
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

void Paint(inout Canvas canvas, float distance, float depth, float3 fill, float3 ink, float lineWidth, float antialias)
{
    if (depth > canvas.depth)
    {
        return;
    }

    float body = 1.0 - smoothstep(-antialias, antialias, distance);
    float stroke = 1.0 - smoothstep(-antialias, antialias, abs(distance) - lineWidth);
    float mask = max(body, stroke);
    if (mask <= 0.0)
    {
        return;
    }

    canvas.color = lerp(canvas.color, fill, body);
    canvas.color = lerp(canvas.color, ink, stroke);
    canvas.depth = (mask > 0.5) ? depth : canvas.depth;
}

Stem BuildStem(int index, int count, float3 bind)
{
    int perRing = max(count / RING_COUNT, 1);
    int ring = min(index / perRing, RING_COUNT - 1);
    int ringStart = ring * perRing;
    int ringSize = (ring == RING_COUNT - 1) ? (count - ringStart) : perRing;
    int local = index - ringStart;

    float azimuth = (local + 0.5) / ringSize * TAU + RING_PHASE[ring]
                  + (StemHash(index, 0) - 0.5) * _SpreadJitter * TAU;

    Stem stem;
    stem.species = RING_SPECIES[ring * 4 + (int)(StemHash(index, 4) * 3.999)];
    stem.roll = StemHash(index, 6) * TAU;
    stem.bloom = PaletteColor(SLOT_BLOOM_A + (int)(StemHash(index, 7) * 2.999));

    float tilt = radians(_Spread) * RING_TILT[ring] * SPECIES_TILT[stem.species] * (0.82 + 0.36 * StemHash(index, 8));

    float headSize = _HeadScale * RING_HEAD[ring] * SPECIES_SIZE[stem.species] * (0.78 + 0.44 * StemHash(index, 5));
    float silhouette = _StemLength * RING_LENGTH[ring] * SPECIES_LENGTH[stem.species] * (0.80 + 0.40 * StemHash(index, 1));
    float reach = max(silhouette - SPECIES_REACH[stem.species] * headSize, _StemLength * 0.25);

    float2 around = float2(cos(azimuth), sin(azimuth));
    float3 heading = float3(around.x * sin(tilt), cos(tilt), around.y * sin(tilt));

    // stems enter the tie spread across it, they do not meet at a point
    float3 anchor = bind + float3(around.x, 0.0, around.y) * _RibbonWidth * BUNDLE_SPREAD * RING_ANCHOR[ring];
    float3 mid = normalize(lerp(heading, float3(0.0, 1.0, 0.0), _Curve));
    float3 control = anchor + mid * reach * CONTROL_POINT_REACH;
    float3 tip = anchor + heading * reach;

    float cutLength = _CutLength * (0.55 + 0.45 * StemHash(index, 3));
    float3 cutEnd = anchor + normalize(float3(-heading.x, -1.1, -heading.z)) * cutLength;

    float3 viewAnchor = Orient(anchor);
    float3 viewControl = Orient(control);
    float3 viewTip = Orient(tip);
    float3 viewCut = Orient(cutEnd);
    float3 viewHeading = normalize(viewTip - viewControl);

    stem.anchor = Project(viewAnchor);
    stem.control = Project(viewControl);
    stem.tip = Project(viewTip);
    stem.cutEnd = Project(viewCut);
    stem.anchorDepth = -viewAnchor.z;
    stem.tipDepth = -viewTip.z;
    stem.cutDepth = -viewCut.z;

    float2 flat = viewHeading.xy;
    float flatLength = length(flat);
    stem.heading = (flatLength > 1e-4) ? flat / flatLength : float2(0.0, 1.0);

    // a head is a flat card: a bloom faces the camera and only tilts a little,
    // a sprig genuinely follows its stem and collapses as the stem turns away
    bool isSprig = stem.species >= SPECIES_LAVENDER;
    stem.foreshorten = isSprig
        ? max(flatLength, MIN_SPRIG_FORESHORTEN)
        : max(lerp(1.0, abs(viewHeading.z), _HeadTilt), MIN_BLOOM_FORESHORTEN);

    stem.headSize = headSize * ViewScale(viewTip);

    return stem;
}

void DrawRose(inout Canvas canvas, float2 local, float depth, float size, float3 bloom, float3 ink, float lineWidth, float antialias, float roll)
{
    Paint(canvas, SdPetalRing(local, 9.0, size, 1.05, roll), depth, bloom, ink, lineWidth, antialias);

    float2 middle = local - float2(size * 0.09, size * 0.06);
    Paint(canvas, SdPetalRing(middle, 7.0, size * 0.74, 1.10, roll + 0.42), depth - DEPTH_LAYER, Lighten(bloom, HIGHLIGHT_STEP), ink, lineWidth, antialias);

    float2 inner = local - float2(-size * 0.05, size * 0.12);
    Paint(canvas, SdPetalRing(inner, 5.0, size * 0.44, 1.15, roll + 1.05), depth - DEPTH_LAYER * 2.0, Lighten(bloom, HIGHLIGHT_STEP * 2.0), ink, lineWidth, antialias);
}

void DrawAnemone(inout Canvas canvas, float2 local, float depth, float size, float3 bloom, float3 ink, float lineWidth, float antialias, float roll)
{
    Paint(canvas, SdPetalRing(local, 6.0, size, 1.12, roll), depth, bloom, ink, lineWidth, antialias);
    Paint(canvas, length(local) - size * 0.30, depth - DEPTH_LAYER, ink, ink, lineWidth, antialias);

    float sector = TAU / ANEMONE_STAMENS;
    float angle = atan2(local.y, local.x) - roll;
    float2 stamen = Rot(local, -(round(angle / sector) * sector + roll));
    float speck = length(stamen - float2(size * 0.37, 0.0)) - size * 0.035;
    Paint(canvas, speck, depth - DEPTH_LAYER * 2.0, ink, ink, lineWidth * 0.6, antialias);
}

void DrawDahlia(inout Canvas canvas, float2 local, float depth, float size, float3 bloom, float3 ink, float lineWidth, float antialias, float roll)
{
    Paint(canvas, SdPetalRing(local, 12.0, size, 0.42, roll), depth, bloom, ink, lineWidth, antialias);
    Paint(canvas, SdPetalRing(local, 10.0, size * 0.76, 0.46, roll + 0.26), depth - DEPTH_LAYER, Lighten(bloom, HIGHLIGHT_STEP), ink, lineWidth, antialias);
    Paint(canvas, SdPetalRing(local, 8.0, size * 0.50, 0.52, roll + 0.58), depth - DEPTH_LAYER * 2.0, Lighten(bloom, HIGHLIGHT_STEP * 2.0), ink, lineWidth, antialias);
    Paint(canvas, length(local) - size * 0.07, depth - DEPTH_LAYER * 3.0, Lighten(bloom, HIGHLIGHT_STEP * 3.0), ink, lineWidth, antialias);
}

void DrawLavender(inout Canvas canvas, float2 local, float depth, float size, float3 bloom, float3 ink, float lineWidth, float antialias)
{
    float spikeLength = size * 2.9;
    float beadRadius = size * 0.155;

    Paint(canvas, SdSegment(local, float2(0.0, 0.0), float2(0.0, spikeLength * 0.35)) - lineWidth * 0.8, depth, bloom, ink, lineWidth * 0.8, antialias);

    [unroll]
    for (int i = 0; i < LAVENDER_BEADS; i++)
    {
        float t = i / (float)(LAVENDER_BEADS - 1);
        float side = (i % 2 == 0) ? -1.0 : 1.0;
        float2 center = float2(side * beadRadius * 0.62, spikeLength * (0.28 + 0.72 * t));
        float radius = beadRadius * (1.0 - 0.5 * t);
        Paint(canvas, length(local - center) - radius, depth - DEPTH_LAYER * (1.0 + i), bloom, ink, lineWidth * 0.8, antialias);
    }
}

void DrawGypsophila(inout Canvas canvas, float2 local, float depth, float size, float3 background, float3 ink, float lineWidth, float antialias, float roll)
{
    float reach = size * 1.9;

    [unroll]
    for (int i = 0; i < GYPSOPHILA_ARMS; i++)
    {
        float t = i / (float)(GYPSOPHILA_ARMS - 1) - 0.5;
        float angle = t * 1.9 + sin(roll + i * 1.7) * 0.14;
        float armLength = reach * (0.55 + 0.45 * frac(sin(roll + i * 4.1) * 43.7));
        float2 tip = float2(sin(angle), cos(angle)) * armLength;
        float armDepth = depth - DEPTH_LAYER * (1.0 + i);

        Paint(canvas, SdSegment(local, float2(0.0, 0.0), tip) - lineWidth * 0.3, armDepth, ink, ink, lineWidth * 0.3, antialias);
        Paint(canvas, length(local - tip) - size * 0.105, armDepth - DEPTH_LAYER * 0.5, background, ink, lineWidth * 0.7, antialias);
        Paint(canvas, length(local - tip - float2(size * 0.17, size * 0.09)) - size * 0.08, armDepth - DEPTH_LAYER * 0.5, background, ink, lineWidth * 0.7, antialias);
        Paint(canvas, length(local - tip + float2(size * 0.16, -size * 0.13)) - size * 0.08, armDepth - DEPTH_LAYER * 0.5, background, ink, lineWidth * 0.7, antialias);
    }
}

void DrawEucalyptus(inout Canvas canvas, float2 local, float depth, float size, float3 leaf, float3 ink, float lineWidth, float antialias)
{
    float spineLength = size * 2.6;
    Paint(canvas, SdSegment(local, float2(0.0, 0.0), float2(0.0, spineLength)) - lineWidth * 0.5, depth, leaf, ink, lineWidth * 0.5, antialias);

    [unroll]
    for (int i = 0; i < EUCALYPTUS_PAIRS; i++)
    {
        float t = (i + 0.35) / EUCALYPTUS_PAIRS;
        float radius = size * 0.34 * (1.0 - 0.30 * t);
        float2 spine = float2(0.0, spineLength * t);
        float leafDepth = depth - DEPTH_LAYER * (1.0 + i);
        Paint(canvas, SdEllipseApprox(local - spine - float2(radius * 0.86, 0.0), float2(radius, radius * 0.86)), leafDepth, leaf, ink, lineWidth, antialias);
        Paint(canvas, SdEllipseApprox(local - spine + float2(radius * 0.86, 0.0), float2(radius, radius * 0.86)), leafDepth, leaf, ink, lineWidth, antialias);
    }
}

void DrawFern(inout Canvas canvas, float2 local, float depth, float size, float3 leaf, float3 ink, float lineWidth, float antialias)
{
    float spineLength = size * 2.8;
    Paint(canvas, SdSegment(local, float2(0.0, 0.0), float2(0.0, spineLength)) - lineWidth * 0.6, depth, leaf, ink, lineWidth * 0.6, antialias);

    [unroll]
    for (int i = 0; i < FERN_LEAFLETS; i++)
    {
        float t = (i + 0.6) / FERN_LEAFLETS;
        float2 spine = float2(0.0, spineLength * t);
        float leafletLength = size * 0.80 * (1.0 - 0.52 * t);
        float2 radii = float2(leafletLength, size * 0.21);
        float leafDepth = depth - DEPTH_LAYER * (1.0 + i);

        float2 right = Rot(local - spine, -0.62) - float2(leafletLength, 0.0);
        Paint(canvas, SdEllipseApprox(right, radii), leafDepth, leaf, ink, lineWidth * 0.9, antialias);

        float2 left = Rot(local - spine, -(TAU * 0.5 + 0.62)) - float2(leafletLength, 0.0);
        Paint(canvas, SdEllipseApprox(left, radii), leafDepth, leaf, ink, lineWidth * 0.9, antialias);
    }
}

void DrawHead(inout Canvas canvas, Stem stem, float2 p, float3 ink, float3 leaf, float3 background, float lineWidth, float antialias)
{
    float2 offset = p - stem.tip;
    float bound = stem.headSize * HEAD_CULL_SCALE * 2.0;
    if (dot(offset, offset) > bound * bound)
    {
        return;
    }

    // the card stands on the projected stem heading, then squashes along it
    float2 right = float2(stem.heading.y, -stem.heading.x);
    float2 aligned = float2(dot(offset, right), dot(offset, stem.heading) / stem.foreshorten);
    float depth = stem.tipDepth;

    if (stem.species == SPECIES_ROSE)
    {
        DrawRose(canvas, aligned, depth, stem.headSize, stem.bloom, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_ANEMONE)
    {
        DrawAnemone(canvas, aligned, depth, stem.headSize, stem.bloom, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_DAHLIA)
    {
        DrawDahlia(canvas, aligned, depth, stem.headSize, stem.bloom, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_LAVENDER)
    {
        DrawLavender(canvas, aligned, depth, stem.headSize, stem.bloom, ink, lineWidth, antialias);
    }
    else if (stem.species == SPECIES_GYPSOPHILA)
    {
        DrawGypsophila(canvas, aligned, depth, stem.headSize, background, ink, lineWidth, antialias, stem.roll);
    }
    else if (stem.species == SPECIES_EUCALYPTUS)
    {
        DrawEucalyptus(canvas, aligned, depth, stem.headSize, leaf, ink, lineWidth, antialias);
    }
    else
    {
        DrawFern(canvas, aligned, depth, stem.headSize, leaf, ink, lineWidth, antialias);
    }
}

void DrawRibbon(inout Canvas canvas, float2 p, float3 bind, float3 ribbon, float3 ink, float lineWidth, float antialias)
{
    float3 viewBind = Orient(bind);
    float2 center = Project(viewBind);
    float width = _RibbonWidth * ViewScale(viewBind);

    // the wrap always hides the stems it crosses, so it sits at the front of the tie
    float bandDepth = -viewBind.z - _RibbonWidth;

    // the knot rides a point on the tie and orbits with the bouquet
    float knotAngle = radians(_KnotAngle);
    float3 viewKnot = Orient(bind + float3(cos(knotAngle), 0.0, sin(knotAngle)) * _RibbonWidth);
    float2 knotCenter = Project(viewKnot);
    float knotScale = ViewScale(viewKnot);
    float knotDepth = -viewKnot.z - DEPTH_LAYER;

    [unroll]
    for (int i = 0; i < RIBBON_TAILS; i++)
    {
        float sway = (i == 0) ? -1.0 : 0.45;
        float tail = _TailLength * knotScale;
        float2 start = knotCenter + float2(-width * (0.18 + 0.30 * i), -width * 0.30);
        float2 control = start + float2(sway * tail * 0.30, -tail * 0.55);
        float2 end = start + float2(sway * tail * 0.16 - tail * 0.10, -tail);
        Paint(canvas, SdCurveAt(p, start, control, end).x - width * 0.26, knotDepth, ribbon, ink, lineWidth, antialias);
    }

    float2 band = Rot(p - center, radians(6.0));
    Paint(canvas, SdRoundBox(band, float2(width, width * 0.42), width * 0.10), bandDepth, ribbon, ink, lineWidth, antialias);

    float2 knot = Rot(p - knotCenter - float2(0.0, width * 0.34), radians(-58.0));
    Paint(canvas, SdRoundBox(knot, float2(width * 0.40, width * 0.24), width * 0.10), knotDepth - DEPTH_LAYER, ribbon, ink, lineWidth, antialias);
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

    Canvas canvas;
    canvas.color = background;
    canvas.depth = BACKGROUND_DEPTH;

    float3 bind = float3(0.0, _BindHeight, 0.0);
    int count = clamp((int)_StemCount, RING_COUNT, MAX_STEMS);

    for (int i = 0; i < MAX_STEMS; i++)
    {
        if (i >= count)
        {
            break;
        }

        Stem stem = BuildStem(i, count, bind);

        float cutDistance = SdSegment(p, stem.anchor, stem.cutEnd) - stemWidth;
        Paint(canvas, cutDistance, max(stem.anchorDepth, stem.cutDepth), stemColor, ink, lineWidth, antialias);

        float2 hit = SdCurveAt(p, stem.anchor, stem.control, stem.tip);
        float stemDepth = lerp(stem.anchorDepth, stem.tipDepth, hit.y);
        Paint(canvas, hit.x - stemWidth, stemDepth, stemColor, ink, lineWidth, antialias);

        DrawHead(canvas, stem, p, ink, leaf, background, lineWidth, antialias);
    }

    DrawRibbon(canvas, p, bind, ribbon, ink, lineWidth, antialias);

    return float4(canvas.color, 1.0);
}

Varyings Vertex(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

#endif
