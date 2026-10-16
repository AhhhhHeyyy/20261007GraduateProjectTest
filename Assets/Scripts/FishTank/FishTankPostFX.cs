using UnityEngine;
#if FISHTANK_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// 軟糖、螢光魚的光暈。掛在場景任何物件上就好：URP 時開場自動建立 Bloom 並打開操作相機的 Post Processing；
/// Built-in 時什麼都不做（Built-in 要光暈請另外安裝 Post Processing Stack v2）。
/// 用程式建立而不是在場景放 Volume，示範場景在 Built-in 專案打開時才不會出現 Missing Script。
/// </summary>
public class FishTankPostFX : MonoBehaviour
{
#pragma warning disable 414 // Built-in 時用不到

    [SerializeField] float bloomIntensity = 1.2f;
    [Tooltip("多亮才發光；越低越多東西發光")]
    [SerializeField] float bloomThreshold = 0.9f;
#pragma warning restore 414

#if FISHTANK_URP
    Camera configured;

    void Start()
    {
        if (FishTankRender.IsBuiltIn) return;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);

        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    void LateUpdate()
    {
        // 操作相機換了（例如運鏡、相機群組切換）就幫新的那台打開 Post Processing
        if (FishTankRender.IsBuiltIn) return;
        Camera cam = ViewCamera.Current;
        if (cam == null || cam == configured) return;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        configured = cam;
    }
#endif
}
