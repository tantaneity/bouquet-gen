using System;
using System.Collections.Generic;
using UnityEngine;

// a dial is a fixed arc track in world space with a bead riding it, which is how
// the reference does it: the track passes through the handle rather than ending there,
// and the rings at the tie read as circles in perspective because they are
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
    private const int TrackSteps = 48;
    private const float HandleRadius = 0.052f;
    private const float TrackWidth = 0.0022f;

    public static Dial[] Defaults()
    {
        return new[]
        {
            new Dial
            {
                label = "palette", center = new Vector3(0.0f, 0.16f, 0.0f), planeEuler = new Vector3(90.0f, 0.0f, 0.0f),
                radius = 0.86f, startAngle = 8.0f, sweepAngle = 164.0f, value = 0.66f
            },
            new Dial
            {
                label = "density", center = new Vector3(0.0f, 0.06f, 0.0f), planeEuler = new Vector3(90.0f, 38.0f, 0.0f),
                radius = 0.70f, startAngle = 128.0f, sweepAngle = 118.0f, value = 0.60f
            },
            new Dial
            {
                label = "spread", center = new Vector3(0.0f, -0.40f, 0.0f), planeEuler = new Vector3(0.0f, 0.0f, 0.0f),
                radius = 0.58f, startAngle = 186.0f, sweepAngle = 198.0f, value = 0.45f
            },
            new Dial
            {
                label = "length", center = new Vector3(0.0f, -0.33f, 0.0f), planeEuler = new Vector3(6.0f, 0.0f, 16.0f),
                radius = 0.19f, startAngle = 0.0f, sweepAngle = 300.0f, value = 0.52f
            }
        };
    }

    public static void BuildMesh(MeshBuffer mesh, Dial[] dials, Color track, Color handle)
    {
        Vector3[] points = new Vector3[TrackSteps + 1];

        BouquetGeometry.SetDialInk(mesh, new Color(0.55f, 0.58f, 0.60f, 1.0f));

        foreach (Dial dial in dials)
        {
            for (int s = 0; s <= TrackSteps; s++)
            {
                points[s] = dial.PointAt(s / (float)TrackSteps);
            }

            BouquetGeometry.AddRibbon(mesh, points, track, TrackWidth);
            BouquetGeometry.AddHandle(mesh, dial.Handle, HandleRadius, handle);
        }
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

    // the pointer ray is dropped onto the dial's own plane, so dragging follows the
    // ring in perspective instead of sliding along the screen
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
        builder.palette = Mathf.Clamp(Mathf.RoundToInt(dials[0].value * 3.0f), 0, 3);

        // density drives every quota plus how much the layers separate, so the
        // silhouette stays put while the bouquet actually fills in
        builder.settings.density = dials[1].value;
        builder.settings.depthSpread = Mathf.Lerp(0.34f, 0.58f, dials[1].value);
        builder.settings.colourVariation = Mathf.Lerp(0.18f, 0.30f, dials[1].value);

        builder.settings.spreadGain = Mathf.Lerp(0.80f, 1.75f, dials[2].value);
        builder.settings.stemLength = Mathf.Lerp(0.74f, 1.24f, dials[3].value);
    }
}
