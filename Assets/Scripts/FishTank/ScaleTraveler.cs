using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛上去就代表「這個物品可以放進魚缸」，不用設定，也不改它原本的物理規則和父子關係：
/// - 在沙灘上進到小魚缸裡 → 搬到魚缸世界、放大（倍數 = ScaleBridge.Factor）。
/// - 在魚缸世界被拿到魚缸頂端上方 → 縮小、放到沙灘的 ExitPoint。
/// 搬的時候只動這個物件本身的位置和大小；有 Rigidbody 的話速度依倍數換算，其他設定都不碰。
/// 換世界時如果它正被別的物件抓著（父物件和開場時不同），會先放回開場時的父物件，免得留在另一個世界的夾子上。
/// （垃圾用 TrashItem 就好，它本身已經會換場。）
/// </summary>
[DisallowMultipleComponent]
public class ScaleTraveler : MonoBehaviour
{
    public static readonly List<ScaleTraveler> Active = new List<ScaleTraveler>();

    [Tooltip("可選：你的腳本想暫時禁止它換場時設成 true（一般不用管）")]
    public bool Held;

    [Tooltip("勾起來：進魚缸後的大小改用 Tank Size，不照比例。帶出來時會還原成原本的大小")]
    public bool customTankSize;
    [Tooltip("進魚缸後的大小。1 = 照比例（從沙灘看起來和掉進去前一樣大），0.5 = 一半，2 = 兩倍")]
    [Range(0.05f, 3f)]
    public float tankSize = 0.5f;

    /// <summary>進魚缸時在「照比例」之外再乘的倍數。</summary>
    public float TankSizeMultiplier => customTankSize ? tankSize : 1f;

    Rigidbody body;
    Transform originalParent;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        originalParent = transform.parent;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    /// <summary>瞬間移到另一個世界的位置並縮放；Rigidbody 的速度跟著倍數換算。</summary>
    public void Teleport(Vector3 position, float scaleFactor)
    {
        if (transform.parent != originalParent) transform.SetParent(originalParent, true);

        Vector3 velocity = body != null ? GetVelocity() : Vector3.zero;
        transform.localScale *= scaleFactor;
        transform.position = position;
        if (body == null) return;

        body.position = position;
        if (!body.isKinematic) SetVelocity(velocity * scaleFactor);
    }

    // Unity 6 把 velocity 改名成 linearVelocity
    Vector3 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    void SetVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = velocity;
#else
        body.velocity = velocity;
#endif
    }
}
