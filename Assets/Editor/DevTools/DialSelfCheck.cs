using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DialSelfCheck
{
    private const string ScenePath = "Assets/Scenes/Bouquet.unity";
    private const float Tolerance = 0.02f;
    private static readonly float[] Probes = { 0.05f, 0.3f, 0.5f, 0.75f, 0.95f };

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera = Object.FindFirstObjectByType<Camera>();
        BouquetController controller = Object.FindFirstObjectByType<BouquetController>();
        if (camera == null || controller == null)
        {
            Debug.LogError("DIAL_CHECK: scene is missing the camera or the controller");
            EditorApplication.Exit(1);
            return;
        }

        int failures = 0;

        for (int i = 0; i < controller.dials.Length; i++)
        {
            foreach (float expected in Probes)
            {
                Dial dial = controller.dials[i];
                dial.value = expected;

                Vector3 screen = camera.WorldToScreenPoint(dial.Handle);
                if (screen.z <= 0.0f)
                {
                    continue;
                }

                Vector2 pointer = new Vector2(screen.x, screen.y);
                float measured = BouquetDialSet.ValueUnderPointer(dial, camera, pointer);

                if (Mathf.Abs(measured - expected) > Tolerance)
                {
                    Debug.LogError($"DIAL_CHECK: dial {i} ({dial.label}) expected {expected:F3}, pointer round trip gave {measured:F3}");
                    failures++;
                }

                controller.dials[i].value = expected;
                int picked = BouquetDialSet.Pick(controller.dials, camera, pointer, 34.0f);
                if (picked != i)
                {
                    Debug.LogError($"DIAL_CHECK: dial {i} ({dial.label}) at value {expected:F3} picked as {picked}");
                    failures++;
                }
            }
        }

        Debug.Log($"DIAL_CHECK: {controller.dials.Length} dials, {Probes.Length} probes each, failures={failures}");
        EditorApplication.Exit(failures > 0 ? 1 : 0);
    }
}
