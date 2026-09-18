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

// one head, drawn. blooms turn outward from the bundle the way cut flowers do, so
// the far side of the bouquet shows the backs of its heads and their calyx
public static class BouquetFlora
{
    private const int GypsophilaArms = 11;
    private const int LavenderBeads = 20;
    private const int EucalyptusLeaves = 7;
    private const int FernLeaflets = 13;
    private const int SprigLeaves = 9;
    private const float Golden = 2.39996f;
    private const int DiscSteps = 16;
    private const int SepalCount = 5;
    private const float SepalReflex = 38.0f;
    private const float SepalDrop = BouquetShapes.LayerStep * 0.8f;
    private const float PetalStep = BouquetShapes.LayerStep * 0.60f;
    private const float VeinLift = BouquetShapes.LayerStep * 0.25f;
    private const float HeadOutward = 1.6f;
    private const float HeadAxis = 0.5f;
    private const float HeadLift = 0.55f;
    private const float HeadNod = 0.5f;

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

        Matrix4x4 head = HeadFrame(tip, axis, roll, seedIndex, seed);

        switch (species)
        {
            case Species.Rose:
                Rose(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1, j2);
                break;
            case Species.OpenBloom:
                OpenBloom(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.Anemone:
                Anemone(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.Dahlia:
                Dahlia(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
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

    private static Matrix4x4 HeadFrame(Vector3 tip, Vector3 axis, float roll, int seedIndex, int seed)
    {
        Vector3 outward = new Vector3(tip.x, 0.0f, tip.z) * HeadOutward;
        Vector3 nod = new Vector3(Hash(seedIndex, 24, seed) - 0.5f, 0.0f, Hash(seedIndex, 25, seed) - 0.5f) * HeadNod;
        Vector3 facing = Vector3.Normalize(outward + axis * HeadAxis + Vector3.up * HeadLift + nod);
        return BouquetShapes.Frame(tip, facing, roll);
    }

    private static Matrix4x4 PetalFrame(Matrix4x4 head, float lift, float turn, float cup)
    {
        return head
             * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, lift))
             * Matrix4x4.Rotate(Quaternion.AngleAxis(turn * Mathf.Rad2Deg, Vector3.forward))
             * Matrix4x4.Rotate(Quaternion.AngleAxis(-cup, Vector3.up));
    }

    private static float PetalTurn(int petals, int p, float phase, float rollJitter, int salt)
    {
        float wiggle = Hash(salt + p, 31, p * 7 + 3);
        return p / (float)petals * Mathf.PI * 2.0f + phase + (wiggle - 0.5f) * rollJitter;
    }

    private static void PetalRing(MeshBuffer mesh, Matrix4x4 head, PetalRingSpec ring, Color fill, float outlineWeight)
    {
        for (int p = 0; p < ring.petals; p++)
        {
            float turn = PetalTurn(ring.petals, p, ring.phase, ring.rollJitter, ring.salt);
            float scale = 1.0f + (Hash(ring.salt + p, 32, p * 11 + 5) - 0.5f) * ring.sizeJitter;
            float skew = (Hash(ring.salt + p, 33, p * 13 + 7) - 0.5f) * 0.5f;

            Vector2[] rim = BouquetShapes.PetalRim(ring.length * scale, ring.width * scale, ring.tipSharp, skew);
            Matrix4x4 frame = PetalFrame(head, ring.layer + p * PetalStep, turn, ring.cup);
            BouquetShapes.AddCardShape(mesh, frame, rim, new Vector2(ring.length * scale * 0.45f, 0.0f), fill, outlineWeight, 0.0f);
        }
    }

    private static void PetalVeins(MeshBuffer mesh, Matrix4x4 head, PetalRingSpec ring, Color ink, float detailWidth)
    {
        mesh.SetInk(ink);
        for (int p = 0; p < ring.petals; p++)
        {
            float turn = PetalTurn(ring.petals, p, ring.phase, ring.rollJitter, ring.salt);
            Matrix4x4 frame = PetalFrame(head, ring.layer + p * PetalStep + VeinLift, turn, ring.cup);
            BouquetShapes.AddCardLine(mesh, frame,
                new[] { new Vector2(ring.length * 0.34f, 0.0f), new Vector2(ring.length * 0.62f, 0.0f) }, ink, detailWidth, 0.0f);
        }
    }

    private static void HeadDisc(MeshBuffer mesh, Matrix4x4 head, float radius, float lift, Color fill, float outlineWeight)
    {
        Matrix4x4 frame = head * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, lift));
        BouquetShapes.AddCardShape(mesh, frame, BouquetShapes.CircleRim(radius, DiscSteps, 1.0f), Vector2.zero, fill, outlineWeight, 0.0f);
    }

