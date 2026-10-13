using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 標記「玩家用來點擊、夾東西的相機」。掛在沙灘相機、魚缸相機上；
/// 只負責畫面效果、UI、RenderTexture 的相機不要掛。
/// 同時有好幾台標記過的相機開著時（例如 zoom 轉場中），Priority 大的優先，一樣大再比 Depth。
/// </summary>
[RequireComponent(typeof(Camera))]
public class InteractionCamera : MonoBehaviour
{
    public static readonly List<InteractionCamera> Active = new List<InteractionCamera>();

    [SerializeField] int priority;

    public int Priority
    {
        get => priority;
        set => priority = value;
    }

    public Camera Camera { get; private set; }

    void Awake() => Camera = GetComponent<Camera>();
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);
}
