using System;
using UnityEngine;

[Serializable]
public sealed class BouquetSettings
{
    [Range(0.0f, 1.0f)] public float density = 0.55f;
    [Range(0.4f, 2.2f)] public float spreadGain = 1.28f;
    [Range(0.2f, 1.6f)] public float stemLength = 1.02f;
    [Range(0, 64)] public int seed = 7;

    [Header("Composition")]
    [Range(0.0f, 0.8f)] public float asymmetry = 0.34f;
    [Range(0.0f, 360.0f)] public float asymmetryAngle = 118.0f;
    [Range(0.0f, 0.6f)] public float faceFlatten = 0.26f;
    [Range(0.0f, 1.2f)] public float outwardCurve = 0.62f;
    [Range(0.0f, 0.5f)] public float sideBend = 0.30f;
    [Range(0.0f, 0.5f)] public float depthSpread = 0.46f;
    [Range(0.0f, 120.0f)] public float bundleTwist = 54.0f;

    [Header("Tie")]
    [Range(-0.9f, 0.2f)] public float bindHeight = -0.40f;
    [Range(0.05f, 0.8f)] public float cutLength = 0.32f;
    [Range(0.02f, 0.3f)] public float ribbonWidth = 0.105f;
    [Range(0.0f, 0.6f)] public float tailLength = 0.34f;
    [Range(0.0f, 360.0f)] public float knotAngle = 200.0f;
    [Range(0.0f, 0.3f)] public float tieHeight = 0.12f;

    [Header("Heads")]
    [Range(0.02f, 0.3f)] public float headScale = 0.095f;
    [Range(0.0f, 0.4f)] public float colourVariation = 0.24f;

    // widths are fractions of screen height, so a stroke stays the same weight
    // at any distance and any capture resolution
    [Header("Line")]
    [Range(0.001f, 0.02f)] public float stemWidth = 0.0027f;
    [Range(0.0002f, 0.004f)] public float detailWidth = 0.0008f;

    private const float Settled = 1e-4f;

    public BouquetSettings Clone()
    {
        return (BouquetSettings)MemberwiseClone();
    }

    // the fields a dial drives. they chase the dial instead of jumping to it, which
    // is how the bouquet opens and stretches in the reference
    public bool EaseToward(BouquetSettings target, float blend)
    {
        bool isMoving = false;
        density = Ease(density, target.density, blend, ref isMoving);
        depthSpread = Ease(depthSpread, target.depthSpread, blend, ref isMoving);
        colourVariation = Ease(colourVariation, target.colourVariation, blend, ref isMoving);
        spreadGain = Ease(spreadGain, target.spreadGain, blend, ref isMoving);
        stemLength = Ease(stemLength, target.stemLength, blend, ref isMoving);
        return isMoving;
    }

    public static float Ease(float value, float target, float blend, ref bool isMoving)
    {
        if (value == target)
        {
            return value;
        }

        isMoving = true;
        return Mathf.Abs(target - value) < Settled ? target : Mathf.Lerp(value, target, blend);
    }
}

[Serializable]
public struct BouquetPalette
{
    public const int PresetCount = 4;

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

    // inner petals lift a little, blade greens drop: a value move, not a wash
    // toward grey, which is what kills a limited palette
    public Color Shift(Color color, float amount)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        if (amount >= 0.0f)
        {
            v = Mathf.Clamp01(v + amount);
            s = Mathf.Clamp01(s * (1.0f - amount * 0.45f));
        }
        else
        {
            v = Mathf.Clamp01(v * (1.0f + amount));
            s = Mathf.Clamp01(s * (1.0f - amount * 0.30f));
        }

