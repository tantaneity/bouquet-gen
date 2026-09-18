using System.Collections.Generic;
using UnityEngine;

public enum Role
{
    Dominant = 0,
    Medium = 1,
    Filler = 2,
    Spire = 3,
    Foliage = 4,
    FrontFoliage = 5
}

// composition. a florist works with a face, a few dominant blooms on it, greens
// on the shoulders and tall thin things behind, which is not what a ring of
// evenly spaced stems produces
public static class BouquetGeometry
{
    private const int StemSteps = 12;
    private const int BandSteps = 22;
    private const int RoleCount = 6;

    private struct Band
    {
        public float azimuthCentre;
        public float azimuthSpread;
        public float tiltLow;
        public float tiltHigh;
        public float lengthLow;
        public float lengthHigh;
        public float scaleLow;
        public float scaleHigh;
        public float depth;
        public float stemWidth;
        public int countLow;
        public int countHigh;
    }

    private static readonly Band[] Bands =
    {
        //                      az centre  spread  tilt lo/hi   length lo/hi  scale lo/hi   depth  count lo/hi
        new Band { azimuthCentre =   0.0f, azimuthSpread = 150.0f, tiltLow = 0.26f, tiltHigh = 0.58f, lengthLow = 0.44f, lengthHigh = 0.62f, scaleLow = 1.25f, scaleHigh = 1.70f, depth =  0.10f, stemWidth = 1.00f, countLow = 3, countHigh = 6 },
        new Band { azimuthCentre =   0.0f, azimuthSpread = 280.0f, tiltLow = 0.46f, tiltHigh = 0.86f, lengthLow = 0.52f, lengthHigh = 0.74f, scaleLow = 0.85f, scaleHigh = 1.22f, depth =  0.02f, stemWidth = 0.92f, countLow = 6, countHigh = 10 },
        new Band { azimuthCentre =   0.0f, azimuthSpread = 360.0f, tiltLow = 0.60f, tiltHigh = 1.15f, lengthLow = 0.52f, lengthHigh = 0.84f, scaleLow = 0.34f, scaleHigh = 0.60f, depth = -0.06f, stemWidth = 0.68f, countLow = 6, countHigh = 14 },
        new Band { azimuthCentre = 180.0f, azimuthSpread = 230.0f, tiltLow = 0.45f, tiltHigh = 0.95f, lengthLow = 0.92f, lengthHigh = 1.22f, scaleLow = 0.55f, scaleHigh = 0.88f, depth = -0.12f, stemWidth = 0.62f, countLow = 4, countHigh = 8 },
        new Band { azimuthCentre = 180.0f, azimuthSpread = 360.0f, tiltLow = 0.80f, tiltHigh = 1.18f, lengthLow = 0.78f, lengthHigh = 1.12f, scaleLow = 0.80f, scaleHigh = 1.25f, depth = -0.08f, stemWidth = 0.82f, countLow = 7, countHigh = 14 },
        new Band { azimuthCentre =   0.0f, azimuthSpread = 160.0f, tiltLow = 0.40f, tiltHigh = 0.80f, lengthLow = 0.44f, lengthHigh = 0.74f, scaleLow = 0.85f, scaleHigh = 1.20f, depth =  0.17f, stemWidth = 0.90f, countLow = 2, countHigh = 4 }
    };

    private static readonly Species[] RoleSpecies =
    {
        Species.Rose, Species.OpenBloom, Species.Rose, Species.Dahlia,
        Species.OpenBloom, Species.Anemone, Species.Rose, Species.Dahlia,
        Species.BerryCluster, Species.Anemone, Species.BerryCluster, Species.Dahlia,
        Species.Lavender, Species.Gypsophila, Species.Lavender, Species.Gypsophila,
        Species.Eucalyptus, Species.Fern, Species.LongBlade, Species.LeafSprig,
        Species.Eucalyptus, Species.LeafSprig, Species.LongBlade, Species.Fern
    };

    private struct Stalk
    {
        public Vector3 anchor;
        public Vector3 tip;
        public Vector3 handleLow;
        public Vector3 handleHigh;
        public Vector3 cutEnd;
        public Vector3 axis;
        public Species species;
        public Color bloom;
        public float headSize;
        public float stemWidth;
        public float roll;
        public int index;
    }

