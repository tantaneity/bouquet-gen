using System.Collections.Generic;
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
    private const int DefaultFramesPerSecond = 30;
    private const float DragShare = 0.7f;

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
        float radius = GetFloatArgument("-radius", 0.0f);
        float yawTo = GetFloatArgument("-yawTo", yaw);
        float frameSeconds = 1.0f / GetIntArgument("-fps", DefaultFramesPerSecond);

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

        if (radius > 0.0f)
        {
            controller.orbitRadius = radius;
        }

        if (!ApplyDials(controller, GetArgument("-dials")))
        {
            EditorApplication.Exit(1);
            return;
        }

        List<float[]> dialKeys = ParseDialKeys(controller, GetArgument("-dialsTo"));
        if (dialKeys == null)
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
        camera.backgroundColor = BouquetPalette.At(builder.palette).background;

        RenderTexture target = new RenderTexture(size, size, DepthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        target.antiAliasing = AntiAliasingSamples;
        RenderTexture resolved = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;
        Texture2D frame = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int i = 0; i < frameCount; i++)
        {
            float progress = frameCount > 1 ? i / (float)(frameCount - 1) : 0.0f;
            controller.yaw = spin && frameCount > 1 ? yaw + 360.0f * i / frameCount : Mathf.Lerp(yaw, yawTo, progress);
            controller.pitch = pitch;
            controller.PlaceCamera();

            if (dialKeys.Count > 1)
            {
                SlideDials(controller, dialKeys, i, frameCount);
                controller.EaseBouquet(frameSeconds);
                camera.backgroundColor = BouquetPalette.At(builder.palette).background;
            }

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

    private static bool ApplyDials(BouquetController controller, string values)
    {
        if (string.IsNullOrEmpty(values))
        {
            return true;
        }

        float[] parsed = ParseDials(controller, values);
        if (parsed == null)
        {
            return false;
        }

        for (int i = 0; i < parsed.Length; i++)
        {
            controller.dials[i].value = parsed[i];
        }

        BouquetDialSet.Apply(controller.dials, controller.builder);
        Debug.Log($"BOUQUET_CAPTURE: dials {values}");
        return true;
    }

    private static float[] ParseDials(BouquetController controller, string values)
    {
        if (string.IsNullOrEmpty(values))
        {
            return null;
        }

        string[] parts = values.Split(',');
        if (parts.Length != controller.dials.Length)
        {
            Debug.LogError($"BOUQUET_CAPTURE: dials need {controller.dials.Length} values, got {parts.Length}");
            return null;
        }

        float[] parsed = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            parsed[i] = Mathf.Clamp01(float.Parse(parts[i].Trim(), CultureInfo.InvariantCulture));
        }

        return parsed;
    }

    private static List<float[]> ParseDialKeys(BouquetController controller, string values)
    {
        List<float[]> keys = new List<float[]> { ReadDialValues(controller) };
        if (string.IsNullOrEmpty(values))
        {
            return keys;
        }

        foreach (string key in values.Split(';'))
        {
            float[] parsed = ParseDials(controller, key);
            if (parsed == null)
            {
                return null;
            }

            keys.Add(parsed);
        }

        return keys;
    }

    private static float[] ReadDialValues(BouquetController controller)
    {
        float[] values = new float[controller.dials.Length];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = controller.dials[i].value;
        }

        return values;
    }

    private static void SlideDials(BouquetController controller, List<float[]> keys, int frame, int frameCount)
    {
        int legs = keys.Count - 1;
        int framesPerLeg = Mathf.Max(frameCount / legs, 2);
        int leg = Mathf.Min(frame / framesPerLeg, legs - 1);
        float progress = Mathf.Clamp01((frame - leg * framesPerLeg) / (float)(framesPerLeg - 1));
        float drag = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(progress / DragShare));

        for (int i = 0; i < controller.dials.Length; i++)
        {
            controller.dials[i].value = Mathf.Lerp(keys[leg][i], keys[leg + 1][i], drag);
        }

        controller.RetargetDials();
    }

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
                builder.palette = float.Parse(raw, CultureInfo.InvariantCulture);
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