        return Color.HSVToRGB(h, s, v);
    }

    // a line that keeps some of its own fill reads as drawn; one shared near black
    // line around everything reads as comic outline. blend 0 is pure ink, 1 is a
    // deep version of the fill itself
    public Color Line(Color fill, float blend)
    {
        Color.RGBToHSV(fill, out float h, out float s, out float v);

        // two pulls at once. a light fill can be darkened freely into a line; a dark
        // one cannot, or the line goes black, yet it still needs a guaranteed gap in
        // value or the flower merges into one mass
        float drop = Mathf.Lerp(0.62f, 0.34f, v);
        float target = Mathf.Max(Mathf.Min(v * drop, v - 0.16f), 0.05f);
        float keep = Mathf.Lerp(0.22f, 0.0f, v);
        Color deep = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.08f), target);
        return Color.Lerp(ink, deep, Mathf.Clamp01(blend + keep));
    }

    public Color Vary(Color color, float saturationRoll, float valueRoll, float amount)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        s = Mathf.Clamp01(s * (1.0f + (saturationRoll - 0.5f) * 2.0f * amount));
        v = Mathf.Clamp01(v * (1.0f + (valueRoll - 0.5f) * amount));
        return Color.HSVToRGB(h, s, v);
    }

    public static BouquetPalette Lerp(BouquetPalette from, BouquetPalette to, float t)
    {
        return new BouquetPalette
        {
            background = Color.Lerp(from.background, to.background, t),
            ink = Color.Lerp(from.ink, to.ink, t),
            stem = Color.Lerp(from.stem, to.stem, t),
            leaf = Color.Lerp(from.leaf, to.leaf, t),
            bloomA = Color.Lerp(from.bloomA, to.bloomA, t),
            bloomB = Color.Lerp(from.bloomB, to.bloomB, t),
            bloomC = Color.Lerp(from.bloomC, to.bloomC, t),
            ribbon = Color.Lerp(from.ribbon, to.ribbon, t)
        };
    }

    public static BouquetPalette At(float position)
    {
        int lower = Mathf.Clamp(Mathf.FloorToInt(position), 0, PresetCount - 1);
        int upper = Mathf.Min(lower + 1, PresetCount - 1);
        return Lerp(Preset(lower), Preset(upper), Mathf.Clamp01(position - lower));
    }

    private static Color Srgb(int hex)
    {
        return new Color(((hex >> 16) & 0xFF) / 255.0f, ((hex >> 8) & 0xFF) / 255.0f, (hex & 0xFF) / 255.0f, 1.0f);
    }

    // eyedropped off the reference frames, then deepened on purpose: blooms gain
    // saturation and lose a little value, foliage goes markedly darker, and ink
    // is a very dark desaturated green rather than black
    public static BouquetPalette Preset(int index)
    {
        switch (Mathf.Clamp(index, 0, 3))
        {
            case 0:
                return new BouquetPalette
                {
                    background = Srgb(0xf5f7f9), ink = Srgb(0x262a26),
                    stem = Srgb(0x7f8c7f), leaf = Srgb(0x687a68),
                    bloomA = Srgb(0xbc0013), bloomB = Srgb(0xca9717),
                    bloomC = Srgb(0x391261), ribbon = Srgb(0xe1dcce)
                };
            case 1:
                return new BouquetPalette
                {
                    background = Srgb(0xf5f7f9), ink = Srgb(0x322a22),
                    stem = Srgb(0x7f8c7f), leaf = Srgb(0x5f806f),
                    bloomA = Srgb(0xdac7b3), bloomB = Srgb(0x6e0020),
                    bloomC = Srgb(0xc9bfa9), ribbon = Srgb(0xe3dac9)
                };
            case 2:
                return new BouquetPalette
                {
                    background = Srgb(0xf5f7f9), ink = Srgb(0x242c26),
                    stem = Srgb(0x364436), leaf = Srgb(0x476767),
                    bloomA = Srgb(0xda8c78), bloomB = Srgb(0x8c0e20),
                    bloomC = Srgb(0xc98e8e), ribbon = Srgb(0xe4d1c7)
                };
            default:
                return new BouquetPalette
                {
                    background = Srgb(0xf5f7f9), ink = Srgb(0x1c241e),
                    stem = Srgb(0x2b382b), leaf = Srgb(0x3d5854),
                    bloomA = Srgb(0xd38478), bloomB = Srgb(0x700b1d),
                    bloomC = Srgb(0xb37373), ribbon = Srgb(0xe0c9bf)
                };
        }
    }
}
