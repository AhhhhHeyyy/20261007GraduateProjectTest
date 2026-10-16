using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 簡易運鏡：物件掉進小魚缸時，鏡頭從目前位置轉向、繞到小魚缸正前方推進，到點瞬移成魚缸世界的 Tank View；
/// 物件被帶出時，瞬移回沙灘、往後拉出，再回到進魚缸前的位置（或指定的 Beach View）。
/// 鏡頭一開始不必在小魚缸前面：路線是一條弧線，先轉向、經過小魚缸正前方 Approach Distance 的點，再直直推進。
/// 推進的終點是「Tank View 換算回沙灘的位置」，所以瞬移前後構圖相同。
/// 掛在相機上，或相機群組的最外層（Target 指向要移動的物件）。不勾這個元件就不會運鏡。
/// 也可以不靠事件，自己呼叫 GoToTank() / GoToBeach()。
/// </summary>
public class TransferCameraMove : MonoBehaviour
{
    [Tooltip("要移動的物件；留空就移動自己。相機群組的話拖最外層")]
    [SerializeField] Transform target;
    [Tooltip("留空會自動尋找")]
    [SerializeField] ScaleBridge bridge;

    [Header("Trigger")]
    [Tooltip("物件掉進小魚缸時跟進去")]
    [SerializeField] bool followIntoTank = true;
    [Tooltip("大型垃圾帶出時跟回沙灘")]
    [SerializeField] bool followOutToBeach = true;
    [Tooltip("測試用：按這個鍵手動切換（None = 不用）")]
    [SerializeField] KeyCode toggleKey = KeyCode.None;
    [Tooltip("開場就在魚缸世界")]
    [SerializeField] bool startInTank;

    [Header("Views")]
    [Tooltip("回沙灘時停的視角；留空時回到進魚缸前的位置")]
    [SerializeField] Transform beachView;
    [Tooltip("進魚缸後停的視角（放在魚缸世界）；留空時用 FishTankSetup 底下的 TankView，再沒有就停在貼著小魚缸玻璃的位置")]
    [SerializeField] Transform tankView;
    [Tooltip("沒有 Tank View 時：停的位置離小魚缸中心多遠（沙灘單位）")]
    [SerializeField] float glassDistance = 1.2f;
    [Tooltip("推進前先經過小魚缸正前方多遠的位置（沙灘單位）；鏡頭不在小魚缸前面時靠它繞過來。0 = 直線過去")]
    [SerializeField] float approachDistance = 2f;

    [Header("Timing")]
    [Tooltip("觸發後等多久才開始動（先看物件掉下去）")]
    [SerializeField] float startDelay = 0.4f;
    [Tooltip("推進（或拉回）一次的時間")]
    [FormerlySerializedAs("pushTime")]
    [SerializeField] float moveTime = 1.2f;
    [Tooltip("進魚缸後 Near / Far Clip 乘上倍數，出來再除回去")]
    [SerializeField] bool scaleClipPlanes = true;

    public bool InTank { get; private set; }
    public bool IsMoving => routine != null;

    Camera cam;
    Pose returnPose;
    bool hasReturnPose;
    Coroutine routine;

    public void Initialize(ScaleBridge scaleBridge, Transform tankViewPose, KeyCode key = KeyCode.None)
    {
        if (bridge != null && isActiveAndEnabled) OnDisable();
        bridge = scaleBridge;
        tankView = tankViewPose;
        toggleKey = key;
        if (isActiveAndEnabled) OnEnable();
    }

    void Awake()
    {
        if (target == null) target = transform;
        if (bridge == null) bridge = FindAnyObjectByType<ScaleBridge>();
        cam = target.GetComponentInChildren<Camera>();
    }

    void OnEnable()
    {
        if (bridge == null) return;
        bridge.MovedToTank += OnMovedToTank;
        bridge.MovedToBeach += OnMovedToBeach;
    }

    void OnDisable()
    {
        if (bridge != null)
        {
            bridge.MovedToTank -= OnMovedToTank;
            bridge.MovedToBeach -= OnMovedToBeach;
        }
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    void Start()
    {
        if (bridge == null) Debug.LogError("TransferCameraMove: 場景裡找不到 ScaleBridge。", this);
        InTank = startInTank;
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && FishTankInput.GetKeyDown(toggleKey))
        {
            if (InTank) GoToBeach(0f);
            else GoToTank(0f);
        }
    }

