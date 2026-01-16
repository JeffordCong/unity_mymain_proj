using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// 材质配置基类
    /// 通过反射读取 [TextureSlot] 特性，自动处理贴图应用
    /// </summary>
    public abstract class MaterialConfig : ScriptableObject
    {
        /// <summary>
        /// 用于下拉菜单显示的名称
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 返回本 Config 对应的 Shader
        /// </summary>
        public abstract Shader GetShader();

        /// <summary>
        /// 应用所有贴图和参数到目标材质球
        /// </summary>
        public virtual void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;
            mat.shader = GetShader();

            // 遍历所有带 [TextureSlot] 特性的字段
            foreach (var (field, attr) in GetTextureSlotFields())
            {
                var tex = field.GetValue(this) as Texture2D;
                ApplyTexture(mat, tex, attr);
            }

            // 收集 Shader Keywords
            CollectKeywords(mat);
        }

        /// <summary>
        /// 应用单个贴图到材质（使用 MaterialHelper）
        /// </summary>
        private void ApplyTexture(Material mat, Texture2D tex, TextureSlotAttribute attr)
        {
            // 设置贴图
            MaterialHelper.SetTexture(mat, attr.PropertyName, tex);

            // 设置关键字
            if (!string.IsNullOrEmpty(attr.KeywordName))
            {
                MaterialHelper.SetKeywordByTexture(mat, attr.KeywordName, tex);
            }
        }

        /// <summary>
        /// 收集材质的 Shader Keywords
        /// </summary>
        private void CollectKeywords(Material mat)
        {
            if (mat.shaderKeywords == null) return;
            foreach (string kw in mat.shaderKeywords)
            {
                if (!string.IsNullOrEmpty(kw))
                    ShaderVariantCollector.AddKeyword(kw);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 批量设置所有贴图的导入参数
        /// </summary>
        public virtual void ApplyAllImportSettings()
        {
            foreach (var (field, attr) in GetTextureSlotFields())
            {
                var tex = field.GetValue(this) as Texture2D;
                if (tex == null) continue;

                var preset = TexturePlatformSettings.BaseMap;
                var config = TexturePlatformSettings.OverrideAllPlatformMaxSize(preset, attr.MaxSize);
                TextureHelper.ApplyImportSettings(tex, config);
            }
        }
#endif

        /// <summary>
        /// 获取所有带 [TextureSlot] 特性的 Texture2D 字段
        /// </summary>
        private IEnumerable<(FieldInfo, TextureSlotAttribute)> GetTextureSlotFields()
        {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(Texture2D)) continue;

                var attr = field.GetCustomAttribute<TextureSlotAttribute>();
                if (attr != null)
                    yield return (field, attr);
            }
        }
    }
}