using UnityEngine;
#if FISHTANK_URP
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// 目前玩家操作用的相機（夾子、發射軟糖用它算點擊射線）。不依賴 MainCamera Tag，依序：
/// 1. ViewCamera.Override（鏡頭組的程式可以直接指定）
/// 2. 掛了 InteractionCamera 且開著的相機：Priority 大的優先，一樣大比 Depth
/// 3. 都沒有時自動挑：開著、畫到螢幕（沒有 Target Texture）、不是 URP Overlay、Depth 最高的那台
/// 好幾台相機同時開著時，請用 1 或 2，第 3 種可能會挑錯。
/// </summary>
public static class ViewCamera
{
    public static Camera Override;

    static Camera[] buffer = new Camera[8];

    public static Camera Current
    {
        get
        {
            if (Override != null && Override.isActiveAndEnabled) return Override;

            InteractionCamera marked = null;
            foreach (InteractionCamera c in InteractionCamera.Active)
            {
                if (c.Camera == null || !c.Camera.enabled) continue;
                if (marked == null || c.Priority > marked.Priority ||
                    (c.Priority == marked.Priority && c.Camera.depth > marked.Camera.depth))
                    marked = c;
            }
            if (marked != null) return marked.Camera;

            return FindScreenCamera();
        }
    }

    static Camera FindScreenCamera()
    {
        int count = Camera.allCamerasCount;
        if (buffer.Length < count) buffer = new Camera[count * 2];
        Camera.GetAllCameras(buffer);

        Camera best = null;
        for (int i = 0; i < count; i++)
        {
            Camera cam = buffer[i];
            if (cam.targetTexture != null) continue;
#if FISHTANK_URP
            if (cam.TryGetComponent(out UniversalAdditionalCameraData data) && data.renderType == CameraRenderType.Overlay) continue;
#endif
            if (best == null || cam.depth > best.depth) best = cam;
        }
        return best != null ? best : Camera.main;
    }
}
