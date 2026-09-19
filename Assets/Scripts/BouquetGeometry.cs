using System.Collections.Generic;
using UnityEngine;

public static class BouquetGeometry
{
    private const int StemSteps = 12;

    public static void Build(MeshBuffer mesh, BouquetSettings settings, BouquetPalette palette)
    {
        Vector3 bind = new Vector3(0.0f, settings.bindHeight, 0.0f);
        List<Stalk> plan = BouquetPlan.Build(settings, palette, bind);

        foreach (Stalk stalk in plan)
        {
            Color stemColour = palette.Vary(palette.stem,
                BouquetHash.Unit(stalk.index, 40, settings.seed),
                BouquetHash.Unit(stalk.index, 41, settings.seed), settings.colourVariation * 0.5f);

            mesh.SetInk(palette.Line(stemColour, 0.74f));

            float width = settings.stemWidth * stalk.stemWidth;
            float weight = Mathf.Lerp(Outline.Small, Outline.Silhouette, stalk.stemWidth);

            BouquetShapes.AddRibbon(mesh, BouquetShapes.CubicPath(stalk.anchor, stalk.handleLow, stalk.handleHigh, stalk.tip, StemSteps),
                stemColour, width, weight, 0.0f);

            BouquetShapes.AddRibbon(mesh, new[] { stalk.anchor, Vector3.Lerp(stalk.anchor, stalk.cutEnd, 0.5f), stalk.cutEnd },
                stemColour, width, weight, 0.0f);

            BouquetFlora.Add(mesh, stalk.species, stalk.tip, stalk.axis, stalk.headSize, stalk.roll,
                stalk.bloom, palette, settings.detailWidth, stalk.index, settings.seed);
        }

        RibbonWrap.Add(mesh, settings, palette, bind);
    }
}
