using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class BouquetBuilder : MonoBehaviour
{
    [Range(0, 3)] public int palette = 2;
    public BouquetSettings settings = new BouquetSettings();

    private Mesh mesh;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        if (mesh == null)
        {
            mesh = new Mesh { name = "Bouquet" };
            mesh.hideFlags = HideFlags.DontSave;
        }

        BouquetPalette colors = BouquetPalette.Preset(palette);

        MeshBuffer buffer = new MeshBuffer();
        BouquetGeometry.Build(buffer, settings, colors);
        buffer.WriteTo(mesh);
        filter.sharedMesh = mesh;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = colors.background;
        }
    }
}
