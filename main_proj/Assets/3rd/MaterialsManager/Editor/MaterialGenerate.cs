using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// 材质生成器
    /// 支持一键生成/更新材质球，自动绑定 Config 和贴图
    /// </summary>
    [CreateAssetMenu(menuName = "材质管理器/创建")]
    public class MaterialGenerate : ScriptableObject
    {
        public MaterialConfig config;
        public Material targetMaterial;

        [HideInInspector]
        public bool isAutoCreatedMaterial = false;

        /// <summary>
        /// 获取所有 MaterialConfig 派生类型（通过反射自动发现）
        /// </summary>
        public static List<Type> AllConfigTypes =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(MaterialConfig).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

        /// <summary>
        /// 获取所有配置的显示名称（用于下拉菜单）
        /// </summary>
        public static List<string> AllConfigNames =>
            AllConfigTypes.Select(t =>
            {
                var tmp = CreateInstance(t) as MaterialConfig;
                return tmp != null ? tmp.DisplayName : t.Name;
            }).ToList();

#if UNITY_EDITOR
        /// <summary>
        /// 自动创建并绑定材质球
        /// </summary>
        public void AutoCreateAndAssignMaterial()
        {
            if (targetMaterial != null) return;

            string genPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(genPath)) return;

            string dir = System.IO.Path.GetDirectoryName(genPath);
            string baseName = System.IO.Path.GetFileNameWithoutExtension(genPath);
            string matPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{baseName}.mat");

            Shader shader = config != null ? config.GetShader() : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("未找到指定 Shader，无法创建材质。");
                return;
            }

            var mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);

            targetMaterial = mat;
            isAutoCreatedMaterial = true;
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 一键生成/更新材质
        /// </summary>
        public void Generate()
        {
            // 如果没有材质球，自动创建（强制创建，忽略 autoCreateMaterial）
            if (targetMaterial == null)
            {
                AutoCreateAndAssignMaterial();
            }

            if (config != null && targetMaterial != null)
            {
                config.ApplyAllImportSettings();
                config.ApplyToMaterial(targetMaterial);
            }
        }

        /// <summary>
        /// 同步材质球命名为本资源名
        /// </summary>
        public void SyncMaterialNameWithGenerate()
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning("未绑定材质球，无法同步命名！");
                return;
            }

            if (!isAutoCreatedMaterial)
            {
                Debug.LogWarning("当前材质球不是自动生成的，不做同步！");
                return;
            }

            string genPath = AssetDatabase.GetAssetPath(this);
            string genName = System.IO.Path.GetFileNameWithoutExtension(genPath);
            string matPath = AssetDatabase.GetAssetPath(targetMaterial);

            string result = AssetDatabase.RenameAsset(matPath, genName);
            if (string.IsNullOrEmpty(result))
                Debug.Log($"已将材质球同步命名为: {genName}.mat");
            else
                Debug.LogError($"材质球重命名失败: {result}");
        }
#endif
    }
}
