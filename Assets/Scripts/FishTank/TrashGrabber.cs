using System;
using UnityEngine;

/// <summary>
/// 夾垃圾：左鍵按住游標下的垃圾就夾住，拖曳移動，放開鬆手（點在垃圾以外的地方是發射軟糖）。
/// 水裡：大型垃圾拉出水面就帶出魚缸，小型垃圾會掙扎、滑掉；沙灘上大小都正常夾。水在哪由 WaterVolume 決定。
/// Joy-Con 直接當滑鼠用（游標 + 左鍵）；震動用 Rumble 事件送出，沒接手把時用鏡頭與夾子抖動模擬。
/// </summary>
public class TrashGrabber : MonoBehaviour
{
    /// <summary>震動：強度 0–1、持續秒數。</summary>
    public static event Action<float, float> Rumble;

    [Tooltip("夾取判定的半徑；放大讓小垃圾比較好夾到")]
    [SerializeField] float grabRadius = 0.25f;
    [SerializeField] float maxRayDistance = 100f;
    [Tooltip("0 左鍵、1 右鍵、2 中鍵；和發射軟糖共用左鍵")]
    [SerializeField] int mouseButton = 0;

    [Header("Rumble")]
    [Tooltip("夾住大／小垃圾那一下的震動；0 = 不震（預設）")]
    [SerializeField] float grabLargeRumble = 0f;
    [SerializeField] float grabSmallRumble = 0f;
    [Tooltip("夾著小型垃圾時持續的震動（乘上吃力程度）")]
    [SerializeField] float struggleRumble = 0.35f;
    [SerializeField] float slipRumble = 1f;
    [SerializeField] float removeRumble = 0.6f;

    [Header("Simulated Rumble")]
    [Tooltip("沒有手把時，用鏡頭與夾子抖動代替震動")]
    [SerializeField] bool simulateRumble = true;
    [SerializeField] float cameraShake = 0.06f;
    [SerializeField] float clawShake = 0.08f;

    [Header("Claw")]
    [SerializeField] float clawSize = 0.18f;
    [SerializeField] Color clawIdleColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] Color clawStrainColor = new Color(1f, 0.25f, 0.2f, 0.9f);

    TrashItem held;
    Plane dragPlane;
    Vector3 grabOffset;
    Transform claw;
    Material clawMaterial;

    float shakeStrength;
    float shakeUntil;
    Transform shakenCamera;
    Vector3 cameraOffset;

    public TrashItem Held => held;

    void Awake() => BuildClaw();

    void OnEnable() => TrashItem.Removed += OnRemoved;
    void OnDisable() => TrashItem.Removed -= OnRemoved;

    void OnRemoved(TrashItem item)
    {
        if (item != held) return;
        SendRumble(removeRumble, 0.15f);
        held = null;
    }

    // ---- 輸入（Joy-Con 以滑鼠模式操作時不用改）----
    protected virtual bool GripDown() => FishTankInput.GetMouseButtonDown(mouseButton);
    protected virtual bool GripHeld() => FishTankInput.GetMouseButton(mouseButton);
    protected virtual Ray PointerRay(Camera cam) => cam.ScreenPointToRay(FishTankInput.PointerPosition);

    void Update()
    {
        Camera cam = ViewCamera.Current;
        if (cam == null) return;
        Ray ray = PointerRay(cam);

        if (held == null && GripDown())
            TryGrab(ray, cam);

        if (held != null)
            UpdateHeld(ray);

        UpdateClaw();
    }

    /// <summary>
    /// 游標下最近的垃圾（射線會先打到玻璃，所以收集所有命中）。
    /// InteractionManager 也用它判斷這次點擊是夾垃圾還是發射軟糖。
    /// </summary>
    public TrashItem FindTrash(Ray ray, out Vector3 point, bool grabbableOnly = true)
    {
        TrashItem best = null;
        float bestDistance = float.MaxValue;
        point = Vector3.zero;
        foreach (RaycastHit hit in Physics.SphereCastAll(ray, grabRadius, maxRayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            var item = hit.collider.GetComponentInParent<TrashItem>();
            if (item == null || item.State == TrashItem.HoldState.Removed) continue;
            if ((grabbableOnly && !item.CanGrab) || hit.distance >= bestDistance) continue;
            best = item;
            bestDistance = hit.distance;
            point = hit.distance > 0f ? hit.point : item.transform.position;
        }
        return best;
    }

    void TryGrab(Ray ray, Camera cam)
    {
        TrashItem best = FindTrash(ray, out Vector3 bestPoint);
        if (best == null) return;

        dragPlane = new Plane(-cam.transform.forward, bestPoint);
        grabOffset = best.transform.position - bestPoint;
        if (!best.TryGrab(best.transform.position)) return;

        held = best;
        bool small = held.Size == TrashItem.TrashSize.Small;
        SendRumble(small ? grabSmallRumble : grabLargeRumble, 0.08f);
    }

    void UpdateHeld(Ray ray)
    {
        if (held.State == TrashItem.HoldState.Slipped)
        {
            SendRumble(slipRumble, 0.25f);
            held = null;
            return;
        }
        // 被帶出、或被 ScaleBridge 搬到另一個世界
        if (held.State != TrashItem.HoldState.Held)
        {
            held = null;
            return;
        }

        if (!GripHeld())
        {
            held.Release();
            held = null;
            return;
        }

        if (dragPlane.Raycast(ray, out float enter))
            held.MoveTo(ray.GetPoint(enter) + grabOffset);

        if (held.Size == TrashItem.TrashSize.Small)
            SendRumble(struggleRumble * Mathf.Lerp(0.4f, 1f, held.Strain), Time.deltaTime * 2f);
    }

    void SendRumble(float strength, float duration)
    {
        if (strength <= 0f) return;
        Rumble?.Invoke(strength, duration);
        if (Time.time >= shakeUntil || strength >= shakeStrength) shakeStrength = strength;
        shakeUntil = Mathf.Max(shakeUntil, Time.time + duration);
    }

    void LateUpdate()
    {
        // 先從上一幀晃的那台相機拿掉偏移，再加這一幀的，不會干擾其他移動或切換鏡頭的程式
        if (shakenCamera != null) shakenCamera.position -= cameraOffset;
        cameraOffset = Vector3.zero;
        shakenCamera = null;

        Camera cam = ViewCamera.Current;
        if (cam == null || !simulateRumble || Time.time >= shakeUntil) return;
        shakenCamera = cam.transform;
        cameraOffset = UnityEngine.Random.insideUnitSphere * (cameraShake * shakeStrength);
        shakenCamera.position += cameraOffset;
    }

    void BuildClaw()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Claw";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * clawSize;
        clawMaterial = go.GetComponent<Renderer>().material;
        clawMaterial.EnableKeyword("_EMISSION");
        claw = go.transform;
        claw.gameObject.SetActive(false);
    }

    void UpdateClaw()
    {
        claw.gameObject.SetActive(held != null);
        if (held == null) return;

        Vector3 shake = simulateRumble && Time.time < shakeUntil
            ? UnityEngine.Random.insideUnitSphere * (clawShake * shakeStrength)
            : Vector3.zero;
        claw.position = held.transform.position - grabOffset + shake;

        Color color = Color.Lerp(clawIdleColor, clawStrainColor, held.Strain);
        clawMaterial.color = color;
        clawMaterial.SetColor("_EmissionColor", color * 0.6f);
    }
}
