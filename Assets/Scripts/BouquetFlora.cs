using UnityEngine;

public enum Species
{
    Rose = 0,
    OpenBloom = 1,
    Anemone = 2,
    Dahlia = 3,
    Carnation = 4,
    BerryCluster = 5,
    Lavender = 6,
    Gypsophila = 7,
    Eucalyptus = 8,
    Fern = 9,
    LongBlade = 10,
    LeafSprig = 11
}

// one head, drawn. blooms turn outward from the bundle the way cut flowers do, so
// the far side of the bouquet shows the backs of its heads and their calyx
public static class BouquetFlora
{
    private const int GypsophilaArms = 7;
    private const int GypsophilaTwigs = 3;
    private const int GypsophilaFlorets = 4;
    private const int DahliaRings = 5;
    private const int CarnationTeeth = 4;
    private const float CarnationJag = 0.10f;
    private const int VeinSteps = 4;
    private const int LavenderWhorls = 10;
    private const int LavenderPerWhorl = 4;
    private const float LavenderBloomPull = 0.2f;
    private const float LavenderBaseLift = 0.14f;
    private const float LavenderTopDrop = 0.16f;
    private const float LavenderBudStretch = 1.12f;
    private const float LavenderBractLean = 0.72f;
    private static readonly Color LavenderLilac = new Color(0.56f, 0.46f, 0.66f, 1.0f);
    private const int EucalyptusPairs = 8;
    private const float EucalyptusReach = 1.4f;
    private const float EucalyptusTwist = 0.45f;
    private const float EucalyptusSage = 0.12f;
    private const float EucalyptusPhase = Mathf.PI * 0.25f;
    private const float EucalyptusPhaseJitter = 0.25f;
    private const int FernLeaflets = 15;
    private const float FernLean = 1.08f;
    private const float CombLean = 0.72f;
    private const float SprayTwist = 0.6f;
    private const float SprayViewerBias = 1.0f;
    private const float SprayOutward = 0.5f;
    private const float LeafInk = 0.85f;
    private const int BladeSteps = 9;
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
    private const float PetalCurl = 0.22f;
    private const float PetalHollow = 0.45f;
    private const float DiscDome = 0.5f;
    private const float LeafBowl = 0.35f;

