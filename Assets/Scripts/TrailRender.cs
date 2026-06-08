using UnityEngine;
using System.Collections.Generic;

public class TrailRender : MonoBehaviour
{
    public Color trailColor = Color.white; // 轨迹颜色
    public float lineWidth = 1f; // 线条宽度
    public float minDistance = 0.5f; // 添加点的最小距离
    public int maxPoints = 2000; // 最大点数，防止内存溢出

    private LineRenderer lineRenderer;
    private List<Vector3> positions = new List<Vector3>();
    private Vector3 lastRecordedPosition;

    void Start()
    {
        // 初始化LineRenderer组件
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = trailColor;
        lineRenderer.endColor = trailColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;

        // 记录初始位置
        lastRecordedPosition = new Vector3(transform.position.x, 10, transform.position.z);
        positions.Add(lastRecordedPosition);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, lastRecordedPosition);
    }

    void Update()
    {
        // 计算在XZ平面上的移动距离（忽略Y轴变化）
        Vector3 currentPos = new Vector3(transform.position.x, 10, transform.position.z);
        Vector3 lastPosOnPlane = new Vector3(lastRecordedPosition.x, 10, lastRecordedPosition.z); // 保持当前Y，但使用上次的XZ
        float distanceInPlane = Vector3.Distance(
            new Vector3(currentPos.x, 0, currentPos.z),
            new Vector3(lastRecordedPosition.x, 0, lastRecordedPosition.z)
        );

        // 如果在平面上移动了足够的距离，则添加新点
        if (distanceInPlane >= minDistance)
        {
            AddTrailPoint(currentPos);
            lastRecordedPosition = currentPos;
        }
    }

    void AddTrailPoint(Vector3 newPosition)
    {
        positions.Add(newPosition);

        // 控制最大点数
        if (positions.Count > maxPoints)
        {
            positions.RemoveAt(0); // 移除最早的点
            // 重新设置所有点的位置，因为索引发生了变化
            for (int i = 0; i < positions.Count; i++)
            {
                lineRenderer.SetPosition(i, positions[i]);
            }
        }
        else
        {
            // 添加新点
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPosition(positions.Count - 1, newPosition);
        }
    }

    // 清除轨迹
    public void ClearTrail()
    {
        positions.Clear();
        lineRenderer.positionCount = 0;
        lastRecordedPosition = transform.position;
        positions.Add(lastRecordedPosition);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, lastRecordedPosition);
    }

    // 更改轨迹颜色
    public void SetTrailColor(Color newColor)
    {
        trailColor = newColor;
        lineRenderer.startColor = newColor;
        lineRenderer.endColor = newColor;
    }
}
