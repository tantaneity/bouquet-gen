using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SceneBootstrap
{
    private const string ScenePath = "Assets/Scenes/Bouquet.unity";
    private const string ShaderPath = "Assets/Shaders/BouquetFlat.shader";
    private const string BouquetMaterialPath = "Assets/Materials/BouquetFlat.mat";
    private const string DialMaterialPath = "Assets/Materials/BouquetDials.mat";

    private const float FieldOfView = 30.0f;
    private const float DialLineWidth = 0.0011f;

    private static readonly Color DialInk = new Color(0.62f, 0.65f, 0.67f, 1.0f);

    public static void Run()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            Debug.LogError($"SCENE_BOOTSTRAP: shader not found at {ShaderPath}");
            EditorApplication.Exit(1);
            return;
        }

        BouquetPalette colors = BouquetPalette.Preset(2);
        Material bouquetMaterial = LoadOrCreate(shader, BouquetMaterialPath, colors.ink, 0.0021f);
        Material dialMaterial = LoadOrCreate(shader, DialMaterialPath, DialInk, DialLineWidth);
        dialMaterial.SetFloat("_ShadeStrength", 0.0f);
        EditorUtility.SetDirty(dialMaterial);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = FieldOfView;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 50.0f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = colors.background;

        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;

        GameObject bouquet = NewRenderer("Bouquet", bouquetMaterial);
        bouquet.AddComponent<BouquetBuilder>().Rebuild();

        GameObject dials = NewRenderer("Dials", dialMaterial);

        BouquetController controller = cameraObject.AddComponent<BouquetController>();
        controller.builder = bouquet.GetComponent<BouquetBuilder>();
        controller.dialMesh = dials.GetComponent<MeshFilter>();
        controller.dialRenderer = dials.GetComponent<MeshRenderer>();
        controller.ApplyDials();
        controller.PlaceCamera();

        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        int bouquetVertices = bouquet.GetComponent<MeshFilter>().sharedMesh.vertexCount;
        int dialVertices = dials.GetComponent<MeshFilter>().sharedMesh.vertexCount;
        Debug.Log($"SCENE_BOOTSTRAP: saved={saved} bouquetVertices={bouquetVertices} dialVertices={dialVertices}");
        EditorApplication.Exit(saved ? 0 : 1);
    }

    private static GameObject NewRenderer(string name, Material material)
    {
        GameObject target = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return target;
    }

    private static Material LoadOrCreate(Shader shader, string path, Color ink, float lineWidth)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Material material = new Material(shader);
        material.SetFloat("_LineWidth", lineWidth);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
