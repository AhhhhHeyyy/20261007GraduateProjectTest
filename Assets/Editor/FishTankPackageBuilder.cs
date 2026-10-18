using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 把 TankBootstrap 在 Play 時生成的東西存成 Prefab / 材質 / 示範場景，並匯出 .unitypackage。
/// 只在本專案用，不會被打包。
///
/// Prefab 結構（外殼與模型分開，之後換模型不用改腳本）：
///   Fish（FishController）→ Visual → Model（換這個）
///   Jelly（Jelly + Point Light），Piece Variants 指向 Piece_* Prefab（換這些）
///   Trash_*（Rigidbody + TrashItem + BoxCollider）→ Model（換這個）
///   FishTankSetup（FishPool + InteractionManager + TrashGrabber + TrashSpawner）
///     → Tank（外觀）、ClickSurface（點擊面）、SwimArea（可游範圍）、TrashWalls（擋垃圾的隱形牆）、Water（WaterVolume，水的範圍與水面）、TankView（運鏡預設停留視角）
///   BeachTankSetup（ScaleBridge + PortalWindow，Scale 0.33，中心點在缸底）
///     → Body（Tank 外觀、TrashWalls、BeachTankArea）、ExitPoint
/// 示範場景：FishTankDemo（只有魚缸）、BeachDemo（沙灘 + 魚缸世界在 X = 1000 + 運鏡）
/// </summary>
public static class FishTankPackageBuilder
{
    const string Root = "Assets/FishTank";
    const string PrefabDir = Root + "/Prefabs";
    const string MaterialDir = Root + "/Materials";
    const string SceneDir = Root + "/Scenes";
    const string ScriptDir = "Assets/Scripts/FishTank";

    // 與 TankBootstrap 的預設值一致
    static readonly Vector3 TankSize = new Vector3(10f, 6f, 6f);
    const float GlassThickness = 0.1f;
    const float SwimMargin = 0.6f;
    const int FishCount = 15;
    const float CameraDistance = 11f;
    const float CameraHeight = 1f;
    const float WaterSurfaceInset = 0.3f;
    const int SmallTrashCount = 12;
    const float BeachTankScale = 0.33f;
    const float TankWorldOffsetX = 1000f;

    [MenuItem("Tools/FishTank/1. 建立 Prefab 與示範場景")]
    static void BuildAll()
    {
        if (AssetDatabase.IsValidFolder(PrefabDir) &&
            !EditorUtility.DisplayDialog("FishTank",
                $"{PrefabDir} 已存在，重新建立會覆蓋 Prefab 與材質（包含你換過的模型）。\n要繼續嗎？",
                "覆蓋", "取消"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabDir);
        EnsureFolder(MaterialDir);
        EnsureFolder(SceneDir);

        // 先開空場景當工作區，暫存物件不會污染使用者的場景
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var fishMat = SaveMaterial("Fish", CreateMaterial(Color.white, transparent: false));
        var pieceMat = SaveMaterial("JellyPiece", CreateMaterial(new Color(1f, 1f, 1f, 0.9f), transparent: true, emissive: true));
        var glassMat = SaveMaterial("Glass", CreateMaterial(new Color(0.6f, 0.85f, 1f, 0.15f), transparent: true));
        var floorMat = SaveMaterial("Floor", CreateMaterial(new Color(0.85f, 0.75f, 0.55f, 1f), transparent: false));

        // 傳送門窗戶：有素材引用 shader，Build 時才不會被剔除。用法：拖進 PortalWindow 的 Window Material
        Shader portalShader = Shader.Find("FishTank/PortalWindow");
        Material portalMat = portalShader != null ? SaveMaterial("PortalWindow", new Material(portalShader)) : null;
        if (portalMat == null) Debug.LogWarning("FishTank：找不到 FishTank/PortalWindow shader，沒有建立 PortalWindow.mat。");

        var fish = BuildFish(fishMat);
        var pieces = new[]
        {
            BuildPiece(PrimitiveType.Sphere, Vector3.one, pieceMat),
            BuildPiece(PrimitiveType.Capsule, new Vector3(0.8f, 0.6f, 0.8f), pieceMat),
        };
        var jelly = BuildJelly(pieces);
        var trash = BuildTrashPrefabs();
        BuildSetup(fish, jelly, trash, glassMat, floorMat);
        BuildBeachTank("BeachTankSetup", glassMat, floorMat, portalMat, usePortal: true);
        BuildBeachTank("BeachTankSetup_NoPortal", glassMat, floorMat, portalMat, usePortal: false);
        BuildBloomProfile();

        // 存檔後素材資料庫會重新整理，前面拿到的 Prefab / 素材參照可能失效，之後一律從路徑重新載入
        AssetDatabase.SaveAssets();
        BuildBeachTrashVariants();
        BuildDemoScene();
        BuildBeachDemoScene("BeachTankSetup", "BeachDemo");
        BuildBeachDemoScene(null, "BeachDemo_NoPortal");

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(Load<GameObject>($"{PrefabDir}/BeachTankSetup.prefab"));
        Debug.Log($"FishTank：已建立 Prefab 與示範場景（{SceneDir}/FishTankDemo、BeachDemo、BeachDemo_NoPortal）。");
    }

    [MenuItem("Tools/FishTank/2. 匯出 unitypackage")]
    static void ExportPackage()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            EditorUtility.DisplayDialog("FishTank", "還沒有 Prefab，請先執行「1. 建立 Prefab 與示範場景」。", "好");
            return;
        }

