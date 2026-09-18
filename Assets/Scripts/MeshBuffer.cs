using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum StrokeKind
{
    Card = 0,
    Stem = 1,
    Billboard = 2
}

// line hierarchy, the thing that decides whether this reads as ink drawing or as
// cartoon outline around every mesh
public static class Outline
{
    public const float Silhouette = 1.00f;
    public const float Contour = 0.44f;
    public const float Small = 0.17f;
    public const float Detail = 0.09f;
    public const float None = 0.0f;
}

public static class Shading
{
    public static readonly Vector4 Flat = Vector4.zero;
    public static readonly Vector4 Tube = new Vector4(0.0f, 0.0f, 0.0f, 3.0f);

    public static Vector4 Surface(Vector3 normal)
    {
        return new Vector4(normal.x, normal.y, normal.z, 1.0f);
    }

    public static Vector4 Sphere(Vector2 offset)
    {
        return new Vector4(offset.x, offset.y, 0.0f, 2.0f);
    }
}

public sealed class MeshBuffer
{
    private readonly List<Vector3> positions = new List<Vector3>();
    private readonly List<Vector3> expansions = new List<Vector3>();
    private readonly List<Vector4> tangents = new List<Vector4>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<Vector4> strokes = new List<Vector4>();
    private readonly List<Vector4> inks = new List<Vector4>();
    private readonly List<Vector3> facings = new List<Vector3>();
    private readonly List<Vector4> shadings = new List<Vector4>();
    private readonly List<int> indices = new List<int>();

    private readonly bool isShaded;

    private Color ink = Color.black;
    private Vector3 facing = Vector3.zero;

    public MeshBuffer(bool isShaded = true)
    {
        this.isShaded = isShaded;
    }

    public int VertexCount => positions.Count;

    // the line colour belongs to the element, not to the scene: one uniform ink
    // forces a stem to wear the same near black line as a peony
    public void SetInk(Color colour)
    {
        ink = colour;
    }

    public void SetFacing(Vector3 faceNormal)
    {
        facing = faceNormal;
    }

    // the project renders linear, and every palette value here was eyedropped off
    // the reference as sRGB, so the conversion happens once, here, or the whole
    // bouquet comes out pale
    public void AddVertex(Vector3 position, Vector3 expansion, Vector4 tangent, Color color,
        StrokeKind kind, float width, float outlineWeight, float depthBias, Vector4 shading = default)
    {
        positions.Add(position);
        expansions.Add(expansion);
        tangents.Add(tangent);
        colors.Add(color.linear);
        strokes.Add(new Vector4((float)kind, width, outlineWeight, depthBias));
        Color line = ink.linear;
        inks.Add(new Vector4(line.r, line.g, line.b, 1.0f));
        facings.Add(facing);
        shadings.Add(isShaded ? shading : Shading.Flat);
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
        mesh.SetUVs(0, strokes);
        mesh.SetUVs(1, inks);
        mesh.SetUVs(2, facings);
        mesh.SetUVs(3, shadings);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
    }
}
