# bouquet-gen

A procedural flower bouquet in Unity. No models, no textures: every petal, leaf, stem and the ribbon are built as a mesh in C# each time a setting changes, then drawn by one small toon shader with ink outlines.

![bouquet](media/bouquet.png)

Started as an attempt to recreate a generative bouquet clip I saw somewhere. The clip looked like vector art, but it spun like a real 3D object, so the whole thing ended up as actual geometry.

## What's in it

Twelve species. Roses, open blooms, anemones, pompon dahlias, carnations with frilled petals, berry clusters, lavender, gypsophila, eucalyptus, two kinds of fern, long blades and small leafy sprigs.

The bouquet gets arranged like a florist would do it, not just scattered around a ring. A few big heads sit in front, medium ones fill the face, greens go on the shoulders and tall thin stuff stands at the back. Heads turn outward from the bundle, so from behind you see their backs and the green calyx. After layout a small relaxation pass pushes leaves and smaller heads out of the bigger flowers (so nothing pokes through a petal).

Everything is seeded. Same seed, same bouquet.

![turntable](media/turntable.png)

## Dials

Four arcs around the bouquet, same layout as the clip. Drag a handle and the bouquet eases into the new shape instead of snapping. Drag anywhere else to spin it.

```
top arc       palette
left arc      density, how many stems and how deep the layers go
ground ring   spread
right arc     stem length
```

![dials](media/dials.png)

![palettes](media/palettes.png)

## Running it

Unity 6000.3.5f2 with URP 17.3 and the Input System. Open the project, load `Assets/Scenes/Bouquet.unity`, press Play.

The builder runs in edit mode too, so tweaking `BouquetBuilder` settings in the inspector rebuilds the bouquet right away.

## Layout

```
BouquetPlan        composition: roles, quotas, where each stem aims
StalkClearance     keeps leaves and small heads out of big flowers
BouquetGeometry    turns the plan into stems and heads
BloomHeads         petalled flowers
LeafSprays         greens and fillers
BouquetShapes      primitives: petal rims, bent cards, ribbons, billboards
BouquetFlat        the shader, two-tone light plus an ink pass
```

## License

MIT
