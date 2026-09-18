using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class SceneBootstrap
{
    private const string ScenePath = "Assets/Scenes/Bouquet.unity";
    private const string ShaderPath = "Assets/Shaders/Bouquet.shader";
    private const string MaterialPath = "Assets/Materials/Bouquet.mat";

    private const float OrthographicSize = 1.0f;
    private const float CameraDistance = 2.0f;
    private const float QuadSize = 2.0f;

    public static void Run()
    {
        Material material = CreateMaterial();
        if (material == null)
        {
            EditorApplication.Exit(1);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = OrthographicSize;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 10.0f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        cameraObject.transform.position = new Vector3(0.0f, 0.0f, -CameraDistance);

        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Bouquet";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.position = Vector3.zero;
        quad.transform.localScale = new Vector3(QuadSize, QuadSize, 1.0f);
        quad.GetComponent<MeshRenderer>().sharedMaterial = material;

        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        Debug.Log($"SCENE_BOOTSTRAP: saved={saved} path={ScenePath}");
        EditorApplication.Exit(saved ? 0 : 1);
    }

    private static Material CreateMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            Debug.LogError($"SCENE_BOOTSTRAP: shader not found at {ShaderPath}");
            return null;
        }

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            return existing;
        }

        Material material = new Material(shader);
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }
}
