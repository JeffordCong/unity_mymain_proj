# PlanarReflectionFeature 使用指南

## 概述

`PlanarReflectionFeature` 是一个**完全独立**的 URP ScriptableRendererFeature，用于实现平面反射效果（如水面、镜面反射）。

## 架构设计

```
PlanarReflectionFeature.cs (ScriptableRendererFeature)
├── 在 URP Renderer Asset 中配置
└── 管理全局反射设置

PlanarReflectionPass.cs (Scriptable RenderPass)
├── 收集场景中的反射平面（通过接口）
├── 执行反射相机渲染
├── 处理 Kawase 模糊
└── 设置全局纹理 _PlanarReflectionTexture

IPlanarReflectionPlane.cs (接口)
└── 定义反射平面的标准接口

PlanarReflectionPlane.cs (MonoBehaviour - ✨ 推荐)
└── 全新的独立反射平面组件
```

> [!IMPORTANT]
> **完全独立**：新的 `PlanarReflectionPlane` 组件**完全不依赖**原有的 `PlanarReflections.cs`，是一个独立的全新实现。

> [!NOTE]
> 如需兼容旧场景中的 `PlanarReflections` 组件，可使用 `PlanarReflectionsAdapter.cs` 适配器。

## 使用步骤

### 1. 添加 RenderFeature

1. 打开 URP Renderer Asset (例如 `UniversalRenderer`)
2. 在 **Renderer Features** 列表中点击 **Add Renderer Feature**
3. 选择 **Planar Reflection Feature**

### 2. 配置 Feature 参数

在 Renderer Asset 中配置全局设置：

#### 渲染设置

- **Resolution Multiplier**: 反射纹理分辨率 (Full/Half/Third/Quarter)
- **Clip Plane Offset**: 裁剪平面偏移 (默认 0.07)
- **Reflect Layers**: 需要反射的图层遮罩
- **Render Shadows**: 是否在反射中渲染阴影

#### 模糊设置

- **Blur Enabled**: 启用/禁用模糊
- **Blur Size**: 模糊强度 (0-5)
- **Blur Iterations**: 模糊迭代次数 (0-10)
- **Downsample**: 降采样比例 (1-4)

#### 执行时机

- **Render Pass Event**: 推荐 `BeforeRenderingOpaques`

### 3. 场景设置

1. 在场景中创建反射平面（如 Plane 或 Quad）
2. 添加 **PlanarReflectionPlane** 组件（新组件！）
3. 配置 `Plane Offset` 参数
4. （可选）设置 `Reference Plane` 指向其他 Transform

### 4. 材质设置

在您的水面/镜面 Shader 中采样反射纹理：

```hlsl
TEXTURE2D(_PlanarReflectionTexture);
SAMPLER(sampler_PlanarReflectionTexture);

// 在片段着色器中
float4 screenPos = ComputeScreenPos(i.positionCS);
float2 screenUV = screenPos.xy / screenPos.w;
float4 reflection = SAMPLE_TEXTURE2D(_PlanarReflectionTexture, sampler_PlanarReflectionTexture, screenUV);
```

## 参数调优建议

### 性能优先

```
Resolution Multiplier: Quarter
Blur Enabled: false
Render Shadows: false
```

### 质量优先

```
Resolution Multiplier: Full
Blur Iterations: 6-8
Blur Size: 1-2
Downsample: 1
```

### 平衡设置（推荐）

```
Resolution Multiplier: Third
Blur Enabled: true
Blur Iterations: 4
Blur Size: 0
Downsample: 1
Render Shadows: false
```

## 组件对比

| 组件                      | 描述         | 推荐             |
| ------------------------- | ------------ | ---------------- |
| **PlanarReflectionPlane** | 全新独立组件 | ✅ 推荐          |
| PlanarReflectionsAdapter  | 适配旧组件   | 仅用于兼容性     |
| PlanarReflections (旧)    | 原有组件     | 不推荐新项目使用 |

## 常见问题

**Q: 反射不显示？**

- 检查是否在 Renderer Asset 中添加了 Feature
- 检查 `Reflect Layers` 是否包含需要反射的物体
- 确认场景中有启用的 `PlanarReflectionPlane` 组件

**Q: 如何迁移旧场景？**

- 方案 1: 为旧的 `PlanarReflections` 添加 `PlanarReflectionsAdapter` 组件
- 方案 2: 替换为新的 `PlanarReflectionPlane` 组件（推荐）

**Q: 性能问题？**

- 降低 `ResolutionMultiplier`
- 减少 `Blur Iterations`
- 增加 `Downsample` 值
- 禁用 `Render Shadows`
