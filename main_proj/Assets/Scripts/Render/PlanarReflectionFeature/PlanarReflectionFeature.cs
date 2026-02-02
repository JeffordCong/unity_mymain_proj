using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP 平面反射渲染特性
/// 在 URP Renderer Asset 中添加此 Feature 以启用平面反射功能
/// </summary>
public class PlanarReflectionFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("渲染设置")]
        [Tooltip("反射纹理分辨率倍数")]
        public PlanarReflections.ResolutionMulltiplier resolutionMultiplier = PlanarReflections.ResolutionMulltiplier.Third;

        [Tooltip("裁剪平面偏移")]
        public float clipPlaneOffset = 0.07f;

        [Tooltip("反射层级遮罩")]
        public LayerMask reflectLayers = -1;

        [Tooltip("是否渲染阴影")]
        public bool renderShadows = false;

        [Header("模糊设置")]
        [Tooltip("启用模糊")]
        public bool blurEnabled = true;

        [Range(0.0f, 5.0f)]
        [Tooltip("模糊强度")]
        public float blurSize = 0f;

        [Range(0, 10)]
        [Tooltip("模糊迭代次数")]
        public int blurIterations = 4;

        [Range(1.0f, 4.0f)]
        [Tooltip("降采样比例")]
        public float downsample = 1f;

        [Header("执行时机")]
        [Tooltip("在哪个渲染阶段执行反射")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }

    public Settings settings = new Settings();
    private PlanarReflectionPass reflectionPass;

    /// <summary>
    /// 创建 Pass（初始化时调用一次）
    /// </summary>
    public override void Create()
    {
        reflectionPass = new PlanarReflectionPass(settings)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // 仅在游戏相机或场景相机渲染前执行
        if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
        {
            if (reflectionPass != null)
            {
                reflectionPass.ExecutePreRender(context, camera);
            }
        }
    }

    /// <summary>
    /// 添加渲染 Pass 到渲染队列
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 只在游戏相机和场景相机中执行
        if (renderingData.cameraData.cameraType == CameraType.Game ||
            renderingData.cameraData.cameraType == CameraType.SceneView)
        {
            renderer.EnqueuePass(reflectionPass);
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        reflectionPass?.Dispose();
    }
}
