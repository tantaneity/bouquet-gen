using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum StrokeKind
{
    Card = 0,
    Stem = 1,
    Billboard = 2
}

public enum Species
{
    Rose = 0,
    Anemone = 1,
    Dahlia = 2,
    Lavender = 3,
    Gypsophila = 4,
    Eucalyptus = 5,
    Fern = 6
}

public sealed class MeshBuffer
{
    private readonly List<Vector3> positions = new List<Vector3>();
    private readonly List<Vector3> expansions = new List<Vector3>();
    private readonly List<Vector4> tangents = new List<Vector4>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<Vector2> kinds = new List<Vector2>();
    private readonly List<int> indices = new List<int>();

    public int VertexCount => positions.Count;

    public void AddVertex(Vector3 position, Vector3 expansion, Vector4 tangent, Color color, StrokeKind kind, float width)
    {
        positions.Add(position);
        expansions.Add(expansion);
        tangents.Add(tangent);
        colors.Add(color);
        kinds.Add(new Vector2((float)kind, width));
    }

    public void AddTriangle(int a, int b, int c)
    {
        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }

    public void AddQuad(int a, int b, int c, int d)
    {
        AddTriangle(a, b, c);
        AddTriangle(a, c, d);
    }

    public void WriteTo(Mesh mesh)
    {
        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(positions);
        mesh.SetNormals(expansions);
        mesh.SetTangents(tangents);
        mesh.SetColors(colors);
        mesh.SetUVs(0, kinds);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
    }
}

public static class BouquetGeometry
{
    private const int RingCount = 3;
    private const int PetalArcSteps = 9;
    private const int StemSteps = 10;
    private const int DiscSteps = 14;
    private const int BandSteps = 20;
    private const int LavenderBeads = 22;
    private const int GypsophilaArms = 13;
    private const int EucalyptusPairs = 4;
    private const int FernLeaflets = 14;
    private const int AnemoneStamens = 9;

    private const float ControlPointReach = 0.55f;
    private const float BundleSpread = 0.80f;
    private const float HighlightStep = 0.06f;
    private const float PetalCup = 0.22f;
    private const float CutDrop = 0.85f;
    private const float HeadLeanRise = 0.45f;
    private const float PetalSpiral = 0.016f;

    private static readonly float[] RingTilt = { 1.00f, 0.72f, 0.46f };
    private static readonly float[] RingLength = { 1.12f, 0.86f, 0.64f };
    private static readonly float[] RingHead = { 0.84f, 1.00f, 1.18f };
    private static readonly float[] RingAnchor = { 1.00f, 0.62f, 0.26f };
    private static readonly float[] RingPhase = { 0.0f, 0.7f, 1.9f };

    private static readonly Species[] RingSpecies =
    {
        Species.Gypsophila, Species.Lavender, Species.Fern, Species.Eucalyptus,
        Species.Rose, Species.Dahlia, Species.Anemone, Species.Eucalyptus,
        Species.Rose, Species.Anemone, Species.Dahlia, Species.Rose
    };

    // how far a head keeps climbing past the stem tip, in head sizes
    private static readonly float[] SpeciesReach = { 0.0f, 0.0f, 0.0f, 2.9f, 1.9f, 2.6f, 2.8f };
    private static readonly float[] SpeciesSize = { 1.0f, 0.92f, 0.95f, 1.0f, 1.5f, 1.0f, 1.0f };

    // tall sprigs rise above the blooms instead of flying out with their ring
    private static readonly float[] SpeciesTilt = { 1.0f, 1.0f, 1.0f, 0.84f, 0.78f, 1.06f, 1.00f };
    private static readonly float[] SpeciesLength = { 1.0f, 1.0f, 1.0f, 1.22f, 1.26f, 1.06f, 1.08f };

    public static void Build(MeshBuffer mesh, BouquetSettings settings, BouquetPalette palette)
    {
        Vector3 bind = new Vector3(0.0f, settings.bindHeight, 0.0f);
        int count = Mathf.Clamp(settings.stemCount, RingCount, 40);

        for (int index = 0; index < count; index++)
        {
            AddStem(mesh, settings, palette, bind, index, count);
        }

        AddRibbon(mesh, settings, palette, bind);
    }

