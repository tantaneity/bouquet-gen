using System.Globalization;
using System.IO;
using System.Reflection;
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
        float yaw = GetFloatArgument("-yaw", 0.0f);
        float pitch = GetFloatArgument("-pitch", 12.0f);
        bool spin = GetIntArgument("-spin", 0) != 0;

        Directory.CreateDirectory(outputDirectory);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera = Object.FindFirstObjectByType<Camera>();
        BouquetBuilder builder = Object.FindFirstObjectByType<BouquetBuilder>();
        BouquetController controller = Object.FindFirstObjectByType<BouquetController>();
        if (camera == null || builder == null || controller == null)
        {
            Debug.LogError("BOUQUET_CAPTURE: scene is missing the camera, the bouquet or the controller");
            EditorApplication.Exit(1);
            return;
        }

        if (!ApplyDials(controller, GetArgument("-dials")))
        {
            EditorApplication.Exit(1);
            return;
        }

        if (!ApplyOverrides(builder, GetArgument("-overrides")))
        {
            EditorApplication.Exit(1);
            return;
        }

        builder.Rebuild();
        controller.RebuildDials();

        if (controller.dialRenderer != null)
        {
            controller.dialRenderer.enabled = GetIntArgument("-hideDials", 0) == 0;
        }
        camera.backgroundColor = BouquetPalette.Preset(builder.palette).background;

        RenderTexture target = new RenderTexture(size, size, DepthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        target.antiAliasing = AntiAliasingSamples;
        RenderTexture resolved = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;
        Texture2D frame = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int i = 0; i < frameCount; i++)
        {
            controller.yaw = spin && frameCount > 1 ? yaw + 360.0f * i / frameCount : yaw;
            controller.pitch = pitch;
            controller.PlaceCamera();
            controller.RebuildDials();

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

    // dial values run 0..1 along their track; passing them straight through is how
    // the gizmo gets verified headlessly, where there is no mouse to drag with
    private static bool ApplyDials(BouquetController controller, string values)
    {
        if (string.IsNullOrEmpty(values))
        {
            return true;
        }

        string[] parts = values.Split(',');
        if (parts.Length != controller.dials.Length)
        {
            Debug.LogError($"BOUQUET_CAPTURE: -dials needs {controller.dials.Length} values, got {parts.Length}");
            return false;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            controller.dials[i].value = Mathf.Clamp01(float.Parse(parts[i].Trim(), CultureInfo.InvariantCulture));
        }

        BouquetDialSet.Apply(controller.dials, controller.builder);
        Debug.Log($"BOUQUET_CAPTURE: dials {values}");
        return true;
    }

    // settings carry a dozen knobs and iterating on them from the CLI is the whole
    // workflow, so the names are resolved against the fields instead of listed twice
    private static bool ApplyOverrides(BouquetBuilder builder, string overrides)
    {
        if (string.IsNullOrEmpty(overrides))
        {
            return true;
        }

        foreach (string entry in overrides.Split(','))
        {
            string[] parts = entry.Split('=');
            if (parts.Length != 2)
            {
                Debug.LogError($"BOUQUET_CAPTURE: malformed override '{entry}', expected name=value");
                return false;
            }

            string name = parts[0].Trim();
            string raw = parts[1].Trim();

            if (name == "palette")
            {
                builder.palette = int.Parse(raw, CultureInfo.InvariantCulture);
                Debug.Log($"BOUQUET_CAPTURE: override palette={raw}");
                continue;
            }

            FieldInfo field = typeof(BouquetSettings).GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError($"BOUQUET_CAPTURE: BouquetSettings has no field '{name}'");
                return false;
            }

            if (field.FieldType == typeof(int))
            {
                field.SetValue(builder.settings, int.Parse(raw, CultureInfo.InvariantCulture));
            }
            else if (field.FieldType == typeof(float))
            {
                field.SetValue(builder.settings, float.Parse(raw, CultureInfo.InvariantCulture));
            }
            else
            {
                Debug.LogError($"BOUQUET_CAPTURE: field '{name}' is {field.FieldType.Name}, only int and float are supported");
                return false;
            }

            Debug.Log($"BOUQUET_CAPTURE: override {name}={raw}");
        }

        return true;
    }

    private static int GetIntArgument(string name, int fallback)
    {
        string raw = GetArgument(name);
        return string.IsNullOrEmpty(raw) ? fallback : int.Parse(raw, CultureInfo.InvariantCulture);
    }

    private static float GetFloatArgument(string name, float fallback)
    {
        string raw = GetArgument(name);
        return string.IsNullOrEmpty(raw) ? fallback : float.Parse(raw, CultureInfo.InvariantCulture);
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
