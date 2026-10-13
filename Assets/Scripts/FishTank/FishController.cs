using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 單隻小魚的行為：自由遊動 → 被軟糖吸引游過去 → 圍著軟糖一口一口吃 → 吃完回到自由遊動。
/// 咬到螢光小塊時身體會逐漸變色並帶螢光。模型節點用彈簧做軟Q的拉長、壓扁、擺尾與轉彎傾斜。
/// </summary>
public class FishController : MonoBehaviour
{
    public enum FishState { FreeSwimming, SeekingJelly, Eating }

    [Header("Free Swimming")]
    [SerializeField] float swimSpeed = 1.5f;
    [SerializeField] float waypointTimeout = 6f;
    [SerializeField] float bobAmplitude = 0.1f;
    [SerializeField] float bobFrequency = 2f;

    [Header("Jelly")]
    [SerializeField] float seekSpeed = 3f;
    [SerializeField] float arriveDistance = 0.15f;
    [Tooltip("魚身中心到嘴的距離，用來決定圍食時離軟糖表面多遠")]
    [SerializeField] float mouthOffset = 0.3f;
    [Tooltip("每口間隔秒數；越大吃越慢")]
    [SerializeField] float biteInterval = 1f;
    [SerializeField] float biteJitter = 0.25f;
    [SerializeField] float peckAmplitude = 0.08f;

    [Header("Fluorescent")]
    [Tooltip("每咬一口螢光小塊，身體往螢光色靠近的比例")]
    [SerializeField] float pigmentPerBite = 0.25f;
    [Tooltip("每咬一口螢光小塊，身體自發光增加多少")]
    [SerializeField] float glowPerBite = 0.5f;
    [SerializeField] float maxGlow = 2.5f;
    [Tooltip("螢光呼吸幅度（0 = 不閃）")]
    [SerializeField] float glowPulse = 0.2f;
    [SerializeField] float glowPulseFrequency = 3f;
    [SerializeField] float colorChangeSpeed = 2f;

    [Header("Turning")]
    [SerializeField] float turnSpeed = 5f;

    [Header("Soft Body")]
    [Tooltip("做擠壓、擺動的模型節點；留空時會自動把所有子物件包進一個 Visual 節點")]
    [SerializeField] Transform visual;
    [Tooltip("每單位速度沿身體拉長多少")]
    [SerializeField] float stretchPerSpeed = 0.05f;
    [SerializeField] float stretchStiffness = 150f;
    [SerializeField] float stretchDamping = 6f;
    [SerializeField] float wiggleAngle = 12f;
    [SerializeField] float wiggleFrequency = 8f;
    [Tooltip("轉彎時每 1°/s 的轉速傾斜幾度")]
    [SerializeField] float bankPerTurn = 0.15f;
    [SerializeField] float maxBank = 35f;
    [SerializeField] float bankStiffness = 90f;
    [SerializeField] float bankDamping = 7f;
    [Tooltip("每咬一口身體壓扁的力道")]
    [SerializeField] float biteSquash = 2.5f;
    [Tooltip("抵達軟糖時煞車壓扁的力道")]
    [SerializeField] float arriveSquash = 2f;

    public FishState State { get; private set; } = FishState.FreeSwimming;
    public bool IsFree => State == FishState.FreeSwimming;
    public Jelly TargetJelly => jelly;
    public Vector3 MouthPosition => transform.position + transform.forward * mouthOffset;

    FishPool pool;
    Bounds swimBounds;
    Vector3 waypoint;
    float waypointTimer;
    float bobPhase;

    Jelly jelly;
    Vector3 slotDirection;
    float biteTimer;
    float peckTimer;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    Material[] materials;
    Color baseColor;
    Color targetColor;
    Color currentColor;
    Color glowColor;
    float targetGlow;
    float currentGlow;

    Vector3 visualBaseScale;
    Quaternion visualBaseRotation;
    Vector3 lastPosition;
    Vector3 lastForward;
    float wigglePhase;
    SoftSpring stretch;
    SoftSpring bank;

