using System;

namespace DataTableEditor
{
    /// <summary>
    /// 支持的列数据类型
    /// </summary>
    public enum ColumnType
    {
        String,
        Int,
        Float,
        Bool,
        Path  // Unity 资源路径
    }

    /// <summary>
    /// 列定义，包含列名和类型
    /// </summary>
    [Serializable]
    public class ColumnDefinition
    {
        public string Name;
        public ColumnType Type;

        public ColumnDefinition(string name = "Column", ColumnType type = ColumnType.String)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// 获取类型的 CSV 标记字符串
        /// </summary>
        public string GetTypeMarker()
        {
            return $"#{Type.ToString().ToLower()}";
        }

        /// <summary>
        /// 从 CSV 标记字符串解析类型
        /// </summary>
        public static ColumnType ParseTypeMarker(string marker)
        {
            if (string.IsNullOrEmpty(marker)) return ColumnType.String;

            string typeName = marker.TrimStart('#').ToLower();
            return typeName switch
            {
                "int" => ColumnType.Int,
                "float" => ColumnType.Float,
                "bool" => ColumnType.Bool,
                "path" => ColumnType.Path,
                _ => ColumnType.String
            };
        }

        /// <summary>
        /// 验证值是否符合类型要求
        /// </summary>
        public bool ValidateValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return true;

            return Type switch
            {
                ColumnType.Int => int.TryParse(value, out _),
                ColumnType.Float => float.TryParse(value, out _),
                ColumnType.Bool => bool.TryParse(value, out _) || value == "0" || value == "1",
                ColumnType.Path => true,  // 路径总是有效的字符串
                _ => true
            };
        }

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        public string GetDefaultValue()
        {
            return Type switch
            {
                ColumnType.Int => "0",
                ColumnType.Float => "0.0",
                ColumnType.Bool => "false",
                ColumnType.Path => "",
                _ => ""
            };
        }
    }
}
