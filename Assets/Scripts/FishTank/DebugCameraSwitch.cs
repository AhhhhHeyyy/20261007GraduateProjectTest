using UnityEngine;

/// <summary>
/// 測試用：按 Tab 在幾台相機之間硬切（同時只開一台）。之後由正式的 zoom 運鏡取代。
/// 不需要改 Tag：夾子和軟糖會自動用畫面上開著的那台（見 ViewCamera）。
/// </summary>
public class DebugCameraSwitch : MonoBehaviour
{
    [SerializeField] Camera[] cameras = new Camera[0];
    [SerializeField] KeyCode key = KeyCode.Tab;
    [SerializeField] int startIndex;

    int index;

    public void Initialize(params Camera[] switchCameras)
    {
        cameras = switchCameras;
        Show(startIndex);
    }

    void Start()
    {
        if (cameras.Length > 0) Show(startIndex);
    }

    void Update()
    {
        if (cameras.Length > 1 && FishTankInput.GetKeyDown(key))
            Show((index + 1) % cameras.Length);
    }

    void Show(int i)
    {
        index = i;
        for (int c = 0; c < cameras.Length; c++)
            if (cameras[c] != null) cameras[c].gameObject.SetActive(c == index);
    }
}
