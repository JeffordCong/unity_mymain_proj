using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// 平面反射渲染 Pass
/// 负责查找场景中的反射平面，渲染反射纹理并应用模糊
/// </summary>
public class PlanarReflectionPass : ScriptableRenderPass
{
    private const string k_BlurShader = "Hidden/KawaseBlur";
    private const string k_ProfilerTag = "Planar Reflection";

    private PlanarReflectionFeature.Settings settings;
    private Material blurMaterial;

    // 反射相机管理
    private Camera reflectionCamera;
    private RenderTexture reflectionTexture;
    private RenderTexture blurReflectionTexture;

    private int planarReflectionTextureID = Shader.PropertyToID("_PlanarReflectionTexture");
    private PlanarReflections.ResolutionMulltiplier oldResolution;



    public PlanarReflectionPass(PlanarReflectionFeature.Settings settings)
    {
        this.settings = settings;

        // 创建模糊材质
        var blurShader = Shader.Find(k_BlurShader);
        if (blurShader != null)
        {
            blurMaterial = CoreUtils.CreateEngineMaterial(blurShader);
        }
        else
        {
            Debug.LogWarning($"[PlanarReflection] 未找到模糊 Shader: {k_BlurShader}");
        }
    }

    /// <summary>
    /// 在主相机渲染前执行，渲染反射纹理
    /// </summary>
    public void ExecutePreRender(ScriptableRenderContext context, Camera mainCamera)
    {
        // 避免递归渲染
        if (mainCamera.cameraType == CameraType.Reflection || mainCamera.cameraType == CameraType.Preview)
            return;

        // 使用局部列表避免状态污染
        var reflectionPlanes = new List<IPlanarReflectionPlane>();
        CollectReflectionPlanes(reflectionPlanes);

        if (reflectionPlanes.Count == 0)
            return;

        // 目前只支持第一个反射平面
        var plane = reflectionPlanes[0];

        // 更新并渲染反射相机
        UpdateReflectionCamera(mainCamera, plane);
        RenderReflection(context, mainCamera);
    }

    /// <summary>
    /// Execute 仅负责将纹理设置到全局 Shader 属性，并执行模糊后处理
    /// </summary>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 如果没有生成反射纹理，则不执行
        if (reflectionTexture == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get(k_ProfilerTag);

        try
        {
            // 应用模糊（如果启用）
            if (settings.blurEnabled && blurMaterial != null)
            {
                ApplyBlur(cmd);
                Shader.SetGlobalTexture(planarReflectionTextureID, blurReflectionTexture);
            }
            else
            {
                Shader.SetGlobalTexture(planarReflectionTextureID, reflectionTexture);
            }
        }
        finally
        {
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    /// <summary>
    /// 收集场景中所有启用的反射平面
    /// 仅查找实现了 IPlanarReflectionPlane 接口的组件
    /// </summary>
    private void CollectReflectionPlanes(List<IPlanarReflectionPlane> targetList)
    {
        targetList.Clear();

        // 1. 查找新的独立组件 (推荐)
        var newPlanes = Object.FindObjectsOfType<PlanarReflectionPlane>();
        foreach (var plane in newPlanes)
        {
            if (plane.enabled && plane.gameObject.activeInHierarchy)
            {
                targetList.Add(plane);
            }
        }

        // 2. 查找适配器组件 (兼容旧版)
        var adapters = Object.FindObjectsOfType<PlanarReflectionsAdapter>();
        foreach (var adapter in adapters)
        {
            if (adapter.enabled && adapter.gameObject.activeInHierarchy)
            {
                targetList.Add(adapter);
            }
        }
    }

    /// <summary>
    /// 更新反射相机的位置和参数
    /// </summary>
    private void UpdateReflectionCamera(Camera mainCamera, IPlanarReflectionPlane plane)
    {
        // 创建反射相机（如果不存在）
        if (reflectionCamera == null)
        {
            reflectionCamera = CreateReflectionCamera(mainCamera);
        }

        // 复制相机设置
        reflectionCamera.CopyFrom(mainCamera);
        reflectionCamera.cameraType = CameraType.Reflection;
        reflectionCamera.useOcclusionCulling = false;
        reflectionCamera.cullingMask = settings.reflectLayers;
        reflectionCamera.depth = -10;
        reflectionCamera.enabled = false;

        // 获取反射平面信息
        Vector3 planePos = plane.GetPlanePosition();
        Vector3 planeNormal = plane.GetPlaneNormal();

        // 计算反射矩阵
        float d = -Vector3.Dot(planeNormal, planePos) - settings.clipPlaneOffset;
        Vector4 reflectionPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);

        Matrix4x4 reflection = Matrix4x4.identity;
        reflection *= Matrix4x4.Scale(new Vector3(1, -1, 1));
        CalculateReflectionMatrix(ref reflection, reflectionPlane);

        // 设置反射相机位置
        Vector3 oldPos = mainCamera.transform.position - new Vector3(0, planePos.y * 2, 0);
        Vector3 newPos = new Vector3(oldPos.x, -oldPos.y, oldPos.z);

        reflectionCamera.transform.position = newPos;
        reflectionCamera.transform.forward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, -1, 1));
        reflectionCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * reflection;

