using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CSV2Mesh.Parser
{
    /// <summary>
    /// CSV 文件解析器
    /// 负责读取和解析 CSV 文件，提取表头和网格数据
    /// </summary>
    public static class CSVParser
    {
        /// <summary>
        /// 解析 CSV 文件的表头
        /// </summary>
        /// <param name="asset">CSV TextAsset</param>
        /// <returns>表头列表（去重后的列名）</returns>
        public static List<string> ParseHeaders(TextAsset asset)
        {
            if (asset == null) return null;

            string assetPath = AssetDatabase.GetAssetPath(asset);
            return ParseHeaders(assetPath);
        }

        /// <summary>
        /// 解析 CSV 文件的表头
        /// </summary>
        /// <param name="csvPath">CSV 文件路径</param>
        /// <returns>表头列表（去重后的列名）</returns>
        public static List<string> ParseHeaders(string csvPath)
        {
            if (!File.Exists(csvPath)) return null;

            string content = File.ReadAllText(csvPath);
            var lines = content.Split('\n');
            if (lines.Length == 0) return null;

            var headerRow = lines[0].Trim().Replace(" ", "").Split(',');

            List<string> headers = new List<string>();
            foreach (var column in headerRow)
            {
                // 如果列名包含点号，只取第一部分（例如 "POSITION.x" -> "POSITION"）
                string headerName = column.Contains(".")
                    ? column.Split('.')[0]
                    : column;

                headers.Add(headerName);
            }

            return headers;
        }

        /// <summary>
        /// 解析 CSV 文件并提取网格数据
        /// </summary>
        public static Core.CSVMeshData ParseMeshData(
            string csvPath,
            Core.MeshDataConfig config,
            Core.MeshImportSettings settings)
        {
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"CSV file not found: {csvPath}");
                return null;
            }

            // 读取文件内容
            string content = File.ReadAllText(csvPath);
            var lines = content.Split('\n');

            if (lines.Length <= 1)
            {
                Debug.LogError("CSV file is empty or has no data rows.");
                return null;
            }

            // 解析表头
            var headerRow = lines[0].Trim().Replace(" ", "").Split(',');
            var headers = ParseHeaders(csvPath);

            // 验证配置索引，如果超出范围则自动 clamp
            if (!config.ValidateIndices(headers.Count))
            {
                Debug.LogWarning($"Column indices exceed header count ({headers.Count}). Auto-clamping to valid range.");
                config.ClampIndices(headers.Count - 1);
            }

            // 提取所有数据行
            List<float[]> allRows = new List<float[]>();
            ReadAllRows(lines, headerRow.Length, ref allRows);

            if (allRows.Count == 0)
            {
                Debug.LogError("No valid data rows found in CSV.");
                return null;
            }

            // 获取列索引
            var columnIndices = GetColumnIndices(headerRow, headers, config);
            if (!ValidateColumnIndices(columnIndices))
            {
                // 构建详细错误信息
                var missing = new List<string>();
                if (columnIndices.IDX < 0) missing.Add("IDX");
                if (columnIndices.PositionX < 0) missing.Add($"Position X (Index: {config.PositionIndex}, Header: {headers[config.PositionIndex]})");

                string headerDump = string.Join(", ", headerRow);
                Debug.LogError($"Missing required columns: {string.Join(", ", missing)}\nDetected Headers: {headerDump}");
                return null;
            }

            // 构建网格数据
            return BuildMeshData(allRows, columnIndices, settings);
        }

        /// <summary>
        /// 读取所有数据行
        /// </summary>
        private static void ReadAllRows(string[] lines, int expectedColumnCount, ref List<float[]> allRows)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length <= 10) continue;

                var cells = line.Trim().Replace(" ", "").Split(',');
                if (cells.Length != expectedColumnCount) continue;

                float[] rowData = new float[cells.Length];
                bool validRow = true;

                for (int j = 0; j < cells.Length; j++)
                {
                    if (!float.TryParse(cells[j], out rowData[j]))
                    {
                        validRow = false;
                        break;
                    }
                }

                if (validRow)
                {
                    allRows.Add(rowData);
                }
            }
        }

        /// <summary>
        /// 获取各数据列的索引位置
        /// </summary>
        private static ColumnIndices GetColumnIndices(
            string[] headerRow,
            List<string> headers,
            Core.MeshDataConfig config)
        {
            var indices = new ColumnIndices();

            indices.IDX = GetColumnIndex(headerRow, "IDX");

            // Position
            indices.PositionX = GetColumnIndex(headerRow, headers[config.PositionIndex] + ".x");
            indices.PositionY = GetColumnIndex(headerRow, headers[config.PositionIndex] + ".y");
            indices.PositionZ = GetColumnIndex(headerRow, headers[config.PositionIndex] + ".z");

            // UV
            string uvHeader = headers[config.UVIndex];
            indices.UVX = GetColumnIndex(headerRow, uvHeader + ".x");
            if (indices.UVX < 0) indices.UVX = GetColumnIndex(headerRow, uvHeader); // 尝试无后缀匹配

            indices.UVY = GetColumnIndex(headerRow, uvHeader + ".y");

            // Normal (optional)
            indices.NormalX = GetColumnIndex(headerRow, headers[config.NormalIndex] + ".x");
            indices.NormalY = GetColumnIndex(headerRow, headers[config.NormalIndex] + ".y");
            indices.NormalZ = GetColumnIndex(headerRow, headers[config.NormalIndex] + ".z");

            // Tangent (optional)
            indices.TangentX = GetColumnIndex(headerRow, headers[config.TangentIndex] + ".x");
            indices.TangentY = GetColumnIndex(headerRow, headers[config.TangentIndex] + ".y");
            indices.TangentZ = GetColumnIndex(headerRow, headers[config.TangentIndex] + ".z");
            indices.TangentW = GetColumnIndex(headerRow, headers[config.TangentIndex] + ".w");

            // Color (optional)
            indices.ColorR = GetColumnIndex(headerRow, headers[config.ColorIndex] + ".x");
            indices.ColorG = GetColumnIndex(headerRow, headers[config.ColorIndex] + ".y");
            indices.ColorB = GetColumnIndex(headerRow, headers[config.ColorIndex] + ".z");
            indices.ColorA = GetColumnIndex(headerRow, headers[config.ColorIndex] + ".w");

            return indices;
        }

        /// <summary>
        /// 查找指定列名的索引
        /// </summary>
        private static int GetColumnIndex(string[] headers, string columnName)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] == columnName)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 验证必需的列索引是否存在
        /// </summary>
        private static bool ValidateColumnIndices(ColumnIndices indices)
        {
            // UV 不再是必需的
            return indices.IDX >= 0
                && indices.PositionX >= 0 && indices.PositionY >= 0 && indices.PositionZ >= 0;
        }

        /// <summary>
        /// 从原始数据构建网格数据
        /// </summary>
        private static Core.CSVMeshData BuildMeshData(
            List<float[]> allRows,
            ColumnIndices indices,
            Core.MeshImportSettings settings)
        {
            // 计算顶点索引范围
            int minIndex = int.MaxValue;
            int maxIndex = int.MinValue;

            foreach (var row in allRows)
            {
                int idx = (int)row[indices.IDX];
                if (idx < minIndex) minIndex = idx;
                if (idx > maxIndex) maxIndex = idx;
            }

            int vertexCount = maxIndex - minIndex + 1;
            int indexCount = allRows.Count;

            // 检查三角形完整性
            if (indexCount % 3 != 0)
            {
                Debug.LogWarning($"Index count ({indexCount}) is not a multiple of 3.");
            }

            // 初始化数据数组
            var meshData = new Core.CSVMeshData
            {
                Vertices = new Vector3[vertexCount],
                Normals = new Vector3[vertexCount],
                Tangents = new Vector4[vertexCount],
                Colors = new Color[vertexCount],
                UVs = new Vector2[vertexCount],
                Indices = new int[indexCount]
            };

            // 检查可选属性
            meshData.HasNormals = indices.NormalX >= 0 && indices.NormalY >= 0 && indices.NormalZ >= 0;
            meshData.HasTangents = indices.TangentX >= 0 && indices.TangentY >= 0
                                && indices.TangentZ >= 0 && indices.TangentW >= 0;
            meshData.HasColors = indices.ColorR >= 0 && indices.ColorG >= 0
                              && indices.ColorB >= 0 && indices.ColorA >= 0;
            meshData.HasUVs = indices.UVX >= 0;

            var rotation = settings.GetRotation();

            // 填充数据
            for (int i = 0; i < allRows.Count; i++)
            {
                var row = allRows[i];
                int vertexIndex = (int)row[indices.IDX] - minIndex;

                meshData.Indices[i] = vertexIndex;

                if (vertexIndex < 0 || vertexIndex >= vertexCount)
                {
                    Debug.LogError($"Vertex index out of range: {vertexIndex}");
                    continue;
                }

                // Position
                Vector3 position = new Vector3(
                    row[indices.PositionX],
                    row[indices.PositionY],
                    row[indices.PositionZ]
                );
                meshData.Vertices[vertexIndex] = settings.ApplyScale(rotation * position);

                // UV
                if (meshData.HasUVs)
                {
                    float uvX = row[indices.UVX];
                    float uvY = (indices.UVY >= 0) ? row[indices.UVY] : 0f; // 如果没有 UVY，默认为 0

                    meshData.UVs[vertexIndex] = new Vector2(
                        uvX,
                        settings.ProcessUvY(uvY)
                    );
                }

                // Normal
                if (meshData.HasNormals)
                {
                    Vector3 normal = new Vector3(
                        row[indices.NormalX],
                        row[indices.NormalY],
                        row[indices.NormalZ]
                    );
                    meshData.Normals[vertexIndex] = rotation * normal;
                }

                // Tangent
                if (meshData.HasTangents)
                {
                    meshData.Tangents[vertexIndex] = new Vector4(
                        row[indices.TangentX],
                        row[indices.TangentY],
                        row[indices.TangentZ],
                        row[indices.TangentW]
                    );
                }

                // Color
                if (meshData.HasColors)
                {
                    meshData.Colors[vertexIndex] = new Color(
                        row[indices.ColorR],
                        row[indices.ColorG],
                        row[indices.ColorB],
                        row[indices.ColorA]
                    );
                }
            }

            return meshData;
        }

        /// <summary>
        /// 列索引辅助类
        /// </summary>
        private class ColumnIndices
        {
            public int IDX;
            public int PositionX, PositionY, PositionZ;
            public int UVX, UVY;
            public int NormalX, NormalY, NormalZ;
            public int TangentX, TangentY, TangentZ, TangentW;
            public int ColorR, ColorG, ColorB, ColorA;
        }
    }
}
