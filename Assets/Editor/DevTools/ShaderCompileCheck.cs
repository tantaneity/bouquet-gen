using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public static class ShaderCompileCheck
{
    private static readonly string[] ShaderPaths =
    {
        "Assets/Shaders/BouquetFlat.shader"
    };

    public static void Run()
    {
        int errorCount = 0;
        StringBuilder report = new StringBuilder();

        foreach (string shaderPath in ShaderPaths)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
            {
                Debug.LogError($"SHADER_CHECK: asset not found at {shaderPath}");
                EditorApplication.Exit(1);
                return;
            }

            ShaderUtil.ClearShaderMessages(shader);
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            ShaderMessage[] messages = ShaderUtil.GetShaderMessages(shader);
            foreach (ShaderMessage message in messages)
            {
                if (message.severity == ShaderCompilerMessageSeverity.Error)
                {
                    errorCount++;
                }

                report.AppendLine($"SHADER_CHECK: {shaderPath} {message.severity} line {message.line} [{message.platform}] {message.message} {message.messageDetails}");
            }

            Debug.Log($"SHADER_CHECK: {shaderPath} hasError={ShaderUtil.ShaderHasError(shader)} messages={messages.Length}");
        }

        if (report.Length > 0)
        {
            Debug.Log(report.ToString());
        }

        EditorApplication.Exit(errorCount > 0 ? 1 : 0);
    }
}