    public static void Build(MeshBuffer mesh, BouquetSettings settings, BouquetPalette palette)
    {
        Vector3 bind = new Vector3(0.0f, settings.bindHeight, 0.0f);
        List<Stalk> plan = BuildPlan(settings, palette, bind);

        foreach (Stalk stalk in plan)
        {
            Color stemColour = palette.Vary(palette.stem,
                BouquetFlora.Hash(stalk.index, 40, settings.seed),
                BouquetFlora.Hash(stalk.index, 41, settings.seed), settings.colourVariation * 0.5f);

            float width = settings.stemWidth * stalk.stemWidth;
            float weight = Mathf.Lerp(Outline.Small, Outline.Silhouette, stalk.stemWidth);

            BouquetShapes.AddRibbon(mesh, BouquetShapes.CubicPath(stalk.anchor, stalk.handleLow, stalk.handleHigh, stalk.tip, StemSteps),
                stemColour, width, weight, 0.0f);

            BouquetShapes.AddRibbon(mesh, new[] { stalk.anchor, Vector3.Lerp(stalk.anchor, stalk.cutEnd, 0.5f), stalk.cutEnd },
                stemColour, width, weight, 0.0f);

            BouquetFlora.Add(mesh, stalk.species, stalk.tip, stalk.axis, stalk.headSize, stalk.roll,
                stalk.bloom, palette, settings.detailWidth, stalk.index, settings.seed);
        }

        AddRibbonWrap(mesh, settings, palette, bind);
    }

    private static List<Stalk> BuildPlan(BouquetSettings settings, BouquetPalette palette, Vector3 bind)
    {
        List<Stalk> plan = new List<Stalk>();
        int index = 0;

        for (int role = 0; role < RoleCount; role++)
        {
            Band band = Bands[role];
            int count = Mathf.RoundToInt(Mathf.Lerp(band.countLow, band.countHigh, settings.density));

            for (int slot = 0; slot < count; slot++)
            {
                plan.Add(BuildStalk(settings, palette, bind, (Role)role, band, slot, count, index));
                index++;
            }
        }

        return plan;
    }

    private static Stalk BuildStalk(BouquetSettings settings, BouquetPalette palette, Vector3 bind,
        Role role, Band band, int slot, int count, int index)
    {
        int seed = settings.seed;

        // stratified inside the role's arc, then jittered: pure random clumps and
        // an even step reads as a machine
        float across = (slot + 0.15f + 0.70f * BouquetFlora.Hash(index, 0, seed)) / count - 0.5f;
        float azimuth = band.azimuthCentre + band.azimuthSpread * across;
        float azimuthRadians = azimuth * Mathf.Deg2Rad;

        float bias = Mathf.Cos(azimuthRadians - settings.asymmetryAngle * Mathf.Deg2Rad);
        float flatten = 1.0f - settings.faceFlatten * Mathf.Cos(2.0f * azimuthRadians);
        float tiltFraction = Mathf.Lerp(band.tiltLow, band.tiltHigh, BouquetFlora.Hash(index, 1, seed));
        float tilt = settings.coneHalfAngle * Mathf.Deg2Rad * tiltFraction * flatten * (1.0f + settings.asymmetry * bias);

        Species species = RoleSpecies[(int)role * 4 + Mathf.FloorToInt(BouquetFlora.Hash(index, 2, seed) * 3.999f)];

        float scale = Mathf.Lerp(band.scaleLow, band.scaleHigh, BouquetFlora.Hash(index, 3, seed));
        float headSize = settings.headScale * scale;

        float lengthRoll = BouquetFlora.Hash(index, 4, seed);
        float kick = BouquetFlora.Hash(index, 5, seed) < 0.18f ? 1.20f : 1.0f;
        float silhouette = settings.stemLength * Mathf.Lerp(band.lengthLow, band.lengthHigh, lengthRoll) * kick;
        float reach = Mathf.Max(silhouette - BouquetFlora.TipReserve(species) * headSize, settings.stemLength * 0.22f);

        Vector2 around = new Vector2(Mathf.Cos(azimuthRadians), Mathf.Sin(azimuthRadians));
        Vector3 heading = new Vector3(around.x * Mathf.Sin(tilt), Mathf.Cos(tilt), around.y * Mathf.Sin(tilt));

        // stems leave the tie at different heights and are twisted round it, which
        // is what a spiral bound bundle looks like instead of a generation point
        float twist = settings.bundleTwist * Mathf.Deg2Rad * tiltFraction;
        Vector2 anchorAround = new Vector2(Mathf.Cos(azimuthRadians + twist), Mathf.Sin(azimuthRadians + twist));
        Vector3 anchor = bind
            + new Vector3(anchorAround.x, 0.0f, anchorAround.y) * settings.ribbonWidth * (0.25f + 0.70f * tiltFraction)
            + Vector3.up * (BouquetFlora.Hash(index, 6, seed) - 0.5f) * settings.tieHeight;

        Vector3 tip = anchor + heading * reach;
        tip += Vector3.forward * band.depth * settings.depthSpread;
        tip += Vector3.up * (BouquetFlora.Hash(index, 7, seed) - 0.5f) * reach * 0.10f;

        Vector3 lateral = Vector3.Normalize(Vector3.Cross(heading, Vector3.up));
        float bend = (BouquetFlora.Hash(index, 8, seed) - 0.5f) * settings.sideBend;
        float curve = settings.outwardCurve * (0.55f + 0.90f * BouquetFlora.Hash(index, 9, seed));

        Vector3 handleLow = anchor + Vector3.up * reach * curve * 0.45f;
        Vector3 handleHigh = tip - heading * reach * 0.34f + lateral * bend * reach;

        float cutLength = settings.cutLength * (0.5f + 0.5f * BouquetFlora.Hash(index, 10, seed));
        Vector3 cutEnd = anchor + Vector3.Normalize(new Vector3(-heading.x, -1.05f, -heading.z)) * cutLength;

        int bloomSlot = Mathf.FloorToInt(BouquetFlora.Hash(index, 11, seed) * 2.999f);
        Color bloom = palette.Vary(palette.Bloom(bloomSlot),
            BouquetFlora.Hash(index, 12, seed), BouquetFlora.Hash(index, 13, seed), settings.colourVariation);

        return new Stalk
        {
            anchor = anchor,
            tip = tip,
            handleLow = handleLow,
            handleHigh = handleHigh,
            cutEnd = cutEnd,
            axis = Vector3.Normalize(tip - handleHigh),
            species = species,
            bloom = bloom,
            headSize = headSize,
            stemWidth = band.stemWidth,
            roll = BouquetFlora.Hash(index, 14, seed) * Mathf.PI * 2.0f,
            index = index
        };
    }

