using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 渲染管線相關的小工具：判斷目前是 Built-in 還是 URP，並建立／設定對應的 Lit 材質。
/// 魚、軟糖、垃圾改顏色時用的 material.color 和 _EmissionColor 兩種管線通用，所以只有「建材質」需要分開。
/// </summary>
public static class FishTankRender
{
    public const string UrpLitShader = "Universal Render Pipeline/Lit";
    public const string BuiltInLitShader = "Standard";

    /// <summary>目前用的是 Built-in（沒有指定 Render Pipeline Asset）。</summary>
    public static bool IsBuiltIn => GraphicsSettings.currentRenderPipeline == null;

    public static Shader LitShader => Shader.Find(IsBuiltIn ? BuiltInLitShader : UrpLitShader);

    public static Material CreateLit(Color color, bool transparent, bool emissive = false)
    {
        var material = new Material(LitShader);
        ConfigureLit(material, color, transparent, emissive);
        return material;
    }

    /// <summary>依目前管線的 Lit shader 設定顏色、透明與自發光（材質的 shader 要先設好）。</summary>
    public static void ConfigureLit(Material material, Color color, bool transparent, bool emissive)
    {
        material.color = color;
        if (transparent)
        {
            if (material.shader.name == BuiltInLitShader) SetStandardFade(material);
            else SetUrpTransparent(material);
        }
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }

    static void SetUrpTransparent(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetShaderPassEnabled("DepthOnly", false);
        material.SetShaderPassEnabled("ShadowCaster", false);
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    // Standard shader 的 Fade 模式（和 Inspector 選 Rendering Mode = Fade 一樣）
    static void SetStandardFade(Material material)
    {
        material.SetFloat("_Mode", 2f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }
}
