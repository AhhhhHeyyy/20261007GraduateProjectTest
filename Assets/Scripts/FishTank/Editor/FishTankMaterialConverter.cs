using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 套件材質跟著專案的渲染管線走：Built-in 用 Standard、URP 用 URP Lit。
/// 匯入套件或開啟專案時自動檢查 Assets/FishTank 底下的材質，管線不對就轉換（顏色、貼圖、透明、自發光都保留）。
/// 也可以手動執行 Tools > FishTank > 材質轉成目前的渲染管線。PortalWindow 材質兩種管線通用，不轉。
/// </summary>
[InitializeOnLoad]
public static class FishTankMaterialConverter
{
    const string Folder = "Assets/FishTank";
    const string ErrorShader = "Hidden/InternalErrorShader";

    static FishTankMaterialConverter()
    {
        // 等編譯、匯入結束再做，避免素材還沒載入完
        EditorApplication.delayCall += () => Convert(silent: true);
    }

    [MenuItem("Tools/FishTank/材質轉成目前的渲染管線")]
    static void ConvertFromMenu() => Convert(silent: false);

    static void Convert(bool silent)
    {
        if (!AssetDatabase.IsValidFolder(Folder)) return;

        Shader target = FishTankRender.LitShader;
        if (target == null)
        {
            if (!silent) Debug.LogError($"FishTank：找不到 {(FishTankRender.IsBuiltIn ? FishTankRender.BuiltInLitShader : FishTankRender.UrpLitShader)} shader。");
            return;
        }

        var converted = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || !NeedsConversion(material)) continue;

            ConvertMaterial(material, target);
            converted.Add(System.IO.Path.GetFileNameWithoutExtension(path));
        }

        if (converted.Count > 0)
        {
            AssetDatabase.SaveAssets();
            string pipeline = FishTankRender.IsBuiltIn ? "Built-in（Standard）" : "URP（URP Lit）";
            Debug.Log($"FishTank：{converted.Count} 個材質已轉成 {pipeline}：{string.Join("、", converted)}");
        }
        else if (!silent)
        {
            Debug.Log("FishTank：材質已經是目前渲染管線的版本，不用轉換。");
        }
    }

    static bool NeedsConversion(Material material)
    {
        string shader = material.shader != null ? material.shader.name : ErrorShader;
        if (shader.StartsWith("FishTank/")) return false; // PortalWindow 兩邊通用

        if (FishTankRender.IsBuiltIn)
            return shader.StartsWith("Universal Render Pipeline/") || shader == ErrorShader;
        return shader == FishTankRender.BuiltInLitShader || shader == ErrorShader;
    }

    static void ConvertMaterial(Material material, Shader target)
    {
        // shader 不見（例如 URP 沒裝）時 material.GetColor 讀不到，直接讀存檔裡的值
        var saved = new SerializedObject(material).FindProperty("m_SavedProperties");
        Color color = SavedColor(saved, "_BaseColor") ?? SavedColor(saved, "_Color") ?? Color.white;
        Color emission = SavedColor(saved, "_EmissionColor") ?? Color.black;
        Texture mainTex = SavedTexture(saved, "_BaseMap") ?? SavedTexture(saved, "_MainTex");
        bool transparent = SavedFloat(saved, "_Surface") == 1f || SavedFloat(saved, "_Mode") >= 2f;
        bool emissive = emission.maxColorComponent > 0.001f;

        Undo.RecordObject(material, "Convert FishTank Material");
        material.shader = target;
        material.shaderKeywords = new string[0];
        FishTankRender.ConfigureLit(material, color, transparent, emissive);
        if (emissive) material.SetColor("_EmissionColor", emission);
        if (mainTex != null) material.mainTexture = mainTex;
        EditorUtility.SetDirty(material);
    }

    static Color? SavedColor(SerializedProperty saved, string name)
    {
        var entry = Find(saved.FindPropertyRelative("m_Colors"), name);
        return entry != null ? entry.FindPropertyRelative("second").colorValue : (Color?)null;
    }

    static float? SavedFloat(SerializedProperty saved, string name)
    {
        var entry = Find(saved.FindPropertyRelative("m_Floats"), name);
        return entry != null ? entry.FindPropertyRelative("second").floatValue : (float?)null;
    }

    static Texture SavedTexture(SerializedProperty saved, string name)
    {
        var entry = Find(saved.FindPropertyRelative("m_TexEnvs"), name);
        return entry != null ? entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture : null;
    }

    static SerializedProperty Find(SerializedProperty array, string name)
    {
        if (array == null) return null;
        for (int i = 0; i < array.arraySize; i++)
        {
            var entry = array.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("first").stringValue == name) return entry;
        }
        return null;
    }
}
