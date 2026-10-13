using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 傳送門窗戶：讓沙灘上的小魚缸看起來裝著魚缸世界。
/// 在魚缸世界放一台鏡像相機，每次畫面前跟著玩家的相機（ViewCamera）走到 BeachToTank 換算後的位置，畫到 RenderTexture；
/// 小魚缸的玻璃用 FishTank/PortalWindow 材質依螢幕座標取那張圖。鏡頭推進小魚缸瞬移時，前後畫面一樣，看不出切換。
/// 掛在小魚缸的根物件上。Window Root 底下的 Renderer（玻璃、缸底）會變成窗戶；框、裝飾放在 Window Root 外面就不會被換掉。
/// </summary>
public class PortalWindow : MonoBehaviour
{
    static readonly int PortalTexId = Shader.PropertyToID("_PortalTex");

    [Tooltip("留空會自動尋找")]
    [SerializeField] ScaleBridge bridge;
    [Tooltip("這個物件底下的 Renderer 會變成窗戶（BeachTankSetup 指向 Body/Tank）；留空就用掛這個元件的物件")]
    [SerializeField] Transform windowRoot;
    [Tooltip("額外指定要變成窗戶的 Renderer；有填的話優先用這裡，不看 Window Root")]
    [SerializeField] Renderer[] windows = new Renderer[0];
    [Tooltip("窗戶材質（Assets/FishTank/Materials/PortalWindow）。留空時在編輯器裡會自動建立，但 Build 出來會因為 shader 被剔除而失效，所以正式場景一定要拖")]
    [SerializeField] Material windowMaterial;
    [Tooltip("RenderTexture 解析度相對螢幕的比例；效能不夠時調低")]
    [Range(0.25f, 1f)]
    [SerializeField] float resolutionScale = 1f;

    Camera portalCamera;
    RenderTexture texture;
    Material material;
    Camera viewCamera;

    void Start()
    {
        if (bridge == null) bridge = GetComponentInParent<ScaleBridge>();
        if (bridge == null) bridge = FindAnyObjectByType<ScaleBridge>();
        if (bridge == null)
        {
            Debug.LogError("PortalWindow: 場景裡找不到 ScaleBridge。", this);
            enabled = false;
            return;
        }

        material = windowMaterial != null ? new Material(windowMaterial) : CreateMaterial();
        if (material == null)
        {
            enabled = false;
            return;
        }

        if (windows.Length == 0) windows = (windowRoot != null ? windowRoot : transform).GetComponentsInChildren<Renderer>();
        foreach (Renderer r in windows)
        {
            var shared = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < shared.Length; i++) shared[i] = material;
            r.sharedMaterials = shared;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        portalCamera = new GameObject($"{name}_PortalCamera").AddComponent<Camera>();
        portalCamera.clearFlags = CameraClearFlags.SolidColor;
        portalCamera.backgroundColor = Color.clear;
        var data = portalCamera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.None;
        data.renderShadows = true;
    }

    void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

    void OnDestroy()
    {
        if (portalCamera != null) Destroy(portalCamera.gameObject);
        if (texture != null) texture.Release();
    }

    void LateUpdate()
    {
        if (portalCamera == null) return;

        viewCamera = ViewCamera.Current;
        // 玩家在魚缸世界（或沒有相機）時不用畫
        bool show = viewCamera != null && viewCamera != portalCamera && !bridge.IsInTankWorld(viewCamera.transform.position);
        portalCamera.enabled = show;
        if (!show) return;

        EnsureTexture();
        // 鏡像相機要比主相機先畫
        portalCamera.depth = viewCamera.depth - 1f;
        // 窗戶要拿到「後面的沙灘」才能透出來
        var viewData = viewCamera.GetUniversalAdditionalCameraData();
        if (viewData.requiresColorOption != CameraOverrideOption.On)
            viewData.requiresColorOption = CameraOverrideOption.On;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        // 在鏡像相機畫之前才對位置，主相機這一幀的移動、震動都已經做完
        if (cam != portalCamera || viewCamera == null) return;

        float k = bridge.Factor;
        Transform view = viewCamera.transform;
        portalCamera.transform.SetPositionAndRotation(bridge.BeachToTank(view.position), view.rotation);
        portalCamera.fieldOfView = viewCamera.fieldOfView;
        portalCamera.orthographic = viewCamera.orthographic;
        portalCamera.orthographicSize = viewCamera.orthographicSize * k;
        portalCamera.nearClipPlane = viewCamera.nearClipPlane * k;
        portalCamera.farClipPlane = viewCamera.farClipPlane * k;
        portalCamera.aspect = viewCamera.aspect;
    }

    void EnsureTexture()
    {
        int width = Mathf.Max(16, Mathf.RoundToInt(viewCamera.pixelWidth * resolutionScale));
        int height = Mathf.Max(16, Mathf.RoundToInt(viewCamera.pixelHeight * resolutionScale));
        if (texture != null && texture.width == width && texture.height == height) return;

        if (texture != null) texture.Release();
        // Half 格式保留 HDR，發光的軟糖透過窗戶還是會有 Bloom
        texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf) { name = "PortalWindow" };
        portalCamera.targetTexture = texture;
        material.SetTexture(PortalTexId, texture);
    }

    Material CreateMaterial()
    {
        Shader shader = Shader.Find("FishTank/PortalWindow");
        if (shader == null)
        {
            Debug.LogError("PortalWindow: 找不到 FishTank/PortalWindow shader。", this);
            return null;
        }
        return new Material(shader) { name = "PortalWindow (runtime)" };
    }
}
