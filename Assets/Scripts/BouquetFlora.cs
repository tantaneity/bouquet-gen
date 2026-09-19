using UnityEngine;

// one head, drawn. blooms turn outward from the bundle the way cut flowers do, so
// the far side of the bouquet shows the backs of its heads and their calyx
public static class BouquetFlora
{
    internal const float LeafInk = 0.85f;
    private const float HeadOutward = 1.6f;
    private const float HeadAxis = 0.5f;
    private const float HeadLift = 0.55f;
    private const float HeadNod = 0.5f;

    public static void Add(MeshBuffer mesh, Species species, Vector3 tip, Vector3 axis, float size, float roll,
        Color bloom, BouquetPalette palette, float detailWidth, int seedIndex, int seed)
    {
        float j0 = BouquetHash.Unit(seedIndex, 20, seed);
        float j1 = BouquetHash.Unit(seedIndex, 21, seed);
        float j2 = BouquetHash.Unit(seedIndex, 22, seed);

        Matrix4x4 head = HeadFrame(tip, axis, roll, seedIndex, seed);

        switch (species)
        {
            case Species.Rose:
                BloomHeads.Rose(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1, j2);
                break;
            case Species.OpenBloom:
                BloomHeads.OpenBloom(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.Anemone:
                BloomHeads.Anemone(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.Dahlia:
                BloomHeads.Dahlia(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.Carnation:
                BloomHeads.Carnation(mesh, head, size, roll, bloom, palette, detailWidth, j0, j1);
                break;
            case Species.BerryCluster:
                BloomHeads.BerryCluster(mesh, tip, size, bloom, j0);
                break;
            case Species.Lavender:
                LeafSprays.Lavender(mesh, tip, axis, size, roll, bloom, palette, j0);
                break;
            case Species.Gypsophila:
                LeafSprays.Gypsophila(mesh, tip, axis, size, roll, palette, j0);
                break;
            case Species.Eucalyptus:
                LeafSprays.Eucalyptus(mesh, tip, axis, size, roll, palette, detailWidth, j0);
                break;
            case Species.Fern:
                LeafSprays.Fern(mesh, tip, axis, size, roll, palette, detailWidth, j0);
                break;
            case Species.LongBlade:
                LeafSprays.LongBlade(mesh, tip, axis, size, roll, palette, detailWidth, j0, j1);
                break;
            default:
                LeafSprays.LeafSprig(mesh, tip, axis, size, roll, palette, detailWidth, j0);
                break;
        }
    }

    private static Matrix4x4 HeadFrame(Vector3 tip, Vector3 axis, float roll, int seedIndex, int seed)
    {
        Vector3 outward = new Vector3(tip.x, 0.0f, tip.z) * HeadOutward;
        Vector3 nod = new Vector3(BouquetHash.Unit(seedIndex, 24, seed) - 0.5f, 0.0f, BouquetHash.Unit(seedIndex, 25, seed) - 0.5f) * HeadNod;
        Vector3 facing = Vector3.Normalize(outward + axis * HeadAxis + Vector3.up * HeadLift + nod);
        return BouquetShapes.Frame(tip, facing, roll);
    }
}
