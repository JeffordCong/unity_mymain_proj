using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Render.PlanarReflectionFeature
{
    public class PlanarReflectionFeature : ScriptableRendererFeature
    {
        public enum ResolutionMultiplier
        {
            Full,
            Half,
            Third,
            Quarter
        }

        [System.Serializable]
        public class Settings
        {
            [Header("执行时机")]
            [Tooltip("在哪个渲染阶段执行反射")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public Settings settings = new Settings();
        private PlanarReflectionPass reflectionPass;

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
            // 只在主相机或场景视图相机中执行
            bool isMainCamera = camera == Camera.main;
            bool isSceneView = camera.cameraType == CameraType.SceneView;

            if (isMainCamera || isSceneView)
            {
                if (reflectionPass != null)
                {
                    reflectionPass.ExecutePreRender(context, camera);
                }
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 只在主相机或场景视图相机中添加渲染 Pass
            bool isMainCamera = renderingData.cameraData.camera == Camera.main;
            bool isSceneView = renderingData.cameraData.cameraType == CameraType.SceneView;

            if (isMainCamera || isSceneView)
            {
                renderer.EnqueuePass(reflectionPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            reflectionPass?.Dispose();
        }
    }

}