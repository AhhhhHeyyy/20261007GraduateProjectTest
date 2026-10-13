using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 沙灘上的小魚缸 ↔ 魚缸世界的換算。魚缸世界用正常比例做在遠處，沙灘上只擺一個小魚缸外殼。
/// - 沙灘的物件（TrashItem 或掛了 ScaleTraveler 的任何物品）掉進小魚缸 → 搬到魚缸世界對應的位置，放大 Factor 倍。
/// - 大型垃圾從魚缸被拉出水面，或 ScaleTraveler 物品被拿到魚缸頂端上方 → 縮小 Factor 倍，放到沙灘的 Exit Point。
/// 鏡頭怎麼在兩個世界之間切換不歸這裡管；要跟著運鏡的話訂閱 MovedToTank / MovedToBeach（例如 TransferCameraMove）。
///
/// 自動配置（BeachTankSetup Prefab 用）：Tank World 留空就自動找場景裡 FishPool 的 SwimArea；
/// Beach Tank 的大小自動對齊魚缸世界，倍數 = 1 / 這組物件的 Scale；擺在沙灘上的垃圾開場自動縮成沙灘尺寸。
/// </summary>
public class ScaleBridge : MonoBehaviour
{
    [Tooltip("沙灘上小魚缸的內部範圍（停用的 Box Collider，只讀大小）")]
    [SerializeField] BoxCollider beachTank;
    [Tooltip("魚缸世界對應的範圍；留空自動用場景裡 FishTankSetup 的 SwimArea")]
    [SerializeField] BoxCollider tankWorld;
    [Tooltip("從魚缸帶出的垃圾出現在沙灘的位置；藍色軸是拋出方向")]
    [SerializeField] Transform exitPoint;
    [SerializeField] float exitPop = 1.5f;
    [Tooltip("掉進魚缸時的下沉速度（魚缸世界單位）")]
    [SerializeField] float enterSinkSpeed = 1f;
    [Tooltip("搬過去之後多久內不會再被搬一次")]
    [SerializeField] float transferCooldown = 1f;
    [Tooltip("ScaleTraveler 物品在魚缸世界高於魚缸頂端這麼多（魚缸世界單位）就搬回沙灘")]
    [SerializeField] float travelerExitHeight = 0.5f;

    [Header("Auto")]
    // 內部用：Beach Tank 自動對齊魚缸世界的形狀，倍數才會剛好 = 1 / Scale。關掉會讓換場位置算錯，所以不顯示在 Inspector
    [HideInInspector]
    [SerializeField] bool matchTankSize = true;
    [Tooltip("開場時把擺在沙灘上的 TrashItem 縮成沙灘尺寸。預設關閉：沙灘請用 Trash_*_Beach（已是沙灘尺寸，所見即所得）；只有把魚缸尺寸的 Trash_* 放在沙灘時才需要打開")]
    [SerializeField] bool scaleBeachItems = false;

    readonly Dictionary<Component, float> cooldowns = new Dictionary<Component, float>();
    readonly List<TrashItem> buffer = new List<TrashItem>();
    readonly List<ScaleTraveler> travelerBuffer = new List<ScaleTraveler>();

    /// <summary>物件從沙灘掉進小魚缸、已經搬到魚缸世界之後觸發（參數是被搬的物件）。</summary>
    public event Action<Transform> MovedToTank;
    /// <summary>物件從魚缸帶出、已經放到沙灘之後觸發（參數是被搬的物件）。</summary>
    public event Action<Transform> MovedToBeach;

    /// <summary>魚缸世界比沙灘上的小魚缸大幾倍。</summary>
    public float Factor => WorldSize(tankWorld).x / WorldSize(beachTank).x;

    /// <summary>魚缸世界裡預設的停留視角（FishTankSetup 底下的 TankView），沒有就是 null。</summary>
    public Transform TankView { get; private set; }

    /// <summary>程式手動接線（測試場景用），會關掉自動對齊大小與自動縮放沙灘垃圾。</summary>
    public void Initialize(BoxCollider beachTankArea, BoxCollider tankWorldArea, Transform exit)
    {
        beachTank = beachTankArea;
        tankWorld = tankWorldArea;
        exitPoint = exit;
        matchTankSize = false;
        scaleBeachItems = false;
        Resolve();
    }

    void Awake() => Resolve();

    void OnEnable() => TrashItem.Removed += OnRemoved;
    void OnDisable() => TrashItem.Removed -= OnRemoved;

    void Start()
    {
        if (beachTank == null || tankWorld == null || exitPoint == null)
        {
            Debug.LogError("ScaleBridge: 找不到 Beach Tank、Tank World 或 Exit Point。場景裡要有 FishTankSetup（FishPool 要指定 Swim Area）。", this);
            return;
        }

        if (Vector3.Distance(BeachTankCenter, TankWorldCenter) < 50f)
            Debug.LogWarning("ScaleBridge: 魚缸世界離沙灘太近，沙灘鏡頭可能直接看到它。建議把 FishTankSetup 移到 X = 1000 左右。", this);

        if (scaleBeachItems) ScaleBeachItems();
    }

    void Resolve()
    {
        if (tankWorld == null)
        {
            var pool = FindAnyObjectByType<FishPool>();
            if (pool != null) tankWorld = pool.SwimAreaCollider;
        }
        if (tankWorld == null || beachTank == null) return;

        if (matchTankSize)
        {
            // 小魚缸內部 = 魚缸世界的形狀 × 這組物件的 Scale
            Vector3 world = WorldSize(tankWorld) * transform.lossyScale.x;
            Vector3 scale = beachTank.transform.lossyScale;
            beachTank.center = Vector3.zero;
            beachTank.size = new Vector3(world.x / scale.x, world.y / scale.y, world.z / scale.z);
        }

        TankView = tankWorld.transform.parent != null ? tankWorld.transform.parent.Find("TankView") : null;
    }

