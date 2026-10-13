using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 發光軟糖（引力球）：由很多不同顏色的小塊黏成一團。
/// 從鏡頭用力射出 → 啪一聲黏在玻璃內側 → 吸引半徑內的小魚圍過來一口一口吃掉 → 吃完縮小消失。
/// 每塊都有自己的彈簧，飛行、撞擊、被咬、堆疊時會軟Q晃動。
/// 每團裡混一兩塊很亮、會閃的螢光小塊；魚咬到螢光塊時會變成那個顏色並帶螢光。
/// </summary>
public class Jelly : MonoBehaviour
{
    [Header("Bites")]
    [Tooltip("一顆軟糖可以被咬幾口；越多吃越久")]
    [SerializeField] int bites = 30;
    [Tooltip("剩一口時的大小比例")]
    [SerializeField] float minScale = 0.35f;

    [Header("Cluster")]
    [Tooltip("黏成一團的小塊。之後換成 3D 物品 Prefab；留空時把這個物件本身的模型當成一整塊")]
    [SerializeField] GameObject[] pieceVariants;
    [SerializeField] int pieceCount = 7;
    [SerializeField] int maxPieces = 20;
    [Tooltip("堆疊一次多黏幾塊")]
    [SerializeField] int piecesPerStack = 3;
    [SerializeField] float clusterRadius = 0.22f;
    [SerializeField] Vector2 pieceScaleRange = new Vector2(0.28f, 0.42f);
    [Tooltip("每塊在 scale = 1 時的大約半徑（Unity Sphere 是 0.5），換 3D 物品後依模型調整")]
    [SerializeField] float pieceRadius = 0.5f;

    [Header("Colors")]
    [Tooltip("是否把顏色套到小塊上（換成有貼圖的 3D 物品時可關掉）")]
    [SerializeField] bool tintPieces = true;
    [Tooltip("普通軟糖每塊從這裡輪流挑顏色")]
    [SerializeField] Color[] pieceColors =
    {
        new Color(1f, 0.25f, 0.3f),
        new Color(1f, 0.6f, 0.15f),
        new Color(1f, 0.9f, 0.25f),
        new Color(0.35f, 0.9f, 0.4f),
        new Color(0.3f, 0.65f, 1f),
        new Color(0.75f, 0.4f, 1f),
    };
    [SerializeField] float hueJitter = 0.03f;

    [Header("Fluorescent")]
    [Tooltip("每團混幾塊螢光小塊（最小、最大）")]
    [SerializeField] Vector2Int fluorescentCount = new Vector2Int(1, 2);
    [Tooltip("堆疊時新黏上的小塊裡有幾塊螢光")]
    [SerializeField] int fluorescentPerStack = 1;
    [SerializeField] Color[] fluorescentColors =
    {
        new Color(1f, 0.1f, 0.65f),
        new Color(0.55f, 1f, 0f),
        new Color(0f, 1f, 0.95f),
        new Color(1f, 0.45f, 0f),
    };
    [Tooltip("螢光小塊的自發光強度（普通小塊用 Emission Intensity）")]
    [SerializeField] float fluorescentEmission = 6f;
    [Tooltip("螢光小塊比普通小塊大多少")]
    [SerializeField] float fluorescentScale = 1.15f;
    [Tooltip("螢光閃爍幅度與頻率")]
    [SerializeField] float fluorescentBlink = 0.5f;
    [SerializeField] float fluorescentBlinkFrequency = 6f;

    [Header("Launch")]
    [Tooltip("飛行時沿飛行方向拉長多少（再乘上力道）")]
    [SerializeField] float flightStretch = 0.35f;
    [Tooltip("撞上玻璃壓扁的力道（再乘上力道）")]
    [SerializeField] float impactSquash = 7f;
    [Tooltip("撞擊時每塊各自亂晃的力道（再乘上力道）")]
    [SerializeField] float impactScatter = 3f;

    [Header("Attraction")]
    [SerializeField] float attractRadius = 4f;
    [SerializeField] int maxEaters = 6;
    [SerializeField] float scanInterval = 0.25f;

