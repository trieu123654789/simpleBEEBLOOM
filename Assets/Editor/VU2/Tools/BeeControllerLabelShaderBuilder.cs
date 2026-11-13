using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Keep SIMPLE graph shading intact; add raster depth bias only for controller labels.
public static class BeeControllerLabelShaderBuilder
{
    const string SourcePath = "Assets/Controller/Shaders/SIMPLE_Controller_Labels.shadergraph";
    const string OutputPath = "Assets/Controller/Shaders/SIMPLE_Controller_Labels_DepthBias.shader";

    [MenuItem("Tools/Let It Bee/Controllers/Rebuild Label Depth Shader")]
    public static void Rebuild()
    {
        var importer = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter"))
            .FirstOrDefault(t => t != null);
        var method = importer?.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == "GetShaderText" && m.GetParameters().Length == 4
                && m.GetParameters()[3].IsOut);
        if (method == null) throw new InvalidOperationException("Shader Graph exporter changed; review this builder before regenerating.");
        var source = (string)method.Invoke(null, new object[] { SourcePath, null, null, null });
        if (string.IsNullOrEmpty(source) || !source.Contains("ZTest LEqual"))
            throw new InvalidOperationException("Expected SIMPLE label depth-tested shader output was not generated.");
        source = Regex.Replace(source, "Shader \\\"[^\\\"]+\\\"", "Shader \"LetItBee/Controllers/SIMPLE_Controller_Labels_DepthBias\"", RegexOptions.None, TimeSpan.FromSeconds(1));
        source = Regex.Replace(source, @"(?m)^(\s*)ZTest LEqual\s*$", "$1ZTest LEqual\n$1Offset -1, -1");
        File.WriteAllText(OutputPath, "// Generated from SIMPLE_Controller_Labels.shadergraph.\n// Rebuild with Tools/Let It Bee/Controllers/Rebuild Label Depth Shader.\n// Preserve LEqual occlusion; apply a small slope + unit bias to avoid coplanar flicker.\n" + source);
        AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Rebuilt controller label shader with depth bias: " + OutputPath);
    }
}