    /// <summary>不在魚缸世界裡、也不在小魚缸裡的垃圾，縮成沙灘尺寸。</summary>
    void ScaleBeachItems()
    {
        float k = Factor;
        foreach (TrashItem item in TrashItem.Active)
        {
            Vector3 p = item.transform.position;
            if (Contains(tankWorld, p, 1f) || Contains(beachTank, p)) continue;
            item.transform.localScale /= k;
        }
    }

    void FixedUpdate()
    {
        if (beachTank == null || tankWorld == null) return;

        // 沙灘上沒被夾著的物件，進到小魚缸範圍就搬過去
        buffer.Clear();
        buffer.AddRange(TrashItem.Active);
        foreach (TrashItem item in buffer)
        {
            if (item.State != TrashItem.HoldState.Free || !Ready(item)) continue;
            if (!Contains(beachTank, item.transform.position)) continue;

            item.Teleport(Map(beachTank, tankWorld, item.transform.position), Factor * item.TankSizeMultiplier, Vector3.down * enterSinkSpeed);
            cooldowns[item] = Time.time + transferCooldown;
            MovedToTank?.Invoke(item.transform);
        }

        // 其他物品（ScaleTraveler）：每次即時判斷在哪個世界，不記狀態
        travelerBuffer.Clear();
        travelerBuffer.AddRange(ScaleTraveler.Active);
        foreach (ScaleTraveler traveler in travelerBuffer)
        {
            // 同時掛了 TrashItem 的交給上面的垃圾規則
            if (traveler.Held || !Ready(traveler) || traveler.TryGetComponent(out TrashItem _)) continue;
            Vector3 p = traveler.transform.position;
            bool inTankWorld = IsInTankWorld(p);

            if (!inTankWorld && Contains(beachTank, p))
            {
                traveler.Teleport(Map(beachTank, tankWorld, p), Factor * traveler.TankSizeMultiplier);
                cooldowns[traveler] = Time.time + transferCooldown;
                MovedToTank?.Invoke(traveler.transform);
            }
            else if (inTankWorld && exitPoint != null && AboveTop(tankWorld, p, travelerExitHeight))
            {
                traveler.Teleport(exitPoint.position, 1f / (Factor * traveler.TankSizeMultiplier));
                cooldowns[traveler] = Time.time + transferCooldown;
                MovedToBeach?.Invoke(traveler.transform);
            }
        }
    }

    /// <summary>用各自世界的距離比：離魚缸世界比較近（換算成沙灘單位後）就算在魚缸世界。</summary>
    public bool IsInTankWorld(Vector3 position)
    {
        float toBeach = Vector3.Distance(position, BeachTankCenter);
        float toTank = Vector3.Distance(position, TankWorldCenter) / Factor;
        return toTank < toBeach;
    }

    void OnRemoved(TrashItem item)
    {
        if (exitPoint == null || tankWorld == null || beachTank == null) return;

        Vector3 velocity = (Vector3.up + exitPoint.forward) * exitPop;
        item.Teleport(exitPoint.position, 1f / (Factor * item.TankSizeMultiplier), velocity);
        cooldowns[item] = Time.time + transferCooldown;
        MovedToBeach?.Invoke(item.transform);
    }

    /// <summary>沙灘上小魚缸的中心（世界座標）。</summary>
    public Vector3 BeachTankCenter => beachTank.transform.TransformPoint(beachTank.center);
    /// <summary>魚缸世界範圍的中心（世界座標）。</summary>
    public Vector3 TankWorldCenter => tankWorld.transform.TransformPoint(tankWorld.center);

    public Vector3 BeachToTank(Vector3 point) => Map(beachTank, tankWorld, point);
    public Vector3 TankToBeach(Vector3 point) => Map(tankWorld, beachTank, point);

    bool Ready(Component item) => !cooldowns.TryGetValue(item, out float until) || Time.time >= until;

    static Vector3 WorldSize(BoxCollider box) => Vector3.Scale(box.size, box.transform.lossyScale);

    static bool Contains(BoxCollider box, Vector3 point, float margin = 0f)
    {
        Vector3 local = box.transform.InverseTransformPoint(point) - box.center;
        Vector3 half = box.size * 0.5f + Vector3.one * margin;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    /// <summary>在範圍正上方、高於頂端 height（世界單位）。</summary>
    static bool AboveTop(BoxCollider box, Vector3 point, float height)
    {
        Vector3 local = box.transform.InverseTransformPoint(point) - box.center;
        Vector3 half = box.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z &&
               local.y > half.y + height / box.transform.lossyScale.y;
    }

    /// <summary>把 from 範圍裡的相對位置換到 to 範圍裡。</summary>
    static Vector3 Map(BoxCollider from, BoxCollider to, Vector3 point)
    {
        Vector3 local = from.transform.InverseTransformPoint(point) - from.center;
        Vector3 normalized = new Vector3(local.x / from.size.x, local.y / from.size.y, local.z / from.size.z);
        return to.transform.TransformPoint(to.center + Vector3.Scale(normalized, to.size));
    }

    void OnDrawGizmosSelected()
    {
        if (beachTank != null) DrawBox(beachTank, new Color(1f, 0.85f, 0.2f, 0.8f));
        if (tankWorld != null) DrawBox(tankWorld, new Color(0.2f, 1f, 0.85f, 0.8f));
        if (exitPoint != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(exitPoint.position, 0.15f);
            Gizmos.DrawRay(exitPoint.position, exitPoint.forward * 0.6f);
        }
    }

    static void DrawBox(BoxCollider box, Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = box.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