    public static float TipReserve(Species species)
    {
        switch (species)
        {
            case Species.Lavender: return 3.0f;
            case Species.Gypsophila: return 2.0f;
            case Species.Eucalyptus: return 3.4f;
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
            case Species.Carnation:
                Carnation(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.BerryCluster:
                BerryCluster(mesh, tip, size, bloom, j0);
                break;
            case Species.Lavender:
                Lavender(mesh, tip, axis, size, roll, bloom, palette, j0);
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

    private static Bend PetalBend(float length, float width, float curl)
    {
        return new Bend(Vector2.zero, curl / length, PetalHollow / Mathf.Max(width, 1e-4f));
    }

    private static float PetalTurn(int petals, int p, float phase, float rollJitter, int salt)
    {
        float wiggle = Hash(salt + p, 31, p * 7 + 3);
        return p / (float)petals * Mathf.PI * 2.0f + phase + (wiggle - 0.5f) * rollJitter;
    }

    private static void PetalRing(MeshBuffer mesh, Matrix4x4 head, PetalRingSpec ring, Color fill, float outlineWeight,
        bool isFrilled = false)
    {
        for (int p = 0; p < ring.petals; p++)
        {
            float turn = PetalTurn(ring.petals, p, ring.phase, ring.rollJitter, ring.salt);
            float scale = PetalScale(ring, p);
            float skew = (Hash(ring.salt + p, 33, p * 13 + 7) - 0.5f) * 0.5f;

            Vector2[] rim = isFrilled
                ? BouquetShapes.FrillRim(ring.length * scale, ring.width * scale, CarnationTeeth, CarnationJag, ring.salt + p)
                : BouquetShapes.PetalRim(ring.length * scale, ring.width * scale, ring.tipSharp, skew);
            Matrix4x4 frame = PetalFrame(head, ring.layer + p * PetalStep, turn, ring.cup);
            BouquetShapes.AddCardShape(mesh, frame, rim, new Vector2(ring.length * scale * 0.45f, 0.0f), fill, outlineWeight, 0.0f,
                PetalBend(ring.length * scale, ring.width * scale, PetalCurl));
        }
    }

    private static float PetalScale(PetalRingSpec ring, int p)
    {
        return 1.0f + (Hash(ring.salt + p, 32, p * 11 + 5) - 0.5f) * ring.sizeJitter;
    }

    private static void PetalVeins(MeshBuffer mesh, Matrix4x4 head, PetalRingSpec ring, Color ink, float detailWidth,
        float from = 0.34f, float to = 0.62f)
    {
        mesh.SetInk(ink);
        for (int p = 0; p < ring.petals; p++)
        {
            float turn = PetalTurn(ring.petals, p, ring.phase, ring.rollJitter, ring.salt);
            float length = ring.length * PetalScale(ring, p);
            Matrix4x4 frame = PetalFrame(head, ring.layer + p * PetalStep + VeinLift, turn, ring.cup);
            Vector2[] path = new Vector2[VeinSteps + 1];
            for (int k = 0; k <= VeinSteps; k++)
            {
                path[k] = new Vector2(length * Mathf.Lerp(from, to, k / (float)VeinSteps), 0.0f);
            }

            BouquetShapes.AddCardLine(mesh, frame, path, ink, detailWidth, 0.0f,
                PetalBend(length, ring.width * PetalScale(ring, p), PetalCurl));
        }
    }

    private static void HeadDisc(MeshBuffer mesh, Matrix4x4 head, float radius, float lift, Color fill, float outlineWeight)
    {
        Matrix4x4 frame = head * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, lift));
        BouquetShapes.AddCardShape(mesh, frame, BouquetShapes.CircleRim(radius, DiscSteps, 1.0f), Vector2.zero, fill, outlineWeight, 0.0f,
            Bend.Bowl(Vector2.zero, -DiscDome / radius));
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
            BouquetShapes.AddCardShape(mesh, frame, rim, new Vector2(length * 0.45f, 0.0f), sepal, Outline.Contour, 0.0f,
                PetalBend(length, size * 0.15f, -PetalCurl));
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

    // a pompon: rings of broad pointed petals, each one folded down its middle, closing
    // up towards a dark stippled eye
    private static void Dahlia(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        Calyx(mesh, head, size * 0.75f, roll, palette, 83);

        for (int ring = 0; ring < DahliaRings; ring++)
        {
            float t = ring / (float)(DahliaRings - 1);
            float length = size * Mathf.Lerp(1.0f, 0.30f, t);
            int petals = 13 - ring * 2 + Mathf.RoundToInt((ring == 0 ? j0 : j1) * 2.0f);
            PetalRingSpec spec = new PetalRingSpec(petals, length, length * 0.27f, 0.70f, roll + ring * 0.55f,
                BouquetShapes.LayerStep * 4.0f * ring, Mathf.Lerp(8.0f, 70.0f, t), 0.14f, 0.14f, 23 + ring * 19);

            Color fill = palette.Shift(bloom, 0.03f * ring);
            mesh.SetInk(palette.Line(fill, ring == 0 ? 0.30f : 0.50f));
            PetalRing(mesh, head, spec, fill, ring == 0 ? Outline.Silhouette : Outline.Contour);
            PetalVeins(mesh, head, spec, palette.Line(fill, 0.62f), detailWidth, 0.50f, 0.86f);
        }

        Color eye = palette.Shift(bloom, -0.62f);
        float eyeLift = BouquetShapes.LayerStep * 4.0f * DahliaRings;
        mesh.SetInk(palette.Line(eye, 0.60f));
        HeadDisc(mesh, head, size * 0.12f, eyeLift, eye, Outline.Small);
        Stamens(mesh, head, new StamenSpec(12, size * 0.02f, size * 0.10f, eyeLift + VeinLift, roll, true),
            palette.Shift(bloom, 0.10f), detailWidth * 0.8f);
    }

    // ruffled, toothed petals cupped into a ball, sitting in a long green tube with
    // its sepals pointing up. the tube is what the reference shows from below
    private static void Carnation(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        CarnationTube(mesh, head, size, palette);

        PetalRingSpec outer = new PetalRingSpec(9 + Mathf.RoundToInt(j0 * 3.0f), size * 0.95f, size * 0.62f, 0.0f, roll, 0.0f, 42.0f, 0.24f, 0.40f, 13);
        PetalRingSpec middle = new PetalRingSpec(8 + Mathf.RoundToInt(j1 * 2.0f), size * 0.74f, size * 0.54f, 0.0f, roll + 0.4f,
            BouquetShapes.LayerStep * 4.0f, 62.0f, 0.24f, 0.45f, 29);
        PetalRingSpec inner = new PetalRingSpec(6, size * 0.50f, size * 0.42f, 0.0f, roll + 0.9f,
            BouquetShapes.LayerStep * 8.0f, 80.0f, 0.24f, 0.50f, 43);

        mesh.SetInk(palette.Line(bloom, 0.22f));
        PetalRing(mesh, head, outer, bloom, Outline.Silhouette, isFrilled: true);

        mesh.SetInk(palette.Line(bloom, 0.46f));
        PetalRing(mesh, head, middle, palette.Shift(bloom, 0.05f), Outline.Contour, isFrilled: true);
        PetalRing(mesh, head, inner, palette.Shift(bloom, 0.10f), Outline.Contour, isFrilled: true);

        PetalVeins(mesh, head, outer, palette.Line(bloom, 0.60f), detailWidth, 0.22f, 0.70f);
        PetalVeins(mesh, head, middle, palette.Line(bloom, 0.60f), detailWidth, 0.25f, 0.65f);
    }

    // two crossed cards so the tube keeps a width from any side
    private static void CarnationTube(MeshBuffer mesh, Matrix4x4 head, float size, BouquetPalette palette)
    {
        Color tube = palette.Shift(palette.leaf, 0.08f);
        mesh.SetInk(palette.Line(tube, LeafInk));
        Vector2[] rim = CarnationTubeRim(size);
        Vector2 centre = new Vector2(0.0f, size * 0.22f);

        Matrix4x4 across = head * new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));
        Matrix4x4 through = head * new Matrix4x4(new Vector4(0, 1, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(1, 0, 0, 0), new Vector4(0, 0, 0, 1));
        BouquetShapes.AddCardShape(mesh, across, rim, centre, tube, Outline.Contour, 0.0f);
        BouquetShapes.AddCardShape(mesh, through, rim, centre, tube, Outline.Contour, 0.0f);
    }

    private static Vector2[] CarnationTubeRim(float size)
    {
        const int sepals = 4;
        const int sideSteps = 4;
        float length = size * 0.60f;
        float top = size * 0.22f;
        float bottom = size * 0.07f;
        float sepalRise = size * 0.20f;

        var rim = new System.Collections.Generic.List<Vector2>();
        for (int k = 0; k <= sepals * 2; k++)
        {
            float u = Mathf.Lerp(-top, top, k / (float)(sepals * 2));
            rim.Add(new Vector2(u, k % 2 == 1 ? -sepalRise : 0.0f));
        }

        for (int i = 1; i <= sideSteps; i++)
        {
            float v = i / (float)sideSteps;
            rim.Add(new Vector2(Mathf.Lerp(top, bottom, Mathf.Pow(v, 1.5f)), length * v));
        }

        for (int i = sideSteps; i >= 1; i--)
        {
            float v = i / (float)sideSteps;
            rim.Add(new Vector2(-Mathf.Lerp(top, bottom, Mathf.Pow(v, 1.5f)), length * v));
        }

        return rim.ToArray();
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

    // whorls of round buds around a rib that runs out past the last of them, lilac
    // whatever the palette, lighter at the base, with two bracts forking under it
    private static void Lavender(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, Color bloom,
        BouquetPalette palette, float j0)
    {
        float spike = size * (2.7f + j0 * 0.6f);
        Matrix4x4 frame = SprayFrame(tip, axis, roll);
        Vector3 across = frame.MultiplyVector(Vector3.right).normalized;
        Vector3 through = frame.MultiplyVector(Vector3.forward).normalized;

        mesh.SetInk(palette.Line(palette.stem, LeafInk));
        BouquetShapes.AddRibbon(mesh, new[] { tip, tip + axis * spike * 0.5f, tip + axis * spike }, palette.stem, 0.0012f, Outline.Contour, 0.0f);
        LavenderBracts(mesh, frame, size, palette);

        Color lilac = Color.Lerp(LavenderLilac, bloom, LavenderBloomPull);
        Color baseColour = palette.Shift(lilac, LavenderBaseLift);
        Color topColour = palette.Shift(lilac, -LavenderTopDrop);

        for (int whorl = 0; whorl < LavenderWhorls; whorl++)
        {
            float t = whorl / (float)(LavenderWhorls - 1);
            float radius = size * Mathf.Lerp(0.125f, 0.060f, t);
            float ring = radius * Mathf.Lerp(1.45f, 0.75f, t);
            Vector3 centre = tip + axis * (spike * (0.06f + 0.86f * t));
            Color bud = Color.Lerp(baseColour, topColour, t);
            mesh.SetInk(palette.Line(bud, LeafInk));

            for (int k = 0; k < LavenderPerWhorl; k++)
            {
                float turn = (k + (whorl % 2) * 0.5f) / LavenderPerWhorl * Mathf.PI * 2.0f + roll;
                Vector3 at = centre + (across * Mathf.Cos(turn) + through * Mathf.Sin(turn)) * ring;
                BouquetShapes.AddBillboardShape(mesh, at, Vector2.zero, BouquetShapes.CircleRim(radius, 12, LavenderBudStretch),
                    bud, Outline.Contour, (whorl * LavenderPerWhorl + k) * BouquetShapes.LayerStep * 0.2f);
            }
        }
    }

    private static void LavenderBracts(MeshBuffer mesh, Matrix4x4 frame, float size, BouquetPalette palette)
    {
        float length = size * 1.05f;
        Vector2 root = new Vector2(0.0f, size * 0.10f);

        for (int side = -1; side <= 1; side += 2)
        {
            float turn = side > 0 ? LavenderBractLean : Mathf.PI - LavenderBractLean;
            Vector2[] rim = BouquetShapes.PetalRim(length, length * 0.10f, 0.9f, side * 0.4f);
            for (int k = 0; k < rim.Length; k++)
            {
                rim[k] = BouquetShapes.Rotate(rim[k], turn) + root;
            }

            Vector2 middle = root + BouquetShapes.Rotate(new Vector2(length * 0.45f, 0.0f), turn);
            BouquetShapes.AddCardShape(mesh, frame, rim, middle, palette.stem, Outline.Contour, BouquetShapes.LayerStep * 0.3f);
        }
    }

    // a cloud, not an umbel: arms fork into twigs and every twig ends in a knot of
    // white florets ringed in ink
    private static void Gypsophila(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette, float j0)
    {
        float reach = size * (1.5f + j0 * 0.5f);
        float floret = size * 0.062f;
        Color hair = Color.Lerp(palette.ink, palette.background, 0.45f);
        Matrix4x4 frame = BouquetShapes.Frame(tip, axis, roll);

        for (int arm = 0; arm < GypsophilaArms; arm++)
        {
            float turn = arm * Golden;
            float lean = 0.45f + 0.65f * Frac(Mathf.Sin(turn * 4.1f) * 43.7f);
            float length = reach * (0.55f + 0.45f * Frac(Mathf.Sin(turn * 7.3f) * 17.1f));
            Vector3 armTip = frame.MultiplyPoint3x4(Spoke(turn, lean) * length);

            mesh.SetInk(Color.Lerp(palette.ink, palette.background, 0.30f));
            BouquetShapes.AddRibbon(mesh, new[] { tip, Vector3.Lerp(tip, armTip, 0.55f), armTip }, hair, 0.0006f, Outline.Detail, 0.0f);

            for (int twig = 0; twig < GypsophilaTwigs; twig++)
            {
                float fork = (twig - (GypsophilaTwigs - 1) * 0.5f) * 0.75f;
                Vector3 twigTip = armTip + frame.MultiplyVector(Spoke(turn + fork, lean + 0.25f)) * reach * 0.26f;
                BouquetShapes.AddRibbon(mesh, new[] { armTip, twigTip }, hair, 0.0005f, Outline.Detail, 0.0f);
                GypsophilaKnot(mesh, twigTip, floret, palette, arm * GypsophilaTwigs + twig);
            }
        }
    }

    private static Vector3 Spoke(float turn, float lean)
    {
        return new Vector3(Mathf.Cos(turn) * Mathf.Sin(lean), Mathf.Sin(turn) * Mathf.Sin(lean), Mathf.Cos(lean));
    }

    private static void GypsophilaKnot(MeshBuffer mesh, Vector3 at, float floret, BouquetPalette palette, int salt)
    {
        mesh.SetInk(Color.Lerp(palette.ink, palette.background, 0.12f));
        for (int d = 0; d < GypsophilaFlorets; d++)
        {
            float spin = d * Golden + salt;
            float radius = floret * (0.85f + 0.3f * Hash(salt, 40 + d, 3));
            Vector2 offset = new Vector2(Mathf.Cos(spin), Mathf.Sin(spin)) * floret * (d == 0 ? 0.0f : 1.35f);
            BouquetShapes.AddBillboardShape(mesh, at, offset, Offset(BouquetShapes.CircleRim(radius, 10, 1.0f), offset),
                palette.background, Outline.Contour, d * BouquetShapes.LayerStep * 0.3f);
        }
    }

    // sprays turn outward like the heads do, with a pull towards the front so the
    // bouquet's face shows leaves flat rather than as edges
    private static Matrix4x4 SprayFrame(Vector3 tip, Vector3 axis, float roll)
    {
        Vector3 facing = new Vector3(tip.x, 0.0f, tip.z) * SprayOutward + Vector3.back * SprayViewerBias;
        return BouquetShapes.SprayFrame(tip, axis, facing, Mathf.Sin(roll) * SprayTwist);
    }

    // round leaves in opposite pairs clasping the stem, overlapping up the spray
    private static void Eucalyptus(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        float spine = size * (3.1f + j0 * 0.6f);
        Color sage = palette.Shift(palette.leaf, EucalyptusSage);
        mesh.SetInk(palette.Line(sage, LeafInk));
        BouquetShapes.AddRibbon(mesh, BouquetShapes.CubicPath(tip, tip + axis * spine * 0.35f, tip + axis * spine * 0.7f, tip + axis * spine, 6),
            palette.leaf, 0.0016f, Outline.Contour, 0.0f);

        for (int pair = 0; pair < EucalyptusPairs; pair++)
        {
            float t = (pair + 0.35f) / EucalyptusPairs;
            float radius = size * Mathf.Lerp(0.23f, 0.12f, t);

            for (int side = 0; side < 2; side++)
            {
                float around = (pair * 0.5f + side) * Mathf.PI + EucalyptusPhase + Mathf.Sin(roll) * EucalyptusPhaseJitter;
                float reach = radius * EucalyptusReach;
                Matrix4x4 frame = SprayFrame(tip, axis, roll + Mathf.Sin(around) * EucalyptusTwist)
                                * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, Mathf.Sin(around) * reach));
                Vector2 at = new Vector2(Mathf.Cos(around) * reach, spine * t);
                float depth = (pair * 2 + side + 1) * BouquetShapes.LayerStep * 0.5f;
                BouquetShapes.AddCardShape(mesh, frame, Offset(BouquetShapes.CircleRim(radius, 15, 0.95f), at), at,
                    sage, Outline.Silhouette, depth, Bend.Bowl(at, LeafBowl / radius));
            }
        }

        Vector2 crown = new Vector2(0.0f, spine * 1.02f);
        float crownRadius = size * 0.10f;
        BouquetShapes.AddCardShape(mesh, SprayFrame(tip, axis, roll), Offset(BouquetShapes.CircleRim(crownRadius, 13, 0.86f), crown), crown,
            sage, Outline.Silhouette, EucalyptusPairs * 2 * BouquetShapes.LayerStep * 0.5f, Bend.Bowl(crown, LeafBowl / crownRadius));
    }

