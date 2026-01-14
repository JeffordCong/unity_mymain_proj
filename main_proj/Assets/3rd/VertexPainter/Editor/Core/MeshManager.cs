using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VertexPainter.Core
{
    /// <summary>
    /// Mesh 管理器 - 负责 Mesh 初始化和管理
    /// </summary>
    public static class MeshManager
    {
        /// <summary>
        /// 从当前选中对象初始化 PaintingObject 数组
        /// </summary>
        public static PaintingObject[] InitializeFromSelection()
        {
            List<PaintingObject> paintingObjects = new List<PaintingObject>();
            Object[] selectedObjects = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.Deep);

            foreach (Object obj in selectedObjects)
            {
                GameObject go = obj as GameObject;
                if (go == null) continue;

                // 尝试添加 SkinnedMeshRenderer
                if (TryAddSkinnedMesh(go, paintingObjects))
                    continue;

                // 尝试添加 MeshFilter
                TryAddMeshFilter(go, paintingObjects);
            }

            return paintingObjects.ToArray();
        }

        /// <summary>
        /// 尝试添加 SkinnedMeshRenderer
        /// </summary>
        private static bool TryAddSkinnedMesh(GameObject go, List<PaintingObject> list)
        {
            SkinnedMeshRenderer skinMr = go.GetComponent<SkinnedMeshRenderer>();
            if (skinMr == null || skinMr.sharedMesh == null)
                return false;

            EnsureMeshReadable(skinMr.sharedMesh);
            list.Add(new PaintingObject(skinMr));
            return true;
        }

        /// <summary>
        /// 尝试添加 MeshFilter
        /// </summary>
        private static bool TryAddMeshFilter(GameObject go, List<PaintingObject> list)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            Renderer r = go.GetComponent<Renderer>();

            if (mf == null || r == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable)
                return false;

            EnsureMeshReadable(mf.sharedMesh);
            list.Add(new PaintingObject(mf, r));
            return true;
        }

        /// <summary>
        /// 确保 Mesh 可读
        /// </summary>
        private static void EnsureMeshReadable(Mesh mesh)
        {
            if (mesh.isReadable) return;

            int instanceID = mesh.GetInstanceID();
            string path = AssetDatabase.GetAssetPath(instanceID);
            ModelImporter importer = ModelImporter.GetAtPath(path) as ModelImporter;

            if (importer != null)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Cleanup(PaintingObject[] objects)
        {
            if (objects == null) return;

            foreach (var obj in objects)
            {
                if (obj != null)
                    obj.RevertMesh();
            }

            System.GC.Collect();
        }
    }
}
