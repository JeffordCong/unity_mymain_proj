using UnityEngine;
using System.Collections.Generic;

namespace VertexPainter.Tools
{
    /// <summary>
    /// 空间网格分区 - 用于快速查找附近的顶点
    /// 将3D空间划分为网格，每个格子存储其中的顶点索引
    /// 查询时只需检查目标位置周围的格子，大幅减少遍历次数
    /// </summary>
    public class SpatialGrid
    {
        private Dictionary<Vector3Int, List<int>> grid;
        private float cellSize;
        private Vector3 boundsMin;
        private bool isBuilt = false;

        /// <summary>
        /// 构建空间网格
        /// </summary>
        /// <param name="vertices">顶点数组</param>
        /// <param name="bounds">边界</param>
        /// <param name="brushSize">笔刷大小</param>
        public void BuildGrid(Vector3[] vertices, Bounds bounds, float brushSize)
        {
            if (vertices == null || vertices.Length == 0)
            {
                isBuilt = false;
                return;
            }

            // 格子大小设置为笔刷大小的2倍，确保覆盖范围
            cellSize = Mathf.Max(brushSize * 2, 0.1f);
            boundsMin = bounds.min;

            grid = new Dictionary<Vector3Int, List<int>>();

            // 将每个顶点分配到对应的格子
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3Int cell = GetCellIndex(vertices[i]);

                if (!grid.ContainsKey(cell))
                {
                    grid[cell] = new List<int>();
                }

                grid[cell].Add(i);
            }

            isBuilt = true;
        }

        /// <summary>
        /// 获取点附近的所有顶点索引
        /// </summary>
        /// <param name="point">查询点</param>
        /// <returns>附近的顶点索引列表</returns>
        public List<int> GetNearbyVertices(Vector3 point)
        {
            var result = new List<int>();

            if (!isBuilt || grid == null)
                return result;

            Vector3Int centerCell = GetCellIndex(point);

            // 检查周围 3x3x3 = 27 个格子
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int cell = centerCell + new Vector3Int(x, y, z);

                        if (grid.ContainsKey(cell))
                        {
                            result.AddRange(grid[cell]);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取顶点所在的格子索引
        /// </summary>
        private Vector3Int GetCellIndex(Vector3 position)
        {
            Vector3 localPos = position - boundsMin;

            return new Vector3Int(
                Mathf.FloorToInt(localPos.x / cellSize),
                Mathf.FloorToInt(localPos.y / cellSize),
                Mathf.FloorToInt(localPos.z / cellSize)
            );
        }

        /// <summary>
        /// 清理网格
        /// </summary>
        public void Clear()
        {
            grid?.Clear();
            isBuilt = false;
        }

        /// <summary>
        /// 获取网格统计信息（用于调试）
        /// </summary>
        public string GetStatistics()
        {
            if (!isBuilt || grid == null)
                return "未构建";

            int totalCells = grid.Count;
            int totalVertices = 0;
            int maxVerticesPerCell = 0;

            foreach (var cell in grid.Values)
            {
                totalVertices += cell.Count;
                maxVerticesPerCell = Mathf.Max(maxVerticesPerCell, cell.Count);
            }

            float avgVerticesPerCell = totalCells > 0 ? (float)totalVertices / totalCells : 0;

            return $"格子数: {totalCells}, 平均顶点/格: {avgVerticesPerCell:F1}, 最大顶点/格: {maxVerticesPerCell}";
        }
        public float GetClosestDistanceSq(Vector3 pos, Vector3[] vertices)
        {
            var indices = GetNearbyVertices(pos);
            float minDistSq = float.MaxValue;

            if (indices.Count > 0)
            {
                foreach (int idx in indices)
                {
                    float d = (vertices[idx] - pos).sqrMagnitude;
                    if (d < minDistSq) minDistSq = d;
                }
            }
            else
            {
                return 10000f;
            }
            return minDistSq;
        }
    }
}