        string path = EditorUtility.SaveFilePanel("匯出 FishTank", "", "FishTank.unitypackage", "unitypackage");
        if (string.IsNullOrEmpty(path)) return;

        // TankBootstrap 只是本專案測試用，不帶走
        var assets = AssetDatabase.FindAssets("", new[] { Root, ScriptDir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !AssetDatabase.IsValidFolder(p))
            .Where(p => Path.GetFileName(p) != "TankBootstrap.cs")
            .Distinct()
            .ToArray();

        AssetDatabase.ExportPackage(assets, path, ExportPackageOptions.Interactive);
    }

    // ---------- Prefab ----------

    static FishController BuildFish(Material material)
    {
        var fish = new GameObject("Fish");
        var visual = new GameObject("Visual").transform;
        visual.SetParent(fish.transform, false);

        // Model 是佔位模型：之後把整個 Model 換成你的魚（魚頭朝 +Z、不要 Collider）
        var model = new GameObject("Model").transform;
        model.SetParent(visual, false);

        var body = CreatePart(PrimitiveType.Capsule, "Body", model, material);
        body.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.localScale = new Vector3(0.25f, 0.4f, 0.25f);

        var tail = CreatePart(PrimitiveType.Cube, "Tail", model, material);
        tail.localPosition = new Vector3(0f, 0f, -0.45f);
        tail.localScale = new Vector3(0.04f, 0.25f, 0.15f);

        var controller = fish.AddComponent<FishController>();
        var so = new SerializedObject(controller);
        so.FindProperty("visual").objectReferenceValue = visual;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(fish, "Fish").GetComponent<FishController>();
    }

    static GameObject BuildPiece(PrimitiveType shape, Vector3 scale, Material material)
    {
        var piece = GameObject.CreatePrimitive(shape);
        piece.name = $"Piece_{shape}";
        Object.DestroyImmediate(piece.GetComponent<Collider>());
        piece.transform.localScale = scale;

        var renderer = piece.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return SavePrefab(piece, piece.name);
    }

    static Jelly BuildJelly(GameObject[] pieces)
    {
        var jelly = new GameObject("Jelly");

        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(jelly.transform, false);
        light.type = LightType.Point;
        light.range = 3f;
        light.intensity = 2f;
        light.shadows = LightShadows.None;

        var component = jelly.AddComponent<Jelly>();
        var so = new SerializedObject(component);
        var variants = so.FindProperty("pieceVariants");
        variants.arraySize = pieces.Length;
        for (int i = 0; i < pieces.Length; i++)
            variants.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(jelly, "Jelly").GetComponent<Jelly>();
    }