        // 设置斜投影矩阵（裁剪反射平面下方的物体）
        Vector4 clipPlane = CameraSpacePlane(reflectionCamera, planePos - Vector3.up * 0.1f, planeNormal, 1.0f);
        Matrix4x4 projection = mainCamera.CalculateObliqueMatrix(clipPlane);
        reflectionCamera.projectionMatrix = projection;
    }

    /// <summary>
    /// 渲染反射纹理
    /// </summary>
    private void RenderReflection(ScriptableRenderContext context, Camera mainCamera)
    {
        // 计算反射纹理分辨率
        var resolution = CalculateResolution(mainCamera);

        // 检查是否需要重建纹理
        if (oldResolution != settings.resolutionMultiplier || reflectionTexture == null)
        {
            oldResolution = settings.resolutionMultiplier;

            if (reflectionTexture != null)
            {
                RenderTexture.ReleaseTemporary(reflectionTexture);
            }

            bool useHDR10 = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float);
            RenderTextureFormat hdrFormat = useHDR10 ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;

            reflectionTexture = RenderTexture.GetTemporary(
                resolution.x, resolution.y, 16,
                GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true)
            );
            reflectionTexture.useMipMap = true;
            reflectionTexture.autoGenerateMips = true;
            reflectionTexture.name = "_PlanarReflectionTexture";
        }

        reflectionCamera.targetTexture = reflectionTexture;

        // 降低质量设置（提升性能）
        GL.invertCulling = true;
        var oldFog = RenderSettings.fog;
        var oldMaxLOD = QualitySettings.maximumLODLevel;
        var oldLODBias = QualitySettings.lodBias;

        RenderSettings.fog = false;
        QualitySettings.maximumLODLevel = 1;
        QualitySettings.lodBias = oldLODBias * 0.5f;

        // 渲染反射相机
        if (!reflectionCamera.orthographic)
        {
            UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
        }

        // 恢复设置
        GL.invertCulling = false;
        RenderSettings.fog = oldFog;
        QualitySettings.maximumLODLevel = oldMaxLOD;
        QualitySettings.lodBias = oldLODBias;
    }

    /// <summary>
    /// 应用 Kawase 模糊
    /// </summary>
    private void ApplyBlur(CommandBuffer cmd)
    {
        var sourceDesc = reflectionTexture.descriptor;
        sourceDesc.msaaSamples = 1;
        sourceDesc.depthBufferBits = 0;
        sourceDesc.width = Mathf.RoundToInt(sourceDesc.width / settings.downsample);
        sourceDesc.height = Mathf.RoundToInt(sourceDesc.height / settings.downsample);

        // 创建模糊目标纹理
        if (blurReflectionTexture == null)
        {
            blurReflectionTexture = RenderTexture.GetTemporary(sourceDesc);
            blurReflectionTexture.name = "Blur ReflectionTex";
        }

        int blurredID = Shader.PropertyToID("_Temp1");
        int blurredID2 = Shader.PropertyToID("_Temp2");
        cmd.GetTemporaryRT(blurredID, sourceDesc);
        cmd.GetTemporaryRT(blurredID2, sourceDesc);

        var sourceID = new RenderTargetIdentifier(reflectionTexture);

        // 第一次模糊
        cmd.SetGlobalFloat("_BlurOffset", 1.0f + settings.blurSize);
        cmd.Blit(sourceID, blurredID, blurMaterial, 0);

        // 迭代模糊
        for (int i = 1; i < settings.blurIterations; i++)
        {
            float iterationOffs = i * 1.0f;
            cmd.SetGlobalFloat("_BlurOffset", iterationOffs + settings.blurSize);
            cmd.Blit(blurredID, blurredID2, blurMaterial, 0);

            // Ping-pong 交换缓冲区
            var temp = blurredID;
            blurredID = blurredID2;
            blurredID2 = temp;
        }

        // 最后一次模糊到目标纹理
        cmd.SetGlobalFloat("_BlurOffset", settings.blurIterations + settings.blurSize);
        cmd.Blit(blurredID, blurReflectionTexture, blurMaterial, 0);

        cmd.ReleaseTemporaryRT(blurredID);
        cmd.ReleaseTemporaryRT(blurredID2);
    }

    /// <summary>
    /// 计算反射纹理分辨率
    /// </summary>
    private Vector2Int CalculateResolution(Camera camera)
    {
        float scale = GetResolutionScale();
        var renderScale = UniversalRenderPipeline.asset.renderScale;

        int x = (int)(camera.pixelWidth * renderScale * scale);
        int y = (int)(camera.pixelHeight * renderScale * scale);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 获取分辨率缩放系数
    /// </summary>
    private float GetResolutionScale()
    {
        switch (settings.resolutionMultiplier)
        {
            case PlanarReflections.ResolutionMulltiplier.Full: return 1f;
            case PlanarReflections.ResolutionMulltiplier.Half: return 0.5f;
            case PlanarReflections.ResolutionMulltiplier.Third: return 0.33f;
            case PlanarReflections.ResolutionMulltiplier.Quarter: return 0.25f;
            default: return 0.5f;
        }
    }

    /// <summary>
    /// 创建反射相机
    /// </summary>
    private Camera CreateReflectionCamera(Camera mainCamera)
    {
        var go = new GameObject($"Planar Reflection Camera");
        go.hideFlags = HideFlags.HideAndDontSave;

        var cam = go.AddComponent<Camera>();

        // 添加 URP 相机数据
        var urpCamData = go.AddComponent<UniversalAdditionalCameraData>();
        urpCamData.renderShadows = settings.renderShadows;
        urpCamData.requiresColorOption = CameraOverrideOption.Off;
        urpCamData.requiresDepthOption = CameraOverrideOption.Off;

        cam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        cam.allowMSAA = mainCamera.allowMSAA;
        cam.allowHDR = mainCamera.allowHDR;
        cam.depth = -10;
        cam.enabled = false;

        return cam;
    }

    /// <summary>
    /// 计算反射矩阵
    /// </summary>
    private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }

    /// <summary>
    /// 计算相机空间平面
    /// </summary>
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * settings.clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        if (reflectionCamera != null)
        {
            reflectionCamera.targetTexture = null;
            if (Application.isPlaying)
                Object.Destroy(reflectionCamera.gameObject);
            else
                Object.DestroyImmediate(reflectionCamera.gameObject);
        }

        if (reflectionTexture != null)
        {
            RenderTexture.ReleaseTemporary(reflectionTexture);
            reflectionTexture = null;
        }

        if (blurReflectionTexture != null)
        {
            RenderTexture.ReleaseTemporary(blurReflectionTexture);
            blurReflectionTexture = null;
        }

        if (blurMaterial != null)
        {
            if (Application.isPlaying)
                Object.Destroy(blurMaterial);
            else
                Object.DestroyImmediate(blurMaterial);
        }
    }
}
