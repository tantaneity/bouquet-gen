using UnityEngine;

// leaves and filler that run on past the stem tip, built in the spray frame where y is the rib
internal static class LeafSprays
{
    private const int GypsophilaArms = 7;
    private const int GypsophilaTwigs = 3;
    private const int GypsophilaFlorets = 4;
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
    private const int BladeSteps = 9;
    private const int SprigLeaves = 9;
    private const float LeafBowl = 0.35f;

    // whorls of round buds around a rib that runs out past the last of them, lilac
    // whatever the palette, lighter at the base, with two bracts forking under it
    internal static void Lavender(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, Color bloom,
        BouquetPalette palette, float j0)
    {
        float spike = size * (2.7f + j0 * 0.6f);
        Matrix4x4 frame = SprayFrame(tip, axis, roll);
        Vector3 across = frame.MultiplyVector(Vector3.right).normalized;
        Vector3 through = frame.MultiplyVector(Vector3.forward).normalized;

        mesh.SetInk(palette.Line(palette.stem, BouquetFlora.LeafInk));
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
            mesh.SetInk(palette.Line(bud, BouquetFlora.LeafInk));

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
    internal static void Gypsophila(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette, float j0)
    {
        float reach = size * (1.5f + j0 * 0.5f);
        float floret = size * 0.062f;
        Color hair = Color.Lerp(palette.ink, palette.background, 0.45f);
        Matrix4x4 frame = BouquetShapes.Frame(tip, axis, roll);

        for (int arm = 0; arm < GypsophilaArms; arm++)
        {
            float turn = arm * BouquetShapes.GoldenAngle;
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
            float spin = d * BouquetShapes.GoldenAngle + salt;
            float radius = floret * (0.85f + 0.3f * BouquetHash.Unit(salt, 40 + d, 3));
            Vector2 offset = new Vector2(Mathf.Cos(spin), Mathf.Sin(spin)) * floret * (d == 0 ? 0.0f : 1.35f);
            BouquetShapes.AddBillboardShape(mesh, at, offset, BouquetShapes.Offset(BouquetShapes.CircleRim(radius, 10, 1.0f), offset),
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
    internal static void Eucalyptus(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        float spine = size * (3.1f + j0 * 0.6f);
        Color sage = palette.Shift(palette.leaf, EucalyptusSage);
        mesh.SetInk(palette.Line(sage, BouquetFlora.LeafInk));
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
                BouquetShapes.AddCardShape(mesh, frame, BouquetShapes.Offset(BouquetShapes.CircleRim(radius, 15, 0.95f), at), at,
                    sage, Outline.Silhouette, depth, Bend.Bowl(at, LeafBowl / radius));
            }
        }

        Vector2 crown = new Vector2(0.0f, spine * 1.02f);
        float crownRadius = size * 0.10f;
        BouquetShapes.AddCardShape(mesh, SprayFrame(tip, axis, roll), BouquetShapes.Offset(BouquetShapes.CircleRim(crownRadius, 13, 0.86f), crown), crown,
            sage, Outline.Silhouette, EucalyptusPairs * 2 * BouquetShapes.LayerStep * 0.5f, Bend.Bowl(crown, LeafBowl / crownRadius));
    }

    // a feather of leaflets packed tight and swept up along the rib. half the ferns
    // are broad herringbone plumes, the rest narrow combs, both as in the reference
    internal static void Fern(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        bool isComb = j0 > 0.5f;
        float spine = size * (2.6f + j0 * 0.5f);
        float reach = isComb ? 0.32f : 0.62f;
        float lean = isComb ? CombLean : FernLean;
        Matrix4x4 frame = SprayFrame(tip, axis, roll);

        Color blade = palette.Shift(palette.leaf, -0.10f);
        mesh.SetInk(palette.Line(blade, BouquetFlora.LeafInk));
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
    internal static void LongBlade(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0, float j1)
    {
        float length = size * (2.6f + j0 * 0.9f);
        float width = size * (0.13f + j1 * 0.07f);
        Matrix4x4 frame = SprayFrame(tip, axis, roll);

        Color blade = palette.Shift(palette.leaf, -0.06f);
        mesh.SetInk(palette.Line(blade, BouquetFlora.LeafInk));
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

    internal static void LeafSprig(MeshBuffer mesh, Vector3 tip, Vector3 axis, float size, float roll, BouquetPalette palette,
        float detailWidth, float j0)
    {
        float spine = size * (2.2f + j0 * 0.6f);
        Matrix4x4 frame = SprayFrame(tip, axis, roll);
        mesh.SetInk(palette.Line(palette.leaf, BouquetFlora.LeafInk));
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

    private static float Frac(float value)
    {
        return value - Mathf.Floor(value);
    }
}
