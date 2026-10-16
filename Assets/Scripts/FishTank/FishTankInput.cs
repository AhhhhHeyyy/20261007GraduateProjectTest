using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 滑鼠與鍵盤輸入，舊版 Input Manager 和新版 Input System 都能用，不用改 Active Input Handling。
/// 兩種都開（Both）時用舊版；只開新版時改用 Input System 的 Mouse / Keyboard。
/// </summary>
public static class FishTankInput
{
    /// <summary>
    /// 從滑鼠位置打出的射線。滑鼠位置無效（編輯器失焦、剛按 Play、滑鼠在畫面外時可能是 NaN）或不在這台相機的畫面內時回傳 false，
    /// 避免 ScreenPointToRay 報「Screen position out of view frustum」。
    /// </summary>
    public static bool TryGetPointerRay(Camera cam, out Ray ray)
    {
        ray = default;
        if (cam == null) return false;
        Vector2 p = PointerPosition;
        if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y)) return false;
        if (!cam.pixelRect.Contains(p)) return false;
        ray = cam.ScreenPointToRay(p);
        return true;
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    public static Vector2 PointerPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    public static bool GetMouseButton(int button) => MouseButton(button)?.isPressed ?? false;
    public static bool GetMouseButtonDown(int button) => MouseButton(button)?.wasPressedThisFrame ?? false;

    public static bool GetKeyDown(KeyCode key)
    {
        if (Keyboard.current == null || !TryMapKey(key, out Key mapped)) return false;
        return Keyboard.current[mapped].wasPressedThisFrame;
    }

    static UnityEngine.InputSystem.Controls.ButtonControl MouseButton(int button)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return null;
        switch (button)
        {
            case 0: return mouse.leftButton;
            case 1: return mouse.rightButton;
            case 2: return mouse.middleButton;
            default: return null;
        }
    }

    // KeyCode 和 Key 大多同名（A–Z、Tab、Space、F1…、方向鍵），只有數字鍵和 Enter 要另外對
    static bool TryMapKey(KeyCode key, out Key mapped)
    {
        // Input System 的數字鍵順序是 Digit1…Digit9、Digit0
        if (key == KeyCode.Alpha0) { mapped = Key.Digit0; return true; }
        if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha9)
        {
            mapped = Key.Digit1 + (key - KeyCode.Alpha1);
            return true;
        }
        if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
        {
            mapped = Key.Numpad0 + (key - KeyCode.Keypad0);
            return true;
        }
        if (key == KeyCode.Return) { mapped = Key.Enter; return true; }
        return System.Enum.TryParse(key.ToString(), out mapped) && mapped != Key.None;
    }
#else
    public static Vector2 PointerPosition => Input.mousePosition;
    public static bool GetMouseButton(int button) => Input.GetMouseButton(button);
    public static bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button);
    public static bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);
#endif
}
