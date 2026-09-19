using System;
using UnityEngine;

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

    public Color Line(Color fill, float blend)
    {
        Color.RGBToHSV(fill, out float h, out float s, out float v);

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
