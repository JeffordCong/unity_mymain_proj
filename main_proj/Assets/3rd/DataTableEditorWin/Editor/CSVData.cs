using System;
using System.Collections.Generic;

namespace DataTableEditor
{
    /// <summary>
    /// CSV 数据容器，包含列定义和行数据
    /// </summary>
    [Serializable]
    public class CSVData
    {
        public List<ColumnDefinition> Columns = new List<ColumnDefinition>();
        public List<List<string>> Rows = new List<List<string>>();

        /// <summary>
        /// 列数
        /// </summary>
        public int ColumnCount => Columns.Count;

        /// <summary>
        /// 行数
        /// </summary>
        public int RowCount => Rows.Count;

        /// <summary>
        /// 添加一列
        /// </summary>
        public void AddColumn(string name = "NewColumn", ColumnType type = ColumnType.String)
        {
            Columns.Add(new ColumnDefinition(name, type));

            // 为所有现有行添加默认值
            string defaultValue = Columns[Columns.Count - 1].GetDefaultValue();
            foreach (var row in Rows)
            {
                row.Add(defaultValue);
            }
        }

        /// <summary>
        /// 删除指定列
        /// </summary>
        public void RemoveColumn(int index)
        {
            if (index < 0 || index >= ColumnCount) return;

            Columns.RemoveAt(index);
            foreach (var row in Rows)
            {
                if (index < row.Count)
                    row.RemoveAt(index);
            }
        }

        /// <summary>
        /// 添加一行
        /// </summary>
        public void AddRow()
        {
            var newRow = new List<string>();
            foreach (var col in Columns)
            {
                newRow.Add(col.GetDefaultValue());
            }
            Rows.Add(newRow);
        }

        /// <summary>
        /// 删除指定行
        /// </summary>
        public void RemoveRow(int index)
        {
            if (index < 0 || index >= RowCount) return;
            Rows.RemoveAt(index);
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        public string GetCell(int row, int col)
        {
            if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
                return "";

            // 确保行有足够的列
            while (Rows[row].Count <= col)
                Rows[row].Add(Columns[Rows[row].Count].GetDefaultValue());

            return Rows[row][col];
        }

        /// <summary>
        /// 设置单元格值
        /// </summary>
        public void SetCell(int row, int col, string value)
        {
            if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
                return;

            // 确保行有足够的列
            while (Rows[row].Count <= col)
                Rows[row].Add(Columns[Rows[row].Count].GetDefaultValue());

            Rows[row][col] = value;
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            Columns.Clear();
            Rows.Clear();
        }

        /// <summary>
        /// 创建一个带有默认列的新表格
        /// </summary>
        public static CSVData CreateDefault()
        {
            var data = new CSVData();
            data.AddColumn("ID", ColumnType.Int);
            data.AddColumn("Name", ColumnType.String);
            data.AddColumn("Value", ColumnType.Float);
            return data;
        }
    }
}
