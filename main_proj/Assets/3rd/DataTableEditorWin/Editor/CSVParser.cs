using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DataTableEditor
{
    /// <summary>
    /// CSV 解析和保存工具类
    /// </summary>
    public static class CSVParser
    {
        private const char Delimiter = ',';
        private const char Quote = '"';
        private const string TypeRowPrefix = "#";

        /// <summary>
        /// 解析 CSV 文件
        /// </summary>
        public static CSVData Parse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"CSV 文件不存在: {filePath}");
                return null;
            }

            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                return ParseLines(lines);
            }
            catch (Exception e)
            {
                Debug.LogError($"解析 CSV 文件失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析 CSV 行数据
        /// </summary>
        private static CSVData ParseLines(string[] lines)
        {
            var data = new CSVData();
            if (lines.Length == 0) return data;

            // 第一行：列名
            var headerRow = ParseRow(lines[0]);

            // 检查第二行是否为类型定义行
            int dataStartIndex = 1;
            List<string> typeRow = null;

            if (lines.Length > 1)
            {
                var secondRow = ParseRow(lines[1]);
                if (secondRow.Count > 0 && secondRow[0].StartsWith(TypeRowPrefix))
                {
                    typeRow = secondRow;
                    dataStartIndex = 2;
                }
            }

            // 创建列定义
            for (int i = 0; i < headerRow.Count; i++)
            {
                string name = headerRow[i];
                ColumnType type = ColumnType.String;

                if (typeRow != null && i < typeRow.Count)
                {
                    type = ColumnDefinition.ParseTypeMarker(typeRow[i]);
                }

                data.Columns.Add(new ColumnDefinition(name, type));
            }

            // 解析数据行
            for (int i = dataStartIndex; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var rowValues = ParseRow(lines[i]);

                // 确保行长度与列数一致
                while (rowValues.Count < data.ColumnCount)
                    rowValues.Add("");
                while (rowValues.Count > data.ColumnCount)
                    rowValues.RemoveAt(rowValues.Count - 1);

                data.Rows.Add(rowValues);
            }

            return data;
        }

        /// <summary>
        /// 解析单行 CSV 数据，处理引号和逗号
        /// </summary>
        private static List<string> ParseRow(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == Quote)
                    {
                        // 检查是否为转义引号
                        if (i + 1 < line.Length && line[i + 1] == Quote)
                        {
                            currentValue.Append(Quote);
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentValue.Append(c);
                    }
                }
                else
                {
                    if (c == Quote)
                    {
                        inQuotes = true;
                    }
                    else if (c == Delimiter)
                    {
                        values.Add(currentValue.ToString());
                        currentValue.Clear();
                    }
                    else
                    {
                        currentValue.Append(c);
                    }
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }

        /// <summary>
        /// 保存 CSV 数据到文件
        /// </summary>
        public static bool Save(CSVData data, string filePath)
        {
            if (data == null)
            {
                Debug.LogError("CSV 数据为空");
                return false;
            }

            try
            {
                var sb = new StringBuilder();

                // 第一行：列名
                for (int i = 0; i < data.ColumnCount; i++)
                {
                    if (i > 0) sb.Append(Delimiter);
                    sb.Append(EscapeValue(data.Columns[i].Name));
                }
                sb.AppendLine();

                // 数据行
                foreach (var row in data.Rows)
                {
                    for (int i = 0; i < data.ColumnCount; i++)
                    {
                        if (i > 0) sb.Append(Delimiter);
                        string value = i < row.Count ? row[i] : "";
                        sb.Append(EscapeValue(value));
                    }
                    sb.AppendLine();
                }

                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存 CSV 文件失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 转义 CSV 值（处理逗号和引号）
        /// </summary>
        private static string EscapeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            bool needsQuotes = value.Contains(Delimiter.ToString()) ||
                              value.Contains(Quote.ToString()) ||
                              value.Contains("\n") ||
                              value.Contains("\r");

            if (needsQuotes)
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}
