using UnityEngine;

/// <summary>
/// PlanarReflections 适配器
/// 将原有的 PlanarReflections 组件适配为 IPlanarReflectionPlane 接口
/// 无需修改原脚本，通过适配器模式桥接新旧代码
/// </summary>
[RequireComponent(typeof(PlanarReflections))]
public class PlanarReflectionsAdapter : MonoBehaviour, IPlanarReflectionPlane
{
    private PlanarReflections planarReflections;

    private void Awake()
    {
        planarReflections = GetComponent<PlanarReflections>();

        if (planarReflections == null)
        {
            Debug.LogError($"[PlanarReflectionsAdapter] 未找到 PlanarReflections 组件: {gameObject.name}");
        }
    }

    /// <summary>
    /// 获取反射平面的世界空间位置
    /// </summary>
    public Vector3 GetPlanePosition()
    {
        if (planarReflections == null)
            return transform.position;

        // 如果 target 存在，使用 target 的位置，否则使用自身 transform
        Transform refTransform = planarReflections.target != null
            ? planarReflections.target.transform
            : transform;

        return refTransform.position + Vector3.up * planarReflections.m_planeOffset;
    }

    /// <summary>
    /// 获取反射平面的世界空间法线
    /// </summary>
    public Vector3 GetPlaneNormal()
    {
        if (planarReflections == null)
            return transform.up;

        // 如果 target 存在，使用 target 的朝向，否则使用自身 transform
        Transform refTransform = planarReflections.target != null
            ? planarReflections.target.transform
            : transform;

        return refTransform.up;
    }

    /// <summary>
    /// 获取平面的垂直偏移
    /// </summary>
    public float GetPlaneOffset()
    {
        return planarReflections != null ? planarReflections.m_planeOffset : 0f;
    }
}
