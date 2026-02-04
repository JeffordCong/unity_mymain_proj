using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

// [ExecuteAlways]
public class PlanarReflections : MonoBehaviour
{
    [System.Serializable]
    public enum ResolutionMulltiplier
    {
        Full,
        Half,
        Third,
        Quarter
    }

    [System.Serializable]
    public enum BlurDownsample
    {
        None = 1,
        Half = 2,
        Quarter = 4
    }

    [System.Serializable]
    public class PlanarReflectionSettings
    {
        [Header("Reflection")]
        [Tooltip("Render scale multiplier for the reflection texture.")]
        public ResolutionMulltiplier m_ResolutionMultiplier = ResolutionMulltiplier.Third;
        [Tooltip("Offset applied to the reflection clip plane to reduce artifacts.")]
        public float m_ClipPlaneOffset = 0.07f;
        [Tooltip("Layers that are rendered into the reflection.")]
        public LayerMask m_ReflectLayers = -1;
        [Tooltip("Render shadows in the reflection camera.")]
        public bool m_shadows;

        [Header("Performance")]
        [Tooltip("Render once every N frames. 1 = every frame.")]
        [Min(1)]
        public int _frameInterval = 1;
        [Tooltip("Skip rendering when the camera is static.")]
        public bool _skipWhenCameraStatic = true;
        [Tooltip("World-space movement threshold to detect camera motion.")]
        [Range(0.0f, 1.0f)]
        public float _cameraMoveThreshold = 0.01f;
        [Tooltip("Rotation threshold (degrees) to detect camera motion.")]
        [Range(0.0f, 5.0f)]
        public float _cameraRotateThreshold = 0.1f;

        [Header("模糊")]
        public bool _blurOn = true;

        [Range(0.0f, 5.0f)]
        public float _blurSize = 0;

        [Range(0, 10)]
        public int _blurIterations = 4;

        public BlurDownsample _downsample = BlurDownsample.Half;
    }


    [SerializeField]
    public PlanarReflectionSettings m_settings = new PlanarReflectionSettings();

    [FormerlySerializedAs("camOffset")] public float m_planeOffset;

    private static Camera m_ReflectionCamera;
    private Vector2Int m_TextureSize = new Vector2Int(256, 128);
    private RenderTexture m_ReflectionTexture = null;
    private RenderTexture m_BlurReflectionTexture = null;
    private static readonly int planarReflectionTextureID = Shader.PropertyToID("_PlanarReflectionTexture");
    private static readonly int blurOffsetID = Shader.PropertyToID("_BlurOffset");
    private static readonly int tempBlur1ID = Shader.PropertyToID("_Temp1");
    private static readonly int tempBlur2ID = Shader.PropertyToID("_Temp2");

    private ResolutionMulltiplier m_OldRes;
    //模糊shader
    const string k_BlurShader = "Hidden/KawaseBlur";
    private Material _blurMaterial;
    private bool m_BlurShaderMissingLogged;
    private Vector3 m_LastCameraPosition;
    private Quaternion m_LastCameraRotation;
    private bool m_HasLastCameraPose;