    // a feather of leaflets packed tight and swept up along the rib. half the ferns
    // are broad herringbone plumes, the rest narrow combs, both as in the reference
    private static void Fern(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        bool isComb = j0 > 0.5f;
        float spine = size * (2.6f + j0 * 0.5f);
        float reach = isComb ? 0.32f : 0.62f;
        float lean = isComb ? CombLean : FernLean;
        Matrix4x4 frame = SprayFrame(tip, axis, roll);

        Color blade = palette.Shift(palette.leaf, -0.10f);
        mesh.SetInk(palette.Line(blade, LeafInk));
        BouquetShapes.AddRibbon(mesh, new[] { tip, tip + axis * spine * 0.5f, tip + axis * spine }, palette.leaf, 0.0014f, Outline.Contour, 0.0f);

        for (int i = 0; i < FernLeaflets; i++)
        {
            float t = (i + 0.5f) / FernLeaflets;
            float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * (0.18f + 0.82f * t)), 0.6f);
            float length = size * reach * Mathf.Max(envelope, 0.25f);
            Vector2 root = new Vector2(0.0f, spine * (0.10f + 0.90f * t));

            for (int side = -1; side <= 1; side += 2)
            {
                float turn = side > 0 ? lean : Mathf.PI - lean;
                Vector2[] rim = BouquetShapes.PetalRim(length, length * 0.24f, 0.8f, 0.0f);
                for (int k = 0; k < rim.Length; k++)
                {
                    rim[k] = BouquetShapes.Rotate(rim[k], turn) + root;
                }

                Vector2 middle = root + BouquetShapes.Rotate(new Vector2(length * 0.45f, 0.0f), turn);
                BouquetShapes.AddCardShape(mesh, frame, rim, middle,
                    blade, Outline.Contour, (i + 1) * BouquetShapes.LayerStep * 0.4f, Bend.Bowl(middle, LeafBowl / length));
            }
        }
    }

    // one long decorative leaf, the element that breaks up a bouquet full of round shapes
    private static void LongBlade(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        float length = size * (2.6f + j0 * 0.9f);
        float width = size * (0.13f + j1 * 0.07f);
        Matrix4x4 frame = SprayFrame(tip, axis, roll);

        Color blade = palette.Shift(palette.leaf, -0.06f);
        mesh.SetInk(palette.Line(blade, LeafInk));
        float bend = length * (0.18f + j1 * 0.22f);
        float curl = width * 0.9f;
        BouquetShapes.AddBlade(mesh, frame, length, width, bend, curl, blade, Outline.Silhouette, BladeSteps);

        Vector3[] midrib = new Vector3[BladeSteps + 1];
        for (int i = 0; i <= BladeSteps; i++)
        {
            float t = Mathf.Lerp(0.08f, 0.88f, i / (float)BladeSteps);
            midrib[i] = frame.MultiplyPoint3x4(BouquetShapes.BladeSpine(length, bend, curl, t));
        }

        BouquetShapes.AddRibbon(mesh, midrib, palette.ink, detailWidth, Outline.Detail, BouquetShapes.LayerStep * 0.7f);
    }

    private static void LeafSprig(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        float spine = size * (2.2f + j0 * 0.6f);
        Matrix4x4 frame = SprayFrame(tip, axis, roll);
        mesh.SetInk(palette.Line(palette.leaf, LeafInk));
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

            Vector2 middle = root + BouquetShapes.Rotate(new Vector2(length * 0.45f, 0.0f), turn);
            BouquetShapes.AddCardShape(mesh, frame, rim, middle,
                palette.leaf, Outline.Contour, (i + 1) * BouquetShapes.LayerStep * 0.4f, Bend.Bowl(middle, LeafBowl / length));
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