    /// <summary>垃圾外殼：BoxCollider 包住模型；Model 是佔位模型，換成正式模型時記得調 Collider 大小。</summary>
    static TrashSpawner.Entry[] BuildTrashPrefabs()
    {
        var large = TrashItem.TrashSize.Large;
        return new[]
        {
            new TrashSpawner.Entry { prefab = BuildTrash("Trash_Bottle", PrimitiveType.Cylinder,
                new Vector3(0.35f, 0.5f, 0.35f), new Color(0.55f, 0.85f, 0.65f, 1f), 1f, large) },
            new TrashSpawner.Entry { prefab = BuildTrash("Trash_Bag", PrimitiveType.Cube,
                new Vector3(0.9f, 0.08f, 0.7f), new Color(0.95f, 0.95f, 0.9f, 1f), 1f, large) },
            new TrashSpawner.Entry { prefab = BuildTrash("Trash_Can", PrimitiveType.Cylinder,
                new Vector3(0.3f, 0.22f, 0.3f), new Color(0.85f, 0.2f, 0.2f, 1f), 1f, large) },
            new TrashSpawner.Entry { prefab = BuildTrash("Trash_Fragment", PrimitiveType.Cube,
                    Vector3.one, Color.white, 0.05f, TrashItem.TrashSize.Small),
                count = SmallTrashCount, scaleRange = new Vector2(0.08f, 0.14f), randomTint = true },
        };
    }

    static TrashItem BuildTrash(string name, PrimitiveType shape, Vector3 modelScale, Color color,
        float mass, TrashItem.TrashSize size)
    {
        var shell = new GameObject(name);
        var model = CreatePart(shape, "Model", shell.transform, SaveMaterial(name, CreateMaterial(color, transparent: false)));
        model.localScale = modelScale;

        // Unity 的 Cylinder 高 2、直徑 1
        Vector3 bounds = shape == PrimitiveType.Cylinder ? Vector3.Scale(modelScale, new Vector3(1f, 2f, 1f)) : modelScale;
        shell.AddComponent<BoxCollider>().size = bounds;
        shell.AddComponent<Rigidbody>().mass = mass;

        var item = shell.AddComponent<TrashItem>();
        var so = new SerializedObject(item);
        so.FindProperty("size").enumValueIndex = (int)size;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(shell, name).GetComponent<TrashItem>();
    }

    static GameObject BuildSetup(FishController fish, Jelly jelly, TrashSpawner.Entry[] trash, Material glass, Material floor)
    {
        var root = new GameObject("FishTankSetup");
        BuildTank(root.transform, glass, floor);

        // 點擊面：魚缸正面（-Z，面向相機）的隱形 Box Collider
        var click = new GameObject("ClickSurface");
        click.transform.SetParent(root.transform, false);
        click.transform.localPosition = new Vector3(0f, 0f, -TankSize.z * 0.5f);
        var clickCollider = click.AddComponent<BoxCollider>();
        clickCollider.size = new Vector3(TankSize.x, TankSize.y, 0.05f);

        // 可游範圍：停用的 Box Collider，只拿來讀大小，不會擋點擊
        var swim = new GameObject("SwimArea");
        swim.transform.SetParent(root.transform, false);
        var swimBox = swim.AddComponent<BoxCollider>();
        swimBox.size = TankSize - Vector3.one * GlassThickness;
        swimBox.enabled = false;

        var pool = root.AddComponent<FishPool>();
        var poolSo = new SerializedObject(pool);
        poolSo.FindProperty("fishPrefab").objectReferenceValue = fish;
        poolSo.FindProperty("fishCount").intValue = FishCount;
        poolSo.FindProperty("randomizeColor").boolValue = true; // 佔位魚用；換成有貼圖的魚時關掉
        poolSo.FindProperty("swimArea").objectReferenceValue = swimBox;
        poolSo.FindProperty("swimMargin").floatValue = SwimMargin;
        poolSo.ApplyModifiedPropertiesWithoutUndo();

        var interaction = root.AddComponent<InteractionManager>();
        var interactionSo = new SerializedObject(interaction);
        interactionSo.FindProperty("jellyPrefab").objectReferenceValue = jelly;
        interactionSo.FindProperty("fishPool").objectReferenceValue = pool;
        interactionSo.FindProperty("glassMask").intValue = ~(1 << IgnoreRaycastLayer); // 不要點到 TrashWalls
        interactionSo.FindProperty("clickSurface").objectReferenceValue = clickCollider; // 和沙灘同場景時，點沙灘不會射軟糖
        interactionSo.ApplyModifiedPropertiesWithoutUndo();

        BuildTrashWalls(root.transform);

        // 水：頂面比魚缸頂端低一點，跟著整組移動
        var water = new GameObject("Water");
        water.transform.SetParent(root.transform, false);
        water.transform.localPosition = new Vector3(0f, -WaterSurfaceInset * 0.5f, 0f);
        var waterVolume = water.AddComponent<WaterVolume>();
        var waterSo = new SerializedObject(waterVolume);
        waterSo.FindProperty("size").vector3Value = new Vector3(TankSize.x, TankSize.y - WaterSurfaceInset, TankSize.z);
        waterSo.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<TrashGrabber>();

        var spawner = root.AddComponent<TrashSpawner>();
        var spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("area").objectReferenceValue = swimBox;
        var entries = spawnerSo.FindProperty("entries");
        entries.arraySize = trash.Length;
        for (int i = 0; i < trash.Length; i++)
        {
            var e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("prefab").objectReferenceValue = trash[i].prefab;
            e.FindPropertyRelative("count").intValue = trash[i].count;
            e.FindPropertyRelative("scaleRange").vector2Value = trash[i].scaleRange;
            e.FindPropertyRelative("randomTint").boolValue = trash[i].randomTint;
        }
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();

        // 運鏡進魚缸後預設停的視角（TransferCameraMove 沒指定 Tank View 時用這個）
        var view = new GameObject("TankView").transform;
        view.SetParent(root.transform, false);
        view.localPosition = new Vector3(0f, 0f, -(TankSize.z * 0.5f + 4f));

        return SavePrefab(root, "FishTankSetup");
    }