    [Header("Motion")]
    [SerializeField] float popInTime = 0.35f;
    [SerializeField] float popStagger = 0.04f;
    [SerializeField] float vanishTime = 0.3f;
    [Tooltip("沒黏在玻璃上時（直接 Init）的漂浮幅度")]
    [SerializeField] float floatAmplitude = 0.08f;
    [SerializeField] float floatFrequency = 1.2f;
    [SerializeField] float scaleSmoothing = 8f;

    [Header("Jiggle")]
    [SerializeField] float jiggleStiffness = 220f;
    [SerializeField] float jiggleDamping = 9f;
    [Tooltip("被咬一口時，靠近嘴的小塊被壓扁的力道")]
    [SerializeField] float biteJiggle = 4f;
    [Tooltip("整團被咬時的晃動力道")]
    [SerializeField] float clusterBiteJiggle = 1.5f;
    [SerializeField] float stackJiggle = 3f;
    [Tooltip("閒置時每塊輕微呼吸的幅度")]
    [SerializeField] float idleWobble = 0.04f;
    [SerializeField] float idleWobbleFrequency = 3f;

    [Header("Glow")]
    [Tooltip("點光源顏色（普通軟糖）")]
    [SerializeField] Color glowColor = new Color(1f, 0.8f, 0.5f, 1f);
    [Tooltip("普通小塊的自發光強度；壓低一點，螢光小塊才跳得出來")]
    [SerializeField] float emissionIntensity = 0.35f;
    [SerializeField] float glowPulse = 0.3f;
    [SerializeField] float glowPulseFrequency = 2.5f;

    class Piece
    {
        public Transform transform;
        public Vector3 baseScale;
        public float radius;
        public float spawnTime;
        public float wobblePhase;
        public bool fluorescent;
        public Color color;
        public SoftSpring spring;
    }

    class Tinted
    {
        public Material material;
        public Color color;
        public float emission;
        public float blink;
        public float blinkPhase;
    }

    static readonly List<Jelly> active = new List<Jelly>();
    public static IReadOnlyList<Jelly> Active => active;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public bool IsEmpty => remaining <= 0;
    /// <summary>已經落地（黏好）可以被吃。</summary>
    public bool IsLanded => !flying;
    public int DefaultBites => bites;
    /// <summary>黏住的玻璃面往缸外的法線；沒黏在玻璃上時為 zero。魚會避開這一側。</summary>
    public Vector3 SurfaceNormal { get; private set; }
    /// <summary>目前整團的半徑（世界座標），給魚決定圍食站位。</summary>
    public float Radius => transform.localScale.x * extent;

    readonly List<FishController> eaters = new List<FishController>();
    readonly List<Piece> pieces = new List<Piece>();
    readonly List<Tinted> tinted = new List<Tinted>();
    FishPool pool;
    Light glowLight;
    float baseLightIntensity;
    Vector3 baseScale;
    Vector3 anchor;
    float extent;
    int capacity;
    int remaining;
    float age;
    float vanishTimer;
    float scanTimer;
    float currentScale;
    float phase;
    int colorCursor;
    SoftSpring clusterSpring;

    bool flying;
    Vector3 flightFrom;
    float flightDuration;
    float flightTime;
    float flightArc;
    float force = 1f;
    float stackRadius;

    void Awake()
    {
        baseScale = transform.localScale;
        glowLight = GetComponentInChildren<Light>();
        if (glowLight != null) baseLightIntensity = glowLight.intensity;
    }

    /// <summary>由程式指定小塊模板（測試場景用；一般直接在 Inspector 設定）。</summary>
    public void SetPieceVariants(params GameObject[] variants) => pieceVariants = variants;

    void OnEnable()  => active.Add(this);
    void OnDisable() => active.Remove(this);

