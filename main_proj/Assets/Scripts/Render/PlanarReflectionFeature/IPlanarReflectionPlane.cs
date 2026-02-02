using UnityEngine;

/// <summary>
/// 平面反射平面接口
/// 定义反射平面需要提供的基本信息
/// </summary>
public interface IPlanarReflectionPlane
{
    /// <summary>
    /// 获取反射平面的世界空间位置
    /// </summary>
    Vector3 GetPlanePosition();
    
    /// <summary>
    /// 获取反射平面的世界空间法线
    /// </summary>
    Vector3 GetPlaneNormal();
    
    /// <summary>
    /// 获取平面的垂直偏移
    /// </summary>
    float GetPlaneOffset();
    
    /// <summary>
    /// 组件的 Transform
    /// </summary>
    Transform transform { get; }
    
    /// <summary>
    /// 组件是否启用
    /// </summary>
    bool enabled { get; }
    
    /// <summary>
    /// 组件所在的 GameObject
    /// </summary>
    GameObject gameObject { get; }
}