    /// <summary>
    /// 沙灘上的小魚缸整組：拖到沙灘上就能用。中心點在缸底，直接放在地面上；Scale 決定倍數（0.33 = 魚缸世界大 3 倍）。
    /// ScaleBridge 自動找魚缸世界、自動對齊大小；PortalWindow 把外觀變成看進魚缸世界的窗戶。
    /// </summary>
    /// <param name="usePortal">true：PortalWindow（玻璃顯示魚缸世界的畫面）；false：TankMiniature（把魚缸世界的模型縮小畫在小魚缸裡，不用投影）</param>
    static GameObject BuildBeachTank(string name, Material glass, Material floor, Material portal, bool usePortal)
    {
        var root = new GameObject(name);

        var body = new GameObject("Body").transform;
        body.SetParent(root.transform, false);
        body.localPosition = new Vector3(0f, TankSize.y * 0.5f, 0f);
        BuildTank(body, glass, floor);
        BuildTrashWalls(body);

        var area = new GameObject("BeachTankArea");
        area.transform.SetParent(body, false);
        var areaBox = area.AddComponent<BoxCollider>();
        areaBox.size = TankSize - Vector3.one * GlassThickness;
        areaBox.enabled = false;

        // 帶出來的垃圾從小魚缸右邊往右拋
        var exit = new GameObject("ExitPoint").transform;
        exit.SetParent(root.transform, false);
        exit.localPosition = new Vector3(TankSize.x * 0.5f + 2.5f, 1.5f, 0f);
        exit.localRotation = Quaternion.LookRotation(Vector3.right);

        var bridge = root.AddComponent<ScaleBridge>();
        var bridgeSo = new SerializedObject(bridge);
        bridgeSo.FindProperty("beachTank").objectReferenceValue = areaBox;
        bridgeSo.FindProperty("exitPoint").objectReferenceValue = exit;
        bridgeSo.ApplyModifiedPropertiesWithoutUndo();

        if (usePortal)
        {
            var window = root.AddComponent<PortalWindow>();
            var windowSo = new SerializedObject(window);
            windowSo.FindProperty("windowMaterial").objectReferenceValue = portal;
            windowSo.FindProperty("windowRoot").objectReferenceValue = body.Find("Tank"); // 只有玻璃和缸底變窗戶，美術加的框、裝飾不會被換掉
            windowSo.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            root.AddComponent<TankMiniature>();
        }

        root.transform.localScale = Vector3.one * BeachTankScale;
        return SavePrefab(root, name);
    }

    const int IgnoreRaycastLayer = 2;