    private static void AddStem(MeshBuffer mesh, BouquetSettings settings, BouquetPalette palette, Vector3 bind, int index, int count)
    {
        int perRing = Mathf.Max(count / RingCount, 1);
        int ring = Mathf.Min(index / perRing, RingCount - 1);
        int ringStart = ring * perRing;
        int ringSize = (ring == RingCount - 1) ? (count - ringStart) : perRing;
        int local = index - ringStart;

        float azimuth = (local + 0.5f) / ringSize * Mathf.PI * 2.0f + RingPhase[ring]
                      + (Hash(index, 0, settings.seed) - 0.5f) * settings.azimuthJitter * Mathf.PI * 2.0f;

        Species species = RingSpecies[ring * 4 + Mathf.FloorToInt(Hash(index, 4, settings.seed) * 3.999f)];
        int speciesIndex = (int)species;

        float tilt = settings.coneHalfAngle * Mathf.Deg2Rad * RingTilt[ring] * SpeciesTilt[speciesIndex]
                   * (0.82f + 0.36f * Hash(index, 8, settings.seed));

        float headSize = settings.headScale * RingHead[ring] * SpeciesSize[speciesIndex]
                       * (0.78f + 0.44f * Hash(index, 5, settings.seed));

        float silhouette = settings.stemLength * RingLength[ring] * SpeciesLength[speciesIndex]
                         * (0.80f + 0.40f * Hash(index, 1, settings.seed));

        float reach = Mathf.Max(silhouette - SpeciesReach[speciesIndex] * headSize, settings.stemLength * 0.25f);

        Vector2 around = new Vector2(Mathf.Cos(azimuth), Mathf.Sin(azimuth));
        Vector3 heading = new Vector3(around.x * Mathf.Sin(tilt), Mathf.Cos(tilt), around.y * Mathf.Sin(tilt));

        // stems enter the tie spread across it, they do not meet at a point
        Vector3 anchor = bind + new Vector3(around.x, 0.0f, around.y) * settings.ribbonWidth * BundleSpread * RingAnchor[ring];
        Vector3 mid = Vector3.Normalize(Vector3.Lerp(heading, Vector3.up, settings.outwardCurve));
        Vector3 control = anchor + mid * reach * ControlPointReach;
        Vector3 tip = anchor + heading * reach;

        AddCurveRibbon(mesh, anchor, control, tip, palette.stem, settings.stemWidth);

        float cutLength = settings.cutLength * (0.55f + 0.45f * Hash(index, 3, settings.seed));
        Vector3 cutEnd = anchor + Vector3.Normalize(new Vector3(-heading.x, -CutDrop, -heading.z)) * cutLength;
        AddCurveRibbon(mesh, anchor, Vector3.Lerp(anchor, cutEnd, 0.5f), cutEnd, palette.stem, settings.stemWidth);

        Vector3 tipTangent = Vector3.Normalize(tip - control);
        float roll = Hash(index, 6, settings.seed) * Mathf.PI * 2.0f;
        Color bloom = palette.Bloom(Mathf.FloorToInt(Hash(index, 7, settings.seed) * 2.999f));

        // a bloom on a near vertical stem points at the sky and the camera only
        // ever sees its edge, so blooms get turned outward the way a florist
        // sets them; sprigs keep following their stem
        Vector3 headAxis = tipTangent;
        if (speciesIndex <= (int)Species.Dahlia)
        {
            Vector3 outward = new Vector3(around.x, HeadLeanRise, around.y).normalized;
            headAxis = Vector3.Normalize(Vector3.Lerp(tipTangent, outward, settings.headLean));
        }

        Matrix4x4 frame = HeadFrame(tip, headAxis, roll);
        AddHead(mesh, species, frame, headSize, bloom, palette, settings);
    }

    private static Matrix4x4 HeadFrame(Vector3 origin, Vector3 axis, float roll)
    {
        Vector3 reference = Mathf.Abs(axis.y) > 0.95f ? Vector3.forward : Vector3.up;
        Vector3 right = Vector3.Normalize(Vector3.Cross(reference, axis));
        Vector3 up = Vector3.Cross(axis, right);
        right = Vector3.Normalize(right * Mathf.Cos(roll) + up * Mathf.Sin(roll));
        up = Vector3.Cross(axis, right);
        return new Matrix4x4(right, up, axis, new Vector4(origin.x, origin.y, origin.z, 1.0f));
    }

