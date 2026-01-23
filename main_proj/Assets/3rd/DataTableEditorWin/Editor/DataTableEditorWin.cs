using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DataTableEditor
{
    /// <summary>
    /// CSV 数据表编辑器窗口
    /// </summary>
    public class DataTableEditorWin : EditorWindow
    {
        #region 常量
        private const float ToolbarHeight = 25f;
        private const float HeaderHeight = 50f;
        private const float RowHeight = 22f;
        private const float ColumnMinWidth = 80f;
        private const float ColumnMaxWidth = 200f;
        private const float DeleteButtonWidth = 25f;
        private const float AddColumnButtonWidth = 30f;
        private const float PreviewPanelWidth = 200f;
        #endregion

        #region 字段
        private CSVData _csvData;
        private string _currentFilePath;
        private Vector2 _scrollPosition;
        private bool _isDirty;

        // 列宽度缓存
        private float[] _columnWidths;

        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _cellStyle;
        private GUIStyle _typePopupStyle;

        // 预览相关
        private UnityEngine.Object _previewAsset;
        private Editor _previewEditor;
        private bool _showPreview = true;
        #endregion

        #region 菜单
        [MenuItem("Tools/Data Table Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DataTableEditorWin>();
            window.titleContent = new GUIContent("CSV 编辑器", EditorGUIUtility.IconContent("d_TextAsset Icon").image);
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        #endregion

        #region 生命周期
        private void OnEnable()
        {
            _csvData = CSVData.CreateDefault();
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

            if (_typePopupStyle == null)
            {
                _typePopupStyle = new GUIStyle(EditorStyles.popup)
                {
                    fontSize = 10
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
            if (_csvData == null || _csvData.ColumnCount == 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 第一行：列名
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(DeleteButtonWidth); // 与删除按钮对齐

            for (int i = 0; i < _csvData.ColumnCount; i++)
            {
                var col = _csvData.Columns[i];
                float width = GetColumnWidth(i);

                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField(col.Name, _cellStyle, GUILayout.Width(width));
                if (EditorGUI.EndChangeCheck())
                {
                    col.Name = newName;
                    MarkDirty();
                }
            }

            // 添加列按钮
            if (GUILayout.Button("+", GUILayout.Width(AddColumnButtonWidth)))
            {
                AddColumn();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 第二行：类型选择 + 删除按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(DeleteButtonWidth);

            for (int i = 0; i < _csvData.ColumnCount; i++)
            {
                int colIndex = i;
                var col = _csvData.Columns[i];
                float width = GetColumnWidth(i);

                EditorGUILayout.BeginHorizontal(GUILayout.Width(width));

                // 类型下拉菜单
                EditorGUI.BeginChangeCheck();
                var newType = (ColumnType)EditorGUILayout.EnumPopup(col.Type, _typePopupStyle, GUILayout.Width(width - DeleteButtonWidth - 2));
                if (EditorGUI.EndChangeCheck())
                {
                    col.Type = newType;
                    MarkDirty();
                }

                // 删除列按钮
                if (GUILayout.Button("×", GUILayout.Width(DeleteButtonWidth - 2)))
                {
                    if (EditorUtility.DisplayDialog("删除列", $"确定要删除列 \"{col.Name}\" 吗？", "删除", "取消"))
                    {
                        RemoveColumn(colIndex);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(AddColumnButtonWidth);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region 数据表格区域
        private void DrawDataTable()
        {
            if (_csvData == null) return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int row = 0; row < _csvData.RowCount; row++)
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
                for (int col = 0; col < _csvData.ColumnCount; col++)
                {
                    float width = GetColumnWidth(col);
                    var columnDef = _csvData.Columns[col];
                    string currentValue = _csvData.GetCell(row, col);

                    EditorGUI.BeginChangeCheck();
                    string newValue = DrawCell(currentValue, columnDef.Type, width);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _csvData.SetCell(row, col, newValue);
                        MarkDirty();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private string DrawCell(string value, ColumnType type, float width)
        {
            switch (type)
            {
                case ColumnType.Int:
                    int intValue = 0;
                    int.TryParse(value, out intValue);
                    return EditorGUILayout.IntField(intValue, GUILayout.Width(width), GUILayout.Height(RowHeight)).ToString();

                case ColumnType.Float:
                    float floatValue = 0f;
                    float.TryParse(value, out floatValue);
                    return EditorGUILayout.FloatField(floatValue, GUILayout.Width(width), GUILayout.Height(RowHeight)).ToString();

                case ColumnType.Bool:
                    bool boolValue = value.ToLower() == "true" || value == "1";
                    return EditorGUILayout.Toggle(boolValue, GUILayout.Width(width), GUILayout.Height(RowHeight)).ToString().ToLower();

                case ColumnType.Path:
                    // 加载当前路径的资源
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
                        // 资源变化时更新预览
                        string newPath = newAsset != null ? AssetDatabase.GetAssetPath(newAsset) : "";
                        SetPreviewAsset(newPath);
                        return newPath;
                    }

                    return value;

                default:
                    return EditorGUILayout.TextField(value, _cellStyle, GUILayout.Width(width), GUILayout.Height(RowHeight));
            }
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

            GUILayout.Label($"列数: {_csvData?.ColumnCount ?? 0}  |  行数: {_csvData?.RowCount ?? 0}", EditorStyles.miniLabel);

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
                EditorGUILayout.HelpBox("选择一个 Path 类型的单元格以预览资源", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 设置预览资源
        /// </summary>
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

            _csvData = CSVData.CreateDefault();
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
            var data = CSVParser.Parse(path);
            if (data != null)
            {
                _csvData = data;
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

            if (CSVParser.Save(_csvData, _currentFilePath))
            {
                _isDirty = false;
                AssetDatabase.Refresh();
            }
        }

        private void SaveFileAs()
        {
            string defaultName = string.IsNullOrEmpty(_currentFilePath) ? "NewTable" : Path.GetFileNameWithoutExtension(_currentFilePath);
            string path = EditorUtility.SaveFilePanel("保存 CSV 文件", Application.dataPath, defaultName, "csv");

            if (!string.IsNullOrEmpty(path))
            {
                if (CSVParser.Save(_csvData, path))
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
            _csvData.AddColumn($"Column{_csvData.ColumnCount + 1}");
            UpdateColumnWidths();
            MarkDirty();
        }

        private void RemoveColumn(int index)
        {
            _csvData.RemoveColumn(index);
            UpdateColumnWidths();
            MarkDirty();
        }

        private void AddRow()
        {
            _csvData.AddRow();
            MarkDirty();
        }

        private void RemoveRow(int index)
        {
            _csvData.RemoveRow(index);
            MarkDirty();
        }

        private void MarkDirty()
        {
            _isDirty = true;
            Repaint();
        }
        #endregion

        #region 辅助方法
        private void UpdateColumnWidths()
        {
            if (_csvData == null || _csvData.ColumnCount == 0)
            {
                _columnWidths = new float[0];
                return;
            }

            _columnWidths = new float[_csvData.ColumnCount];
            for (int i = 0; i < _csvData.ColumnCount; i++)
            {
                // 根据列名和类型计算宽度
                float nameWidth = _csvData.Columns[i].Name.Length * 8f + 20f;
                _columnWidths[i] = Mathf.Clamp(nameWidth, ColumnMinWidth, ColumnMaxWidth);
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
}