    private static void AddRibbonWrap(MeshBuffer mesh, BouquetSettings settings, BouquetPalette palette, Vector3 bind)
    {
        float radius = settings.ribbonWidth;
        float halfHeight = radius * 0.30f;

        int first = mesh.VertexCount;
        for (int s = 0; s <= BandSteps; s++)
        {
            float t = s / (float)BandSteps * Mathf.PI * 2.0f;
            Vector3 outward = new Vector3(Mathf.Cos(t), 0.0f, Mathf.Sin(t));
            Vector3 rim = bind + outward * radius;

            mesh.AddVertex(rim + Vector3.down * halfHeight, Vector3.down, Vector4.zero, palette.ribbon, StrokeKind.Card, 0.0f, Outline.Silhouette, 0.0f);
            mesh.AddVertex(rim + Vector3.up * halfHeight, Vector3.up, Vector4.zero, palette.ribbon, StrokeKind.Card, 0.0f, Outline.Silhouette, 0.0f);

            if (s > 0)
            {
                int here = first + s * 2;
                mesh.AddQuad(here - 2, here - 1, here + 1, here);
            }
        }

        float knotAngle = settings.knotAngle * Mathf.Deg2Rad;
        Vector3 knotOut = new Vector3(Mathf.Cos(knotAngle), 0.0f, Mathf.Sin(knotAngle));
        Vector3 knot = bind + knotOut * radius * 1.04f;

        Matrix4x4 knotFrame = BouquetShapes.Frame(knot, knotOut, 0.0f);
        Vector2[] knotRim = BouquetShapes.PetalRim(radius * 0.62f, radius * 0.26f, 0.7f, 0.0f);
        for (int i = 0; i < knotRim.Length; i++)
        {
            knotRim[i] = BouquetShapes.Rotate(knotRim[i], Mathf.PI * 0.62f);
        }

        BouquetShapes.AddCardShape(mesh, knotFrame, knotRim, new Vector2(0.0f, radius * 0.2f), palette.ribbon, Outline.Contour, BouquetShapes.LayerStep);

        Vector3 sideways = Vector3.Normalize(Vector3.Cross(knotOut, Vector3.up));
        for (int i = 0; i < 2; i++)
        {
            float sway = (i == 0) ? -1.0f : 0.5f;
            Vector3 start = knot - Vector3.up * halfHeight * 0.5f + sideways * radius * (0.12f + 0.26f * i);
            Vector3 low = start + sideways * sway * settings.tailLength * 0.26f - Vector3.up * settings.tailLength * 0.42f;
            Vector3 high = start + sideways * sway * settings.tailLength * 0.34f - Vector3.up * settings.tailLength * 0.78f;
            Vector3 end = start + sideways * sway * settings.tailLength * 0.18f - Vector3.up * settings.tailLength;
            BouquetShapes.AddCubicRibbon(mesh, start, low, high, end, palette.ribbon, settings.stemWidth * 2.4f, Outline.Contour);
        }
    }

    public static void AddRibbon(MeshBuffer mesh, IReadOnlyList<Vector3> points, Color fill, float width)
    {
        BouquetShapes.AddRibbon(mesh, points, fill, width, Outline.Contour, 0.0f);
    }

    public static void AddHandle(MeshBuffer mesh, Vector3 position, float radius, Color fill)
    {
        BouquetShapes.AddBillboardDisc(mesh, position, radius, fill, Outline.Contour, 0.0f);
    }
}
