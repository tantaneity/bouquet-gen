using System;
using UnityEngine;

[Serializable]
public sealed class BouquetSettings
{
    [Range(3, 40)] public int stemCount = 28;
    [Range(5.0f, 70.0f)] public float coneHalfAngle = 27.0f;
    [Range(0.0f, 1.0f)] public float azimuthJitter = 0.35f;
    [Range(0.2f, 1.6f)] public float stemLength = 0.82f;
    [Range(0.0f, 1.0f)] public float outwardCurve = 0.72f;
    [Range(0, 64)] public int seed = 7;

    [Range(-0.9f, 0.2f)] public float bindHeight = -0.40f;
    [Range(0.05f, 0.8f)] public float cutLength = 0.31f;
    [Range(0.02f, 0.3f)] public float headScale = 0.135f;
    [Range(0.0f, 1.0f)] public float headLean = 0.55f;

    [Range(0.02f, 0.3f)] public float ribbonWidth = 0.105f;
    [Range(0.0f, 0.6f)] public float tailLength = 0.36f;
    [Range(0.0f, 360.0f)] public float knotAngle = 200.0f;

    // widths are fractions of screen height, so a stroke stays the same weight
    // at any distance and any capture resolution
    [Range(0.001f, 0.02f)] public float stemWidth = 0.0036f;
    [Range(0.001f, 0.02f)] public float lineWidth = 0.0019f;
}

[Serializable]
public struct BouquetPalette
{
    public Color background;
    public Color ink;
    public Color stem;
    public Color leaf;
    public Color bloomA;
    public Color bloomB;
    public Color bloomC;
    public Color ribbon;

    public Color Bloom(int index)
    {
        return index <= 0 ? bloomA : (index == 1 ? bloomB : bloomC);
    }

    private static Color Rgb(float r, float g, float b)
    {
        return new Color(r, g, b, 1.0f);
    }

    // measured off the reference frames, not picked by eye
    public static BouquetPalette Preset(int index)
    {
        switch (Mathf.Clamp(index, 0, 3))
        {
            case 0:
                return new BouquetPalette
                {
                    background = Rgb(0.961f, 0.969f, 0.976f), ink = Rgb(0.114f, 0.118f, 0.114f),
                    stem = Rgb(0.659f, 0.706f, 0.659f), leaf = Rgb(0.561f, 0.627f, 0.561f),
                    bloomA = Rgb(0.784f, 0.157f, 0.220f), bloomB = Rgb(0.863f, 0.682f, 0.235f),
                    bloomC = Rgb(0.282f, 0.157f, 0.408f), ribbon = Rgb(0.902f, 0.882f, 0.831f)
                };
            case 1:
                return new BouquetPalette
                {
                    background = Rgb(0.961f, 0.969f, 0.976f), ink = Rgb(0.165f, 0.149f, 0.125f),
                    stem = Rgb(0.659f, 0.706f, 0.659f), leaf = Rgb(0.533f, 0.659f, 0.596f),
                    bloomA = Rgb(0.910f, 0.847f, 0.784f), bloomB = Rgb(0.471f, 0.031f, 0.157f),
                    bloomC = Rgb(0.847f, 0.816f, 0.745f), ribbon = Rgb(0.910f, 0.875f, 0.816f)
                };
            case 2:
                return new BouquetPalette
                {
                    background = Rgb(0.961f, 0.969f, 0.976f), ink = Rgb(0.106f, 0.118f, 0.110f),
                    stem = Rgb(0.290f, 0.341f, 0.290f), leaf = Rgb(0.408f, 0.533f, 0.533f),
                    bloomA = Rgb(0.910f, 0.659f, 0.596f), bloomB = Rgb(0.596f, 0.157f, 0.220f),
                    bloomC = Rgb(0.847f, 0.659f, 0.659f), ribbon = Rgb(0.914f, 0.843f, 0.808f)
                };
            default:
                return new BouquetPalette
                {
                    background = Rgb(0.961f, 0.969f, 0.976f), ink = Rgb(0.078f, 0.094f, 0.082f),
                    stem = Rgb(0.235f, 0.282f, 0.235f), leaf = Rgb(0.353f, 0.455f, 0.439f),
                    bloomA = Rgb(0.878f, 0.627f, 0.588f), bloomB = Rgb(0.478f, 0.125f, 0.188f),
                    bloomC = Rgb(0.753f, 0.549f, 0.549f), ribbon = Rgb(0.898f, 0.812f, 0.776f)
                };
        }
    }
}
