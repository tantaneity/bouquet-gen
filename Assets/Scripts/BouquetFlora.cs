using UnityEngine;

public enum Species
{
    Rose = 0,
    OpenBloom = 1,
    Anemone = 2,
    Dahlia = 3,
    BerryCluster = 4,
    Lavender = 5,
    Gypsophila = 6,
    Eucalyptus = 7,
    Fern = 8,
    LongBlade = 9,
    LeafSprig = 10
}

// one head, drawn. blooms are billboards so they stay face on like drawn flowers,
// foliage follows its stem because that is where its silhouette comes from
public static class BouquetFlora
{
    private const int GypsophilaArms = 11;
    private const int LavenderBeads = 20;
    private const int EucalyptusLeaves = 7;
    private const int FernLeaflets = 13;
    private const int SprigLeaves = 9;
    private const float Golden = 2.39996f;

    public static float TipReserve(Species species)
    {
        switch (species)
        {
            case Species.Lavender: return 3.0f;
            case Species.Gypsophila: return 2.0f;
            case Species.Eucalyptus: return 2.7f;
            case Species.Fern: return 2.9f;
            case Species.LongBlade: return 3.2f;
            case Species.LeafSprig: return 2.8f;
            default: return 0.0f;
        }
    }

    public static bool IsBloom(Species species)
    {
        return species <= Species.BerryCluster;
    }

