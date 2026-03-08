using UnityEngine;

/// <summary>
/// 可视化 FlowField：显示格子热力图（integrationCost）与箭头（bestDirection）
///
/// 用法：把此脚本挂在 GridManager 相同 GameObject 或任意对象，运行时勾选 ShowHeatmap/ShowArrows。
/// 注意：热力图对大网格可能较慢：可以设置 samplingStep > 1 以降低绘制格子数。
/// </summary>
[ExecuteAlways]
public class FlowFieldVisualizer : MonoBehaviour
{
    public GridManager gridManager;
    public bool showHeatmap = true;
    public bool showArrows = true;
    [Range(1, 8)] public int samplingStep = 1; // 每几个格子采样一次（降低绘制量）
    public float arrowScale = 0.4f;
    public float maxCostForColor = 2000f; // 用于热力图归一化

    void OnValidate()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
    }

    void OnDrawGizmos()
    {
        if(!Application.isPlaying)return;
        if (gridManager == null) return;
        Gizmos.matrix = Matrix4x4.identity;

        for (int y = 0; y < gridManager.height; y += samplingStep)
            for (int x = 0; x < gridManager.width; x += samplingStep)
            {
                var n = gridManager.GetNode(x, y);
                if (n == null) continue;
                Vector3 pos = gridManager.GetWorldPosition(x, y);

                // Heatmap
                if (showHeatmap)
                {
                    float cost = (n.integrationCost >= int.MaxValue / 8) ? maxCostForColor : n.integrationCost;
                    float t = Mathf.Clamp01(cost / maxCostForColor);
                    // Blue (low) -> Red (high): use HSV or lerp
                    Color col = Color.Lerp(Color.blue, Color.red, t);
                    col.a = 0.4f;
                    Gizmos.color = col;
                    Gizmos.DrawCube(pos, new Vector3(gridManager.cellSize * 0.9f * samplingStep, 0.01f, gridManager.cellSize * 0.9f * samplingStep));
                }

                // Arrows
                if (showArrows)
                {
                    if (n.bestDirection.sqrMagnitude > 0.0001f)
                    {
                        Vector3 dir = new Vector3(n.bestDirection.x, 0f, n.bestDirection.y);
                        if (dir.sqrMagnitude > 0.001f)
                        {
                            Gizmos.color = Color.black;
                            Vector3 from = pos;
                            Vector3 to = pos + dir.normalized * arrowScale * gridManager.cellSize;
                            Gizmos.DrawLine(from, to);
                            // small head
                            Quaternion q = Quaternion.LookRotation(dir);
                            Vector3 right = q * Quaternion.Euler(0, 160, 0) * Vector3.forward * (arrowScale * 0.25f * gridManager.cellSize);
                            Vector3 left = q * Quaternion.Euler(0, -160, 0) * Vector3.forward * (arrowScale * 0.25f * gridManager.cellSize);
                            Gizmos.DrawLine(to, to + right);
                            Gizmos.DrawLine(to, to + left);
                        }
                    }
                }
            }
    }
}
