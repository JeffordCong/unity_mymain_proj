#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MyEditor.MaterialSystem
{

    // 纹理的命名规范预设
    public static class ComTextureSuffixPresets
    {
        // Key是Com类型（或直接类型名字符串），Value是后缀（null或""表示不检查）
        public static readonly Dictionary<Type, string> SuffixMap = new Dictionary<Type, string>
        {
            // { typeof(ComBaseMap), "_D" },
            // { typeof(ComNormalMap), "_N" },
            //  { typeof(ComLightMap), "_L" },
            // MaskMap没有后缀，直接不配或用null/空字符串
            // { typeof(ComMaskMap), null }
        };

        public static string GetComSuffix(Type comType)
        {
            return SuffixMap.TryGetValue(comType, out var suffix) ? suffix : null;
        }
    }

    /// <summary>
    // 检查目录规范
    // 材质生成器和材质球在同一Materials目录下，贴图在同级Textures目录下
    /// <summary>
    public static class AssetDirectoryChecker
    {
        /// <summary>
        /// 检查MaterialGenerate资源、材质球、贴图目录规范。
        /// 规范：MaterialGenerate和Material必须在名为Materials的同一目录，贴图必须在同级Textures目录。
        /// </summary>

        public static bool CheckMaterialGenerateDirectory(MaterialGenerate gen, out string errorInfo)
        {
            errorInfo = "";

            // 1. 获取MaterialGenerate自己的目录
            string genPath = AssetDatabase.GetAssetPath(gen);
            string genMaterialsDir = System.IO.Path.GetDirectoryName(genPath).Replace("\\", "/");

            // 检查MaterialGenerate是否在Materials目录
            if (!genMaterialsDir.EndsWith("/Materials"))
            {
                errorInfo += $"材质生成器 {gen.name} 不在名为Materials的文件夹下！实际:{genMaterialsDir}\n";
            }

            // 2. 检查材质球是否也在该Materials目录
            if (gen.targetMaterial != null)
            {
                string matPath = AssetDatabase.GetAssetPath(gen.targetMaterial);
                string matDir = System.IO.Path.GetDirectoryName(matPath).Replace("\\", "/");
                if (matDir != genMaterialsDir)
                {
                    errorInfo += $"材质球 {gen.targetMaterial.name} 不在同一Materials文件夹！期望:{genMaterialsDir} 实际:{matDir}\n";
                }
            }

            // 3. 计算同级Textures目录
            string parentDir = System.IO.Path.GetDirectoryName(genMaterialsDir).Replace("\\", "/"); // .../角色A/
            string texturesDir = System.IO.Path.Combine(parentDir, "Textures").Replace("\\", "/");

            // 4. 检查所有贴图是否都在Textures目录
            if (gen.config != null)
            {
                var textures = GetAllTexturesInConfig(gen.config);
                foreach (var tex in textures)
                {
                    if (tex == null)
                        continue;
                    if (TextureHelper.IsInCommonDirectory(tex))
                        continue; // 如果贴图在公共目录中，则不检查
                    string texPath = AssetDatabase.GetAssetPath(tex);
                    string texDir = System.IO.Path.GetDirectoryName(texPath).Replace("\\", "/");

                    // 使用 StartsWith 来判断贴图是否位于 Textures 文件夹及其子目录中
                    if (!texDir.StartsWith(texturesDir))
                    {
                        errorInfo += $"贴图 {tex.name} 不在同级Textures文件夹！期望:{texturesDir} 实际:{texDir}\n";
                    }
                }
            }

            // 返回true表示全部合规，否则有错误
            return string.IsNullOrEmpty(errorInfo);
        }


        private static void CollectAllTextures(object obj, List<Texture2D> list)
        {
            if (obj == null) return;
            var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                if (value == null) continue;

                // 直接是Texture2D
                if (value is Texture2D tex)
                {
                    list.Add(tex);
                }
                // 如果是自定义类对象（StandardTextureGroup、MaterialCom、其它group等），递归遍历其内部
                else if (!field.FieldType.IsPrimitive && !field.FieldType.IsEnum && field.FieldType != typeof(string))
                {
                    CollectAllTextures(value, list);
                }
            }
        }

        // 用法
        private static List<Texture2D> GetAllTexturesInConfig(MaterialConfig config)
        {
            var list = new List<Texture2D>();
            CollectAllTextures(config, list);
            return list;
        }

    }
}
#endif