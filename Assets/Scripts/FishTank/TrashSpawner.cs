using UnityEngine;

/// <summary>
/// 開場時把垃圾 Prefab 隨機撒在魚缸底部。每種垃圾可設數量、隨機大小與隨機上色。
/// 可直接在 Inspector 設定，或由程式呼叫 Initialize()。
/// </summary>
public class TrashSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public TrashItem prefab;
        public int count = 1;
        [Tooltip("整件垃圾的隨機縮放（乘上 Prefab 本身的 Scale）")]
        public Vector2 scaleRange = Vector2.one;
        [Tooltip("每件隨機上色（佔位模型用；有貼圖的模型關掉）")]
        public bool randomTint;
    }

    [SerializeField] Entry[] entries = new Entry[0];

    [Header("Area")]
    [Tooltip("撒垃圾的範圍，垃圾會出現在底部附近。指定 BoxCollider 時以它為準（可以和 FishPool 共用 SwimArea）；否則用下方中心與尺寸。")]
    [SerializeField] BoxCollider area;
    [SerializeField] Vector3 areaCenter = Vector3.zero;
    [SerializeField] Vector3 areaSize = new Vector3(8f, 4f, 4f);
    [Tooltip("從範圍邊界往內縮，避免生在玻璃裡")]
    [SerializeField] float margin = 0.5f;
    [Tooltip("生在底部往上多高的範圍內，之後自己沉下去")]
    [SerializeField] float dropHeight = 0.5f;

    Transform container;
    bool initialized;

    void Start()
    {
        if (initialized) return;
        Spawn(GetConfiguredBounds());
    }

    public void Initialize(Entry[] trashEntries, Bounds bounds)
    {
        entries = trashEntries;
        initialized = true;
        Spawn(bounds);
    }

    void Spawn(Bounds bounds)
    {
        if (container == null)
        {
            container = new GameObject("Trash").transform;
            container.SetParent(transform, false);
        }

        foreach (Entry entry in entries)
        {
            if (entry.prefab == null)
            {
                Debug.LogError("TrashSpawner: 有一筆沒有指定 Prefab。", this);
                continue;
            }
            for (int i = 0; i < entry.count; i++)
                SpawnOne(entry, bounds, i);
        }
    }

    void SpawnOne(Entry entry, Bounds bounds, int index)
    {
        Vector3 position = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y + Random.Range(0f, dropHeight),
            Random.Range(bounds.min.z, bounds.max.z));

        TrashItem item = Instantiate(entry.prefab, position, Random.rotation, container);
        item.name = $"{entry.prefab.name}_{index}";
        item.transform.localScale *= Random.Range(entry.scaleRange.x, entry.scaleRange.y);

        if (entry.randomTint)
        {
            Color color = Color.HSVToRGB(Random.value, 0.6f, 1f);
            foreach (Renderer r in item.GetComponentsInChildren<Renderer>(true))
                r.material.color = color;
        }

        item.gameObject.SetActive(true);
    }

    Bounds GetConfiguredBounds()
    {
        Bounds bounds;
        if (area != null)
        {
            Transform t = area.transform;
            bounds = new Bounds(t.TransformPoint(area.center), Vector3.Scale(area.size, t.lossyScale));
        }
        else
        {
            bounds = new Bounds(transform.TransformPoint(areaCenter), areaSize);
        }

        bounds.Expand(-margin * 2f);
        return bounds;
    }

    void OnDrawGizmosSelected()
    {
        Bounds b = GetConfiguredBounds();
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(
            new Vector3(b.center.x, b.min.y + dropHeight * 0.5f, b.center.z),
            new Vector3(b.size.x, dropHeight, b.size.z));
    }
}