    /// <summary>由物件池在每次生成時呼叫，把狀態重設回自由遊動。</summary>
    public void ResetState(FishPool owner, Bounds bounds)
    {
        pool = owner;
        swimBounds = bounds;
        bobPhase = Random.value * Mathf.PI * 2f;

        // 第一次生成時記下原色（FishPool 隨機上色在此之前完成）
        if (materials == null)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            materials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                materials[i] = renderers[i].material;
                materials[i].EnableKeyword("_EMISSION");
            }
            baseColor = materials.Length > 0 ? materials[0].color : Color.white;
        }
        currentColor = targetColor = baseColor;
        currentGlow = targetGlow = 0f;
        glowColor = Color.black;
        ApplyColor();

        EnsureVisual();
        stretch.Reset();
        bank.Reset();
        wigglePhase = Random.value * Mathf.PI * 2f;
        lastPosition = transform.position;
        lastForward = transform.forward;

        StopEating();
    }

    /// <summary>由軟糖呼叫：游到軟糖旁邊的一個隨機站位開始吃。</summary>
    public void SeekJelly(Jelly target)
    {
        jelly = target;

        // 站位偏水平分布，看起來像一圈魚圍著軟糖
        Vector3 dir = Random.onUnitSphere;
        dir.y *= 0.4f;

        // 軟糖黏在玻璃上時，站位改到缸內那一側，避免魚穿出玻璃
        Vector3 n = target.SurfaceNormal;
        float outward = Vector3.Dot(dir, n);
        if (n != Vector3.zero && outward > -0.3f)
            dir -= n * (outward + 0.8f);

        slotDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : -n;
        if (slotDirection == Vector3.zero) slotDirection = Vector3.forward;

        State = FishState.SeekingJelly;
    }

    /// <summary>每咬到一口螢光小塊呼叫一次：身體顏色往螢光色靠近，自發光變強。</summary>
    public void AbsorbPigment(Color pigment)
    {
        targetColor = Color.Lerp(targetColor, pigment, pigmentPerBite);
        glowColor = targetGlow <= 0f ? pigment : Color.Lerp(glowColor, pigment, 0.5f);
        targetGlow = Mathf.Min(maxGlow, targetGlow + glowPerBite);
    }

    void Update()
    {
        switch (State)
        {
            case FishState.FreeSwimming: FreeSwimBehavior(); break;
            case FishState.SeekingJelly: SeekJellyBehavior(); break;
            case FishState.Eating:       EatBehavior();       break;
        }

        UpdateSoftBody();
        UpdateColor();
    }

    void FreeSwimBehavior()
    {
        transform.position = Vector3.MoveTowards(transform.position, waypoint, swimSpeed * Time.deltaTime);

        // 輕微上下擺動，讓遊動比較生動（只動 y，並夾在可游範圍內）
        float bob = Mathf.Cos(Time.time * bobFrequency + bobPhase) * bobAmplitude * bobFrequency * Time.deltaTime;
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y + bob, swimBounds.min.y, swimBounds.max.y);
        transform.position = pos;

        FaceTowards(waypoint - transform.position);

        waypointTimer -= Time.deltaTime;
        if (waypointTimer <= 0f || (waypoint - transform.position).sqrMagnitude < 0.04f)
            PickNewWaypoint();
    }

    void SeekJellyBehavior()
    {
        if (!JellyAvailable()) { StopEating(); return; }

        Vector3 slot = SlotPosition();
        transform.position = Vector3.MoveTowards(transform.position, slot, seekSpeed * Time.deltaTime);
        FaceTowards(jelly.transform.position - transform.position);

        if ((slot - transform.position).sqrMagnitude < arriveDistance * arriveDistance)
        {
            State = FishState.Eating;
            biteTimer = NextBiteDelay();
            peckTimer = 0f;
            stretch.Kick(-arriveSquash);
        }
    }

    void EatBehavior()
    {
        if (!JellyAvailable()) { StopEating(); return; }

        // 跟著軟糖（漂浮、縮小）調整站位，並往軟糖方向啄
        peckTimer += Time.deltaTime;
        float peck = Mathf.Max(0f, Mathf.Sin(peckTimer / biteInterval * Mathf.PI * 2f)) * peckAmplitude;
        Vector3 slot = SlotPosition() - slotDirection * peck;
        transform.position = Vector3.Lerp(transform.position, slot, 1f - Mathf.Exp(-10f * Time.deltaTime));
        FaceTowards(jelly.transform.position - transform.position);

        biteTimer -= Time.deltaTime;
        if (biteTimer <= 0f)
        {
            biteTimer = NextBiteDelay();
            if (jelly.TryBite(this)) stretch.Kick(-biteSquash);
            else StopEating();
        }
    }

    void StopEating()
    {
        if (jelly != null) jelly.RemoveEater(this);
        jelly = null;
        State = FishState.FreeSwimming;
        PickNewWaypoint();
    }

    bool JellyAvailable() => jelly != null && !jelly.IsEmpty;

    Vector3 SlotPosition() =>
        jelly.transform.position + slotDirection * (jelly.Radius + mouthOffset);

    float NextBiteDelay() => biteInterval + Random.Range(-biteJitter, biteJitter);

    void UpdateSoftBody()
    {
        float dt = Time.deltaTime;
        if (visual == null || dt <= 0f) return;

        float speed = (transform.position - lastPosition).magnitude / dt;
        float yawRate = Vector3.SignedAngle(lastForward, transform.forward, Vector3.up) / dt;
        lastPosition = transform.position;
        lastForward = transform.forward;

        // 速度越快身體拉越長；彈簧欠阻尼，加減速時會晃一下
        stretch.Step(speed * stretchPerSpeed, stretchStiffness, stretchDamping, dt);
        bank.Step(Mathf.Clamp(-yawRate * bankPerTurn, -maxBank, maxBank), bankStiffness, bankDamping, dt);

        // 擺尾：游越快擺越快越大，停著吃東西時只剩輕微擺動
        float speedRatio = Mathf.Clamp01(speed / Mathf.Max(seekSpeed, 0.01f));
        wigglePhase += dt * wiggleFrequency * (0.3f + speedRatio);
        float wiggle = Mathf.Sin(wigglePhase) * wiggleAngle * (0.25f + 0.75f * speedRatio);

        visual.localRotation = visualBaseRotation * Quaternion.Euler(0f, wiggle, bank.Value);
        visual.localScale = Vector3.Scale(visualBaseScale, SoftSpring.Stretch(stretch.Value, 2));
    }

    void EnsureVisual()
    {
        if (visual == null)
        {
            // 把現有子物件包進 Visual，彈簧只動模型，不影響移動與朝向
            var children = new List<Transform>();
            foreach (Transform child in transform) children.Add(child);

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            foreach (Transform child in children) child.SetParent(visual, false);

            visualBaseScale = Vector3.one;
            visualBaseRotation = Quaternion.identity;
        }
        else if (visualBaseScale == Vector3.zero)
        {
            visualBaseScale = visual.localScale;
            visualBaseRotation = visual.localRotation;
        }

        visual.localScale = visualBaseScale;
        visual.localRotation = visualBaseRotation;
    }

    void UpdateColor()
    {
        if (materials == null) return;
        float k = 1f - Mathf.Exp(-colorChangeSpeed * Time.deltaTime);

        if (currentColor != targetColor)
        {
            currentColor = Color.Lerp(currentColor, targetColor, k);
            if (Mathf.Abs(currentColor.r - targetColor.r) + Mathf.Abs(currentColor.g - targetColor.g)
                + Mathf.Abs(currentColor.b - targetColor.b) < 0.005f)
                currentColor = targetColor;
        }
        currentGlow = Mathf.Lerp(currentGlow, targetGlow, k);

        if (targetGlow > 0f || currentColor != targetColor) ApplyColor();
    }

    void ApplyColor()
    {
        float pulse = 1f + Mathf.Sin(Time.time * glowPulseFrequency + bobPhase) * glowPulse;
        Color emission = glowColor * (currentGlow * pulse);
        foreach (Material m in materials)
        {
            m.color = currentColor;
            m.SetColor(EmissionColorId, emission);
        }
    }

    void FaceTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    void PickNewWaypoint()
    {
        waypoint = new Vector3(
            Random.Range(swimBounds.min.x, swimBounds.max.x),
            Random.Range(swimBounds.min.y, swimBounds.max.y),
            Random.Range(swimBounds.min.z, swimBounds.max.z));
        waypointTimer = waypointTimeout;
    }
}
