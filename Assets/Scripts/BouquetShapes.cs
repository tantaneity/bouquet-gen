using System.Collections.Generic;
using UnityEngine;

// the drawing primitives. everything the bouquet is made of comes from here, and
// nothing here knows about composition
public static class BouquetShapes
{
    public const float LayerStep = 0.0034f;

    private const int PetalSteps = 11;
    private const int DiscSteps = 16;
    private const int RibbonSteps = 12;
    private const float BladeFold = 0.7f;

    // a petal is fat in the middle and closes at both ends, which reads as drawn;
    // an ellipse reads as a mathematical blob
    public static Vector2[] PetalRim(float length, float width, float tip, float skew)
    {
        List<Vector2> rim = new List<Vector2>(PetalSteps * 2);

        for (int i = 0; i <= PetalSteps; i++)
        {
            float t = i / (float)PetalSteps;
            rim.Add(new Vector2(length * t, Shoulder(width, t, tip) * (1.0f + skew * t)));
        }

        for (int i = PetalSteps - 1; i >= 1; i--)
        {
            float t = i / (float)PetalSteps;
            rim.Add(new Vector2(length * t, -Shoulder(width, t, tip) * (1.0f - skew * t)));
        }

        return rim.ToArray();
    }

    // Mathf.Sin(Mathf.PI) lands a hair below zero, and Pow of a negative base with a
    // fractional exponent is NaN, so every petal tip poisoned the mesh bounds
    private static float Shoulder(float width, float t, float tip)
    {
        return width * Mathf.Pow(Mathf.Max(Mathf.Sin(Mathf.PI * t), 0.0f), tip);
    }

