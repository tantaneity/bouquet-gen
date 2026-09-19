using UnityEngine;

// petalled heads, built in the head frame where z is the face
internal static class BloomHeads
{
    private const int DahliaRings = 5;
    private const int CarnationTeeth = 4;
    private const float CarnationJag = 0.10f;
    private const int VeinSteps = 4;
    private const int DiscSteps = 16;
    private const int SepalCount = 5;
    private const float SepalReflex = 38.0f;
    private const float SepalDrop = BouquetShapes.LayerStep * 0.8f;
    private const float PetalStep = BouquetShapes.LayerStep * 0.60f;
    private const float VeinLift = BouquetShapes.LayerStep * 0.25f;
    private const float PetalCurl = 0.22f;
    private const float PetalHollow = 0.45f;
    private const float DiscDome = 0.5f;

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
        float wiggle = BouquetHash.Unit(salt + p, 31, p * 7 + 3);
        return p / (float)petals * Mathf.PI * 2.0f + phase + (wiggle - 0.5f) * rollJitter;
    }

    private static void PetalRing(MeshBuffer mesh, Matrix4x4 head, PetalRingSpec ring, Color fill, float outlineWeight,
        bool isFrilled = false)
    {
        for (int p = 0; p < ring.petals; p++)
        {
            float turn = PetalTurn(ring.petals, p, ring.phase, ring.rollJitter, ring.salt);
            float scale = PetalScale(ring, p);
            float skew = (BouquetHash.Unit(ring.salt + p, 33, p * 13 + 7) - 0.5f) * 0.5f;

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
        return 1.0f + (BouquetHash.Unit(ring.salt + p, 32, p * 11 + 5) - 0.5f) * ring.sizeJitter;
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
            float turn = spec.spread ? i * BouquetShapes.GoldenAngle : i / (float)spec.count * Mathf.PI * 2.0f + spec.phase;
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
            float length = size * (0.58f + 0.20f * BouquetHash.Unit(salt + p, 34, p * 5 + 1));
            Vector2[] rim = BouquetShapes.PetalRim(length, size * 0.15f, 0.80f, 0.0f);
            Matrix4x4 frame = PetalFrame(head, -SepalDrop - p * PetalStep, turn, -SepalReflex);
            BouquetShapes.AddCardShape(mesh, frame, rim, new Vector2(length * 0.45f, 0.0f), sepal, Outline.Contour, 0.0f,
                PetalBend(length, size * 0.15f, -PetalCurl));
        }
    }

    internal static void Rose(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
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
    internal static void OpenBloom(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
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

    internal static void Anemone(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
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
    internal static void Dahlia(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
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
    internal static void Carnation(MeshBuffer mesh, Matrix4x4 head, float size, float roll, Color bloom, BouquetPalette palette,
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
        mesh.SetInk(palette.Line(tube, BouquetFlora.LeafInk));
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

    internal static void BerryCluster(MeshBuffer mesh, Vector3 tip, float size, Color bloom, float j0)
    {
        int berries = 5 + Mathf.RoundToInt(j0 * 5.0f);
        mesh.SetInk(Line(bloom));
        for (int i = 0; i < berries; i++)
        {
            float turn = i * BouquetShapes.GoldenAngle;
            float spill = size * (0.18f + 0.52f * i / berries);
            Vector2 at = new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * spill;
            float radius = size * (0.30f - 0.10f * i / berries);
            BouquetShapes.AddBillboardShape(mesh, tip, at, BouquetShapes.Offset(BouquetShapes.CircleRim(radius, 12, 1.0f), at),
                bloom, Outline.Small, i * BouquetShapes.LayerStep);
        }
    }

    private static Color Line(Color fill)
    {
        Color.RGBToHSV(fill, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s * 1.05f), Mathf.Clamp01(v * 0.42f));
    }
}