    public static void Add(MeshBuffer mesh, Species species, Vector3 tip, Vector3 axis, float size, float roll,
        Color bloom, BouquetPalette palette, float detailWidth, int seedIndex, int seed)
    {
        float j0 = Hash(seedIndex, 20, seed);
        float j1 = Hash(seedIndex, 21, seed);
        float j2 = Hash(seedIndex, 22, seed);

        switch (species)
        {
            case Species.Rose:
                Rose(mesh, tip, size, roll, bloom, palette, detailWidth, j0, j1, j2);
                break;
            case Species.OpenBloom:
                OpenBloom(mesh, tip, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.Anemone:
                Anemone(mesh, tip, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.Dahlia:
                Dahlia(mesh, tip, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.BerryCluster:
                BerryCluster(mesh, tip, size, bloom, j0);
                break;
            case Species.Lavender:
                Lavender(mesh, tip, axis, size, bloom, palette, j0);
                break;
            case Species.Gypsophila:
                Gypsophila(mesh, tip, axis, size, roll, palette, j0);
                break;
            case Species.Eucalyptus:
                Eucalyptus(mesh, tip, axis, size, roll, palette, detailWidth, j0);
                break;
            case Species.Fern:
                Fern(mesh, tip, axis, size, roll, palette, detailWidth, j0);
                break;
            case Species.LongBlade:
                LongBlade(mesh, tip, axis, size, roll, palette, detailWidth, j0, j1);
                break;
            default:
                LeafSprig(mesh, tip, axis, size, roll, palette, detailWidth, j0);
                break;
        }
    }

    private static void PetalRing(MeshBuffer mesh, Vector3 tip, int petals, float length, float width, float tipSharp,
        float phase, float layer, Color fill, float outlineWeight, float sizeJitter, float rollJitter, int salt)
    {
        for (int p = 0; p < petals; p++)
        {
            float wiggle = Hash(salt + p, 31, p * 7 + 3);
            float turn = p / (float)petals * Mathf.PI * 2.0f + phase + (wiggle - 0.5f) * rollJitter;
            float scale = 1.0f + (Hash(salt + p, 32, p * 11 + 5) - 0.5f) * sizeJitter;
            float skew = (Hash(salt + p, 33, p * 13 + 7) - 0.5f) * 0.5f;

            Vector2[] rim = BouquetShapes.PetalRim(length * scale, width * scale, tipSharp, skew);
            Vector2 centre = Vector2.zero;

            for (int i = 0; i < rim.Length; i++)
            {
                rim[i] = BouquetShapes.Rotate(rim[i], turn);
            }

            centre = BouquetShapes.Rotate(new Vector2(length * scale * 0.45f, 0.0f), turn);

            BouquetShapes.AddBillboardShape(mesh, tip, centre, rim, fill, outlineWeight,
                layer + p * BouquetShapes.LayerStep * 0.35f);
        }
    }

    private static void PetalVeins(MeshBuffer mesh, Vector3 tip, int petals, float length, float phase, float layer,
        Color ink, float detailWidth, float rollJitter, int salt)
    {
        mesh.SetInk(ink);
        for (int p = 0; p < petals; p++)
        {
            float wiggle = Hash(salt + p, 31, p * 7 + 3);
            float turn = p / (float)petals * Mathf.PI * 2.0f + phase + (wiggle - 0.5f) * rollJitter;
            Vector2 from = BouquetShapes.Rotate(new Vector2(length * 0.18f, 0.0f), turn);
            Vector2 to = BouquetShapes.Rotate(new Vector2(length * 0.74f, 0.0f), turn);
            BouquetShapes.AddBillboardLine(mesh, tip, new[] { from, to }, ink, detailWidth,
                layer + p * BouquetShapes.LayerStep * 0.35f + BouquetShapes.LayerStep * 0.2f);
        }
    }

    private static void Rose(MeshBuffer mesh, Vector3 tip, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1, float j2)
    {
        int outer = 8 + Mathf.RoundToInt(j0 * 3.0f);
        int middle = 6 + Mathf.RoundToInt(j1 * 2.0f);
        int inner = 4 + Mathf.RoundToInt(j2 * 2.0f);

        mesh.SetInk(palette.Line(bloom, 0.16f));
        PetalRing(mesh, tip, outer, size, size * 0.46f, 0.62f, roll, 0.0f, bloom, Outline.Silhouette, 0.26f, 0.30f, 11);

        Color mid = palette.Shift(bloom, 0.06f);
        mesh.SetInk(palette.Line(bloom, 0.48f));
        PetalRing(mesh, tip, middle, size * 0.70f, size * 0.36f, 0.70f, roll + 0.5f,
            BouquetShapes.LayerStep * 4.0f, mid, Outline.Contour, 0.24f, 0.32f, 37);

        Color core = palette.Shift(bloom, 0.13f);
        PetalRing(mesh, tip, inner, size * 0.42f, size * 0.26f, 0.80f, roll + 1.2f,
            BouquetShapes.LayerStep * 8.0f, core, Outline.Contour, 0.22f, 0.34f, 61);

        PetalVeins(mesh, tip, outer, size, roll, BouquetShapes.LayerStep * 0.5f,
            palette.Line(bloom, 0.62f), detailWidth, 0.30f, 11);
    }

    // the flat wide open flower the reference leans on for its big shapes
    private static void OpenBloom(MeshBuffer mesh, Vector3 tip, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        int petals = 5 + Mathf.RoundToInt(j0 * 3.0f);

        mesh.SetInk(palette.Line(bloom, 0.16f));
        PetalRing(mesh, tip, petals, size * 1.16f, size * 0.62f, 0.52f, roll, 0.0f, bloom, Outline.Silhouette, 0.30f, 0.26f, 5);
        PetalVeins(mesh, tip, petals, size * 0.92f, roll, BouquetShapes.LayerStep * 0.5f,
            palette.Line(bloom, 0.60f), detailWidth * 0.85f, 0.26f, 5);

        Color inner = palette.Shift(bloom, -0.30f);
        mesh.SetInk(palette.Line(inner, 0.55f));
        BouquetShapes.AddBillboardDisc(mesh, tip, size * 0.24f, inner, Outline.Contour, BouquetShapes.LayerStep * 4.0f);

        // stamens are strokes, not filled dots: filled ink pips read as black holes
        Color stamenInk = palette.Line(inner, 0.40f);
        int stamens = 7 + Mathf.RoundToInt(j1 * 4.0f);
        for (int i = 0; i < stamens; i++)
        {
            float turn = i * Golden;
            Vector2 from = BouquetShapes.Rotate(new Vector2(size * 0.08f, 0.0f), turn);
            Vector2 to = BouquetShapes.Rotate(new Vector2(size * 0.21f, 0.0f), turn);
            BouquetShapes.AddBillboardLine(mesh, tip, new[] { from, to }, stamenInk, detailWidth * 0.8f,
                BouquetShapes.LayerStep * 6.0f);
        }
    }

    private static void Anemone(MeshBuffer mesh, Vector3 tip, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        int petals = 6 + Mathf.RoundToInt(j0 * 2.0f);

        mesh.SetInk(palette.Line(bloom, 0.16f));
        PetalRing(mesh, tip, petals, size, size * 0.56f, 0.55f, roll, 0.0f, bloom, Outline.Silhouette, 0.28f, 0.28f, 17);
        PetalVeins(mesh, tip, petals, size, roll, BouquetShapes.LayerStep * 0.5f,
            palette.Line(bloom, 0.60f), detailWidth, 0.28f, 17);

        // the dark eye is the one place a near black mass belongs, but it was far
        // too wide and read as a hole punched through the flower
        Color eye = palette.Shift(bloom, -0.66f);
        mesh.SetInk(palette.Line(eye, 0.70f));
        BouquetShapes.AddBillboardDisc(mesh, tip, size * 0.20f, eye, Outline.Small, BouquetShapes.LayerStep * 4.0f);

        Color stamenInk = palette.Line(eye, 0.45f);
        int stamens = 9 + Mathf.RoundToInt(j1 * 5.0f);
        for (int i = 0; i < stamens; i++)
        {
            float turn = i / (float)stamens * Mathf.PI * 2.0f + roll;
            Vector2 from = BouquetShapes.Rotate(new Vector2(size * 0.20f, 0.0f), turn);
            Vector2 to = BouquetShapes.Rotate(new Vector2(size * 0.34f, 0.0f), turn);
            BouquetShapes.AddBillboardLine(mesh, tip, new[] { from, to }, stamenInk, detailWidth * 0.85f,
                BouquetShapes.LayerStep * 5.0f);
        }
    }

    private static void Dahlia(MeshBuffer mesh, Vector3 tip, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        int outer = 12 + Mathf.RoundToInt(j0 * 4.0f);
        int middle = 9 + Mathf.RoundToInt(j1 * 3.0f);

        mesh.SetInk(palette.Line(bloom, 0.24f));
        PetalRing(mesh, tip, outer, size, size * 0.17f, 0.85f, roll, 0.0f, bloom, Outline.Contour, 0.22f, 0.18f, 23);

        Color mid = palette.Shift(bloom, 0.07f);
        mesh.SetInk(palette.Line(bloom, 0.55f));
        PetalRing(mesh, tip, middle, size * 0.66f, size * 0.15f, 0.9f, roll + 0.3f,
            BouquetShapes.LayerStep * 4.0f, mid, Outline.Small, 0.22f, 0.20f, 47);

        Color core = palette.Shift(bloom, 0.15f);
        BouquetShapes.AddBillboardDisc(mesh, tip, size * 0.15f, core, Outline.Small, BouquetShapes.LayerStep * 8.0f);
    }

    private static void BerryCluster(MeshBuffer mesh, Vector3 tip, float size, Color bloom, float j0)
    {
        int berries = 5 + Mathf.RoundToInt(j0 * 5.0f);
        mesh.SetInk(Line(bloom));
        for (int i = 0; i < berries; i++)
        {
            float turn = i * Golden;
            float spill = size * (0.18f + 0.52f * i / berries);
            Vector2 at = new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * spill;
            float radius = size * (0.30f - 0.10f * i / berries);
            BouquetShapes.AddBillboardShape(mesh, tip, at, Offset(BouquetShapes.CircleRim(radius, 12, 1.0f), at),
                bloom, Outline.Small, i * BouquetShapes.LayerStep);
        }
    }

    private static void Lavender(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, Color bloom, BouquetPalette palette, float j0)
    {
        float spike = size * (2.7f + j0 * 0.6f);
        float bead = size * 0.085f;
        mesh.SetInk(palette.Line(bloom, 0.72f));

        for (int i = 0; i < LavenderBeads; i++)
        {
            float t = i / (float)(LavenderBeads - 1);
            float turn = i * Golden;
            Vector3 at = tip + axis * (spike * (0.18f + 0.82f * t));
            Vector3 lateral = Vector3.Normalize(Vector3.Cross(axis, Vector3.forward)) * Mathf.Cos(turn)
                            + Vector3.Normalize(Vector3.Cross(axis, Vector3.right)) * Mathf.Sin(turn);
            at += lateral * bead * 0.8f;
            BouquetShapes.AddBillboardDisc(mesh, at, bead * (1.0f - 0.4f * t), bloom, Outline.Small, i * BouquetShapes.LayerStep * 0.4f);
        }
    }

    private static void Gypsophila(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette, float j0)
    {
        float reach = size * (1.7f + j0 * 0.6f);
        Color hair = Color.Lerp(palette.ink, palette.background, 0.52f);
        mesh.SetInk(Color.Lerp(palette.ink, palette.background, 0.34f));
        Matrix4x4 frame = BouquetShapes.Frame(tip, axis, roll);

        for (int i = 0; i < GypsophilaArms; i++)
        {
            float turn = i * Golden;
            float lean = 0.5f + 0.5f * Frac(Mathf.Sin(turn * 4.1f) * 43.7f);
            Vector3 local = new Vector3(Mathf.Cos(turn) * Mathf.Sin(0.95f), Mathf.Sin(turn) * Mathf.Sin(0.95f), Mathf.Cos(0.95f));
            Vector3 armTip = frame.MultiplyPoint3x4(local * reach * lean);

            BouquetShapes.AddRibbon(mesh, new[] { tip, Vector3.Lerp(tip, armTip, 0.55f), armTip }, hair, 0.0007f, Outline.Detail, 0.0f);

            for (int d = 0; d < 4; d++)
            {
                float spin = d * Golden;
                Vector2 at = new Vector2(Mathf.Cos(spin), Mathf.Sin(spin)) * size * 0.075f * (1.0f + d * 0.4f);
                BouquetShapes.AddBillboardShape(mesh, armTip, at, Offset(BouquetShapes.CircleRim(size * 0.030f, 9, 1.0f), at),
                    palette.background, Outline.Small, d * BouquetShapes.LayerStep * 0.3f);
            }
        }
    }

    private static void Eucalyptus(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        float spine = size * (2.4f + j0 * 0.5f);
        Matrix4x4 frame = BouquetShapes.Frame(tip, axis, roll);
        mesh.SetInk(palette.Line(palette.leaf, 0.42f));
        BouquetShapes.AddRibbon(mesh, BouquetShapes.CubicPath(tip, tip + axis * spine * 0.35f, tip + axis * spine * 0.7f, tip + axis * spine, 6),
            palette.leaf, 0.0016f, Outline.Contour, 0.0f);

        for (int i = 0; i < EucalyptusLeaves; i++)
        {
            float t = (i + 0.4f) / EucalyptusLeaves;
            float side = (i % 2 == 0) ? 1.0f : -1.0f;
            float radius = size * 0.30f * (1.0f - 0.34f * t);
            Vector2 at = new Vector2(side * radius * 1.05f, spine * t);
            Matrix4x4 leaf = frame * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, 0.0f));

            BouquetShapes.AddCardShape(mesh, leaf, Offset(BouquetShapes.CircleRim(radius, 13, 0.88f), at), at,
                palette.leaf, Outline.Contour, (i + 1) * BouquetShapes.LayerStep * 0.5f);
            BouquetShapes.AddCardLine(mesh, leaf, new[] { at - new Vector2(side * radius * 0.7f, 0.0f), at + new Vector2(side * radius * 0.7f, 0.0f) },
                palette.ink, detailWidth, (i + 1) * BouquetShapes.LayerStep * 0.5f + BouquetShapes.LayerStep * 0.25f);
        }
    }

    private static void Fern(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        float spine = size * (2.6f + j0 * 0.5f);
        Matrix4x4 frame = BouquetShapes.Frame(tip, axis, roll);
        mesh.SetInk(palette.Line(palette.leaf, 0.56f));
        BouquetShapes.AddRibbon(mesh, new[] { tip, tip + axis * spine * 0.5f, tip + axis * spine }, palette.leaf, 0.0014f, Outline.Contour, 0.0f);

        Color blade = palette.Shift(palette.leaf, -0.10f);

        for (int i = 0; i < FernLeaflets; i++)
        {
            float t = (i + 0.5f) / FernLeaflets;
            float length = size * 0.42f * (1.0f - 0.52f * t);

            for (int side = -1; side <= 1; side += 2)
            {
                float turn = side > 0 ? 0.62f : Mathf.PI - 0.62f;
                Vector2[] rim = BouquetShapes.PetalRim(length, length * 0.17f, 0.75f, 0.0f);
                Vector2 root = new Vector2(0.0f, spine * (0.05f + 0.95f * t));
                for (int k = 0; k < rim.Length; k++)
                {
                    rim[k] = BouquetShapes.Rotate(rim[k], turn) + root;
                }

                BouquetShapes.AddCardShape(mesh, frame, rim, root + BouquetShapes.Rotate(new Vector2(length * 0.45f, 0.0f), turn),
                    blade, Outline.Small, (i + 1) * BouquetShapes.LayerStep * 0.4f);
            }
        }
    }

    // one long decorative leaf, the element that breaks up a bouquet full of round shapes
    private static void LongBlade(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        float length = size * (2.6f + j0 * 0.9f);
        float width = size * (0.24f + j1 * 0.12f);
        Matrix4x4 frame = BouquetShapes.Frame(tip, axis, roll);

        Color blade = palette.Shift(palette.leaf, -0.06f);
        mesh.SetInk(palette.Line(blade, 0.38f));
        BouquetShapes.AddBlade(mesh, frame, length, width, length * (0.18f + j1 * 0.22f), width * 0.9f,
            blade, Outline.Contour, 9);
        BouquetShapes.AddCardLine(mesh, frame, new[] { new Vector2(0.0f, length * 0.08f), new Vector2(0.0f, length * 0.88f) },
            palette.ink, detailWidth, BouquetShapes.LayerStep * 0.7f);
    }

    private static void LeafSprig(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        float spine = size * (2.2f + j0 * 0.6f);
        Matrix4x4 frame = BouquetShapes.Frame(tip, axis, roll);
        mesh.SetInk(palette.Line(palette.leaf, 0.66f));
        BouquetShapes.AddRibbon(mesh, new[] { tip, tip + axis * spine * 0.5f, tip + axis * spine }, palette.leaf, 0.0013f, Outline.Small, 0.0f);

        for (int i = 0; i < SprigLeaves; i++)
        {
            float t = (i + 0.5f) / SprigLeaves;
            float side = (i % 2 == 0) ? 1.0f : -1.0f;
            float length = size * 0.34f * (1.0f - 0.38f * t);
            float turn = side > 0 ? 0.95f : Mathf.PI - 0.95f;

            Vector2[] rim = BouquetShapes.PetalRim(length, length * 0.40f, 0.62f, 0.0f);
            Vector2 root = new Vector2(0.0f, spine * (0.08f + 0.92f * t));
            for (int k = 0; k < rim.Length; k++)
            {
                rim[k] = BouquetShapes.Rotate(rim[k], turn) + root;
            }

            BouquetShapes.AddCardShape(mesh, frame, rim, root + BouquetShapes.Rotate(new Vector2(length * 0.45f, 0.0f), turn),
                palette.leaf, Outline.Small, (i + 1) * BouquetShapes.LayerStep * 0.4f);
        }
    }

    private static Color Line(Color fill)
    {
        Color.RGBToHSV(fill, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s * 1.05f), Mathf.Clamp01(v * 0.42f));
    }

    private static Vector2[] Offset(Vector2[] rim, Vector2 by)
    {
        for (int i = 0; i < rim.Length; i++)
        {
            rim[i] += by;
        }

        return rim;
    }

    private static float Frac(float value)
    {
        return value - Mathf.Floor(value);
    }

    public static float Hash(int index, int channel, int seed)
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