    private static void Stamens(MeshBuffer mesh, Matrix4x4 head, StamenSpec spec, Color ink, float detailWidth)
    {
        Matrix4x4 frame = head * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, spec.lift));
        for (int i = 0; i < spec.count; i++)
        {
            float turn = spec.spread ? i * Golden : i / (float)spec.count * Mathf.PI * 2.0f + spec.phase;
            Vector2 from = BouquetShapes.Rotate(new Vector2(spec.inner, 0.0f), turn);
            Vector2 to = BouquetShapes.Rotate(new Vector2(spec.outer, 0.0f), turn);
            BouquetShapes.AddCardLine(mesh, frame, new[] { from, to }, ink, detailWidth, 0.0f);
        }
    }

    // the green collar under the head. from the front the petals hide it, from the
    // side and behind it is what tells a flower's back from its face
    private static void Calyx(MeshBuffer mesh, Matrix4x4 head, float size, float roll, BouquetPalette palette, int salt)
    {
        Color sepal = palette.Shift(palette.leaf, 0.04f);
        mesh.SetInk(palette.Line(sepal, 0.42f));
        for (int p = 0; p < SepalCount; p++)
        {
            float turn = PetalTurn(SepalCount, p, roll + 0.3f, 0.40f, salt);
            float length = size * (0.58f + 0.20f * Hash(salt + p, 34, p * 5 + 1));
            Vector2[] rim = BouquetShapes.PetalRim(length, size * 0.15f, 0.80f, 0.0f);
            Matrix4x4 frame = PetalFrame(head, -SepalDrop - p * PetalStep, turn, -SepalReflex);
            BouquetShapes.AddCardShape(mesh, frame, rim, new Vector2(length * 0.45f, 0.0f), sepal, Outline.Contour, 0.0f);
        }
    }

    private static void Rose(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1, float j2)
    {
        PetalRingSpec outer = new PetalRingSpec(8 + Mathf.RoundToInt(j0 * 3.0f), size, size * 0.46f, 0.62f, roll, 0.0f, 24.0f, 0.26f, 0.30f, 11);
        PetalRingSpec middle = new PetalRingSpec(6 + Mathf.RoundToInt(j1 * 2.0f), size * 0.70f, size * 0.36f, 0.70f, roll + 0.5f,
            BouquetShapes.LayerStep * 4.0f, 44.0f, 0.24f, 0.32f, 37);
        PetalRingSpec inner = new PetalRingSpec(4 + Mathf.RoundToInt(j2 * 2.0f), size * 0.42f, size * 0.26f, 0.80f, roll + 1.2f,
            BouquetShapes.LayerStep * 8.0f, 62.0f, 0.22f, 0.34f, 61);

        Calyx(mesh, head, size, roll, palette, 71);

        mesh.SetInk(palette.Line(bloom, 0.16f));
        PetalRing(mesh, head, outer, bloom, Outline.Silhouette);

        mesh.SetInk(palette.Line(bloom, 0.48f));
        PetalRing(mesh, head, middle, palette.Shift(bloom, 0.06f), Outline.Contour);
        PetalRing(mesh, head, inner, palette.Shift(bloom, 0.13f), Outline.Contour);

        PetalVeins(mesh, head, outer, palette.Line(bloom, 0.62f), detailWidth);
    }

    // the flat wide open flower the reference leans on for its big shapes
    private static void OpenBloom(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        PetalRingSpec ring = new PetalRingSpec(5 + Mathf.RoundToInt(j0 * 3.0f), size * 1.16f, size * 0.62f, 0.52f, roll, 0.0f, 14.0f, 0.30f, 0.26f, 5);

        Calyx(mesh, head, size * 0.8f, roll, palette, 73);

        mesh.SetInk(palette.Line(bloom, 0.16f));
        PetalRing(mesh, head, ring, bloom, Outline.Silhouette);
        PetalVeins(mesh, head, ring, palette.Line(bloom, 0.60f), detailWidth * 0.85f);

        Color inner = palette.Shift(bloom, -0.30f);
        mesh.SetInk(palette.Line(inner, 0.55f));
        float centreLift = BouquetShapes.LayerStep * 4.0f;
        HeadDisc(mesh, head, size * 0.24f, centreLift, inner, Outline.Contour);

        // stamens are strokes, not filled dots: filled ink pips read as black holes
        Stamens(mesh, head, new StamenSpec(7 + Mathf.RoundToInt(j1 * 4.0f), size * 0.08f, size * 0.21f, centreLift + VeinLift, 0.0f, true),
            palette.Line(inner, 0.40f), detailWidth * 0.8f);
    }

    private static void Anemone(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        PetalRingSpec ring = new PetalRingSpec(6 + Mathf.RoundToInt(j0 * 2.0f), size, size * 0.56f, 0.55f, roll, 0.0f, 20.0f, 0.28f, 0.28f, 17);

        Calyx(mesh, head, size * 0.7f, roll, palette, 79);

        mesh.SetInk(palette.Line(bloom, 0.16f));
        PetalRing(mesh, head, ring, bloom, Outline.Silhouette);
        PetalVeins(mesh, head, ring, palette.Line(bloom, 0.60f), detailWidth);

        // the dark eye is the one place a near black mass belongs, but it was far
        // too wide and read as a hole punched through the flower
        Color eye = palette.Shift(bloom, -0.66f);
        mesh.SetInk(palette.Line(eye, 0.70f));
        float eyeLift = BouquetShapes.LayerStep * 4.0f;
        HeadDisc(mesh, head, size * 0.20f, eyeLift, eye, Outline.Small);

        Stamens(mesh, head, new StamenSpec(9 + Mathf.RoundToInt(j1 * 5.0f), size * 0.20f, size * 0.34f, eyeLift + VeinLift, roll, false),
            palette.Line(eye, 0.45f), detailWidth * 0.85f);
    }

    private static void Dahlia(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        PetalRingSpec outer = new PetalRingSpec(12 + Mathf.RoundToInt(j0 * 4.0f), size, size * 0.17f, 0.85f, roll, 0.0f, 12.0f, 0.22f, 0.18f, 23);
        PetalRingSpec middle = new PetalRingSpec(9 + Mathf.RoundToInt(j1 * 3.0f), size * 0.66f, size * 0.15f, 0.9f, roll + 0.3f,
            BouquetShapes.LayerStep * 4.0f, 34.0f, 0.22f, 0.20f, 47);

        Calyx(mesh, head, size * 0.75f, roll, palette, 83);

        mesh.SetInk(palette.Line(bloom, 0.24f));
        PetalRing(mesh, head, outer, bloom, Outline.Contour);

        mesh.SetInk(palette.Line(bloom, 0.55f));
        PetalRing(mesh, head, middle, palette.Shift(bloom, 0.07f), Outline.Small);
        HeadDisc(mesh, head, size * 0.15f, BouquetShapes.LayerStep * 8.0f, palette.Shift(bloom, 0.15f), Outline.Small);
    }

    private readonly struct PetalRingSpec
    {
        public readonly int petals;
        public readonly float length;
        public readonly float width;
        public readonly float tipSharp;
        public readonly float phase;
        public readonly float layer;
        public readonly float cup;
        public readonly float sizeJitter;
        public readonly float rollJitter;
        public readonly int salt;

        public PetalRingSpec(int petals, float length, float width, float tipSharp, float phase, float layer, float cup,
            float sizeJitter, float rollJitter, int salt)
        {
            this.petals = petals;
            this.length = length;
            this.width = width;
            this.tipSharp = tipSharp;
            this.phase = phase;
            this.layer = layer;
            this.cup = cup;
            this.sizeJitter = sizeJitter;
            this.rollJitter = rollJitter;
            this.salt = salt;
        }
    }

    private readonly struct StamenSpec
    {
        public readonly int count;
        public readonly float inner;
        public readonly float outer;
        public readonly float lift;
        public readonly float phase;
        public readonly bool spread;

        public StamenSpec(int count, float inner, float outer, float lift, float phase, bool spread)
        {
            this.count = count;
            this.inner = inner;
            this.outer = outer;
            this.lift = lift;
            this.phase = phase;
            this.spread = spread;
        }
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
