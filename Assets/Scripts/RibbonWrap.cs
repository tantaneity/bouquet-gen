using UnityEngine;

internal static class RibbonWrap
{
    private const int BandSteps = 22;

    public static void Add(MeshBuffer mesh, BouquetSettings settings, BouquetPalette palette, Vector3 bind)
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
