using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Dial
{
    public string label;
    public Vector3 center;
    public Vector3 planeEuler;
    public float radius;
    public float startAngle;
    public float sweepAngle;
    [Range(0.0f, 1.0f)] public float value;

    public Quaternion Plane => Quaternion.Euler(planeEuler);

    public Vector3 PointAt(float t)
    {
        float angle = (startAngle + sweepAngle * t) * Mathf.Deg2Rad;
        return center + Plane * new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * radius;
    }

    public Vector3 Handle => PointAt(value);
}

public static class BouquetDialSet
{
    private const int TrackSteps = 64;
    private const float HandleRadius = 0.036f;
    private const float TrackWidth = 0.0036f;

    private const float TieRadius = 0.24f;
    private const float TieSweep = 330.0f;
    private const float TieStartAngle = 200.0f;
    private const float TieRise = 0.06f;

    private static readonly Color TrackInk = new Color(0.66f, 0.68f, 0.70f, 1.0f);

    public static Dial[] Defaults()
    {
        return new[]
        {
            new Dial
            {
                label = "palette", center = new Vector3(0.0f, 0.08f, 0.0f), planeEuler = new Vector3(90.0f, 0.0f, 0.0f),
                radius = 0.76f, startAngle = -165.0f, sweepAngle = 135.0f, value = 0.66f
            },
            new Dial
            {
                label = "density", center = new Vector3(0.0f, 0.08f, 0.0f), planeEuler = new Vector3(90.0f, 35.0f, 0.0f),
                radius = 0.76f, startAngle = 136.0f, sweepAngle = 64.0f, value = 0.60f
            },
            new Dial
            {
                label = "spread", center = new Vector3(-0.10f, -0.60f, 0.05f), planeEuler = new Vector3(0.0f, 0.0f, 0.0f),
                radius = 0.55f, startAngle = 95.0f, sweepAngle = 235.0f, value = 0.45f
            },
            new Dial
            {
                label = "length", center = new Vector3(0.0f, 0.08f, 0.0f), planeEuler = new Vector3(90.0f, -35.0f, 0.0f),
                radius = 0.76f, startAngle = 44.0f, sweepAngle = -68.0f, value = 0.52f
            }
        };
    }

    public static void BuildMesh(MeshBuffer mesh, Dial[] dials, Vector3 tie, Color track, Color handle)
    {
        Vector3[] points = new Vector3[TrackSteps + 1];
        mesh.SetInk(TrackInk);

        foreach (Dial dial in dials)
        {
            for (int s = 0; s <= TrackSteps; s++)
            {
                points[s] = dial.PointAt(s / (float)TrackSteps);
            }

            BouquetShapes.AddRibbon(mesh, points, track, TrackWidth, Outline.Silhouette, 0.0f);
        }

        AddTieRing(mesh, points, tie, track);

        foreach (Dial dial in dials)
        {
            BouquetShapes.AddBillboardDisc(mesh, dial.Handle, HandleRadius, handle, Outline.Silhouette, 0.0f);
        }
    }

    private static void AddTieRing(MeshBuffer mesh, Vector3[] points, Vector3 tie, Color track)
    {
        for (int s = 0; s <= TrackSteps; s++)
        {
            float t = s / (float)TrackSteps;
            float angle = (TieStartAngle + TieSweep * t) * Mathf.Deg2Rad;
            points[s] = tie + new Vector3(Mathf.Cos(angle) * TieRadius, TieRise * (t - 0.5f), Mathf.Sin(angle) * TieRadius);
        }

        BouquetShapes.AddRibbon(mesh, points, track, TrackWidth, Outline.Silhouette, 0.0f);
    }

    public static int Pick(Dial[] dials, Camera camera, Vector2 pointer, float pixelRadius)
    {
        int best = -1;
        float bestDistance = pixelRadius;

        for (int i = 0; i < dials.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(dials[i].Handle);
            if (screen.z <= 0.0f)
            {
                continue;
            }

            float distance = Vector2.Distance(new Vector2(screen.x, screen.y), pointer);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    public static float ValueUnderPointer(Dial dial, Camera camera, Vector2 pointer)
    {
        Quaternion plane = dial.Plane;
        Vector3 normal = plane * Vector3.up;
        Ray ray = camera.ScreenPointToRay(pointer);

        float denominator = Vector3.Dot(normal, ray.direction);
        if (Mathf.Abs(denominator) < 1e-5f)
        {
            return dial.value;
        }

        float hit = Vector3.Dot(normal, dial.center - ray.origin) / denominator;
        if (hit <= 0.0f)
        {
            return dial.value;
        }

        Vector3 local = Quaternion.Inverse(plane) * (ray.GetPoint(hit) - dial.center);
        float angle = Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;

        float offset = Mathf.DeltaAngle(dial.startAngle, angle);
        if (dial.sweepAngle > 0.0f && offset < 0.0f)
        {
            offset += 360.0f;
        }

        return Mathf.Clamp01(offset / dial.sweepAngle);
    }

    public static void Apply(Dial[] dials, BouquetBuilder builder)
    {
        builder.palette = Resolve(dials, builder.settings);
    }

    public static float Resolve(Dial[] dials, BouquetSettings target)
    {
        target.density = dials[1].value;
        target.depthSpread = Mathf.Lerp(0.34f, 0.58f, dials[1].value);
        target.colourVariation = Mathf.Lerp(0.18f, 0.30f, dials[1].value);

        target.spreadGain = Mathf.Lerp(0.80f, 1.75f, dials[2].value);
        target.stemLength = Mathf.Lerp(0.74f, 1.24f, dials[3].value);

        return Mathf.Round(dials[0].value * (BouquetPalette.PresetCount - 1));
    }
}
