using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 掛在場景中的空物件上，Play 時用 Primitive 自動生成魚缸、小魚、軟糖模板、垃圾、控制器與 Bloom。
/// 勾 Build Beach 時，另外在旁邊生成一個簡易沙灘：上面擺一個縮小的魚缸外殼，用 ScaleBridge 和魚缸世界相連。
/// 相機從沙灘開始，由 TransferCameraMove 跟著物件進出魚缸運鏡，Tab 手動切換。
/// </summary>
public class TankBootstrap : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField] Vector3 tankCenter = Vector3.zero;
    [SerializeField] Vector3 tankSize = new Vector3(10f, 6f, 6f);
    [SerializeField] float glassThickness = 0.1f;
    [SerializeField] float swimMargin = 0.6f;
    [SerializeField] Color glassColor = new Color(0.6f, 0.85f, 1f, 0.15f);
    [SerializeField] Color floorColor = new Color(0.85f, 0.75f, 0.55f, 1f);

    [Header("Fish")]
    [SerializeField] int fishCount = 15;

    [Header("Jelly")]
    [Tooltip("小塊材質的底色與透明度（顏色會被 Jelly 依每塊覆寫）")]
    [SerializeField] Color jellyColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] float jellyLightRange = 3f;
    [SerializeField] float jellyLightIntensity = 2f;

    [Header("Trash")]
    [SerializeField] int largeTrashCount = 3;
    [SerializeField] int smallTrashCount = 12;
    [Tooltip("水面離魚缸頂端多遠")]
    [SerializeField] float waterSurfaceInset = 0.3f;

    [Header("Beach (scale bridge test)")]
    [SerializeField] bool buildBeach = true;
    [SerializeField] Vector3 beachCenter = new Vector3(40f, 0f, 0f);
    [Tooltip("魚缸世界是沙灘上小魚缸的幾倍大")]
    [SerializeField] float scaleFactor = 3f;
    [SerializeField] int beachTrashCount = 3;
    [SerializeField] Color sandColor = new Color(0.93f, 0.84f, 0.62f, 1f);

    [Header("Bloom")]
    [SerializeField] bool addBloom = true;
    [SerializeField] float bloomIntensity = 1.2f;
    [SerializeField] float bloomThreshold = 0.9f;

    [Header("Camera")]
    [SerializeField] float cameraDistance = 11f;
    [SerializeField] float cameraHeight = 1f;

    void Awake()
    {
        Collider frontGlass = BuildTank("Tank", tankCenter, tankSize, glassThickness);

        // 水：頂面比魚缸頂端低一點
        var water = new GameObject("Water").AddComponent<WaterVolume>();
        water.transform.SetParent(transform, false);
        water.transform.position = tankCenter + Vector3.down * (waterSurfaceInset * 0.5f);
        water.Initialize(new Vector3(tankSize.x, tankSize.y - waterSurfaceInset, tankSize.z));

        Bounds swimBounds = new Bounds(tankCenter, tankSize - Vector3.one * (glassThickness + swimMargin * 2f));

        var pool = new GameObject("FishPool").AddComponent<FishPool>();
        pool.transform.SetParent(transform, false);
        pool.Initialize(BuildFishTemplate(), swimBounds, fishCount, randomizeColors: true);

        var interaction = new GameObject("InteractionManager").AddComponent<InteractionManager>();
        interaction.transform.SetParent(transform, false);
        interaction.Initialize(BuildJellyTemplate(), pool, frontGlass);

        var grabber = new GameObject("TrashGrabber").AddComponent<TrashGrabber>();
        grabber.transform.SetParent(transform, false);
        TrashItem[] largeTemplates = BuildTrash(swimBounds);

        Camera tankCamera = SetupCamera();
        if (buildBeach) BuildBeach(largeTemplates, tankCamera);
        if (addBloom) SetupBloom();
    }

    /// <summary>玻璃盒（頂端開口）；牆保留 collider，供點擊射線命中、擋住垃圾。回傳正面（-Z）那片玻璃。</summary>
    Collider BuildTank(string name, Vector3 center, Vector3 size, float thickness)
    {
        var tank = new GameObject(name).transform;
        tank.SetParent(transform, false);
        Collider front = null;

        Material glass = CreateMaterial(glassColor, transparent: true);
        Material floor = CreateMaterial(floorColor, transparent: false);
        Vector3 half = size * 0.5f;

        for (int axis = 0; axis < 3; axis++)
        {
            foreach (float sign in new[] { -1f, 1f })
            {
                if (axis == 1 && sign > 0f) continue;

                Vector3 offset = Vector3.zero;
                offset[axis] = half[axis] * sign;

                Vector3 scale = size;
                scale[axis] = thickness;

                bool isFloor = axis == 1 && sign < 0f;
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = isFloor ? "Floor" : $"Glass_{"XYZ"[axis]}{(sign > 0 ? "+" : "-")}";
                wall.transform.SetParent(tank, false);
                wall.transform.position = center + offset;
                wall.transform.localScale = scale;

                var renderer = wall.GetComponent<Renderer>();
                renderer.sharedMaterial = isFloor ? floor : glass;
                renderer.shadowCastingMode = isFloor ? ShadowCastingMode.On : ShadowCastingMode.Off;
                if (axis == 2 && sign < 0f) front = wall.GetComponent<Collider>();
            }
        }
        return front;
    }

    void BuildBeach(TrashItem[] largeTemplates, Camera tankCamera)
    {
        var beach = new GameObject("Beach").transform;
        beach.SetParent(transform, false);
        beach.position = beachCenter;

        var sand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sand.name = "Sand";
        sand.transform.SetParent(beach, false);
        sand.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        sand.transform.localScale = new Vector3(30f, 0.2f, 20f);
        sand.GetComponent<Renderer>().sharedMaterial = CreateMaterial(sandColor, transparent: false);

        // 沙灘上的小魚缸外殼：和魚缸世界同形狀、縮小 scaleFactor 倍
        Vector3 proxySize = tankSize / scaleFactor;
        Vector3 proxyCenter = beachCenter + Vector3.up * (proxySize.y * 0.5f);
        BuildTank("BeachTank", proxyCenter, proxySize, glassThickness / scaleFactor);
        // 小魚缸的玻璃變成看進魚缸世界的窗戶
        transform.Find("BeachTank").gameObject.AddComponent<PortalWindow>();

        var beachArea = CreateArea("BeachTankArea", proxyCenter, proxySize - Vector3.one * (glassThickness / scaleFactor));
        var tankArea = CreateArea("TankWorldArea", tankCenter, tankSize - Vector3.one * glassThickness);

        // 從魚缸帶出的垃圾掉在小魚缸右邊，往右拋
        var exit = new GameObject("ExitPoint").transform;
        exit.SetParent(beach, false);
        exit.position = proxyCenter + new Vector3(proxySize.x * 0.5f + 0.8f, 0.5f, 0f);
        exit.rotation = Quaternion.LookRotation(Vector3.right);

        var bridge = new GameObject("ScaleBridge").AddComponent<ScaleBridge>();
        bridge.transform.SetParent(transform, false);
        bridge.Initialize(beachArea, tankArea, exit);

        // 沙灘上的垃圾：用同一批 Prefab，縮小 scaleFactor 倍，擺在小魚缸左邊
        for (int i = 0; i < beachTrashCount; i++)
        {
            TrashItem template = largeTemplates[i % largeTemplates.Length];
            Vector3 position = beachCenter + new Vector3(-proxySize.x * 0.5f - 0.8f - i * 0.7f, 0.3f, 0f);
            TrashItem item = Instantiate(template, position, Quaternion.identity, beach);
            item.name = $"Beach_{template.name}";
            item.transform.localScale /= scaleFactor;
            item.gameObject.SetActive(true);
        }

        // 同一台相機：原本看魚缸的位置當作進魚缸後的停留視角，相機搬到沙灘開始
        var tankView = new GameObject("TankView").transform;
        tankView.SetParent(transform, false);
        tankView.SetPositionAndRotation(tankCamera.transform.position, tankCamera.transform.rotation);

        tankCamera.transform.position = proxyCenter + new Vector3(-0.8f, 1.2f, -6f);
        tankCamera.transform.LookAt(proxyCenter + new Vector3(-0.8f, 0f, 0f));

        // 物件掉進去 / 帶出來時自動運鏡；Tab 手動切換
        var move = tankCamera.gameObject.AddComponent<TransferCameraMove>();
        move.Initialize(bridge, tankView, KeyCode.Tab);
    }

    BoxCollider CreateArea(string name, Vector3 center, Vector3 size)
    {
        var area = new GameObject(name).AddComponent<BoxCollider>();
        area.transform.SetParent(transform, false);
        area.transform.position = center;
        area.size = size;
        area.enabled = false;
        return area;
    }

    FishController BuildFishTemplate()
    {
        var fish = new GameObject("FishTemplate");
        fish.SetActive(false);
        fish.transform.SetParent(transform, false);

        // 身體：沿 local Z 拉長的膠囊
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(fish.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);

        // 尾鰭：標示魚的朝向（魚頭朝 +Z）
        var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.name = "Tail";
        DestroyImmediate(tail.GetComponent<Collider>());
        tail.transform.SetParent(fish.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0f, -0.45f);
        tail.transform.localScale = new Vector3(0.04f, 0.25f, 0.15f);

        Material fishMaterial = CreateMaterial(Color.white, transparent: false);
        body.GetComponent<Renderer>().sharedMaterial = fishMaterial;
        tail.GetComponent<Renderer>().sharedMaterial = fishMaterial;

        return fish.AddComponent<FishController>();
    }

    Jelly BuildJellyTemplate()
    {
        var jelly = new GameObject("JellyTemplate");
        jelly.SetActive(false);
        jelly.transform.SetParent(transform, false);

        // 點光源讓軟糖照亮圍過來的魚
        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(jelly.transform, false);
        light.type = LightType.Point;
        light.range = jellyLightRange;
        light.intensity = jellyLightIntensity;
        light.shadows = LightShadows.None;

        // 黏成一團的小塊：圓球與膠囊兩種，之後換成 3D 物品
        Material material = CreateMaterial(jellyColor, transparent: true, emissive: true);
        var component = jelly.AddComponent<Jelly>();
        component.SetPieceVariants(
            BuildJellyPiece(PrimitiveType.Sphere, Vector3.one, material),
            BuildJellyPiece(PrimitiveType.Capsule, new Vector3(0.8f, 0.6f, 0.8f), material));
        return component;
    }

    GameObject BuildJellyPiece(PrimitiveType shape, Vector3 scale, Material material)
    {
        var piece = GameObject.CreatePrimitive(shape);
        piece.name = $"JellyPiece_{shape}";
        piece.SetActive(false);
        DestroyImmediate(piece.GetComponent<Collider>());
        piece.transform.SetParent(transform, false);
        piece.transform.localScale = scale;

        var renderer = piece.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return piece;
    }

    /// <summary>生成魚缸裡的垃圾，回傳大型垃圾的模板（沙灘也用同一批）。</summary>
    TrashItem[] BuildTrash(Bounds swimBounds)
    {
        // 結構和打包的 Prefab 一樣：外殼（Rigidbody + TrashItem + Collider）→ Model（純模型）
        var large = TrashItem.TrashSize.Large;
        var entries = new[]
        {
            // 大型：瓶子、塑膠袋、鋁罐
            new TrashSpawner.Entry { prefab = BuildTrashTemplate("Trash_Bottle", PrimitiveType.Cylinder,
                new Vector3(0.35f, 0.5f, 0.35f), new Color(0.55f, 0.85f, 0.65f, 1f), 1f, large) },
            new TrashSpawner.Entry { prefab = BuildTrashTemplate("Trash_Bag", PrimitiveType.Cube,
                new Vector3(0.9f, 0.08f, 0.7f), new Color(0.95f, 0.95f, 0.9f, 1f), 1f, large) },
            new TrashSpawner.Entry { prefab = BuildTrashTemplate("Trash_Can", PrimitiveType.Cylinder,
                new Vector3(0.3f, 0.22f, 0.3f), new Color(0.85f, 0.2f, 0.2f, 1f), 1f, large) },
            // 小型：塑膠碎片
            new TrashSpawner.Entry { prefab = BuildTrashTemplate("Trash_Fragment", PrimitiveType.Cube,
                    Vector3.one, Color.white, 0.05f, TrashItem.TrashSize.Small),
                count = smallTrashCount, scaleRange = new Vector2(0.08f, 0.14f), randomTint = true },
        };
        for (int i = 0; i < 3; i++)
            entries[i].count = largeTrashCount / 3 + (i < largeTrashCount % 3 ? 1 : 0);

        var spawner = new GameObject("TrashSpawner").AddComponent<TrashSpawner>();
        spawner.transform.SetParent(transform, false);
        spawner.Initialize(entries, swimBounds);
        return new[] { entries[0].prefab, entries[1].prefab, entries[2].prefab };
    }

    TrashItem BuildTrashTemplate(string name, PrimitiveType shape, Vector3 modelScale, Color color,
        float mass, TrashItem.TrashSize size)
    {
        var shell = new GameObject(name);
        shell.SetActive(false);
        shell.transform.SetParent(transform, false);

        var model = GameObject.CreatePrimitive(shape);
        model.name = "Model";
        DestroyImmediate(model.GetComponent<Collider>());
        model.transform.SetParent(shell.transform, false);
        model.transform.localScale = modelScale;
        model.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color, transparent: false);

        // Unity 的 Cylinder 高 2、直徑 1
        Vector3 bounds = shape == PrimitiveType.Cylinder ? Vector3.Scale(modelScale, new Vector3(1f, 2f, 1f)) : modelScale;
        shell.AddComponent<BoxCollider>().size = bounds;
        shell.AddComponent<Rigidbody>().mass = mass;
        var item = shell.AddComponent<TrashItem>();
        item.Configure(size);
        return item;
    }

    Camera SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        cam.transform.position = tankCenter + new Vector3(0f, cameraHeight, -(tankSize.z * 0.5f + cameraDistance));
        cam.transform.LookAt(tankCenter);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = addBloom;
        if (!cam.TryGetComponent(out InteractionCamera _)) cam.gameObject.AddComponent<InteractionCamera>();
        return cam;
    }

    void SetupBloom()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);

        var volume = new GameObject("PostFX").AddComponent<Volume>();
        volume.transform.SetParent(transform, false);
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    static Material CreateMaterial(Color color, bool transparent, bool emissive = false)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
        }
        return material;
    }
}
