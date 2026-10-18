using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 小魚物件池：逃出魚缸的魚隱藏後放回池中，延遲後在缸內重生，維持魚的數量。
/// 可直接在 Inspector 設定（魚 Prefab、數量、可游範圍），或由程式呼叫 Initialize()。
/// 整組魚缸可以直接縮放：魚、軟糖、垃圾用公尺算的參數都會乘上這個物件的 Scale（見 Scale）。
/// </summary>
public class FishPool : MonoBehaviour
{
    [Header("Fish")]
    [SerializeField] FishController fishPrefab;
    [SerializeField] int fishCount = 15;
    [SerializeField] float respawnDelay = 1f;
    [SerializeField] bool randomizeColor = false;

    [Header("Swim Area")]
    [Tooltip("可游範圍。指定 BoxCollider 時以它為準（建議勾 Is Trigger 或停用，避免擋住點擊射線）；否則用下方中心與尺寸。")]
    [SerializeField] BoxCollider swimArea;
    [SerializeField] Vector3 swimCenter = Vector3.zero;
    [SerializeField] Vector3 swimSize = new Vector3(8f, 4f, 4f);
    [Tooltip("從範圍邊界往內縮的距離，避免魚貼著玻璃")]
    [SerializeField] float swimMargin = 0.3f;

    readonly Queue<FishController> pool = new Queue<FishController>();
    readonly List<FishController> activeFish = new List<FishController>();
    Bounds swimBounds;
    bool initialized;

    public IReadOnlyList<FishController> ActiveFish => activeFish;
    public Bounds SwimBounds => swimBounds;
    /// <summary>Inspector 指定的可游範圍（ScaleBridge 用來自動找魚缸世界）。</summary>
    public BoxCollider SwimAreaCollider => swimArea;
    /// <summary>魚缸的縮放倍數（這個物件的 Scale）。魚、軟糖用公尺算的參數都乘上它，縮小整組魚缸手感不變。</summary>
    public float Scale => transform.lossyScale.x;

    void Start()
    {
        if (initialized) return;

        if (fishPrefab == null)
        {
            Debug.LogError("FishPool: 尚未指定 Fish Prefab。", this);
            return;
        }
        Initialize(fishPrefab, GetConfiguredBounds(), fishCount, randomizeColor);
    }

    public void Initialize(FishController template, Bounds bounds, int count, bool randomizeColors = false)
    {
        fishPrefab = template;
        swimBounds = bounds;
        randomizeColor = randomizeColors;
        initialized = true;

        for (int i = 0; i < count; i++)
            pool.Enqueue(CreateFish(i));

        for (int i = 0; i < count; i++)
            Spawn();
    }

    public FishController Spawn()
    {
        FishController fish = pool.Count > 0 ? pool.Dequeue() : CreateFish(activeFish.Count);

        fish.transform.SetPositionAndRotation(
            new Vector3(
                Random.Range(swimBounds.min.x, swimBounds.max.x),
                Random.Range(swimBounds.min.y, swimBounds.max.y),
                Random.Range(swimBounds.min.z, swimBounds.max.z)),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        fish.ResetState(this, swimBounds);
        fish.gameObject.SetActive(true);

        activeFish.Add(fish);
        return fish;
    }

    public void Release(FishController fish)
    {
        if (!activeFish.Remove(fish)) return;

        fish.gameObject.SetActive(false);
        pool.Enqueue(fish);
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Spawn();
    }

    FishController CreateFish(int index)
    {
        FishController fish = Instantiate(fishPrefab, transform);
        fish.gameObject.SetActive(false);
        fish.name = $"Fish_{index}";

        // 每隻魚一個隨機顏色，方便區分（使用自己的魚模型時通常關閉）
        if (randomizeColor)
        {
            Color color = Color.HSVToRGB(Random.value, 0.7f, 1f);
            foreach (Renderer r in fish.GetComponentsInChildren<Renderer>(true))
                r.material.color = color;
        }

        return fish;
    }

    Bounds GetConfiguredBounds()
    {
        Bounds bounds;
        if (swimArea != null)
        {
            Transform t = swimArea.transform;
            bounds = new Bounds(t.TransformPoint(swimArea.center), Vector3.Scale(swimArea.size, t.lossyScale));
        }
        else
        {
            bounds = new Bounds(transform.TransformPoint(swimCenter), swimSize * Scale);
        }

        bounds.Expand(-swimMargin * Scale * 2f);
        return bounds;
    }

    void OnDrawGizmosSelected()
    {
        Bounds b = Application.isPlaying && initialized ? swimBounds : GetConfiguredBounds();
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