    public static Vector2[] CircleRim(float radius, int steps, float squash)
    {
        Vector2[] rim = new Vector2[steps];
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)steps * Mathf.PI * 2.0f;
            rim[i] = new Vector2(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius * squash);
        }

        return rim;
    }

    public static Vector2 Rotate(Vector2 p, float angle)
    {
        float s = Mathf.Sin(angle);
        float c = Mathf.Cos(angle);
        return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
    }

    // a flat shape that always faces the viewer: this is what keeps a flower
    // reading as an illustration instead of a disc seen at an angle
    public static void AddBillboardShape(MeshBuffer mesh, Vector3 anchor, Vector2 centre, IReadOnlyList<Vector2> rim,
        Color fill, float outlineWeight, float depthBias)
    {
        float radius = 1e-5f;
        for (int i = 0; i < rim.Count; i++)
        {
            radius = Mathf.Max(radius, (rim[i] - centre).magnitude);
        }

        int first = mesh.VertexCount;
        mesh.AddVertex(anchor, new Vector3(centre.x, centre.y, 0.0f), Vector4.zero, fill, StrokeKind.Billboard, 0.0f, 0.0f, depthBias,
            Shading.Sphere(Vector2.zero));

        for (int i = 0; i < rim.Count; i++)
        {
            Vector2 point = rim[i];
            Vector2 behind = rim[(i - 1 + rim.Count) % rim.Count];
            Vector2 ahead = rim[(i + 1) % rim.Count];

            Vector2 edge = (ahead - behind).normalized;
            Vector2 outward = new Vector2(edge.y, -edge.x);
            if (Vector2.Dot(outward, point - centre) < 0.0f)
            {
                outward = -outward;
            }

            mesh.AddVertex(anchor, new Vector3(point.x, point.y, 0.0f), new Vector4(outward.x, outward.y, 0.0f, 0.0f),
                fill, StrokeKind.Billboard, 0.0f, outlineWeight, depthBias, Shading.Sphere((point - centre) / radius));
        }

        for (int i = 0; i < rim.Count; i++)
        {
            mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % rim.Count);
        }
    }

    // an ink stroke drawn inside a billboard shape. both rails sit on the same
    // offset and the shader pushes them apart in pixels, so a vein keeps its
    // weight whatever the flower is doing
    public static void AddBillboardLine(MeshBuffer mesh, Vector3 anchor, IReadOnlyList<Vector2> path,
        Color ink, float halfWidth, float depthBias)
    {
        if (path.Count < 2)
        {
            return;
        }

        int first = mesh.VertexCount;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 point = path[i];
            Vector2 ahead = path[Mathf.Min(i + 1, path.Count - 1)];
            Vector2 behind = path[Mathf.Max(i - 1, 0)];
            Vector2 edge = (ahead - behind).normalized;
            Vector2 side = new Vector2(edge.y, -edge.x);

            Vector3 offset = new Vector3(point.x, point.y, 0.0f);
            mesh.AddVertex(anchor, offset, new Vector4(-side.x, -side.y, 0.0f, 0.0f), ink, StrokeKind.Billboard, halfWidth, Outline.Detail, depthBias);
            mesh.AddVertex(anchor, offset, new Vector4(side.x, side.y, 0.0f, 0.0f), ink, StrokeKind.Billboard, halfWidth, Outline.Detail, depthBias);

            if (i > 0)
            {
                int here = first + i * 2;
                mesh.AddQuad(here - 2, here - 1, here + 1, here);
            }
        }
    }

    public static void AddCardShape(MeshBuffer mesh, Matrix4x4 frame, IReadOnlyList<Vector2> rim, Vector2 centre,
        Color fill, float outlineWeight, float depthBias, Bend bend = default)
    {
        int first = mesh.VertexCount;
        mesh.SetFacing(frame.MultiplyVector(Vector3.forward).normalized);
        mesh.AddVertex(frame.MultiplyPoint3x4(bend.Lift(centre)), Vector3.zero, Vector4.zero, fill, StrokeKind.Card, 0.0f, 0.0f, depthBias,
            Shading.Surface(frame.MultiplyVector(bend.Normal(centre)).normalized));

        for (int i = 0; i < rim.Count; i++)
        {
            Vector2 point = rim[i];
            Vector2 behind = rim[(i - 1 + rim.Count) % rim.Count];
            Vector2 ahead = rim[(i + 1) % rim.Count];

            Vector2 edge = (ahead - behind).normalized;
            Vector2 outward = new Vector2(edge.y, -edge.x);
            if (Vector2.Dot(outward, point - centre) < 0.0f)
            {
                outward = -outward;
            }

            mesh.AddVertex(frame.MultiplyPoint3x4(bend.Lift(point)), frame.MultiplyVector(outward).normalized, Vector4.zero,
                fill, StrokeKind.Card, 0.0f, outlineWeight, depthBias, Shading.Surface(frame.MultiplyVector(bend.Normal(point)).normalized));
        }

        mesh.SetFacing(Vector3.zero);

        for (int i = 0; i < rim.Count; i++)
        {
            mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % rim.Count);
        }
    }

    public static void AddCardLine(MeshBuffer mesh, Matrix4x4 frame, IReadOnlyList<Vector2> path,
        Color ink, float halfWidth, float depthBias)
    {
        Vector3[] points = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            points[i] = frame.MultiplyPoint3x4(path[i]);
        }

        AddRibbon(mesh, points, ink, halfWidth, Outline.Detail, depthBias);
    }

    public static void AddBillboardDisc(MeshBuffer mesh, Vector3 anchor, float radius, Color fill, float outlineWeight, float depthBias)
    {
        AddBillboardShape(mesh, anchor, Vector2.zero, CircleRim(radius, DiscSteps, 1.0f), fill, outlineWeight, depthBias);
    }

    // a screen facing ribbon rather than a tube: at this line weight the two are
    // indistinguishable, and the ribbon keeps its width constant in pixels
    public static void AddRibbon(MeshBuffer mesh, IReadOnlyList<Vector3> points, Color fill, float halfWidth,
        float outlineWeight, float depthBias)
    {
        if (points.Count < 2)
        {
            return;
        }

        int first = mesh.VertexCount;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = points[i];
            Vector3 ahead = points[Mathf.Min(i + 1, points.Count - 1)];
            Vector3 behind = points[Mathf.Max(i - 1, 0)];
            Vector3 tangent = Vector3.Normalize(ahead - behind);

            mesh.AddVertex(point, Vector3.zero, new Vector4(tangent.x, tangent.y, tangent.z, -1.0f), fill, StrokeKind.Stem, halfWidth, outlineWeight, depthBias, Shading.Tube);
            mesh.AddVertex(point, Vector3.zero, new Vector4(tangent.x, tangent.y, tangent.z, 1.0f), fill, StrokeKind.Stem, halfWidth, outlineWeight, depthBias, Shading.Tube);

            if (i > 0)
            {
                int here = first + i * 2;
                mesh.AddQuad(here - 2, here - 1, here + 1, here);
            }
        }
    }

    public static Vector3[] CubicPath(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int steps)
    {
        Vector3[] points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float u = 1.0f - t;
            points[i] = u * u * u * a + 3.0f * u * u * t * b + 3.0f * u * t * t * c + t * t * t * d;
        }

        return points;
    }

    public static void AddCubicRibbon(MeshBuffer mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        Color fill, float halfWidth, float outlineWeight)
    {
        AddRibbon(mesh, CubicPath(a, b, c, d, RibbonSteps), fill, halfWidth, outlineWeight, 0.0f);
    }

    // a long leaf built as a curled strip rather than a flat card: a flat one seen
    // exactly edge on loses its width and breaks into a dashed hairline
    public static void AddBlade(MeshBuffer mesh, Matrix4x4 frame, float length, float width, float bend, float curl,
        Color fill, float outlineWeight, int steps)
    {
        int first = mesh.VertexCount;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float half = width * Mathf.Pow(Mathf.Max(Mathf.Sin(Mathf.PI * Mathf.Min(t + 0.06f, 1.0f)), 0.0f), 0.55f);
            Vector3 spine = BladeSpine(length, bend, curl, t);
            Vector3 left = frame.MultiplyPoint3x4(spine + new Vector3(-half, 0.0f, 0.0f));
            Vector3 right = frame.MultiplyPoint3x4(spine + new Vector3(half, 0.0f, 0.0f));

            Vector3 outward = frame.MultiplyVector(Vector3.right).normalized;
            Vector3 face = frame.MultiplyVector(Vector3.forward).normalized;
            mesh.AddVertex(left, -outward, Vector4.zero, fill, StrokeKind.Card, 0.0f, outlineWeight, 0.0f,
                Shading.Surface(Vector3.Normalize(face - outward * BladeFold)));
            mesh.AddVertex(right, outward, Vector4.zero, fill, StrokeKind.Card, 0.0f, outlineWeight, 0.0f,
                Shading.Surface(Vector3.Normalize(face + outward * BladeFold)));

            if (i > 0)
            {
                int here = first + i * 2;
                mesh.AddQuad(here - 2, here - 1, here + 1, here);
            }
        }
    }

    public static Vector3 BladeSpine(float length, float bend, float curl, float t)
    {
        return new Vector3(bend * t * t, length * t, curl * Mathf.Sin(Mathf.PI * t));
    }

    // a leafy spray is laid out along its stem, so local y runs up the axis and the
    // card faces whichever way it is told, twisted about the stem
    public static Matrix4x4 SprayFrame(Vector3 origin, Vector3 axis, Vector3 facing, float twist)
    {
        Vector3 up = axis.normalized;
        Vector3 normal = facing - up * Vector3.Dot(facing, up);
        if (normal.sqrMagnitude < 1e-6f)
        {
            normal = Mathf.Abs(up.z) > 0.95f ? Vector3.up : Vector3.back;
            normal -= up * Vector3.Dot(normal, up);
        }

        normal = Quaternion.AngleAxis(twist * Mathf.Rad2Deg, up) * normal.normalized;
        Vector3 right = Vector3.Cross(up, normal);
        return new Matrix4x4(right, up, normal, new Vector4(origin.x, origin.y, origin.z, 1.0f));
    }

    public static Matrix4x4 Frame(Vector3 origin, Vector3 axis, float roll)
    {
        Vector3 reference = Mathf.Abs(axis.y) > 0.95f ? Vector3.forward : Vector3.up;
        Vector3 right = Vector3.Normalize(Vector3.Cross(reference, axis));
        Vector3 up = Vector3.Cross(axis, right);
        right = Vector3.Normalize(right * Mathf.Cos(roll) + up * Mathf.Sin(roll));
        up = Vector3.Cross(axis, right);
        return new Matrix4x4(right, up, axis, new Vector4(origin.x, origin.y, origin.z, 1.0f));
    }
}

// a card bowed into a shallow paraboloid around a pivot. the rim barely moves, but
// the normals turn across the surface and that is what the light reads as form
public readonly struct Bend
{
    private readonly Vector2 pivot;
    private readonly float along;
    private readonly float across;

    public Bend(Vector2 pivot, float along, float across)
    {
        this.pivot = pivot;
        this.along = along;
        this.across = across;
    }

    public static Bend Bowl(Vector2 pivot, float depth)
    {
        return new Bend(pivot, depth, depth);
    }

    public Vector3 Lift(Vector2 point)
    {
        Vector2 local = point - pivot;
        return new Vector3(point.x, point.y, along * local.x * local.x + across * local.y * local.y);
    }

    public Vector3 Normal(Vector2 point)
    {
        Vector2 local = point - pivot;
        return new Vector3(-2.0f * along * local.x, -2.0f * across * local.y, 1.0f).normalized;
    }
}
