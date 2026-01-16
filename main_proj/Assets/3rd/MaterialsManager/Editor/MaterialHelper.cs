using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// 材质辅助工具类
    /// 提供关键字、属性、Pass 等批量操作方法
    /// </summary>
    internal static class MaterialHelper
    {
        #region Keyword 操作

        /// <summary>
        /// 获取 Shader 所有可用关键字（通过反射）
        /// </summary>
        public static string[] GetShaderKeywords(Shader shader)
        {
            List<string> selectedKeywords = new List<string>();
            string[] keywordLists = null, remainingKeywords = null;
            int[] filteredVariantTypes = null;
            var svc = new ShaderVariantCollection();

            MethodInfo getShaderVariantEntries = typeof(ShaderUtil).GetMethod(
                "GetShaderVariantEntriesFiltered",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            object[] args = new object[]
            {
                shader, 256, selectedKeywords.ToArray(), svc,
                filteredVariantTypes, keywordLists, remainingKeywords
            };
            getShaderVariantEntries?.Invoke(null, args);

            return args[6] as string[] ?? new string[0];
        }

        /// <summary>
        /// 启用/禁用 Shader 关键字
        /// </summary>
        public static void SetKeyword(Material mat, string keyword, bool enable)
        {
            if (mat == null || string.IsNullOrEmpty(keyword)) return;

            bool current = mat.IsKeywordEnabled(keyword);
            if (current == enable) return;

            if (enable)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);

            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 根据贴图是否存在自动设置关键字
        /// </summary>
        public static void SetKeywordByTexture(Material mat, string keyword, Texture2D tex)
        {
            SetKeyword(mat, keyword, tex != null);
            SetInt(mat, keyword, tex != null ? 1 : 0);
        }

        #endregion

        #region Pass 操作

        /// <summary>
        /// 启用/禁用 Shader Pass
        /// </summary>
        public static void SetPassEnabled(Material mat, string passName, bool enabled)
        {
            if (mat == null || string.IsNullOrEmpty(passName)) return;

            bool current = mat.GetShaderPassEnabled(passName);
            if (current == enabled) return;

            mat.SetShaderPassEnabled(passName, enabled);
            EditorUtility.SetDirty(mat);
        }

        #endregion

        #region 属性操作

        /// <summary>
        /// 设置 int 属性
        /// </summary>
        public static void SetInt(Material mat, string property, int value)
        {
            if (mat == null || !mat.HasProperty(property)) return;
            if (mat.GetInt(property) == value) return;

            mat.SetInt(property, value);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 设置 float 属性
        /// </summary>
        public static void SetFloat(Material mat, string property, float value)
        {
            if (mat == null || !mat.HasProperty(property)) return;
            if (Mathf.Approximately(mat.GetFloat(property), value)) return;

            mat.SetFloat(property, value);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 设置贴图属性
        /// </summary>
        public static void SetTexture(Material mat, string property, Texture tex)
        {
            if (mat == null || !mat.HasProperty(property)) return;
            if (mat.GetTexture(property) == tex) return;

            mat.SetTexture(property, tex);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 设置贴图缩放
        /// </summary>
        public static void SetTextureScale(Material mat, string property, Vector2 scale)
        {
            if (mat == null || !mat.HasProperty(property)) return;
            if (mat.GetTextureScale(property) == scale) return;

            mat.SetTextureScale(property, scale);
            EditorUtility.SetDirty(mat);
        }

        #endregion
    }
}