    /// <summary>擋住垃圾的隱形牆：底部和四面，頂端開口讓大型垃圾拉得出去。和外觀分開，換魚缸模型不影響。</summary>
    static void BuildTrashWalls(Transform parent)
    {
        var walls = new GameObject("TrashWalls").transform;
        walls.SetParent(parent, false);
        walls.gameObject.layer = IgnoreRaycastLayer;
        Vector3 half = TankSize * 0.5f;

        for (int axis = 0; axis < 3; axis++)
        {
            foreach (float sign in new[] { -1f, 1f })
            {
                if (axis == 1 && sign > 0f) continue;

                var wall = new GameObject($"Wall_{"XYZ"[axis]}{(sign > 0 ? "+" : "-")}");
                wall.layer = IgnoreRaycastLayer;
                wall.transform.SetParent(walls, false);
                Vector3 offset = Vector3.zero;
                offset[axis] = half[axis] * sign;
                wall.transform.localPosition = offset;
                Vector3 size = TankSize;
                size[axis] = GlassThickness;
                wall.AddComponent<BoxCollider>().size = size;
            }
        }
    }

    /// <summary>魚缸外觀（純視覺、沒有 Collider，頂端開口），之後可整個換成你的魚缸模型。</summary>
    static void BuildTank(Transform parent, Material glass, Material floor)
    {
        var tank = new GameObject("Tank").transform;
        tank.SetParent(parent, false);
        Vector3 half = TankSize * 0.5f;

        for (int axis = 0; axis < 3; axis++)
        {
            foreach (float sign in new[] { -1f, 1f })
            {
                if (axis == 1 && sign > 0f) continue;

                Vector3 offset = Vector3.zero;
                offset[axis] = half[axis] * sign;
                Vector3 scale = TankSize;
                scale[axis] = GlassThickness;

                bool isFloor = axis == 1 && sign < 0f;
                var wall = CreatePart(PrimitiveType.Cube,
                    isFloor ? "Floor" : $"Glass_{"XYZ"[axis]}{(sign > 0 ? "+" : "-")}",
                    tank, isFloor ? floor : glass);
                wall.localPosition = offset;
                wall.localScale = scale;
                wall.GetComponent<Renderer>().shadowCastingMode = isFloor ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }
    }

    // ---------- Bloom 與示範場景 ----------

    static VolumeProfile BuildBloomProfile()
    {
        string path = $"{Root}/FishTankBloom.asset";
        AssetDatabase.DeleteAsset(path);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var bloom = profile.Add<Bloom>(true);
        bloom.name = nameof(Bloom);
        bloom.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        bloom.intensity.Override(1.2f);
        bloom.threshold.Override(0.9f);
        AssetDatabase.AddObjectToAsset(bloom, profile);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    static void BuildDemoScene()
    {
        var setupPrefab = Load<GameObject>($"{PrefabDir}/FishTankSetup.prefab");

        // 目前是 NewScene 建出的工作區：清掉暫存物件，只留相機與燈
        foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (go.GetComponent<Camera>() == null && go.GetComponent<Light>() == null)
                Object.DestroyImmediate(go);

        PrefabUtility.InstantiatePrefab(setupPrefab);

        Camera cam = Camera.main;
        cam.transform.position = new Vector3(0f, CameraHeight, -(TankSize.z * 0.5f + CameraDistance));
        cam.transform.LookAt(Vector3.zero);
        cam.gameObject.AddComponent<InteractionCamera>(); // 標記為玩家操作用的相機

        // 光暈在開場由 FishTankPostFX 建立，場景裡不放 URP 的 Volume
        new GameObject("PostFX").AddComponent<FishTankPostFX>();
        StripPipelineComponents();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SceneDir}/FishTankDemo.unity");
    }

    /// <summary>
    /// 拿掉場景裡 URP 專用的元件（相機／燈光的 Additional Data、Volume），示範場景在 Built-in 專案打開時才不會出現 Missing Script。
    /// URP 專案執行時會自動補回相機與燈光的 Additional Data。
    /// </summary>
    static void StripPipelineComponents()
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string ns = component != null ? component.GetType().Namespace : null;
                if (ns != null && ns.StartsWith("UnityEngine.Rendering")) Object.DestroyImmediate(component);
            }
    }

    static readonly string[] BeachTrashNames = { "Trash_Bottle", "Trash_Bag", "Trash_Can" };

    /// <summary>
    /// 沙灘尺寸的垃圾：原本 Trash_* 的 Prefab Variant，Scale = BeachTankSetup 的 Scale（預設 0.33）。
    /// 換原本 Prefab 的模型，Variant 會跟著變；沙灘上用這些，編輯和遊玩時大小一致。
    /// </summary>
    static void BuildBeachTrashVariants()
    {
        foreach (string name in BeachTrashNames)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>($"{PrefabDir}/{name}.prefab"));
            instance.transform.localScale = Vector3.one * BeachTankScale;
            PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDir}/{name}_Beach.prefab");
            Object.DestroyImmediate(instance);
        }
    }

    /// <summary>
    /// 沙灘示範：拖兩個 Prefab + 沙灘垃圾 + 相機掛 InteractionCamera / TransferCameraMove，其他都自動。
    /// beachTankName = null：單一魚缸，FishTankSetup 直接縮小擺在沙灘上（不用 ScaleBridge）。
    /// </summary>
    static void BuildBeachDemoScene(string beachTankName, string sceneName)
    {
        var setupPrefab = Load<GameObject>($"{PrefabDir}/FishTankSetup.prefab");
        var beachTankPrefab = beachTankName != null ? Load<GameObject>($"{PrefabDir}/{beachTankName}.prefab") : null;
        var sandMaterial = Load<Material>($"{MaterialDir}/Floor.mat");

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var tankWorld = (GameObject)PrefabUtility.InstantiatePrefab(setupPrefab);
        if (beachTankPrefab != null)
        {
            tankWorld.transform.position = new Vector3(TankWorldOffsetX, 0f, 0f);
        }
        else
        {
            // 缸底貼著沙面（FishTankSetup 的中心在魚缸正中央）
            tankWorld.transform.position = new Vector3(0f, (TankSize.y * 0.5f + GlassThickness * 0.5f) * BeachTankScale, 0f);
            tankWorld.transform.localScale = Vector3.one * BeachTankScale;
            var water = new SerializedObject(tankWorld.GetComponentInChildren<WaterVolume>());
            water.FindProperty("dropOutside").boolValue = true;
            water.ApplyModifiedPropertiesWithoutUndo();
        }

        var sand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sand.name = "Sand";
        sand.transform.position = new Vector3(0f, -0.1f, 0f);
        sand.transform.localScale = new Vector3(20f, 0.2f, 12f);
        sand.GetComponent<Renderer>().sharedMaterial = sandMaterial;

        if (beachTankPrefab != null) PrefabUtility.InstantiatePrefab(beachTankPrefab);

        // 沙灘垃圾：用沙灘尺寸的 Variant，編輯時看到多大玩的時候就多大
        for (int i = 0; i < BeachTrashNames.Length; i++)
        {
            var item = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>($"{PrefabDir}/{BeachTrashNames[i]}_Beach.prefab"));
            item.transform.position = new Vector3(-2.6f - i * 0.8f, 0.3f, 0f);
        }

        Camera cam = Camera.main;
        cam.transform.position = new Vector3(-0.8f, 1.8f, -6f);
        cam.transform.LookAt(new Vector3(-0.8f, 0.7f, 0f));
        cam.gameObject.AddComponent<InteractionCamera>();
        cam.gameObject.AddComponent<TransferCameraMove>();

        // 光暈在開場由 FishTankPostFX 建立，場景裡不放 URP 的 Volume
        new GameObject("PostFX").AddComponent<FishTankPostFX>();
        StripPipelineComponents();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SceneDir}/{sceneName}.unity");
    }

    // ---------- 工具 ----------

    static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new System.InvalidOperationException($"FishTank：找不到 {path}，請重新執行「1. 建立 Prefab 與示範場景」。");
        return asset;
    }

    static Transform CreatePart(PrimitiveType shape, string name, Transform parent, Material material)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go.transform;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Material SaveMaterial(string name, Material material)
    {
        string path = $"{MaterialDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            // 保留 GUID，既有 Prefab 的參照不會斷
            existing.shader = material.shader;
            existing.CopyPropertiesFromMaterial(material);
            existing.shaderKeywords = material.shaderKeywords;
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // 依目前的渲染管線建 Lit 材質（URP Lit / Standard）
    static Material CreateMaterial(Color color, bool transparent, bool emissive = false) =>
        FishTankRender.CreateLit(color, transparent, emissive);

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
