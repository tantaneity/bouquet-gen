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