    private static void AddHead(MeshBuffer mesh, Species species, Matrix4x4 frame, float size, Color bloom, BouquetPalette palette, BouquetSettings settings)
    {
        if (species <= Species.Dahlia)
        {
            AddDisc(mesh, frame, Vector3.back * size * 0.04f, size * 0.46f, Color.Lerp(bloom, palette.leaf, 0.55f));
        }

        switch (species)
        {
            case Species.Rose:
                AddPetalRing(mesh, frame, 9, size, 1.05f, 0.0f, PetalCup, bloom, 0.0f);
                AddPetalRing(mesh, frame, 7, size * 0.74f, 1.10f, 0.42f, PetalCup * 1.4f, Lighten(bloom, HighlightStep), size * 0.22f);
                AddPetalRing(mesh, frame, 5, size * 0.44f, 1.15f, 1.05f, PetalCup * 1.9f, Lighten(bloom, HighlightStep * 2.0f), size * 0.40f);
                break;

            case Species.Anemone:
                AddPetalRing(mesh, frame, 6, size, 1.12f, 0.0f, PetalCup * 0.7f, bloom, 0.0f);
                AddDisc(mesh, frame, Vector3.forward * size * 0.20f, size * 0.30f, palette.ink);
                for (int i = 0; i < AnemoneStamens; i++)
                {
                    float angle = i / (float)AnemoneStamens * Mathf.PI * 2.0f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f) * size * 0.37f;
                    AddDisc(mesh, frame, offset + Vector3.forward * size * 0.24f, size * 0.04f, palette.ink);
                }
                break;

            case Species.Dahlia:
                AddPetalRing(mesh, frame, 12, size, 0.42f, 0.0f, PetalCup * 0.8f, bloom, 0.0f);
                AddPetalRing(mesh, frame, 10, size * 0.76f, 0.46f, 0.26f, PetalCup * 1.3f, Lighten(bloom, HighlightStep), size * 0.30f);
                AddPetalRing(mesh, frame, 8, size * 0.50f, 0.52f, 0.58f, PetalCup * 1.8f, Lighten(bloom, HighlightStep * 2.0f), size * 0.52f);
                AddDisc(mesh, frame, Vector3.forward * size * 0.66f, size * 0.07f, Lighten(bloom, HighlightStep * 3.0f));
                break;

            case Species.Lavender:
                AddLavender(mesh, frame, size, bloom, settings);
                break;

            case Species.Gypsophila:
                AddGypsophila(mesh, frame, size, palette, settings);
                break;

            case Species.Eucalyptus:
                AddEucalyptus(mesh, frame, size, palette, settings);
                break;

            default:
                AddFern(mesh, frame, size, palette, settings);
                break;
        }
    }

    // one petal is a flat leaf shaped polygon, cupped so it never goes perfectly
    // edge on: a truly flat card loses its outline when viewed along its plane
    private static void AddPetalRing(MeshBuffer mesh, Matrix4x4 frame, int petals, float radius, float aspect, float phase, float cup, Color fill)
    {
        AddPetalRing(mesh, frame, petals, radius, aspect, phase, cup, fill, 0.0f);
    }

    private static void AddPetalRing(MeshBuffer mesh, Matrix4x4 frame, int petals, float radius, float aspect, float phase, float cup, Color fill, float shelf)
    {
        float halfLength = radius * 0.5f;
        float halfWidth = halfLength * aspect;

        for (int p = 0; p < petals; p++)
        {
            float lift = shelf + p * radius * PetalSpiral;
            float angle = p / (float)petals * Mathf.PI * 2.0f + phase;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            int center = mesh.VertexCount;
            Vector3 centerLocal = new Vector3(cos, sin, 0.0f) * halfLength + Vector3.forward * lift;
            mesh.AddVertex(frame.MultiplyPoint3x4(centerLocal), Vector3.zero, Vector4.zero, fill, StrokeKind.Card, 0.0f);

            for (int s = 0; s <= PetalArcSteps; s++)
            {
                float t = s / (float)PetalArcSteps * Mathf.PI * 2.0f;
                Vector2 rim = new Vector2(Mathf.Cos(t) * halfLength, Mathf.Sin(t) * halfWidth);
                Vector2 outward = new Vector2(Mathf.Cos(t) * halfWidth, Mathf.Sin(t) * halfLength).normalized;

                Vector3 local = new Vector3(cos * rim.x - sin * rim.y, sin * rim.x + cos * rim.y, 0.0f) + centerLocal;
                local.z = lift - cup * (rim.x * rim.x + rim.y * rim.y) / Mathf.Max(halfLength * halfLength, 1e-5f) * halfLength;

                Vector3 outLocal = new Vector3(cos * outward.x - sin * outward.y, sin * outward.x + cos * outward.y, 0.0f);

                mesh.AddVertex(frame.MultiplyPoint3x4(local), frame.MultiplyVector(outLocal).normalized, Vector4.zero, fill, StrokeKind.Card, 0.0f);

                if (s > 0)
                {
                    mesh.AddTriangle(center, center + s, center + s + 1);
                }
            }
        }
    }

    private static void AddDisc(MeshBuffer mesh, Matrix4x4 frame, Vector3 offset, float radius, Color fill)
    {
        int center = mesh.VertexCount;
        mesh.AddVertex(frame.MultiplyPoint3x4(offset), Vector3.zero, Vector4.zero, fill, StrokeKind.Card, 0.0f);

        for (int s = 0; s <= DiscSteps; s++)
        {
            float t = s / (float)DiscSteps * Mathf.PI * 2.0f;
            Vector3 rim = new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0.0f);
            mesh.AddVertex(frame.MultiplyPoint3x4(offset + rim * radius), frame.MultiplyVector(rim).normalized, Vector4.zero, fill, StrokeKind.Card, 0.0f);

            if (s > 0)
            {
                mesh.AddTriangle(center, center + s, center + s + 1);
            }
        }
    }

    private static void AddBillboardDisc(MeshBuffer mesh, Vector3 position, float radius, Color fill)
    {
        int center = mesh.VertexCount;
        mesh.AddVertex(position, Vector3.zero, Vector4.zero, fill, StrokeKind.Billboard, 0.0f);

        for (int s = 0; s <= DiscSteps; s++)
        {
            float t = s / (float)DiscSteps * Mathf.PI * 2.0f;
            Vector3 offset = new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0.0f);
            mesh.AddVertex(position, offset, Vector4.zero, fill, StrokeKind.Billboard, 0.0f);

            if (s > 0)
            {
                mesh.AddTriangle(center, center + s, center + s + 1);
            }
        }
    }

    // a screen facing ribbon rather than a tube: at this line weight the two are
    // indistinguishable, and the ribbon keeps its width constant in pixels
    private static void AddCurveRibbon(MeshBuffer mesh, Vector3 a, Vector3 b, Vector3 c, Color fill, float width)
    {
        Vector3[] points = new Vector3[StemSteps + 1];
        for (int s = 0; s <= StemSteps; s++)
        {
            float t = s / (float)StemSteps;
            points[s] = Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t);
        }

        AddRibbon(mesh, points, fill, width);
    }

    public static void AddRibbon(MeshBuffer mesh, IReadOnlyList<Vector3> points, Color fill, float width)
    {
        if (points.Count < 2)
        {
            return;
        }

        int start = mesh.VertexCount;

        for (int s = 0; s < points.Count; s++)
        {
            Vector3 point = points[s];
            Vector3 ahead = points[Mathf.Min(s + 1, points.Count - 1)];
            Vector3 behind = points[Mathf.Max(s - 1, 0)];
            Vector3 tangent = Vector3.Normalize(ahead - behind);

            mesh.AddVertex(point, Vector3.zero, new Vector4(tangent.x, tangent.y, tangent.z, -1.0f), fill, StrokeKind.Stem, width);
            mesh.AddVertex(point, Vector3.zero, new Vector4(tangent.x, tangent.y, tangent.z, 1.0f), fill, StrokeKind.Stem, width);

            if (s > 0)
            {
                int here = start + s * 2;
                mesh.AddQuad(here - 2, here - 1, here + 1, here);
            }
        }
    }

    public static void AddHandle(MeshBuffer mesh, Vector3 position, float radius, Color fill)
    {
        AddBillboardDisc(mesh, position, radius, fill);
    }

    private static void AddLavender(MeshBuffer mesh, Matrix4x4 frame, float size, Color bloom, BouquetSettings settings)
    {
        float spikeLength = size * 2.9f;
        float beadRadius = size * 0.082f;

        AddCurveRibbon(mesh, frame.MultiplyPoint3x4(Vector3.zero),
            frame.MultiplyPoint3x4(new Vector3(0.0f, 0.0f, spikeLength * 0.2f)),
            frame.MultiplyPoint3x4(new Vector3(0.0f, 0.0f, spikeLength * 0.4f)), bloom, settings.stemWidth * 0.8f);

        for (int i = 0; i < LavenderBeads; i++)
        {
            float t = i / (float)(LavenderBeads - 1);
            float turn = i * 2.39996f;
            Vector3 local = new Vector3(Mathf.Cos(turn), Mathf.Sin(turn), 0.0f) * beadRadius * 0.85f;
            local.z = spikeLength * (0.24f + 0.76f * t);
            AddBillboardDisc(mesh, frame.MultiplyPoint3x4(local), beadRadius * (1.0f - 0.42f * t), bloom);
        }
    }

    private static void AddGypsophila(MeshBuffer mesh, Matrix4x4 frame, float size, BouquetPalette palette, BouquetSettings settings)
    {
        float reach = size * 1.9f;
        Color hair = Color.Lerp(palette.ink, palette.background, 0.45f);

        for (int i = 0; i < GypsophilaArms; i++)
        {
            float turn = i * 2.39996f;
            float lean = 0.55f + 0.45f * Fract(Mathf.Sin(turn * 4.1f) * 43.7f);
            Vector3 direction = new Vector3(Mathf.Cos(turn) * Mathf.Sin(0.9f), Mathf.Sin(turn) * Mathf.Sin(0.9f), Mathf.Cos(0.9f));
            Vector3 tip = direction * reach * lean;

            Vector3 worldBase = frame.MultiplyPoint3x4(Vector3.zero);
            Vector3 worldTip = frame.MultiplyPoint3x4(tip);
            AddCurveRibbon(mesh, worldBase, Vector3.Lerp(worldBase, worldTip, 0.5f), worldTip, hair, settings.lineWidth * 0.12f);

            for (int d = 0; d < 5; d++)
            {
                float spin = d * 2.39996f;
                float spill = size * 0.055f * (1.0f + d * 0.45f);
                Vector3 dot = tip + new Vector3(Mathf.Cos(spin), Mathf.Sin(spin), 0.25f) * spill;
                AddBillboardDisc(mesh, frame.MultiplyPoint3x4(dot), size * 0.034f, palette.background);
            }
        }
    }

    private static void AddEucalyptus(MeshBuffer mesh, Matrix4x4 frame, float size, BouquetPalette palette, BouquetSettings settings)
    {
        float spineLength = size * 2.6f;
        Vector3 spineBase = frame.MultiplyPoint3x4(Vector3.zero);
        Vector3 spineTip = frame.MultiplyPoint3x4(new Vector3(0.0f, 0.0f, spineLength));
        AddCurveRibbon(mesh, spineBase, Vector3.Lerp(spineBase, spineTip, 0.5f), spineTip, palette.leaf, settings.stemWidth * 0.7f);

        for (int i = 0; i < EucalyptusPairs; i++)
        {
            float t = (i + 0.35f) / EucalyptusPairs;
            float radius = size * 0.27f * (1.0f - 0.30f * t);
            float turn = i * 1.1f;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 outward = new Vector3(Mathf.Cos(turn), Mathf.Sin(turn), 0.0f) * side;
                Vector3 local = outward * radius * 0.95f + Vector3.forward * spineLength * t;
                Matrix4x4 leafFrame = LeafFrame(frame, local, outward, 0.45f);
                AddDisc(mesh, leafFrame, Vector3.zero, radius, palette.leaf);
            }
        }
    }

    private static void AddFern(MeshBuffer mesh, Matrix4x4 frame, float size, BouquetPalette palette, BouquetSettings settings)
    {
        float spineLength = size * 2.8f;
        Vector3 spineBase = frame.MultiplyPoint3x4(Vector3.zero);
        Vector3 spineTip = frame.MultiplyPoint3x4(new Vector3(0.0f, 0.0f, spineLength));
        AddCurveRibbon(mesh, spineBase, Vector3.Lerp(spineBase, spineTip, 0.5f), spineTip, palette.leaf, settings.stemWidth * 0.8f);

        for (int i = 0; i < FernLeaflets; i++)
        {
            float t = (i + 0.5f) / FernLeaflets;
            float leafletLength = size * 0.46f * (1.0f - 0.55f * t);
            float sweep = 0.72f + 0.30f * t;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 outward = new Vector3(side, 0.0f, 0.0f);
                Vector3 local = outward * leafletLength * 0.42f + Vector3.forward * spineLength * (0.06f + 0.94f * t);
                Matrix4x4 leafFrame = LeafFrame(frame, local, outward, sweep);
                AddPetalRing(mesh, leafFrame, 1, leafletLength * 2.0f, 0.30f, 0.0f, 0.05f, palette.leaf, i * 0.0012f);
            }
        }
    }

    private static Matrix4x4 LeafFrame(Matrix4x4 parent, Vector3 localOrigin, Vector3 localOutward, float lean)
    {
        Vector3 axis = Vector3.Normalize(localOutward * Mathf.Cos(lean) + Vector3.forward * Mathf.Sin(lean));
        Vector3 reference = Mathf.Abs(axis.z) > 0.95f ? Vector3.right : Vector3.forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(reference, axis));
        Vector3 up = Vector3.Cross(axis, right);

        Matrix4x4 local = new Matrix4x4(right, up, axis, new Vector4(localOrigin.x, localOrigin.y, localOrigin.z, 1.0f));
        return parent * local;
    }

    private static void AddRibbon(MeshBuffer mesh, BouquetSettings settings, BouquetPalette palette, Vector3 bind)
    {
        float radius = settings.ribbonWidth;
        float halfHeight = radius * 0.30f;

        // the wrap is a real band around the tie, so it turns with the bouquet
        int start = mesh.VertexCount;
        for (int s = 0; s <= BandSteps; s++)
        {
            float t = s / (float)BandSteps * Mathf.PI * 2.0f;
            Vector3 outward = new Vector3(Mathf.Cos(t), 0.0f, Mathf.Sin(t));
            Vector3 rim = bind + outward * radius;

            mesh.AddVertex(rim + Vector3.down * halfHeight, Vector3.down, Vector4.zero, palette.ribbon, StrokeKind.Card, 0.0f);
            mesh.AddVertex(rim + Vector3.up * halfHeight, Vector3.up, Vector4.zero, palette.ribbon, StrokeKind.Card, 0.0f);

            if (s > 0)
            {
                int here = start + s * 2;
                mesh.AddQuad(here - 2, here - 1, here + 1, here);
            }
        }

        float knotAngle = settings.knotAngle * Mathf.Deg2Rad;
        Vector3 knotOut = new Vector3(Mathf.Cos(knotAngle), 0.0f, Mathf.Sin(knotAngle));
        Vector3 knot = bind + knotOut * radius;

        Matrix4x4 knotFrame = LeafFrame(Matrix4x4.Translate(knot + knotOut * radius * 0.02f), Vector3.zero, knotOut, 0.0f);
        AddPetalRing(mesh, knotFrame, 1, radius * 0.66f, 0.62f, 0.0f, 0.0f, palette.ribbon);

        Vector3 sideways = Vector3.Normalize(Vector3.Cross(knotOut, Vector3.up));
        for (int i = 0; i < 2; i++)
        {
            float sway = (i == 0) ? -1.0f : 0.45f;
            Vector3 tailStart = knot - Vector3.up * halfHeight * 0.6f + sideways * radius * (0.10f + 0.28f * i);
            Vector3 tailControl = tailStart + sideways * sway * settings.tailLength * 0.30f - Vector3.up * settings.tailLength * 0.55f;
            Vector3 tailEnd = tailStart + sideways * sway * settings.tailLength * 0.16f - Vector3.up * settings.tailLength;
            AddCurveRibbon(mesh, tailStart, tailControl, tailEnd, palette.ribbon, settings.stemWidth * 2.2f);
        }
    }

    private static Color Lighten(Color color, float amount)
    {
        return Color.Lerp(color, Color.white, amount);
    }

    private static float Fract(float value)
    {
        return value - Mathf.Floor(value);
    }

    private static float Hash(int index, int channel, int seed)
    {
        unchecked
        {
            uint value = (uint)(index * 7 + channel * 131 + seed * 977 + 11);
            value = (value << 13) ^ value;
            value = value * (value * value * 15731u + 789221u) + 1376312589u;
            return (value & 0x7fffffffu) / (float)0x7fffffff;
        }
    }
}
