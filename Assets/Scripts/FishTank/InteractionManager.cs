using UnityEngine;

/// <summary>
/// 點擊總控：滑鼠點擊魚缸玻璃 → 從鏡頭用力射出一團軟糖，飛到玻璃內側黏住，軟糖自己吸引附近的魚來吃。
/// 每團裡混一兩塊螢光小塊（由 Jelly 決定）；射在既有軟糖附近會黏上去堆疊變大。
/// 和夾垃圾共用左鍵：游標下有垃圾時交給 TrashGrabber，不發射。
/// </summary>
public class InteractionManager : MonoBehaviour
{
    [SerializeField] Jelly jellyPrefab;
    [SerializeField] FishPool fishPool;
    [Tooltip("可留空，會自動尋找；場景沒有夾子時左鍵只發射軟糖")]
    [SerializeField] TrashGrabber trashGrabber;
    [SerializeField] LayerMask glassMask = ~0;
    [Tooltip("只有點到這個 Collider 才發射；留空時點到任何東西都發射。和沙灘同場景時一定要指定，不然點沙灘也會射進魚缸")]
    [SerializeField] Collider clickSurface;
    [SerializeField] float maxRayDistance = 100f;

    [Header("Launch")]
    [Tooltip("發射力道：越大飛越快、撞上玻璃壓得越扁晃得越兇")]
    [Range(0.2f, 3f)]
    [SerializeField] float launchForce = 1f;
    [Tooltip("力道 1 時的飛行速度（單位/秒）")]
    [SerializeField] float launchSpeed = 18f;
    [Tooltip("飛行弧度高度")]
    [SerializeField] float launchArc = 0.4f;
    [Tooltip("發射點相對鏡頭的位置（右、上、前）")]
    [SerializeField] Vector3 launchOffset = new Vector3(0f, -0.6f, 0.8f);

    [Header("Jelly")]
    [Tooltip("軟糖黏在玻璃內側多深的位置（會再夾進可游範圍內）")]
    [SerializeField] float insetDepth = 0.4f;
    [Tooltip("落點這個距離內已有軟糖時，改為黏到那顆上")]
    [SerializeField] float stackRadius = 0.8f;
    [SerializeField] int maxJellies = 5;

    Transform jellyContainer;

    public void Initialize(Jelly template, FishPool pool, Collider surface = null)
    {
        jellyPrefab = template;
        fishPool = pool;
        clickSurface = surface;
    }

    void Awake()
    {
        jellyContainer = new GameObject("Jellies").transform;
        jellyContainer.SetParent(transform, false);
    }

    void Start()
    {
        if (fishPool == null)
            fishPool = FindAnyObjectByType<FishPool>();
        if (trashGrabber == null)
            trashGrabber = FindAnyObjectByType<TrashGrabber>();
        if (fishPool == null || jellyPrefab == null)
            Debug.LogError("InteractionManager: 尚未指定 Jelly Prefab 或找不到 FishPool。", this);
    }

    void Update()
    {
        if (fishPool == null || jellyPrefab == null) return;

        if (!Input.GetMouseButtonDown(0)) return;

        Camera cam = ViewCamera.Current;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (trashGrabber != null && (trashGrabber.Held != null || trashGrabber.FindTrash(ray, out _, grabbableOnly: false) != null))
            return;
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, glassMask, QueryTriggerInteraction.Ignore))
            return;
        if (clickSurface != null && hit.collider != clickSurface)
            return;

        // hit.normal 朝向缸外，往反方向推進一點就是缸內
        Vector3 position = ClampToSwimArea(hit.point - hit.normal * insetDepth);
        FireJelly(cam.transform, position, hit.normal);
    }

    void FireJelly(Transform cam, Vector3 position, Vector3 surfaceNormal)
    {
        // 射向既有軟糖的會在落地時黏上去，不算新的一顆
        if (Jelly.Active.Count >= maxJellies && FindNearestJelly(position, stackRadius) == null) return;

        Vector3 from = cam.TransformPoint(launchOffset);
        Jelly jelly = Instantiate(jellyPrefab, from, Quaternion.identity, jellyContainer);
        jelly.gameObject.SetActive(true);
        jelly.Launch(fishPool, from, position, surfaceNormal, launchSpeed, launchForce, launchArc, stackRadius);
    }

    static Jelly FindNearestJelly(Vector3 position, float radius)
    {
        Jelly nearest = null;
        float best = radius * radius;
        foreach (Jelly jelly in Jelly.Active)
        {
            if (jelly.IsEmpty || !jelly.IsLanded) continue;
            float d = (jelly.transform.position - position).sqrMagnitude;
            if (d <= best) { best = d; nearest = jelly; }
        }
        return nearest;
    }

    Vector3 ClampToSwimArea(Vector3 position)
    {
        Bounds b = fishPool.SwimBounds;
        return new Vector3(
            Mathf.Clamp(position.x, b.min.x, b.max.x),
            Mathf.Clamp(position.y, b.min.y, b.max.y),
            Mathf.Clamp(position.z, b.min.z, b.max.z));
    }
}
