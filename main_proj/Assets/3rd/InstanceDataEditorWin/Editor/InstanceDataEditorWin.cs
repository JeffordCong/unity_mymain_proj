using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace InstanceDataEditor
{
    /// <summary>
    /// 实例数据编辑器窗口
    /// </summary>
    public class InstanceDataEditorWin : EditorWindow
    {
        #region 常量
        private const float ToolbarHeight = 25f;
        private const float HeaderHeight = 30f;
        private const float RowHeight = 22f;
        private const float ColumnMinWidth = 100f;
        private const float ColumnMaxWidth = 250f;
        private const float DeleteButtonWidth = 25f;
        private const float AddColumnButtonWidth = 30f;
        private const float PreviewPanelWidth = 200f;

        // 固定列名
        private const string ColumnName = "name";
        private const string ColumnUnityInstance = "unity_instance";
        #endregion

        #region 字段
        private InstanceData _data;
        private string _currentFilePath;
        private Vector2 _scrollPosition;
        private bool _isDirty;

        // 列宽度缓存
        private float[] _columnWidths;

        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _cellStyle;

        // 预览相关
        private UnityEngine.Object _previewAsset;
        private Editor _previewEditor;
        private bool _showPreview = true;
        #endregion

        #region 菜单
        [MenuItem("Tools/Instance Data Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<InstanceDataEditorWin>();
            window.titleContent = new GUIContent("实例数据编辑器", EditorGUIUtility.IconContent("d_Prefab Icon").image);
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        #endregion

        #region 生命周期
        private void OnEnable()
        {
            _data = InstanceData.CreateDefault();
            InitializeStyles();
            UpdateColumnWidths();
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawToolbar();

            // 主内容区域（左侧表格 + 右侧预览）
            EditorGUILayout.BeginHorizontal();

            // 左侧表格区域
            float tableWidth = _showPreview ? position.width - PreviewPanelWidth - 10 : position.width;
            EditorGUILayout.BeginVertical(GUILayout.Width(tableWidth));
            DrawHeader();
            DrawDataTable();
            DrawFooter();
            EditorGUILayout.EndVertical();

            // 右侧预览区域
            if (_showPreview)
            {
                DrawPreviewPanel();
            }

            EditorGUILayout.EndHorizontal();

            HandleDragAndDrop();
        }

        private void OnDisable()
        {
            // 清理预览编辑器
            if (_previewEditor != null)
            {
                DestroyImmediate(_previewEditor);
                _previewEditor = null;
            }
        }
        #endregion

        #region 样式初始化
        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };
            }

            if (_cellStyle == null)
            {
                _cellStyle = new GUIStyle(EditorStyles.textField)
                {
                    alignment = TextAnchor.MiddleLeft
                };
            }
        }
        #endregion

        #region 工具栏
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight));

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                NewFile();
            }

            if (GUILayout.Button("打开", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                OpenFile();
            }

            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                SaveFile();
            }

            if (GUILayout.Button("另存为", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveFileAs();
            }

            GUILayout.Space(10);

            // 预览开关按钮
            string previewBtnText = _showPreview ? "隐藏预览" : "显示预览";
            if (GUILayout.Button(previewBtnText, EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _showPreview = !_showPreview;
            }

            GUILayout.FlexibleSpace();

            // 显示当前文件路径
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                string displayPath = Path.GetFileName(_currentFilePath);
                if (_isDirty) displayPath += " *";
                GUILayout.Label(displayPath, EditorStyles.toolbarButton);
            }
            else
            {
                GUILayout.Label("未保存", EditorStyles.toolbarButton);
            }

            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region 表头区域
        private void DrawHeader()
        {
            if (_data == null || _data.ColumnCount == 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(DeleteButtonWidth); // 与删除按钮对齐

            for (int i = 0; i < _data.ColumnCount; i++)
            {
                var col = _data.Columns[i];
                float width = GetColumnWidth(i);

                EditorGUILayout.BeginHorizontal(GUILayout.Width(width));

                // 固定列显示为标签，自定义列可编辑
                if (col.IsFixed)
                {
                    GUILayout.Label(col.Name, _headerStyle, GUILayout.Width(width - DeleteButtonWidth - 2));
                    GUILayout.Space(DeleteButtonWidth); // 占位，保持对齐
                }
                else
                {
                    // 可编辑的列名
                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUILayout.TextField(col.Name, _cellStyle, GUILayout.Width(width - DeleteButtonWidth - 2));
                    if (EditorGUI.EndChangeCheck())
                    {
                        col.Name = newName;
                        MarkDirty();
                    }

                    // 删除列按钮
                    if (GUILayout.Button("×", GUILayout.Width(DeleteButtonWidth - 2)))
                    {
                        if (EditorUtility.DisplayDialog("删除列", $"确定要删除列 \"{col.Name}\" 吗？", "删除", "取消"))
                        {
                            RemoveColumn(i);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            // 添加列按钮
            if (GUILayout.Button("+", GUILayout.Width(AddColumnButtonWidth)))
            {
                AddColumn();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region 数据表格区域
        private void DrawDataTable()
        {
            if (_data == null) return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int row = 0; row < _data.RowCount; row++)
            {
                int rowIndex = row;
                EditorGUILayout.BeginHorizontal();

                // 删除行按钮
                if (GUILayout.Button("×", GUILayout.Width(DeleteButtonWidth), GUILayout.Height(RowHeight)))
                {
                    RemoveRow(rowIndex);
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                // 绘制每个单元格
                for (int col = 0; col < _data.ColumnCount; col++)
                {
                    float width = GetColumnWidth(col);
                    var columnDef = _data.Columns[col];
                    string currentValue = _data.GetCell(row, col);

                    EditorGUI.BeginChangeCheck();
                    string newValue = DrawCell(currentValue, columnDef.Name, width, row, col);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _data.SetCell(row, col, newValue);

                        // 如果修改的是 unity_instance 列，自动更新 name 列
                        if (columnDef.Name == ColumnUnityInstance)
                        {
                            UpdateNameFromAsset(row, newValue);
                        }

                        MarkDirty();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private string DrawCell(string value, string columnName, float width, int row, int col)
        {
            // name 列显示为只读标签（自动从资源获取）
            if (columnName == ColumnName)
            {
                GUILayout.Label(value, _cellStyle, GUILayout.Width(width), GUILayout.Height(RowHeight));
                return value;
            }

            // unity_instance 列使用 ObjectField
            if (columnName == ColumnUnityInstance)
            {
                UnityEngine.Object currentAsset = null;
                if (!string.IsNullOrEmpty(value))
                {
                    currentAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(value);
                }

                // 当鼠标悬停时更新预览
                Rect cellRect = GUILayoutUtility.GetRect(width, RowHeight);
                if (cellRect.Contains(Event.current.mousePosition) && currentAsset != null)
                {
                    SetPreviewAsset(value);
                }

                // 使用 ObjectField 选择资源
                var newAsset = EditorGUI.ObjectField(cellRect, currentAsset, typeof(UnityEngine.Object), false);

                if (newAsset != currentAsset)
                {
                    string newPath = newAsset != null ? AssetDatabase.GetAssetPath(newAsset) : "";
                    SetPreviewAsset(newPath);
                    return newPath;
                }

                return value;
            }

            // 其他列使用普通文本框
            return EditorGUILayout.TextField(value, _cellStyle, GUILayout.Width(width), GUILayout.Height(RowHeight));
        }
        #endregion

        #region 底部区域
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (GUILayout.Button("+ 添加行", GUILayout.Height(25)))
            {
                AddRow();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label($"列数: {_data?.ColumnCount ?? 0}  |  行数: {_data?.RowCount ?? 0}", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region 预览面板
        private void DrawPreviewPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(PreviewPanelWidth));

            // 标题栏
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("资源预览", EditorStyles.boldLabel);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                _showPreview = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (_previewAsset != null)
            {
                // 显示资源名称
                EditorGUILayout.LabelField("名称:", _previewAsset.name, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("类型:", _previewAsset.GetType().Name, EditorStyles.miniLabel);

                EditorGUILayout.Space(10);

                // 绘制预览
                if (_previewEditor == null || _previewEditor.target != _previewAsset)
                {
                    if (_previewEditor != null)
                    {
                        DestroyImmediate(_previewEditor);
                    }
                    _previewEditor = Editor.CreateEditor(_previewAsset);
                }

                if (_previewEditor != null && _previewEditor.HasPreviewGUI())
                {
                    Rect previewRect = GUILayoutUtility.GetRect(PreviewPanelWidth - 20, PreviewPanelWidth - 20);
                    _previewEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.helpBox);
                }
                else
                {
                    // 如果没有专用预览，尝试显示图标
                    Texture2D preview = AssetPreview.GetAssetPreview(_previewAsset);
                    if (preview == null)
                    {
                        preview = AssetPreview.GetMiniThumbnail(_previewAsset);
                    }

                    if (preview != null)
                    {
                        Rect previewRect = GUILayoutUtility.GetRect(PreviewPanelWidth - 20, PreviewPanelWidth - 20);
                        GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("无法预览此资源", MessageType.Info);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("将鼠标悬停在 unity_instance 单元格上以预览资源", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void SetPreviewAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                _previewAsset = null;
                return;
            }

            _previewAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            Repaint();
        }
        #endregion

        #region 拖放处理
        private void HandleDragAndDrop()
        {
            Event evt = Event.current;
            Rect dropArea = new Rect(0, 0, position.width, position.height);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (string path in DragAndDrop.paths)
                        {
                            if (Path.GetExtension(path).ToLower() == ".csv")
                            {
                                LoadFile(path);
                                break;
                            }
                        }
                    }

                    evt.Use();
                    break;
            }
        }
        #endregion

        #region 文件操作
        private void NewFile()
        {
            if (_isDirty)
            {
                if (!EditorUtility.DisplayDialog("新建文件", "当前文件有未保存的更改，是否放弃？", "放弃", "取消"))
                    return;
            }

            _data = InstanceData.CreateDefault();
            _currentFilePath = null;
            _isDirty = false;
            UpdateColumnWidths();
            Repaint();
        }

        private void OpenFile()
        {
            if (_isDirty)
            {
                if (!EditorUtility.DisplayDialog("打开文件", "当前文件有未保存的更改，是否放弃？", "放弃", "取消"))
                    return;
            }

            string path = EditorUtility.OpenFilePanel("打开 CSV 文件", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path))
            {
                LoadFile(path);
            }
        }

        private void LoadFile(string path)
        {
            var data = InstanceDataParser.Parse(path);
            if (data != null)
            {
                _data = data;
                _currentFilePath = path;
                _isDirty = false;
                UpdateColumnWidths();
                Repaint();
            }
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveFileAs();
                return;
            }

            if (InstanceDataParser.Save(_data, _currentFilePath))
            {
                _isDirty = false;
                AssetDatabase.Refresh();
            }
        }

        private void SaveFileAs()
        {
            string defaultName = string.IsNullOrEmpty(_currentFilePath) ? "InstanceData" : Path.GetFileNameWithoutExtension(_currentFilePath);
            string path = EditorUtility.SaveFilePanel("保存 CSV 文件", Application.dataPath, defaultName, "csv");

            if (!string.IsNullOrEmpty(path))
            {
                if (InstanceDataParser.Save(_data, path))
                {
                    _currentFilePath = path;
                    _isDirty = false;
                    AssetDatabase.Refresh();
                }
            }
        }
        #endregion

        #region 数据操作
        private void AddColumn()
        {
            _data.AddColumn($"custom_{_data.ColumnCount - 1}");
            UpdateColumnWidths();
            MarkDirty();
        }

        private void RemoveColumn(int index)
        {
            _data.RemoveColumn(index);
            UpdateColumnWidths();
            MarkDirty();
        }

        private void AddRow()
        {
            _data.AddRow();
            MarkDirty();
        }

        private void RemoveRow(int index)
        {
            _data.RemoveRow(index);
            MarkDirty();
        }

        private void MarkDirty()
        {
            _isDirty = true;
            Repaint();
        }

        /// <summary>
        /// 从资源路径更新 name 列
        /// </summary>
        private void UpdateNameFromAsset(int row, string assetPath)
        {
            // 找到 name 列的索引
            int nameColumnIndex = -1;
            for (int i = 0; i < _data.ColumnCount; i++)
            {
                if (_data.Columns[i].Name == ColumnName)
                {
                    nameColumnIndex = i;
                    break;
                }
            }

            if (nameColumnIndex == -1) return;

            // 从资源路径提取名称
            string assetName = "";
            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset != null)
                {
                    assetName = asset.name;
                }
            }

            _data.SetCell(row, nameColumnIndex, assetName);
        }
        #endregion

        #region 辅助方法
        private void UpdateColumnWidths()
        {
            if (_data == null || _data.ColumnCount == 0)
            {
                _columnWidths = new float[0];
                return;
            }

            _columnWidths = new float[_data.ColumnCount];
            for (int i = 0; i < _data.ColumnCount; i++)
            {
                // unity_instance 列需要更宽以显示资源路径
                if (_data.Columns[i].Name == ColumnUnityInstance)
                {
                    _columnWidths[i] = ColumnMaxWidth;
                }
                else
                {
                    float nameWidth = _data.Columns[i].Name.Length * 8f + 40f;
                    _columnWidths[i] = Mathf.Clamp(nameWidth, ColumnMinWidth, ColumnMaxWidth);
                }
            }
        }

        private float GetColumnWidth(int index)
        {
            if (_columnWidths == null || index < 0 || index >= _columnWidths.Length)
                return ColumnMinWidth;
            return _columnWidths[index];
        }
        #endregion
    }

    #region 数据模型
    /// <summary>
    /// 实例列定义
    /// </summary>
    [Serializable]
    public class InstanceColumn
    {
        public string Name;
        public bool IsFixed;

        public InstanceColumn(string name, bool isFixed = false)
        {
            Name = name;
            IsFixed = isFixed;
        }
    }

    /// <summary>
    /// 实例数据容器
    /// </summary>
    [Serializable]
    public class InstanceData
    {
        public List<InstanceColumn> Columns = new List<InstanceColumn>();
        public List<List<string>> Rows = new List<List<string>>();

        public int ColumnCount => Columns.Count;
        public int RowCount => Rows.Count;

        public void AddColumn(string name, bool isFixed = false)
        {
            Columns.Add(new InstanceColumn(name, isFixed));

            // 为所有现有行添加空值
            foreach (var row in Rows)
            {
                row.Add("");
            }
        }

        public void RemoveColumn(int index)
        {
            if (index < 0 || index >= ColumnCount) return;
            if (Columns[index].IsFixed) return; // 不能删除固定列

            Columns.RemoveAt(index);
            foreach (var row in Rows)
            {
                if (index < row.Count)
                    row.RemoveAt(index);
            }
        }

        public void AddRow()
        {
            var newRow = new List<string>();
            foreach (var col in Columns)
            {
                newRow.Add("");
            }
            Rows.Add(newRow);
        }

        public void RemoveRow(int index)
        {
            if (index < 0 || index >= RowCount) return;
            Rows.RemoveAt(index);
        }

        public string GetCell(int row, int col)
        {
            if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
                return "";

            while (Rows[row].Count <= col)
                Rows[row].Add("");

            return Rows[row][col];
        }

        public void SetCell(int row, int col, string value)
        {
            if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
                return;

            while (Rows[row].Count <= col)
                Rows[row].Add("");

            Rows[row][col] = value;
        }

        public static InstanceData CreateDefault()
        {
            var data = new InstanceData();
            data.AddColumn("name", true);
            data.AddColumn("unity_instance", true);
            return data;
        }
    }
    #endregion

    #region CSV 解析器
    /// <summary>
    /// 实例数据 CSV 解析工具
    /// </summary>
    public static class InstanceDataParser
    {
        private const char Delimiter = ',';
        private const char Quote = '"';

        public static InstanceData Parse(string filePath)
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

        private static InstanceData ParseLines(string[] lines)
        {
            var data = new InstanceData();
            if (lines.Length == 0) return data;

            // 第一行：列名
            var headerRow = ParseRow(lines[0]);

            // 创建列定义
            for (int i = 0; i < headerRow.Count; i++)
            {
                string name = headerRow[i];
                bool isFixed = name == "name" || name == "unity_instance";
                data.Columns.Add(new InstanceColumn(name, isFixed));
            }

            // 解析数据行
            for (int i = 1; i < lines.Length; i++)
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

        public static bool Save(InstanceData data, string filePath)
        {
            if (data == null)
            {
                Debug.LogError("实例数据为空");
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
    #endregion
}
