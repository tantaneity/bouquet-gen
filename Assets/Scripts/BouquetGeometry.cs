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
    private const int ClearancePasses = 4;
    private const float HeadClearance = 1.05f;
    private const float MaxReserveShare = 0.55f;

    private struct Band
    {
        public float azimuthCentre;
        public float azimuthSpread;
        public float radiusLow;
        public float radiusHigh;
        public float heightLow;
        public float heightHigh;
        public float scaleLow;
        public float scaleHigh;
        public float depth;
        public float stemWidth;
        public int countLow;
        public int countHigh;
    }

    // tips are aimed at a place in the bouquet rather than derived from an angle and
    // a length, so the shape is a decision instead of an outcome
    private static readonly Band[] Bands =
    {
        //                          az centre  spread   radius lo/hi   height lo/hi   scale lo/hi    depth  stem   count
        new Band { azimuthCentre =   0.0f, azimuthSpread =   0.0f, radiusLow = 0.00f, radiusHigh = 0.00f, heightLow = 0.00f, heightHigh = 0.00f, scaleLow = 1.30f, scaleHigh = 1.75f, depth =  0.00f, stemWidth = 1.00f, countLow = 3, countHigh = 5 },
        new Band { azimuthCentre =   0.0f, azimuthSpread = 300.0f, radiusLow = 0.30f, radiusHigh = 0.56f, heightLow = 0.34f, heightHigh = 0.62f, scaleLow = 0.72f, scaleHigh = 1.02f, depth = -0.02f, stemWidth = 0.88f, countLow = 5, countHigh = 8 },
        new Band { azimuthCentre =   0.0f, azimuthSpread = 360.0f, radiusLow = 0.10f, radiusHigh = 0.40f, heightLow = 0.20f, heightHigh = 0.50f, scaleLow = 0.30f, scaleHigh = 0.50f, depth = -0.30f, stemWidth = 0.58f, countLow = 6, countHigh = 12 },
        new Band { azimuthCentre = 180.0f, azimuthSpread = 190.0f, radiusLow = 0.26f, radiusHigh = 0.58f, heightLow = 0.72f, heightHigh = 1.06f, scaleLow = 0.52f, scaleHigh = 0.82f, depth = -0.46f, stemWidth = 0.52f, countLow = 3, countHigh = 5 },
        new Band { azimuthCentre = 180.0f, azimuthSpread = 360.0f, radiusLow = 0.42f, radiusHigh = 0.70f, heightLow = 0.36f, heightHigh = 0.78f, scaleLow = 0.82f, scaleHigh = 1.20f, depth = -0.12f, stemWidth = 0.78f, countLow = 6, countHigh = 10 },
        new Band { azimuthCentre =   0.0f, azimuthSpread = 170.0f, radiusLow = 0.26f, radiusHigh = 0.50f, heightLow = 0.16f, heightHigh = 0.42f, scaleLow = 1.00f, scaleHigh = 1.35f, depth =  0.34f, stemWidth = 0.86f, countLow = 2, countHigh = 4 }
    };

    private static readonly Species[] RoleSpecies =
    {
        Species.Rose, Species.Carnation, Species.Rose, Species.Dahlia,
        Species.Carnation, Species.Anemone, Species.OpenBloom, Species.Dahlia,
        Species.BerryCluster, Species.Anemone, Species.BerryCluster, Species.Gypsophila,
        Species.Lavender, Species.Gypsophila, Species.Lavender, Species.Gypsophila,
        Species.Eucalyptus, Species.Fern, Species.LongBlade, Species.LeafSprig,
        Species.LongBlade, Species.Eucalyptus, Species.LeafSprig, Species.LongBlade
    };

    // a loose asymmetric arc, not a ring. offsets are lateral, height, depth, scaled
    // by stem length so the dial still moves the whole bouquet
    private static readonly Vector3 TowardViewer = Vector3.back;

    private static readonly Vector3[] DominantTargets =
    {
        new Vector3(-0.34f, 0.28f,  0.20f),
        new Vector3( 0.30f, 0.46f, -0.02f),
        new Vector3( 0.06f, 0.14f,  0.26f),
        new Vector3(-0.48f, 0.44f, -0.06f),
        new Vector3( 0.52f, 0.22f,  0.08f),
        new Vector3( 0.18f, 0.06f,  0.22f)
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
        public Role role;
        public Vector3 target;
        public float growth;
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

            // a stem outline that goes near black turns the bundle into spaghetti,
            // so the line sinks most of the way into the stem's own green
            mesh.SetInk(palette.Line(stemColour, 0.74f));

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
        List<GrownTip> bloomTips = new List<GrownTip>();
        int index = 0;

        for (int role = 0; role < RoleCount; role++)
        {
            if ((Role)role == Role.Filler)
            {
                continue;
            }

            Band band = Bands[role];
            float quota = Quota(band, settings.density);

            for (int slot = 0; slot < band.countHigh; slot++, index++)
            {
                float growth = Growth(quota, slot);
                Vector3 target = TargetFor(settings, (Role)role, band, slot, Mathf.Max(quota, 1.0f), index);
                Stalk stalk = GrowStalk(settings, palette, bind, index, (Role)role, target * growth, growth);

                if (BouquetFlora.IsBloom(stalk.species) && role <= (int)Role.Medium)
                {
                    bloomTips.Add(new GrownTip { tip = stalk.tip, growth = growth });
                }

                if (growth > 0.0f)
                {
                    plan.Add(stalk);
                }
            }
        }

        AddConnectors(plan, settings, palette, bind, bloomTips, index);
        KeepSpraysClearOfHeads(plan, settings, palette, bind);
        return plan;
    }

    private static Stalk GrowStalk(BouquetSettings settings, BouquetPalette palette, Vector3 bind,
        int index, Role role, Vector3 target, float growth)
    {
        Stalk stalk = MakeStalk(settings, palette, bind, index, role, Bands[(int)role], target);
        stalk.headSize *= growth;
        stalk.growth = growth;
        return stalk;
    }

    // every stalk is a capsule running on from its tip (a bloom's is just a ball).
    // where one overlaps a bloom head its target is walked out of the ball, so leaves
    // lie beside the petals instead of through them. of two heads the smaller gives way
    private static void KeepSpraysClearOfHeads(List<Stalk> plan, BouquetSettings settings, BouquetPalette palette, Vector3 bind)
    {
        for (int pass = 0; pass < ClearancePasses; pass++)
        {
            for (int i = 0; i < plan.Count; i++)
            {
                Stalk stalk = plan[i];
                Vector3 push = WithoutSinking(OverlapWithHeads(plan, i, stalk), stalk.target);
                if (push.sqrMagnitude > 1e-8f)
                {
                    plan[i] = GrowStalk(settings, palette, bind, stalk.index, stalk.role, stalk.target + push, stalk.growth);
                }
            }
        }
    }

    private static float BodyRadius(Stalk stalk)
    {
        return BouquetFlora.IsBloom(stalk.species)
            ? stalk.headSize * HeadClearance
            : BouquetFlora.SprayRadius(stalk.species) * stalk.headSize;
    }

    private static Vector3 OverlapWithHeads(List<Stalk> plan, int self, Stalk stalk)
    {
        Vector3 start = stalk.tip;
        Vector3 end = stalk.tip + stalk.axis * BouquetFlora.TipReserve(stalk.species) * stalk.headSize;
        float radius = BodyRadius(stalk);
        bool isBloom = BouquetFlora.IsBloom(stalk.species);
        Vector3 push = Vector3.zero;

        for (int j = 0; j < plan.Count; j++)
        {
            Stalk head = plan[j];
            if (j == self || !BouquetFlora.IsBloom(head.species))
            {
                continue;
            }

            if (isBloom && (head.headSize < stalk.headSize || (head.headSize == stalk.headSize && j > self)))
            {
                continue;
            }

            Vector3 nearest = NearestOnSegment(start, end, head.tip);
            Vector3 away = nearest - head.tip;
            float distance = away.magnitude;
            float overlap = BodyRadius(head) + radius - distance;
            if (overlap <= 0.0f)
            {
                continue;
            }

            Vector3 direction = distance > 1e-4f ? away / distance : Vector3.Cross(stalk.axis, Vector3.up).normalized;
            push += direction * overlap;
        }

        return push;
    }

    // sliding a stalk back toward the tie would bury its head in the bundle, and
    // pushing it down lays it flat across the ribbon, so only the sideways, outward
    // and upward part of a push is kept
    private static Vector3 WithoutSinking(Vector3 push, Vector3 target)
    {
        push.y = Mathf.Max(push.y, 0.0f);
        Vector3 reach = target.normalized;
        return push - reach * Mathf.Min(0.0f, Vector3.Dot(push, reach));
    }

    private static Vector3 NearestOnSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 span = end - start;
        float t = Mathf.Clamp01(Vector3.Dot(point - start, span) / Mathf.Max(span.sqrMagnitude, 1e-8f));
        return start + span * t;
    }

    private struct GrownTip
    {
        public Vector3 tip;
        public float growth;
    }

    // every slot up to the band's ceiling keeps its index whatever the density, so
    // a density change grows or shrinks the last stems instead of reshuffling the
    // species of everything that comes after them
    private static float Quota(Band band, float density)
    {
        return Mathf.Lerp(band.countLow, band.countHigh, density);
    }

    private static float Growth(float quota, int slot)
    {
        return Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(quota - slot));
    }

    private static Vector3 TargetFor(BouquetSettings settings, Role role, Band band, int slot, float count, int index)
    {
        int seed = settings.seed;
        float scale = settings.stemLength;

        if (role == Role.Dominant)
        {
            Vector3 staged = DominantTargets[slot % DominantTargets.Length];
            Vector3 jitter = new Vector3(
                (BouquetFlora.Hash(index, 15, seed) - 0.5f) * 0.14f,
                (BouquetFlora.Hash(index, 16, seed) - 0.5f) * 0.12f,
                (BouquetFlora.Hash(index, 17, seed) - 0.5f) * 0.10f);
            Vector3 offset = (staged + jitter) * scale;
            return new Vector3(offset.x, offset.y, 0.0f) + TowardViewer * offset.z;
        }

        float across = (slot + 0.15f + 0.70f * BouquetFlora.Hash(index, 0, seed)) / count - 0.5f;
        float azimuth = (band.azimuthCentre + band.azimuthSpread * across) * Mathf.Deg2Rad;

        float bias = Mathf.Cos(azimuth - settings.asymmetryAngle * Mathf.Deg2Rad);
        float flatten = 1.0f - settings.faceFlatten * Mathf.Cos(2.0f * azimuth);

        float radius = Mathf.Lerp(band.radiusLow, band.radiusHigh, BouquetFlora.Hash(index, 1, seed))
                     * flatten * (1.0f + settings.asymmetry * bias) * settings.spreadGain;
        float height = Mathf.Lerp(band.heightLow, band.heightHigh, BouquetFlora.Hash(index, 4, seed));

        return new Vector3(Mathf.Cos(azimuth) * radius, height, Mathf.Sin(azimuth) * radius) * scale
             + TowardViewer * band.depth * settings.depthSpread;
    }

    // filler is connective tissue: it goes where two major heads leave a hole, set
    // back so it reads behind them, not scattered on its own ring
    private static void AddConnectors(List<Stalk> plan, BouquetSettings settings, BouquetPalette palette,
        Vector3 bind, List<GrownTip> bloomTips, int index)
    {
        Band band = Bands[(int)Role.Filler];
        float quota = Quota(band, settings.density);
        if (bloomTips.Count < 2)
        {
            return;
        }

        for (int slot = 0; slot < band.countHigh; slot++, index++)
        {
            int a = Mathf.FloorToInt(BouquetFlora.Hash(index, 18, settings.seed) * (bloomTips.Count - 0.001f));
            int b = (a + 1 + Mathf.FloorToInt(BouquetFlora.Hash(index, 19, settings.seed) * (bloomTips.Count - 1.001f))) % bloomTips.Count;

            float growth = Growth(quota, slot) * Mathf.Min(bloomTips[a].growth, bloomTips[b].growth);
            if (growth <= 0.0f)
            {
                continue;
            }

            Vector3 gap = Vector3.Lerp(bloomTips[a].tip, bloomTips[b].tip, 0.35f + 0.30f * BouquetFlora.Hash(index, 23, settings.seed));
            gap -= bind;
            gap += (new Vector3(
                (BouquetFlora.Hash(index, 24, settings.seed) - 0.5f) * 0.16f,
                (BouquetFlora.Hash(index, 25, settings.seed) - 0.5f) * 0.20f + 0.06f,
                0.0f)
                - TowardViewer * (0.10f + 0.18f * BouquetFlora.Hash(index, 26, settings.seed))) * settings.stemLength;

            plan.Add(GrowStalk(settings, palette, bind, index, Role.Filler, gap * growth, growth));
        }
    }

    private static Stalk MakeStalk(BouquetSettings settings, BouquetPalette palette, Vector3 bind,
        int index, Role role, Band band, Vector3 target)
    {
        int seed = settings.seed;

        Species species = RoleSpecies[(int)role * 4 + Mathf.FloorToInt(BouquetFlora.Hash(index, 2, seed) * 3.999f)];
        float headSize = settings.headScale * Mathf.Lerp(band.scaleLow, band.scaleHigh, BouquetFlora.Hash(index, 3, seed));

        Vector3 targetTip = bind + target;
        Vector3 heading = Vector3.Normalize(targetTip - bind);

        // a sprig head keeps climbing past the stem, so the stem stops short of the
        // place the silhouette is supposed to reach. a low target cannot give up more
        // than part of its stem, or the tip sinks into the tie and the spray points sideways
        float reserve = Mathf.Min(BouquetFlora.TipReserve(species) * headSize, target.magnitude * MaxReserveShare);
        Vector3 tip = targetTip - heading * reserve;

        Vector3 flat = new Vector3(target.x, 0.0f, target.z);
        float radial = flat.magnitude;
        float azimuth = Mathf.Atan2(target.z, target.x);
        float rank = Mathf.Clamp01(radial / Mathf.Max(settings.stemLength * 0.7f, 1e-3f));

        float twist = settings.bundleTwist * Mathf.Deg2Rad * rank;
        Vector2 around = new Vector2(Mathf.Cos(azimuth + twist), Mathf.Sin(azimuth + twist));
        Vector3 anchor = bind
            + new Vector3(around.x, 0.0f, around.y) * settings.ribbonWidth * (0.25f + 0.70f * rank)
            + Vector3.up * (BouquetFlora.Hash(index, 6, seed) - 0.5f) * settings.tieHeight;

        Vector3 span = tip - anchor;
        float reach = Mathf.Max(span.magnitude, 1e-3f);
        Vector3 direction = span / reach;

        Vector3 lateral = Vector3.Cross(direction, Vector3.up);
        lateral = lateral.sqrMagnitude > 1e-4f ? lateral.normalized : Vector3.right;

        float bend = (BouquetFlora.Hash(index, 8, seed) - 0.5f) * settings.sideBend;
        float curve = settings.outwardCurve * (0.50f + 1.00f * BouquetFlora.Hash(index, 9, seed));

        Vector3 handleLow = anchor + Vector3.up * reach * curve * 0.48f;
        Vector3 handleHigh = tip - direction * reach * 0.34f + lateral * bend * reach;

        float cutLength = settings.cutLength * (0.5f + 0.5f * BouquetFlora.Hash(index, 10, seed));
        Vector3 cutEnd = anchor + Vector3.Normalize(new Vector3(-direction.x, -1.05f, -direction.z)) * cutLength;

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
            index = index,
            role = role,
            target = target
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

            mesh.SetInk(palette.Line(palette.ribbon, 0.34f));
            mesh.AddVertex(rim + Vector3.down * halfHeight, Vector3.down, Vector4.zero, palette.ribbon, StrokeKind.Card, 0.0f, Outline.Silhouette, 0.0f,
                Shading.Surface(outward));
            mesh.AddVertex(rim + Vector3.up * halfHeight, Vector3.up, Vector4.zero, palette.ribbon, StrokeKind.Card, 0.0f, Outline.Silhouette, 0.0f,
                Shading.Surface(outward));

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
}
