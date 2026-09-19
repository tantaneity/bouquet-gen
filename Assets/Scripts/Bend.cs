using UnityEngine;

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
