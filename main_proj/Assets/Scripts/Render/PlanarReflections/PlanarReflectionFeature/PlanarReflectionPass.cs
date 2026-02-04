using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace Render.PlanarReflectionFeature
{
    public class PlanarReflectionPass : ScriptableRenderPass
    {
        private const string k_BlurShader = "Hidden/KawaseBlur";
        private const string k_ProfilerTag = "Planar Reflection";
        private const string k_PlanarReflectionKeyword = "_PLANER_REFLECTION_ON";

        private PlanarReflectionFeature.Settings settings;
        private Material blurMaterial;

        private Camera reflectionCamera;
        private RenderTexture reflectionTexture;
        private RenderTexture blurReflectionTexture;

        // 预缓存的 Shader 属性 ID
        private static readonly int s_PlanarReflectionTextureID = Shader.PropertyToID("_PlanarReflectionTexture");
        private static readonly int s_BlurOffsetID = Shader.PropertyToID("_BlurOffset");
        private static readonly int s_TempRT1ID = Shader.PropertyToID("_PlanarReflection_Temp1");
        private static readonly int s_TempRT2ID = Shader.PropertyToID("_PlanarReflection_Temp2");

        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_ProfilerTag);

        private Vector3 lastCameraPosition;
        private Quaternion lastCameraRotation;
        private bool hasLastCameraPose;
        private const float cameraMoveThreshold = 0.01f;
        private const float cameraRotateThreshold = 0.1f;



        public PlanarReflectionPass(PlanarReflectionFeature.Settings settings)
        {
            this.settings = settings;

            var blurShader = Shader.Find(k_BlurShader);
            if (blurShader != null)
            {
                blurMaterial = CoreUtils.CreateEngineMaterial(blurShader);
            }
            else
            {
                Debug.LogWarning($"[PlanarReflection] Missing Shader: {k_BlurShader}");
            }
        }


        private List<PlanarReflectionPlane> reflectionPlanes = new List<PlanarReflectionPlane>();

        public void ExecutePreRender(ScriptableRenderContext context, Camera mainCamera)
        {
            UnityEngine.Profiling.Profiler.BeginSample("PlanarReflection.ExecutePreRender");

            if (mainCamera.cameraType == CameraType.Reflection || mainCamera.cameraType == CameraType.Preview)
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            var volume = VolumeManager.instance.stack.GetComponent<PlanarReflectionVolume>();
            if (volume == null || !volume.IsActive())
            {
                Shader.DisableKeyword(k_PlanarReflectionKeyword);
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            // 帧间隔跳过但相机未移动时，保持使用之前的反射纹理，不禁用关键字
            if (volume.frameInterval.value > 1 && Time.frameCount % volume.frameInterval.value != 0 && !HasCameraMoved(mainCamera))
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            CollectReflectionPlanes(reflectionPlanes);

            if (reflectionPlanes.Count == 0)
            {
                Shader.DisableKeyword(k_PlanarReflectionKeyword);
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            var plane = reflectionPlanes[0];

            if (!IsPlaneVisible(mainCamera, plane))
            {
                Shader.DisableKeyword(k_PlanarReflectionKeyword);
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            UpdateReflectionCamera(mainCamera, plane, volume);
            RenderReflection(context, mainCamera, volume);
            CacheCameraPose(mainCamera);

            // 反射渲染成功，启用平面反射关键字
            Shader.EnableKeyword(k_PlanarReflectionKeyword);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void CollectReflectionPlanes(List<PlanarReflectionPlane> targetList)
        {
            targetList.Clear();

            targetList.AddRange(PlanarReflectionPlane.ActivePlanes);
        }

        private bool IsPlaneVisible(Camera camera, PlanarReflectionPlane plane)
        {

            var renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), renderer.bounds);
            }

            return true;
        }

        private bool HasCameraMoved(Camera camera)
        {
            if (!hasLastCameraPose)
                return true;

            if ((camera.transform.position - lastCameraPosition).sqrMagnitude > cameraMoveThreshold * cameraMoveThreshold)
                return true;

            if (Quaternion.Angle(camera.transform.rotation, lastCameraRotation) > cameraRotateThreshold)
                return true;

            return false;
        }

        private void CacheCameraPose(Camera camera)
        {
            lastCameraPosition = camera.transform.position;
            lastCameraRotation = camera.transform.rotation;
            hasLastCameraPose = true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            if (reflectionTexture == null)
                return;

            var volume = VolumeManager.instance.stack.GetComponent<PlanarReflectionVolume>();
            if (volume == null || !volume.IsActive()) return;

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {

                if (volume.blurEnabled.value && blurMaterial != null)
                {
                    ApplyBlur(cmd, volume);
                    Shader.SetGlobalTexture(s_PlanarReflectionTextureID, blurReflectionTexture);
                }
                else
                {
                    Shader.SetGlobalTexture(s_PlanarReflectionTextureID, reflectionTexture);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void UpdateReflectionCamera(Camera mainCamera, PlanarReflectionPlane plane, PlanarReflectionVolume volume)
        {

            if (reflectionCamera == null)
                reflectionCamera = CreateReflectionCamera(mainCamera, volume.renderShadows.value);

            reflectionCamera.CopyFrom(mainCamera);
            reflectionCamera.farClipPlane = 800f;
            var cameraData = reflectionCamera.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;

            // reflectionCamera.backgroundColor = Color.black;
            reflectionCamera.cameraType = CameraType.Reflection;
            reflectionCamera.useOcclusionCulling = false;
            reflectionCamera.cullingMask = volume.reflectLayers.value;
            reflectionCamera.depth = -10;
            reflectionCamera.enabled = false;
            // reflectionCamera.clearFlags = CameraClearFlags.SolidColor;


            // 控制反射相机在 Hierarchy 中的可见性
            reflectionCamera.gameObject.hideFlags = volume.hideReflectionCamera.value
                ? HideFlags.HideAndDontSave
                : HideFlags.DontSave;

            Vector3 planePos = plane.GetPlanePosition();
            Vector3 planeNormal = plane.GetPlaneNormal().normalized;

            // 计算反射平面方程
            float d = -Vector3.Dot(planeNormal, planePos);
            Vector4 reflectionPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);

            // 计算反射矩阵 (与 PlanarReflectionManager 保持一致)
            Matrix4x4 reflection = Matrix4x4.identity;
            reflection *= Matrix4x4.Scale(new Vector3(1, -1, 1));
            CalculateReflectionMatrix(ref reflection, reflectionPlane);

            // 计算反射相机位置 (与 PlanarReflectionManager 保持一致)
            Vector3 oldPosition = mainCamera.transform.position - new Vector3(0, planePos.y * 2, 0);
            Vector3 newPosition = new Vector3(oldPosition.x, -oldPosition.y, oldPosition.z);
            reflectionCamera.transform.position = newPosition;
            reflectionCamera.transform.forward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, -1, 1));

            // 设置反射相机的 worldToCameraMatrix
            reflectionCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * reflection;

            // 计算相机空间中的裁剪平面 (用于斜投影，防止水下物体穿帮)
            Vector4 clipPlane = CameraSpacePlane(reflectionCamera, planePos - Vector3.up * 0.1f, planeNormal, 1.0f, volume.clipPlaneOffset.value);

            // 使用主相机计算斜投影矩阵 (与 PlanarReflectionManager 保持一致)
            reflectionCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(clipPlane);
        }

        private void RenderReflection(ScriptableRenderContext context, Camera mainCamera, PlanarReflectionVolume volume)
        {

            var resolution = CalculateResolution(mainCamera, volume);

            if (oldResolution != volume.resolutionMultiplier.value || reflectionTexture == null)
            {
                oldResolution = volume.resolutionMultiplier.value;

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

            GL.invertCulling = true;
            var oldFog = RenderSettings.fog;
            var oldMaxLOD = QualitySettings.maximumLODLevel;
            var oldLODBias = QualitySettings.lodBias;
            var oldShadowQuality = QualitySettings.shadows;
            var oldShadowDistance = QualitySettings.shadowDistance;

            RenderSettings.fog = false;
            QualitySettings.maximumLODLevel = 1;
            QualitySettings.lodBias = oldLODBias * 0.5f;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;

            if (!reflectionCamera.orthographic)
            {
#pragma warning disable 0618
                UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
#pragma warning restore 0618
            }

            GL.invertCulling = false;
            RenderSettings.fog = oldFog;
            QualitySettings.maximumLODLevel = oldMaxLOD;
            QualitySettings.lodBias = oldLODBias;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = oldShadowDistance;
        }

        private void ApplyBlur(CommandBuffer cmd, PlanarReflectionVolume volume)
        {
            var sourceDesc = reflectionTexture.descriptor;
            sourceDesc.msaaSamples = 1;
            sourceDesc.depthBufferBits = 0;
            sourceDesc.width = Mathf.RoundToInt(sourceDesc.width / volume.downsample.value);
            sourceDesc.height = Mathf.RoundToInt(sourceDesc.height / volume.downsample.value);

            if (blurReflectionTexture == null)
            {
                blurReflectionTexture = RenderTexture.GetTemporary(sourceDesc);
                blurReflectionTexture.name = "Blur ReflectionTex";
            }

            cmd.GetTemporaryRT(s_TempRT1ID, sourceDesc);
            cmd.GetTemporaryRT(s_TempRT2ID, sourceDesc);

            var sourceID = new RenderTargetIdentifier(reflectionTexture);
            int currentSrc = s_TempRT1ID;
            int currentDst = s_TempRT2ID;

            cmd.SetGlobalFloat(s_BlurOffsetID, 1.0f + volume.blurSize.value);
            cmd.Blit(sourceID, currentSrc, blurMaterial, 0);

            for (int i = 1; i < volume.blurIterations.value; i++)
            {
                float iterationOffs = i * 1.0f;
                cmd.SetGlobalFloat(s_BlurOffsetID, iterationOffs + volume.blurSize.value);
                cmd.Blit(currentSrc, currentDst, blurMaterial, 0);

                // 交换
                int temp = currentSrc;
                currentSrc = currentDst;
                currentDst = temp;
            }

            cmd.SetGlobalFloat(s_BlurOffsetID, volume.blurIterations.value + volume.blurSize.value);
            cmd.Blit(currentSrc, blurReflectionTexture, blurMaterial, 0);

            cmd.ReleaseTemporaryRT(s_TempRT1ID);
            cmd.ReleaseTemporaryRT(s_TempRT2ID);
        }

        private Vector2Int CalculateResolution(Camera camera, PlanarReflectionVolume volume)
        {
            float scale = GetResolutionScale(volume);
            var renderScale = UniversalRenderPipeline.asset.renderScale;

            int x = (int)(camera.pixelWidth * renderScale * scale);
            int y = (int)(camera.pixelHeight * renderScale * scale);

            return new Vector2Int(x, y);
        }

        private PlanarReflectionFeature.ResolutionMultiplier oldResolution;

        private float GetResolutionScale(PlanarReflectionVolume volume)
        {
            switch (volume.resolutionMultiplier.value)
            {
                case PlanarReflectionFeature.ResolutionMultiplier.Full: return 1f;
                case PlanarReflectionFeature.ResolutionMultiplier.Half: return 0.5f;
                case PlanarReflectionFeature.ResolutionMultiplier.Third: return 0.33f;
                case PlanarReflectionFeature.ResolutionMultiplier.Quarter: return 0.25f;
                default: return 0.5f;
            }
        }

        private Camera CreateReflectionCamera(Camera mainCamera, bool renderShadows)
        {
            var go = new GameObject($"Planar Reflection Camera");
            go.hideFlags = HideFlags.HideAndDontSave;

            var cam = go.AddComponent<Camera>();

            var urpCamData = go.AddComponent<UniversalAdditionalCameraData>();
            urpCamData.renderShadows = false;
            urpCamData.requiresColorOption = CameraOverrideOption.Off;
            urpCamData.requiresDepthOption = CameraOverrideOption.Off;

            cam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            cam.allowMSAA = mainCamera.allowMSAA;
            cam.allowHDR = mainCamera.allowHDR;
            cam.depth = -10;
            cam.enabled = false;

            return cam;
        }

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

        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign, float clipPlaneOffset)
        {
            Vector3 offsetPos = pos + normal * clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

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

}

