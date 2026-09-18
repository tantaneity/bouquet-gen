using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class SceneBootstrap
{
    private const string ScenePath = "Assets/Scenes/Bouquet.unity";
    private const string ShaderPath = "Assets/Shaders/BouquetFlat.shader";
    private const string MaterialPath = "Assets/Materials/BouquetFlat.mat";

    public const float OrbitRadius = 3.9f;
    public const float FieldOfView = 30.0f;
    public static readonly Vector3 OrbitTarget = new Vector3(0.0f, 0.18f, 0.0f);

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
        camera.orthographic = false;
        camera.fieldOfView = FieldOfView;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 50.0f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BouquetPalette.Preset(2).background;

        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;

        PlaceCamera(camera, 0.0f, 12.0f);

        GameObject bouquet = new GameObject("Bouquet", typeof(MeshFilter), typeof(MeshRenderer), typeof(BouquetBuilder));
        MeshRenderer renderer = bouquet.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        bouquet.GetComponent<BouquetBuilder>().Rebuild();

        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        Debug.Log($"SCENE_BOOTSTRAP: saved={saved} path={ScenePath} vertices={bouquet.GetComponent<MeshFilter>().sharedMesh.vertexCount}");
        EditorApplication.Exit(saved ? 0 : 1);
    }

    public static void PlaceCamera(Camera camera, float yawDegrees, float pitchDegrees)
    {
        Quaternion orbit = Quaternion.Euler(pitchDegrees, yawDegrees, 0.0f);
        camera.transform.position = OrbitTarget + orbit * (Vector3.back * OrbitRadius);
        camera.transform.rotation = Quaternion.LookRotation(OrbitTarget - camera.transform.position, Vector3.up);
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
