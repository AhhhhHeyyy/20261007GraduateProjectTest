using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一片水：盒狀範圍，頂面就是水面。垃圾在裡面會受水的阻力與浮力，小型垃圾被夾時會滑掉。
/// 魚缸裡放一個；之後沙灘的海也可以放一個（把 Remove On Exit 關掉）。
/// 範圍以這個物件的位置為中心，跟著物件移動、縮放（不要旋轉 X / Z）。
/// </summary>
public class WaterVolume : MonoBehaviour
{
    public static readonly List<WaterVolume> Active = new List<WaterVolume>();

    [SerializeField] Vector3 center = Vector3.zero;
    [SerializeField] Vector3 size = new Vector3(10f, 5.7f, 6f);
    [Tooltip("把大型垃圾拉出這片水面時算「帶出」（觸發 TrashItem.Removed）。沙灘的海要關掉")]
    [SerializeField] bool removeOnExit = true;

    public bool RemoveOnExit => removeOnExit;
    public float SurfaceY => transform.TransformPoint(center + Vector3.up * (size.y * 0.5f)).y;

    public void Initialize(Vector3 volumeSize, bool removeWhenLifted = true)
    {
        center = Vector3.zero;
        size = volumeSize;
        removeOnExit = removeWhenLifted;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    /// <summary>在水裡。</summary>
    public bool Contains(Vector3 point)
    {
        Vector3 local = transform.InverseTransformPoint(point) - center;
        Vector3 half = size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    /// <summary>在水面正上方（水平位置在範圍內、高度高於水面）。</summary>
    public bool IsAbove(Vector3 point)
    {
        Vector3 local = transform.InverseTransformPoint(point) - center;
        Vector3 half = size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z && local.y > half.y;
    }

    public static WaterVolume Find(Vector3 point)
    {
        foreach (WaterVolume water in Active)
            if (water.Contains(point)) return water;
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(center, size);
    }
}
