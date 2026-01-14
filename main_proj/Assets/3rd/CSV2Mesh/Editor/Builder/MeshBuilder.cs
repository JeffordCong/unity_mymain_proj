using System.Linq;
using UnityEngine;

namespace CSV2Mesh.Builder
{
    /// <summary>
    /// Mesh 构建器
    /// 负责从 CSVMeshData 构建最终的 Unity Mesh
    /// </summary>
    public static class MeshBuilder
    {
        /// <summary>
        /// 从 CSVMeshData 构建 Unity Mesh
        /// </summary>
        /// <param name="meshData">CSV 解析得到的网格数据</param>
        /// <param name="reverseTriangles">是否反转三角形顶点顺序</param>
        /// <returns>构建完成的 Mesh</returns>
        public static Mesh BuildMesh(Core.CSVMeshData meshData, bool reverseTriangles = false)
        {
            if (meshData == null || !meshData.IsValid())
            {
                Debug.LogError("Invalid mesh data.");
                return null;
            }

            Mesh mesh = new Mesh
            {
                name = "CSV Generated Mesh"
            };

            // 设置顶点
            mesh.vertices = meshData.Vertices;

            // 设置三角形索引
            if (reverseTriangles)
            {
                mesh.triangles = meshData.Indices.Reverse().ToArray();
            }
            else
            {
                mesh.SetTriangles(meshData.Indices, 0);
            }

            // 设置 UV
            if (meshData.HasUVs && meshData.UVs != null)
            {
                mesh.uv = meshData.UVs;
            }

            // 设置法线
            if (meshData.HasNormals && meshData.Normals != null)
            {
                mesh.normals = meshData.Normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }

            // 设置切线
            if (meshData.HasTangents && meshData.Tangents != null)
            {
                mesh.tangents = meshData.Tangents;
            }
            else
            {
                mesh.RecalculateTangents();
            }

            // 设置顶点颜色
            if (meshData.HasColors && meshData.Colors != null)
            {
                mesh.colors = meshData.Colors;
            }

            // 重新计算包围盒
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 获取 Mesh 信息摘要
        /// </summary>
        public static string GetMeshInfo(Mesh mesh)
        {
            if (mesh == null) return "Null mesh";

            return $"Vertices: {mesh.vertexCount}, Triangles: {mesh.triangles.Length / 3}, " +
                   $"Has Normals: {mesh.normals?.Length > 0}, Has Tangents: {mesh.tangents?.Length > 0}, " +
                   $"Has Colors: {mesh.colors?.Length > 0}";
        }
    }
}