    /// <summary>直接放在 position，原地漂浮。</summary>
    public void Init(FishPool owner, Vector3 position)
    {
        pool = owner;
        anchor = position;
        transform.position = position;
        capacity = remaining = Mathf.Max(1, bites);
        SurfaceNormal = Vector3.zero;
        age = 0f;
        vanishTimer = 0f;
        scanTimer = 0f;
        currentScale = 1f;
        phase = Random.value * Mathf.PI * 2f;
        colorCursor = Random.Range(0, Mathf.Max(1, pieceColors.Length));
        clusterSpring.Reset();
        if (glowLight != null) glowLight.color = glowColor;

        if (pieceVariants != null && pieceVariants.Length > 0)
        {
            int count = Mathf.Max(1, pieceCount);
            int glowing = Mathf.Min(count - 1, Random.Range(fluorescentCount.x, fluorescentCount.y + 1));
            for (int i = 0; i < count; i++)
            {
                // 螢光塊排在最後、放在外圈，不會被中間那塊擋住
                bool fluorescent = i >= count - glowing;
                Vector3 pos = i == 0 ? Vector3.zero
                    : fluorescent ? FluorescentSlot(clusterRadius)
                    : Random.onUnitSphere * (clusterRadius * Random.Range(0.7f, 1f));
                AddPiece(pos, i * popStagger, fluorescent);
            }
        }
        else
        {
            // 沒有小塊就把自己的模型當成一整塊
            extent = pieceRadius;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                AddTint(r, NextPieceColor(), emissionIntensity, 0f);
        }

        UpdatePieces(0f);
        transform.localScale = baseScale;
    }

