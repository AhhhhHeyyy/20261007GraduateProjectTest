using UnityEngine;

/// <summary>
/// 阻尼彈簧：讓軟糖與魚有軟Q的擠壓、回彈。
/// Value 為 0 是原狀；用在縮放時正值拉長、負值壓扁。
/// </summary>
public struct SoftSpring
{
    public float Value;
    public float Velocity;

    public void Step(float target, float stiffness, float damping, float deltaTime)
    {
        // 切成小步積分，掉幀時也不會彈爆
        int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime * 120f));
        float h = deltaTime / steps;
        for (int i = 0; i < steps; i++)
        {
            Velocity += ((target - Value) * stiffness - Velocity * damping) * h;
            Value += Velocity * h;
        }
    }

    public void Kick(float impulse) => Velocity += impulse;

    public void Reset()
    {
        Value = 0f;
        Velocity = 0f;
    }

    /// <summary>沿 axis 軸拉長 amount，其餘兩軸等比例收縮（體積大致不變）。</summary>
    public static Vector3 Stretch(float amount, int axis)
    {
        float s = Mathf.Max(1f + amount, 0.4f);
        float side = 1f / Mathf.Sqrt(s);
        Vector3 scale = new Vector3(side, side, side);
        scale[axis] = s;
        return scale;
    }
}
