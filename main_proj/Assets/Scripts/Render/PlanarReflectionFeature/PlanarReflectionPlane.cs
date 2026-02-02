using UnityEngine;

/// <summary>
/// 平面反射平面组件
/// 标记场景中的反射平面，完全独立的新实现
/// </summary>
[ExecuteAlways]
public class PlanarReflectionPlane : MonoBehaviour, IPlanarReflectionPlane
{
    [Header("反射平面设置")]
    [Tooltip("平面垂直偏移")]
    public float planeOffset = 0f;
    
    [Header("可选参数")]
    [Tooltip("可选的参考平面对象，如果为空则使用自身 Transform")]
    public Transform referencePlane;
    
    /// <summary>
    /// 获取反射平面的世界空间位置
    /// </summary>
    public Vector3 GetPlanePosition()
    {
        Transform refTransform = referencePlane != null ? referencePlane : transform;
        return refTransform.position + refTransform.up * planeOffset;
    }
    
    /// <summary>
    /// 获取反射平面的世界空间法线
    /// </summary>
    public Vector3 GetPlaneNormal()
    {
        Transform refTransform = referencePlane != null ? referencePlane : transform;
        return refTransform.up;
    }
    
    /// <summary>
    /// 获取平面的垂直偏移
    /// </summary>
    public float GetPlaneOffset()
    {
        return planeOffset;
    }
    
    // 可视化调试
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = GetPlanePosition();
        Vector3 normal = GetPlaneNormal();
        
        // 绘制反射平面
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Vector3 right = Vector3.Cross(normal, Vector3.forward);
        if (right.magnitude < 0.1f)
            right = Vector3.Cross(normal, Vector3.up);
        right = right.normalized;
        Vector3 forward = Vector3.Cross(right, normal).normalized;
        
        float size = 5f;
        Vector3[] corners = new Vector3[4]
        {
            pos + right * size + forward * size,
            pos - right * size + forward * size,
            pos - right * size - forward * size,
            pos + right * size - forward * size
        };
        
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
        
        // 绘制法线
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(pos, normal * 2f);
    }
}