    void OnMovedToTank(Transform item)
    {
        if (followIntoTank) GoToTank(startDelay);
    }

    void OnMovedToBeach(Transform item)
    {
        if (followOutToBeach) GoToBeach(startDelay);
    }

    public void GoToTank(float delay = 0f)
    {
        if (InTank || IsMoving || bridge == null) return;
        routine = StartCoroutine(EnterTank(delay));
    }

    public void GoToBeach(float delay = 0f)
    {
        if (!InTank || IsMoving || bridge == null) return;
        routine = StartCoroutine(ExitTank(delay));
    }

    IEnumerator EnterTank(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // 記住出發位置，回來時用
        returnPose = ToPose(target);
        hasReturnPose = true;

        // 轉向、經過小魚缸正前方，推到「停留視角在沙灘上對應的位置」→ 瞬移，剛好就是魚缸世界的停留視角
        Pose tankPose = TankPose();
        Pose entry = new Pose(bridge.TankToBeach(tankPose.position), tankPose.rotation);
        Vector3 approach = entry.position - entry.forward * approachDistance;
        yield return Curve(ToPose(target), approach, entry, moveTime, turnEarly: true);

        Place(tankPose);
        ScaleClip(bridge.Factor);
        InTank = true;
        routine = null;
    }

    IEnumerator ExitTank(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // 瞬移回沙灘上對應的位置（構圖不變）→ 往後拉出小魚缸 → 回到出發前的位置
        Pose exit = new Pose(bridge.TankToBeach(target.position), target.rotation);
        Place(exit);
        ScaleClip(1f / bridge.Factor);
        InTank = false;

        Vector3 pullBack = exit.position - exit.forward * approachDistance;
        yield return Curve(exit, pullBack, ReturnPose(exit), moveTime, turnEarly: false);
        routine = null;
    }

    /// <summary>回沙灘停的位置：Beach View → 進魚缸前的位置 → 都沒有時（開場就在魚缸）停在小魚缸前方。</summary>
    Pose ReturnPose(Pose exit)
    {
        if (beachView != null) return ToPose(beachView);
        if (hasReturnPose) return returnPose;
        return new Pose(exit.position - exit.forward * (approachDistance * 2f), exit.rotation);
    }

    /// <summary>魚缸世界裡的停留視角；沒指定時用貼著小魚缸玻璃、正對中心的位置換算過去。</summary>
    Pose TankPose()
    {
        if (tankView != null) return ToPose(tankView);
        if (bridge.TankView != null) return ToPose(bridge.TankView);
        Pose glass = GlassPose();
        return new Pose(bridge.BeachToTank(glass.position), glass.rotation);
    }

    Pose GlassPose()
    {
        Vector3 center = bridge.BeachTankCenter;
        Vector3 direction = center - target.position;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        return new Pose(center - direction * glassDistance, Quaternion.LookRotation(direction));
    }

    /// <summary>
    /// 沿弧線（二次貝茲：from → 經過 via 附近 → to）移動。
    /// turnEarly：前半段就轉到 to 的方向（推進用，先對準再直直推）；否則整段慢慢轉。
    /// </summary>
    IEnumerator Curve(Pose from, Vector3 via, Pose to, float duration, bool turnEarly)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            float u = 1f - k;
            Vector3 position = u * u * from.position + 2f * u * k * via + k * k * to.position;
            float turn = turnEarly ? Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, k * 1.8f)) : k;
            Place(new Pose(position, Quaternion.Slerp(from.rotation, to.rotation, turn)));
            yield return null;
        }
        Place(to);
    }

    void Place(Pose pose) => target.SetPositionAndRotation(pose.position, pose.rotation);

    void ScaleClip(float factor)
    {
        if (!scaleClipPlanes || cam == null) return;
        cam.nearClipPlane *= factor;
        cam.farClipPlane *= factor;
    }

    static Pose ToPose(Transform t) => new Pose(t.position, t.rotation);
}
