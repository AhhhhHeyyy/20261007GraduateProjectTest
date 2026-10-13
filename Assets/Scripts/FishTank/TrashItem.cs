using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可以夾的物件（垃圾），沙灘和魚缸共用同一個 Prefab。
/// 在水裡（WaterVolume）：大型夾得住、拉得出水面；小型夾住後跟不上、一直抖，最後一定滑掉沉回去。
/// 在水外（沙灘）：大小都正常跟手。跨世界時由 ScaleBridge 呼叫 Teleport() 換位置與尺寸。
/// 手感參數都在這裡；TrashGrabber 只負責輸入、判定與回饋。需要 Rigidbody + Collider。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TrashItem : MonoBehaviour
{
    public enum TrashSize { Small, Large }
    public enum HoldState { Free, Held, Slipped, Removed }

    public static readonly List<TrashItem> Active = new List<TrashItem>();

    /// <summary>大型垃圾被拉出水面（WaterVolume 勾 Remove On Exit）時觸發。有 ScaleBridge 時會把它送回沙灘，否則消失。</summary>
    public static event Action<TrashItem> Removed;

    [SerializeField] TrashSize size = TrashSize.Large;
    [Tooltip("勾起來：進魚缸後的大小改用 Tank Size，不照比例。帶出來時會還原成原本的大小")]
    [SerializeField] bool customTankSize;
    [Tooltip("進魚缸後的大小。1 = 照比例（從沙灘看起來和掉進去前一樣大），0.5 = 一半，2 = 兩倍")]
    [Range(0.05f, 3f)]
    [SerializeField] float tankSize = 0.5f;

    [Header("Water")]
    [SerializeField] float waterDrag = 3f;
    [SerializeField] float waterAngularDrag = 2f;
    [Tooltip("在水裡抵銷多少重力（1 = 懸浮不動）")]
    [Range(0f, 1f)]
    [SerializeField] float buoyancy = 0.6f;

    [Header("Held")]
    [Tooltip("跟上夾子的速度")]
    [SerializeField] float followGain = 12f;
    [SerializeField] float maxFollowSpeed = 8f;
    [Tooltip("大型垃圾拉到水面上多高算帶出")]
    [SerializeField] float removeHeight = 0.6f;
    [SerializeField] float removeTime = 0.4f;

    [Header("Small: Slip (in water)")]
    [Tooltip("小型垃圾的阻力：0 = 跟大型一樣順，1 = 完全拉不動")]
    [Range(0f, 1f)]
    [SerializeField] float resistance = 0.75f;
    [Tooltip("夾住後多久一定滑掉（秒，隨機範圍）")]
    [SerializeField] Vector2 slipTime = new Vector2(0.8f, 1.8f);
    [Tooltip("夾子和垃圾拉開超過這個距離就滑掉（拉太快）")]
    [SerializeField] float slipDistance = 0.8f;
    [Tooltip("離水面還剩這個距離就一定滑掉，所以小型垃圾永遠出不了水")]
    [SerializeField] float surfaceSlipMargin = 0.8f;
    [Tooltip("被夾著時亂抖的幅度")]
    [SerializeField] float struggleJitter = 0.12f;
    [SerializeField] float struggleFrequency = 14f;
    [Tooltip("滑掉時往下、往旁邊彈開的力道")]
    [SerializeField] float slipKick = 1.5f;
    [Tooltip("滑掉後多久才能再夾")]
    [SerializeField] float regrabDelay = 0.5f;

    public TrashSize Size => size;
    /// <summary>進魚缸時在「照比例」之外再乘的倍數。</summary>
    public float TankSizeMultiplier => customTankSize ? tankSize : 1f;
    public HoldState State { get; private set; } = HoldState.Free;
    public bool CanGrab => State == HoldState.Free && Time.time >= regrabTime;
    /// <summary>夾子目標點和垃圾目前位置的距離（0–1，越大越吃力），給回饋用。</summary>
    public float Strain { get; private set; }
    /// <summary>目前所在（或正上方）的水；在沙灘上是 null。</summary>
    public WaterVolume Water => water;

    Rigidbody body;
    WaterVolume water;
    bool inWater;
    Vector3 target;
    float holdTimer;
    float slipDeadline;
    float regrabTime;
    float noiseSeed;

    public void Configure(TrashSize trashSize) => size = trashSize;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        noiseSeed = UnityEngine.Random.value * 100f;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    public bool TryGrab(Vector3 grabTarget)
    {
        if (!CanGrab) return false;

        State = HoldState.Held;
        target = grabTarget;
        holdTimer = 0f;
        slipDeadline = UnityEngine.Random.Range(slipTime.x, slipTime.y);
        body.useGravity = false;
        return true;
    }

    public void MoveTo(Vector3 grabTarget) => target = grabTarget;

    public void Release()
    {
        if (State != HoldState.Held) return;
        State = HoldState.Free;
        body.useGravity = true;
        Strain = 0f;
    }

    /// <summary>
    /// 瞬間移到另一個位置並縮放（跨沙灘／魚缸用）。會中斷被夾、帶出動畫，回到自由狀態。
    /// </summary>
    public void Teleport(Vector3 position, float scaleFactor, Vector3 velocity)
    {
        StopAllCoroutines();
        State = HoldState.Free;
        Strain = 0f;
        water = null;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = true;
        body.isKinematic = false;
        body.useGravity = true;

        transform.localScale *= scaleFactor;
        transform.position = position;
        body.position = position;
        SetVelocity(velocity);
        body.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (State == HoldState.Removed) return;

        UpdateWater();
        SetDamping(inWater ? waterDrag : 0f, inWater ? waterAngularDrag : 0.05f);

        if (State == HoldState.Slipped && Time.time >= regrabTime)
            State = HoldState.Free;

        if (State != HoldState.Held)
        {
            if (inWater) body.AddForce(-Physics.gravity * buoyancy, ForceMode.Acceleration);
            return;
        }

        holdTimer += Time.fixedDeltaTime;
        if (size == TrashSize.Small && water != null) UpdateSmallInWater();
        else UpdateFollow();
    }

    void UpdateWater()
    {
        WaterVolume current = WaterVolume.Find(body.position);
        inWater = current != null;
        if (current != null) water = current;
        // 被拉到水面上方時還記得是哪片水，才判斷得了「帶出」
        else if (water != null && !water.IsAbove(body.position)) water = null;
    }

    void UpdateFollow()
    {
        Follow(target, followGain);
        Strain = Mathf.Clamp01((target - body.position).magnitude / 1f);

        if (size == TrashSize.Large && water != null && water.RemoveOnExit &&
            body.position.y > water.SurfaceY + removeHeight)
            StartCoroutine(RemoveRoutine());
    }

    void UpdateSmallInWater()
    {
        // 阻力：跟得很慢、一直亂抖，接近水面一定滑掉
        float t = Time.time * struggleFrequency;
        Vector3 jitter = new Vector3(
            Mathf.PerlinNoise(noiseSeed, t) - 0.5f,
            Mathf.PerlinNoise(noiseSeed + 10f, t) - 0.5f,
            Mathf.PerlinNoise(noiseSeed + 20f, t) - 0.5f) * (2f * struggleJitter);
        Follow(target + jitter, followGain * (1f - resistance));
        body.AddTorque(jitter * 20f, ForceMode.Acceleration);

        float gap = (target - body.position).magnitude;
        Strain = Mathf.Clamp01(gap / slipDistance);

        bool timeUp = holdTimer >= slipDeadline;
        bool pulledTooFast = gap > slipDistance;
        bool nearSurface = body.position.y > water.SurfaceY - surfaceSlipMargin;
        if (timeUp || pulledTooFast || nearSurface)
            Slip();
    }

    void Follow(Vector3 point, float gain)
    {
        Vector3 velocity = (point - body.position) * gain;
        SetVelocity(Vector3.ClampMagnitude(velocity, maxFollowSpeed));
    }

    void Slip()
    {
        State = HoldState.Slipped;
        regrabTime = Time.time + regrabDelay;
        body.useGravity = true;
        Strain = 0f;

        Vector3 side = UnityEngine.Random.insideUnitSphere;
        side.y = 0f;
        SetVelocity((Vector3.down + side).normalized * slipKick);
        body.AddTorque(UnityEngine.Random.insideUnitSphere * slipKick * 5f, ForceMode.VelocityChange);
    }

    // Unity 6 把 velocity / drag / angularDrag 改名成 linearVelocity / linearDamping / angularDamping。
    // 包一層讓 2023 和 Unity 6 都能直接編譯，匯入時不會跳 Script Updating Consent。
    void SetVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = velocity;
#else
        body.velocity = velocity;
#endif
    }

    void SetDamping(float linear, float angular)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearDamping = linear;
        body.angularDamping = angular;
#else
        body.drag = linear;
        body.angularDrag = angular;
#endif
    }

    IEnumerator RemoveRoutine()
    {
        State = HoldState.Removed;
        Strain = 0f;
        body.isKinematic = true;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        // 有人接手（例如 ScaleBridge 把它 Teleport 到沙灘）就不播消失動畫
        Removed?.Invoke(this);
        if (State != HoldState.Removed) yield break;

        // 沒人接手：往上彈一下再縮掉
        Vector3 from = transform.position;
        Vector3 startScale = transform.localScale;
        for (float t = 0f; t < removeTime; t += Time.deltaTime)
        {
            float k = t / removeTime;
            transform.position = from + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.4f + k * 0.3f);
            transform.localScale = startScale * (1f - k * k);
            yield return null;
        }
        Destroy(gameObject);
    }
}
