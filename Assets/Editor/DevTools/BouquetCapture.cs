using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BouquetCapture
{
    private const string ScenePath = "Assets/Scenes/Bouquet.unity";
    private const int DefaultFrames = 1;
    private const int DefaultSize = 1080;
    private const int AntiAliasingSamples = 4;
    private const int DepthBits = 24;
    private const float PaletteMax = 3.0f;

    public static void Run()
    {
        string outputDirectory = GetArgument("-outputDir");
        if (string.IsNullOrEmpty(outputDirectory))
        {
            Debug.LogError("BOUQUET_CAPTURE: missing -outputDir");
            EditorApplication.Exit(1);
            return;
        }

        int frameCount = GetIntArgument("-frames", DefaultFrames);
        int size = GetIntArgument("-size", DefaultSize);
        bool sweep = GetIntArgument("-sweep", 0) != 0;

        Directory.CreateDirectory(outputDirectory);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera = Object.FindFirstObjectByType<Camera>();
        MeshRenderer renderer = Object.FindFirstObjectByType<MeshRenderer>();
        if (camera == null || renderer == null)
        {
            Debug.LogError("BOUQUET_CAPTURE: scene is missing the camera or the quad");
            EditorApplication.Exit(1);
            return;
        }

        Material material = renderer.sharedMaterial;
        ApplyOverrides(material, GetArgument("-overrides"));

        RenderTexture target = new RenderTexture(size, size, DepthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        target.antiAliasing = AntiAliasingSamples;
        RenderTexture resolved = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;
        Texture2D frame = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int i = 0; i < frameCount; i++)
        {
            if (sweep && frameCount > 1)
            {
                float t = i / (float)(frameCount - 1);
                material.SetFloat("_Palette", t * PaletteMax);
                material.SetFloat("_Seed", Mathf.Floor(t * 8.0f));
            }

            camera.Render();
            Graphics.Blit(target, resolved);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = resolved;
            frame.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            frame.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(outputDirectory, $"frame_{i:D4}.png"), frame.EncodeToPNG());
        }

        camera.targetTexture = null;
        Debug.Log($"BOUQUET_CAPTURE: {frameCount} frames at {size}px written to {outputDirectory}");
        EditorApplication.Exit(0);
    }

    private static void ApplyOverrides(Material material, string overrides)
    {
        if (string.IsNullOrEmpty(overrides))
        {
            return;
        }

        foreach (string entry in overrides.Split(','))
        {
            string[] parts = entry.Split('=');
            if (parts.Length != 2)
            {
                Debug.LogError($"BOUQUET_CAPTURE: malformed override '{entry}', expected _Name=value");
                EditorApplication.Exit(1);
                return;
            }

            string name = parts[0].Trim();
            if (!material.HasFloat(name))
            {
                Debug.LogError($"BOUQUET_CAPTURE: material has no float property '{name}'");
                EditorApplication.Exit(1);
                return;
            }

            material.SetFloat(name, float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
            Debug.Log($"BOUQUET_CAPTURE: override {name}={parts[1].Trim()}");
        }
    }

    private static int GetIntArgument(string name, int fallback)
    {
        string raw = GetArgument(name);
        return string.IsNullOrEmpty(raw) ? fallback : int.Parse(raw, CultureInfo.InvariantCulture);
    }

    private static string GetArgument(string name)
    {
        string[] arguments = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == name)
            {
                return arguments[i + 1];
            }
        }

        return null;
    }
}
