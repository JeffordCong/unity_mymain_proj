using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using System.Collections.Generic;

namespace CSV2Mesh.Exporter
{
    /// <summary>
    /// Mesh 导出工具
    /// 负责将 Mesh 导出为资源文件或 FBX 格式
    /// </summary>
    public static class MeshExporter
    {
        private static List<GameObject> exportedObjects = new List<GameObject>();

        /// <summary>
        /// 导出 Mesh 为 FBX 并在场景中显示预览
        /// </summary>
        /// <param name="mesh">要导出的 Mesh</param>
        /// <param name="sourcePath">源 CSV 文件路径</param>
        /// <param name="material">使用的材质，为 null 则使用默认材质</param>
        /// <returns>创建的 GameObject</returns>
        public static GameObject ExportAndShow(Mesh mesh, string sourcePath, Material material = null)
        {
            if (mesh == null)
            {
                Debug.LogError("Cannot export null mesh.");
                return null;
            }

            // 计算最终文件名 (使用 CSV 文件名)
            string finalName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);

            // 设置 Mesh 名称（FBX 内部资源名称）
            mesh.name = finalName;

            // 获取输出路径
            string outputPath = GetOutputPath(sourcePath);

            // 获取材质
            Material useMaterial = material ?? GetDefaultMaterial();

            // 创建 GameObject 用于预览（使用正确的名称）
            GameObject previewObject = CreatePreviewObject(mesh, useMaterial, finalName);

            // 导出为 FBX
            try
            {
                // 完整路径包含 .fbx 扩展名
                string fullPath = outputPath + ".fbx";

                // 先删除已存在的同名文件，避免冲突
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                ModelExporter.ExportObject(fullPath, previewObject);
                AssetDatabase.Refresh();
                Debug.Log($"Mesh exported to: {fullPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export mesh: {e.Message}");
            }

            // 记录导出的对象，便于后续清理
            exportedObjects.Add(previewObject);

            return previewObject;
        }

        /// <summary>
        /// 清理所有导出的预览对象
        /// </summary>
        public static void ClearAllExportedObjects()
        {
            foreach (var obj in exportedObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            exportedObjects.Clear();
            Debug.Log("All exported preview objects cleared.");
        }

        /// <summary>
        /// 获取导出对象数量
        /// </summary>
        public static int GetExportedObjectCount()
        {
            return exportedObjects.Count;
        }

        /// <summary>
        /// 创建预览 GameObject
        /// </summary>
        /// <param name="mesh">网格</param>
        /// <param name="material">材质</param>
        /// <param name="name">GameObject 名称</param>
        private static GameObject CreatePreviewObject(Mesh mesh, Material material, string name)
        {
            GameObject obj = new GameObject(name);

            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            return obj;
        }

        /// <summary>
        /// 获取输出文件路径（不含扩展名）
        /// 输出到 CSV 所在目录（同级目录）
        /// </summary>
        /// <param name="sourcePath">源 CSV 文件路径</param>
        private static string GetOutputPath(string sourcePath)
        {
            // 获取 CSV 所在目录
            string directory = System.IO.Path.GetDirectoryName(sourcePath);

            // 使用 CSV 文件名
            string fileName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);

            // 返回完整输出路径（不含扩展名）
            return System.IO.Path.Combine(directory, fileName);
        }

        /// <summary>
        /// 获取默认材质
        /// </summary>
        private static Material GetDefaultMaterial()
        {
            return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
        }
    }
}
