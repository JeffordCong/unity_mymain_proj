using UnityEngine;

namespace CSV2Mesh.Core
{
    /// <summary>
    /// CSV 解析后的网格数据
    /// 封装所有从 CSV 提取的顶点属性数据
    /// </summary>
    public class CSVMeshData
    {
        public Vector3[] Vertices { get; set; }
        public Vector3[] Normals { get; set; }
        public Vector4[] Tangents { get; set; }
        public Color[] Colors { get; set; }
        public Vector2[] UVs { get; set; }
        public int[] Indices { get; set; }

        // 标记是否包含可选属性
        public bool HasNormals { get; set; }
        public bool HasTangents { get; set; }
        public bool HasColors { get; set; }
        public bool HasUVs { get; set; }

        /// <summary>
        /// 数据是否有效
        /// </summary>
        public bool IsValid()
        {
            return Vertices != null && Vertices.Length > 0
                && Indices != null && Indices.Length > 0;
        }

        /// <summary>
        /// 获取顶点数量
        /// </summary>
        public int VertexCount => Vertices?.Length ?? 0;

        /// <summary>
        /// 获取三角形数量
        /// </summary>
        public int TriangleCount => Indices?.Length / 3 ?? 0;
    }
}