    /// <summary>
    /// 從 from 射向 target，撞上後黏住。surfaceNormal 是玻璃往缸外的法線；
    /// launchForce 越大飛越快、壓扁越用力；落點 stackRadius 內已有軟糖時會黏到那顆上。
    /// </summary>
    public void Launch(FishPool owner, Vector3 from, Vector3 target, Vector3 surfaceNormal,
                       float speed, float launchForce, float arc, float stackRange)
    {
        Init(owner, target);

        force = Mathf.Max(0.05f, launchForce);
        stackRadius = stackRange;
        flightFrom = from;
        flightArc = arc;
        flightDuration = Vector3.Distance(from, target) / Mathf.Max(0.01f, speed * force);
        flightTime = 0f;
        flying = true;
        SurfaceNormal = surfaceNormal.normalized;
        transform.position = from;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, target - from);
    }

    /// <summary>堆疊：同一處再發射一次，在表面黏上新的小塊並增加口數。</summary>
    public void AddBites(int amount, float kick = 1f)
    {
        if (IsEmpty) return;
        remaining += amount;
        capacity = Mathf.Max(capacity, remaining);

        int add = Mathf.Min(piecesPerStack, maxPieces - pieces.Count);
        for (int i = 0; i < add; i++)
        {
            bool fluorescent = i < fluorescentPerStack;
            Vector3 pos = fluorescent ? FluorescentSlot(extent * 0.8f) : Random.onUnitSphere * (extent * 0.75f);
            AddPiece(pos, i * popStagger, fluorescent);
        }

        clusterSpring.Kick(-stackJiggle * kick);
        foreach (Piece p in pieces) p.spring.Kick(Random.Range(-1f, 1f) * stackJiggle * kick);
    }

    /// <summary>魚咬一口。成功時回傳 true；咬到的是螢光小塊時會把顏色染給魚。</summary>
    public bool TryBite(FishController fish)
    {
        if (IsEmpty || flying) return false;

        remaining--;
        Piece bitten = NearestPiece(fish.MouthPosition);
        if (bitten != null && bitten.fluorescent) fish.AbsorbPigment(bitten.color);
        Jiggle(fish.transform.position);
        if (IsEmpty) eaters.Clear();
        return true;
    }

    /// <summary>魚離開（被別的事打斷）時呼叫，讓出圍食位置。</summary>
    public void RemoveEater(FishController fish) => eaters.Remove(fish);

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        if (flying)
        {
            Fly(dt);
            return;
        }

        clusterSpring.Step(0f, jiggleStiffness * 0.6f, jiggleDamping * 0.8f, dt);
        UpdatePieces(dt);

        if (IsEmpty)
        {
            vanishTimer += dt;
            float k = 1f - Mathf.Clamp01(vanishTimer / vanishTime);
            transform.localScale = baseScale * (currentScale * k);
            UpdateGlow(k);
            if (vanishTimer >= vanishTime) Destroy(gameObject);
            return;
        }

        // 大小：依剩餘口數平滑縮小，整團再疊一層彈簧晃動
        currentScale = Mathf.Lerp(currentScale, TargetScale(), 1f - Mathf.Exp(-scaleSmoothing * dt));
        transform.localScale = Vector3.Scale(baseScale * currentScale, SoftSpring.Stretch(clusterSpring.Value, 1));

        // 黏在玻璃上就不漂；直接放置的才輕輕漂浮
        float drift = SurfaceNormal == Vector3.zero ? Mathf.Sin(age * floatFrequency + phase) * floatAmplitude : 0f;
        transform.position = anchor + Vector3.up * drift;

        UpdateGlow(Mathf.Clamp01((float)remaining / capacity));

        scanTimer -= dt;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            AttractFish();
        }
    }

    void Fly(float dt)
    {
        flightTime += dt;
        float t = Mathf.Clamp01(flightTime / flightDuration);

        // 直線加一點拋物線弧度，沿飛行方向拉長
        Vector3 prev = transform.position;
        Vector3 pos = Vector3.Lerp(flightFrom, anchor, t) + Vector3.up * (flightArc * 4f * t * (1f - t));
        transform.position = pos;
        if ((pos - prev).sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.FromToRotation(Vector3.up, pos - prev);

        clusterSpring.Step(flightStretch * force, jiggleStiffness * 0.6f, jiggleDamping * 0.8f, dt);
        UpdatePieces(dt);
        transform.localScale = Vector3.Scale(baseScale, SoftSpring.Stretch(clusterSpring.Value, 1));
        UpdateGlow(1f);

        if (t >= 1f) Land();
    }

    void Land()
    {
        flying = false;
        transform.position = anchor;

        // 落點附近已有軟糖：黏到那顆上，自己消失
        Jelly other = FindLandedNear(anchor, stackRadius);
        if (other != null)
        {
            other.AddBites(bites / 2, force);
            Destroy(gameObject);
            return;
        }

        // 啪：沿玻璃法線壓扁，每塊各自亂晃
        transform.rotation = Quaternion.FromToRotation(Vector3.up, SurfaceNormal);
        clusterSpring.Value = 0f;
        clusterSpring.Velocity = 0f;
        clusterSpring.Kick(-impactSquash * force);
        foreach (Piece p in pieces) p.spring.Kick(Random.Range(-1f, 0.5f) * impactScatter * force);
    }

    Jelly FindLandedNear(Vector3 position, float radius)
    {
        Jelly nearest = null;
        float best = radius * radius;
        foreach (Jelly jelly in active)
        {
            if (jelly == this || jelly.IsEmpty || !jelly.IsLanded) continue;
            float d = (jelly.transform.position - position).sqrMagnitude;
            if (d <= best) { best = d; nearest = jelly; }
        }
        return nearest;
    }

    void AddPiece(Vector3 localPosition, float delay, bool fluorescent)
    {
        GameObject variant = pieceVariants[Random.Range(0, pieceVariants.Length)];
        GameObject go = Instantiate(variant, transform);
        go.SetActive(true);

        float scale = Random.Range(pieceScaleRange.x, pieceScaleRange.y);
        if (fluorescent) scale = pieceScaleRange.y * fluorescentScale;
        Color color = fluorescent ? fluorescentColors[Random.Range(0, fluorescentColors.Length)] : NextPieceColor();
        var piece = new Piece
        {
            transform = go.transform,
            baseScale = variant.transform.localScale * scale,
            radius = pieceRadius * scale,
            spawnTime = age + delay,
            wobblePhase = Random.value * Mathf.PI * 2f,
            fluorescent = fluorescent,
            color = color,
        };
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = Random.rotation;
        piece.transform.localScale = Vector3.zero;
        pieces.Add(piece);

        extent = Mathf.Max(extent, localPosition.magnitude + piece.radius);

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (fluorescent) AddTint(r, color, fluorescentEmission, fluorescentBlink);
            else AddTint(r, color, emissionIntensity, 0f);
        }
    }

    /// <summary>
    /// 螢光塊放在外圈、偏向本地 -y。落地後本地 +y 朝玻璃外，所以螢光塊會在缸內那一側，魚咬得到。
    /// </summary>
    static Vector3 FluorescentSlot(float radius)
    {
        Vector3 dir = Random.onUnitSphere;
        dir.y = -Mathf.Abs(dir.y) - 0.3f;
        return dir.normalized * radius;
    }

    Piece NearestPiece(Vector3 worldPosition)
    {
        Piece nearest = null;
        float best = float.MaxValue;
        foreach (Piece p in pieces)
        {
            float d = (p.transform.position - worldPosition).sqrMagnitude;
            if (d < best) { best = d; nearest = p; }
        }
        return nearest;
    }

    /// <summary>普通小塊從調色盤輪流挑（相鄰不重複）。</summary>
    Color NextPieceColor()
    {
        if (pieceColors.Length == 0) return glowColor;

        Color c = pieceColors[colorCursor % pieceColors.Length];
        colorCursor += Random.Range(1, Mathf.Max(2, pieceColors.Length - 1));

        Color.RGBToHSV(c, out float h, out float s, out float v);
        return Color.HSVToRGB(Mathf.Repeat(h + Random.Range(-hueJitter, hueJitter), 1f), s, v);
    }

    void AddTint(Renderer r, Color color, float emission, float blink)
    {
        Material m = r.material;
        m.EnableKeyword("_EMISSION");
        if (tintPieces) m.color = new Color(color.r, color.g, color.b, m.color.a);
        tinted.Add(new Tinted
        {
            material = m,
            color = color,
            emission = emission,
            blink = blink,
            blinkPhase = Random.value * Mathf.PI * 2f,
        });
    }

    void UpdatePieces(float dt)
    {
        foreach (Piece p in pieces)
        {
            float t = (age - p.spawnTime) / popInTime;
            float pop = t <= 0f ? 0f : t < 1f ? EaseOutBack(t) : 1f;

            float wobble = Mathf.Sin(age * idleWobbleFrequency + p.wobblePhase) * idleWobble;
            p.spring.Step(wobble, jiggleStiffness, jiggleDamping, dt);
            p.transform.localScale = Vector3.Scale(p.baseScale * pop, SoftSpring.Stretch(p.spring.Value, 1));
        }
    }

    /// <summary>被咬一口：越靠近嘴的小塊被壓得越扁，整團也跟著抖一下。</summary>
    void Jiggle(Vector3 mouthWorld)
    {
        clusterSpring.Kick(-clusterBiteJiggle);

        float reach = Mathf.Max(Radius * 2f, 0.01f);
        foreach (Piece p in pieces)
        {
            float d = Vector3.Distance(p.transform.position, mouthWorld);
            float falloff = 1f - Mathf.Clamp01(d / reach);
            p.spring.Kick(-biteJiggle * falloff * falloff);
        }
    }

    void AttractFish()
    {
        eaters.RemoveAll(f => f == null || !f.isActiveAndEnabled || f.TargetJelly != this);
        if (pool == null || eaters.Count >= maxEaters) return;

        // 由近到遠補位
        float radiusSqr = attractRadius * attractRadius;
        while (eaters.Count < maxEaters)
        {
            FishController nearest = null;
            float best = radiusSqr;
            foreach (FishController fish in pool.ActiveFish)
            {
                if (!fish.IsFree) continue;
                float d = (fish.transform.position - transform.position).sqrMagnitude;
                if (d <= best) { best = d; nearest = fish; }
            }
            if (nearest == null) break;

            nearest.SeekJelly(this);
            eaters.Add(nearest);
        }
    }

    void UpdateGlow(float fullness)
    {
        float pulse = 1f + Mathf.Sin(age * glowPulseFrequency + phase) * glowPulse;
        float strength = Mathf.Lerp(0.4f, 1f, fullness) * pulse;

        foreach (Tinted t in tinted)
        {
            float blink = 1f + Mathf.Sin(age * fluorescentBlinkFrequency + t.blinkPhase) * t.blink;
            t.material.SetColor(EmissionColorId, t.color * (t.emission * strength * blink));
        }
        if (glowLight != null)
            glowLight.intensity = baseLightIntensity * strength;
    }

    float TargetScale()
    {
        // 體積與剩餘比例成正比 → 半徑取立方根；堆疊變大靠新黏上的小塊
        float s = Mathf.Pow((float)remaining / capacity, 1f / 3f);
        return Mathf.Clamp(s, minScale, 1f);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