    private void Awake()
    {

    }
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += ExecuteBeforeCameraRender;
    }

    // Cleanup all the objects we possibly have created
    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    void Cleanup()
    {
        RenderPipelineManager.beginCameraRendering -= ExecuteBeforeCameraRender;

        if (m_ReflectionCamera)
        {
            m_ReflectionCamera.targetTexture = null;
            SafeDestroy(m_ReflectionCamera.gameObject);
        }
        if (m_ReflectionTexture)
        {
            RenderTexture.ReleaseTemporary(m_ReflectionTexture);
        }
        if (m_BlurReflectionTexture)
        {
            RenderTexture.ReleaseTemporary(m_BlurReflectionTexture);
        }
        SafeDestroy(_blurMaterial);
    }

    void SafeDestroy(Object obj)
    {
        if (Application.isEditor)
        {
            DestroyImmediate(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    private void UpdateCamera(Camera src, Camera dest)
    {
        if (dest == null)
            return;
        dest.CopyFrom(src);
        dest.cameraType = CameraType.Game;
        dest.useOcclusionCulling = false;
    }

    private void UpdateReflectionCamera(Camera realCamera)
    {
        if (m_ReflectionCamera == null)
            m_ReflectionCamera = CreateMirrorObjects(realCamera);

        // find out the reflection plane: position and normal in world space
        Vector3 normal = transform.up;
        Vector3 pos = transform.position + normal * m_planeOffset;

        UpdateCamera(realCamera, m_ReflectionCamera);

        // Render reflection
        // Reflect camera around reflection plane
        float d = -Vector3.Dot(normal, pos) - m_settings.m_ClipPlaneOffset;
        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.identity;
        CalculateReflectionMatrix(ref reflection, reflectionPlane);
        Vector3 newpos = reflection.MultiplyPoint(realCamera.transform.position);
        Vector3 newForward = reflection.MultiplyVector(realCamera.transform.forward);
        Vector3 newUp = reflection.MultiplyVector(realCamera.transform.up);
        m_ReflectionCamera.transform.SetPositionAndRotation(newpos, Quaternion.LookRotation(newForward, newUp));
        m_ReflectionCamera.worldToCameraMatrix = realCamera.worldToCameraMatrix * reflection;

        // Setup oblique projection matrix so that near plane is our reflection
        // plane. This way we clip everything below/above it for free.
        Vector4 clipPlane = CameraSpacePlane(m_ReflectionCamera, pos - normal * 0.1f, normal, 1.0f);
        Matrix4x4 projection = realCamera.CalculateObliqueMatrix(clipPlane);
        m_ReflectionCamera.projectionMatrix = projection;
        m_ReflectionCamera.cullingMask = m_settings.m_ReflectLayers; // never render water layer
    }

    // Calculates reflection matrix around the given plane
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

    private float GetScaleValue()
    {
        switch (m_settings.m_ResolutionMultiplier)
        {
            case ResolutionMulltiplier.Full:
                return 1f;
            case ResolutionMulltiplier.Half:
                return 0.5f;
            case ResolutionMulltiplier.Third:
                return 0.33f;
            case ResolutionMulltiplier.Quarter:
                return 0.25f;
        }
        return 0.5f; // default to half res
    }

    private bool HasCameraMoved(Camera camera)
    {
        if (!m_HasLastCameraPose)
            return true;

        float moveThreshold = Mathf.Max(0.0f, m_settings._cameraMoveThreshold);
        float rotateThreshold = Mathf.Max(0.0f, m_settings._cameraRotateThreshold);

        if ((camera.transform.position - m_LastCameraPosition).sqrMagnitude > moveThreshold * moveThreshold)
            return true;

        if (Quaternion.Angle(camera.transform.rotation, m_LastCameraRotation) > rotateThreshold)
            return true;

        return false;
    }

    private void CacheCameraPose(Camera camera)
    {
        m_LastCameraPosition = camera.transform.position;
        m_LastCameraRotation = camera.transform.rotation;
        m_HasLastCameraPose = true;
    }

    // Compare two int2
    private static bool Int2Compare(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x && a.y == b.y)
            return true;
        else
            return false;
    }

    // Given position/normal of the plane, calculates plane in camera space.
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * m_settings.m_ClipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    // 创建镜像相机
    private Camera CreateMirrorObjects(Camera currentCamera)
    {
        GameObject go =
            new GameObject($"Planar Refl Camera id{GetInstanceID().ToString()} for {currentCamera.GetInstanceID().ToString()}",
                typeof(Camera));
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData lwrpCamData =
            go.AddComponent(typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)) as UnityEngine.Rendering.Universal.UniversalAdditionalCameraData;
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData lwrpCamDataCurrent = currentCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        lwrpCamData.renderShadows = false; // turn off shadows for the reflection camera
        lwrpCamData.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
        lwrpCamData.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
        var reflectionCamera = go.GetComponent<Camera>();
        reflectionCamera.transform.SetPositionAndRotation(transform.position, transform.rotation);
        //reflectionCamera.targetTexture = m_ReflectionTexture;
        reflectionCamera.allowMSAA = false;
        reflectionCamera.depth = -10;
        reflectionCamera.enabled = false;
        reflectionCamera.allowHDR = currentCamera.allowHDR;
        go.hideFlags = HideFlags.HideAndDontSave;

        return reflectionCamera;
    }

    private Vector2Int ReflectionResolution(Camera cam, float scale)
    {
        var x = (int)(cam.pixelWidth * scale * GetScaleValue());
        var y = (int)(cam.pixelHeight * scale * GetScaleValue());
        return new Vector2Int(x, y);
    }

    public void ExecuteBeforeCameraRender(ScriptableRenderContext context, Camera camera)
    {

        if (!enabled)
            return;
        if (camera == null || camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
            return;
        // 只对主相机渲染反射
        if (!camera.CompareTag("MainCamera"))
            return;
        if (camera.pixelWidth <= 0 || camera.pixelHeight <= 0)
            return;
#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
            return;
#endif
        if (m_ReflectionCamera != null && camera == m_ReflectionCamera)
            return;

        bool cameraMoved = HasCameraMoved(camera);
        if (m_settings._frameInterval > 1)
        {
            if (!cameraMoved && (Time.frameCount % m_settings._frameInterval != 0))
                return;
        }
        else if (m_settings._skipWhenCameraStatic && !cameraMoved)
        {
            return;
        }

        bool blurEnabled = m_settings._blurOn;
        if (blurEnabled && _blurMaterial == null)
        {
            var blurShader = Shader.Find(k_BlurShader);
            if (blurShader == null)
            {
                if (!m_BlurShaderMissingLogged)
                {
                    Debug.LogError("Reflection Not Find Blur Shader");
                    m_BlurShaderMissingLogged = true;
                }
                blurEnabled = false;
            }
            else
            {
                _blurMaterial = CoreUtils.CreateEngineMaterial(blurShader);
            }
        }

        GL.invertCulling = true;
        var oldFog = RenderSettings.fog;
        RenderSettings.fog = false;
        var max = QualitySettings.maximumLODLevel;
        var bias = QualitySettings.lodBias;
        QualitySettings.maximumLODLevel = 1;
        QualitySettings.lodBias = bias * 0.5f;

        UpdateReflectionCamera(camera);
        m_ReflectionCamera.cameraType = CameraType.Reflection;

        var res = ReflectionResolution(camera, UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset.renderScale);
        if (m_OldRes != m_settings.m_ResolutionMultiplier)
        {
            m_OldRes = m_settings.m_ResolutionMultiplier;
            if (m_ReflectionTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_ReflectionTexture);
                m_ReflectionTexture = null;
            }
        }
        if (m_ReflectionTexture == null || !Int2Compare(m_TextureSize, res))
        {
            bool useHDR10 = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float);
            RenderTextureFormat hdrFormat =
                useHDR10 ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
            m_ReflectionTexture = RenderTexture.GetTemporary(res.x, res.y, 16,
                GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
            m_ReflectionTexture.useMipMap = true;
            m_ReflectionTexture.autoGenerateMips = true;
            m_ReflectionTexture.name = "_PlanarReflectionTexture";
            m_TextureSize = res;
        }

        m_ReflectionCamera.targetTexture = m_ReflectionTexture;

        if (!m_ReflectionCamera.orthographic)
            UnityEngine.Rendering.Universal.UniversalRenderPipeline.RenderSingleCamera(context, m_ReflectionCamera);

        GL.invertCulling = false;
        RenderSettings.fog = oldFog;
        QualitySettings.maximumLODLevel = max;
        QualitySettings.lodBias = bias;

        Debug.LogError($"[PlanarReflections] 反射纹理已生成 - Camera: {camera.name}, Size: {m_ReflectionTexture.width}x{m_ReflectionTexture.height}, Frame: {Time.frameCount}");

        //模糊
        if (blurEnabled && _blurMaterial != null)
        {
            var sourceDesc = m_ReflectionTexture.descriptor;
            sourceDesc.msaaSamples = 1;
            sourceDesc.depthBufferBits = 0;
            var sourceID = new RenderTargetIdentifier(m_ReflectionTexture);
            if (m_BlurReflectionTexture == null ||
                m_BlurReflectionTexture.width != sourceDesc.width ||
                m_BlurReflectionTexture.height != sourceDesc.height ||
                m_BlurReflectionTexture.graphicsFormat != sourceDesc.graphicsFormat)
            {
                if (m_BlurReflectionTexture != null)
                    RenderTexture.ReleaseTemporary(m_BlurReflectionTexture);
                m_BlurReflectionTexture = RenderTexture.GetTemporary(sourceDesc);
                m_BlurReflectionTexture.name = "Blur ReflectionTex";
            }
            var buf = CommandBufferPool.Get("Blur Reflection");
            float width = sourceDesc.width;
            float height = sourceDesc.height;
            sourceDesc.width = Mathf.RoundToInt(width / (int)m_settings._downsample);
            sourceDesc.height = Mathf.RoundToInt(height / (int)m_settings._downsample);

            int blurredID = tempBlur1ID;
            int blurredID2 = tempBlur2ID;
            buf.GetTemporaryRT(blurredID, sourceDesc);
            buf.GetTemporaryRT(blurredID2, sourceDesc);

            buf.SetGlobalFloat(blurOffsetID, 1.0f + m_settings._blurSize);
            buf.Blit(sourceID, blurredID, _blurMaterial, 0);

            for (int i = 1; i < m_settings._blurIterations; i++)
            {
                float iterationOffs = (i * 1.0f);
                buf.SetGlobalFloat(blurOffsetID, iterationOffs + m_settings._blurSize);
                buf.Blit(blurredID, blurredID2, _blurMaterial, 0);

                // pingpong
                var rttmp = blurredID;
                blurredID = blurredID2;
                blurredID2 = rttmp;
            }

            buf.SetGlobalFloat(blurOffsetID, m_settings._blurIterations + m_settings._blurSize);
            buf.Blit(blurredID, m_BlurReflectionTexture, _blurMaterial, 0);

            Shader.SetGlobalTexture(planarReflectionTextureID, m_BlurReflectionTexture);
            buf.ReleaseTemporaryRT(blurredID);
            buf.ReleaseTemporaryRT(blurredID2);

            context.ExecuteCommandBuffer(buf);
            CommandBufferPool.Release(buf);
        }
        else
        {
            Shader.SetGlobalTexture(planarReflectionTextureID, m_ReflectionTexture);
        }

        CacheCameraPose(camera);
    }
}
