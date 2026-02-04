using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Render.PlanarReflectionFeature
{
    [System.Serializable, VolumeComponentMenu("Planar Reflection")]
    public class PlanarReflectionVolume : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isActive = new BoolParameter(false);

        [Range(1, 10)]
        [Tooltip("分帧渲染间隔 (1 = 每帧更新, 2 = 每2帧更新 ...) ")]
        public ClampedIntParameter frameInterval = new ClampedIntParameter(1, 1, 10);

        [Tooltip("反射纹理分辨率倍率")]
        public ResolutionMultiplierParameter resolutionMultiplier = new ResolutionMultiplierParameter(PlanarReflectionFeature.ResolutionMultiplier.Third);

        [Tooltip("裁剪平面偏移")]
        public FloatParameter clipPlaneOffset = new FloatParameter(0.07f);

        [Tooltip("反射层级遮罩")]
        public LayerMaskParameter reflectLayers = new LayerMaskParameter(-1);

        [Tooltip("隐藏反射相机 (调试用)")]
        public BoolParameter hideReflectionCamera = new BoolParameter(true);

        [Tooltip("是否渲染阴影")]
        public BoolParameter renderShadows = new BoolParameter(false);

        [Header("模糊设置")]
        [Tooltip("启用模糊")]
        public BoolParameter blurEnabled = new BoolParameter(true);

        [Range(0.0f, 5.0f)]
        [Tooltip("模糊强度")]
        public ClampedFloatParameter blurSize = new ClampedFloatParameter(0f, 0f, 5f);

        [Range(0, 10)]
        [Tooltip("模糊迭代次数")]
        public ClampedIntParameter blurIterations = new ClampedIntParameter(4, 0, 10);

        [Range(1.0f, 4.0f)]
        [Tooltip("降采样比例")]
        public ClampedFloatParameter downsample = new ClampedFloatParameter(1f, 1f, 4f);

        public bool IsActive() => isActive.value;

        public bool IsTileCompatible() => false;
    }

    [System.Serializable]
    public class ResolutionMultiplierParameter : VolumeParameter<PlanarReflectionFeature.ResolutionMultiplier>
    {
        public ResolutionMultiplierParameter(PlanarReflectionFeature.ResolutionMultiplier value, bool overrideState = false) : base(value, overrideState) { }
    }
}
