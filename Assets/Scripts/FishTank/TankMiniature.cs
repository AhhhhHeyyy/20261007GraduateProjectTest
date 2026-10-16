using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 不用投影的版本：每一幀把魚缸世界裡的模型（魚、軟糖、垃圾、夾子、掉進去的物品）縮小畫在沙灘的小魚缸裡。
/// 只是「畫」出來：沒有額外物件、物理或程式，也點不到；真正的互動仍在魚缸世界。
/// 不需要額外相機、RenderTexture 或特殊 shader，任何相機設定、Built-in / URP 都能用。
/// 掛在小魚缸的根物件上（BeachTankSetup_NoPortal 已掛好），和 PortalWindow 二選一。
/// </summary>
[DisallowMultipleComponent]
public class TankMiniature : MonoBehaviour
{
    [Tooltip("留空會自動尋找")]
    [SerializeField] ScaleBridge bridge;
    [Tooltip("魚缸世界裡不要畫的子物件名稱（預設 Tank：魚缸外觀，小魚缸有自己的外觀）")]
    [SerializeField] string[] skipChildren = { "Tank" };
    [SerializeField] bool castShadows = true;
    [Tooltip("多久重新收集一次要畫的模型（秒）；新生成的魚、軟糖最多延遲這麼久才出現在小魚缸裡")]
    [SerializeField] float refreshInterval = 0.2f;
    [Tooltip("操作相機在魚缸世界時不畫（那時看不到小魚缸）")]
    [SerializeField] bool hideWhenInTank = true;

    readonly List<Renderer> renderers = new List<Renderer>();
    readonly Dictionary<SkinnedMeshRenderer, Mesh> baked = new Dictionary<SkinnedMeshRenderer, Mesh>();
    Transform tankRoot;
    float nextRefresh;

    void Start()
    {
        if (bridge == null) bridge = GetComponentInParent<ScaleBridge>();
        if (bridge == null) bridge = FindAnyObjectByType<ScaleBridge>();
        if (bridge == null)
        {
            Debug.LogError("TankMiniature: 場景裡找不到 ScaleBridge。", this);
            enabled = false;
        }
    }

    void OnDestroy()
    {
        foreach (Mesh mesh in baked.Values) Destroy(mesh);
    }

    void LateUpdate()
    {
        if (bridge == null) return;
        if (tankRoot == null) tankRoot = bridge.TankWorldRoot;
        if (tankRoot == null) return;

        Camera cam = ViewCamera.Current;
        if (hideWhenInTank && cam != null && bridge.IsInTankWorld(cam.transform.position)) return;

        if (Time.time >= nextRefresh)
        {
            Collect();
            nextRefresh = Time.time + refreshInterval;
        }

        Matrix4x4 toBeach = bridge.TankToBeachMatrix;
        foreach (Renderer r in renderers)
            if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                Draw(r, toBeach);
    }

    /// <summary>魚缸世界底下的模型（扣掉魚缸外觀），加上從沙灘掉進魚缸世界的物品。</summary>
    void Collect()
    {
        renderers.Clear();
        foreach (Transform child in tankRoot)
        {
            if (System.Array.IndexOf(skipChildren, child.name) >= 0) continue;
            renderers.AddRange(child.GetComponentsInChildren<Renderer>());
        }
        renderers.AddRange(tankRoot.GetComponents<Renderer>());

        foreach (TrashItem item in TrashItem.Active) AddVisitor(item.transform);
        foreach (ScaleTraveler traveler in ScaleTraveler.Active) AddVisitor(traveler.transform);
    }

    void AddVisitor(Transform visitor)
    {
        if (visitor.IsChildOf(tankRoot) || !bridge.IsInTankWorld(visitor.position)) return;
        renderers.AddRange(visitor.GetComponentsInChildren<Renderer>());
    }

    void Draw(Renderer r, Matrix4x4 toBeach)
    {
        Mesh mesh;
        Matrix4x4 matrix;
        if (r is MeshRenderer)
        {
            var filter = r.GetComponent<MeshFilter>();
            mesh = filter != null ? filter.sharedMesh : null;
            matrix = toBeach * r.localToWorldMatrix;
        }
        else if (r is SkinnedMeshRenderer skinned)
        {
            // 有骨架動畫的模型：每幀把目前的姿勢烤成一個 Mesh 再畫（縮放已烤進去）
            if (!baked.TryGetValue(skinned, out mesh)) baked[skinned] = mesh = new Mesh();
            skinned.BakeMesh(mesh, true);
            Transform t = skinned.transform;
            matrix = toBeach * Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
        }
        else
        {
            return; // 粒子、線條等不畫
        }
        if (mesh == null) return;

        Material[] materials = r.sharedMaterials;
        if (materials.Length == 0) return;
        ShadowCastingMode shadows = castShadows ? r.shadowCastingMode : ShadowCastingMode.Off;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            Material material = materials[Mathf.Min(i, materials.Length - 1)];
            if (material != null)
                Graphics.DrawMesh(mesh, matrix, material, r.gameObject.layer, null, i, null, shadows, r.receiveShadows);
        }
    }
}
