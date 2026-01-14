using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CSV2Mesh.Core;
using CSV2Mesh.Parser;
using CSV2Mesh.Builder;
using CSV2Mesh.Exporter;

namespace CSV2Mesh
{
    /// <summary>
    /// CSV 转 Mesh 工具窗口
    /// </summary>
    public class CSV2MeshWindow : EditorWindow
    {
        [SerializeField] private TextAsset dataTypes;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private List<TextAsset> csvFiles = new List<TextAsset>();

        private readonly MeshDataConfig dataConfig = new MeshDataConfig();
        private readonly MeshImportSettings importSettings = new MeshImportSettings();
        private SerializedObject serializedObject;
        private Vector2 scrollPosition;
        private List<string> currentHeaders;

        // UI State
        private bool showColumnMapping = false;
        private bool showImportOptions = false;
        private bool convertDataTypeFile = true;
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;

        [MenuItem("Tools/CSV2Mesh")]
        static void ShowWindow() => GetWindow<CSV2MeshWindow>("CSV2Mesh").minSize = new Vector2(400, 500);

        private void OnEnable() => serializedObject = new SerializedObject(this);

        private void OnGUI()
        {
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("CSV to Mesh", headerStyle);
            EditorGUILayout.Space(5);

            // 1. 核心配置区
            DrawSection("配置", () =>
            {
                EditorGUI.BeginChangeCheck();
                // 配置表头, 获取数据格式
                dataTypes = EditorGUILayout.ObjectField("CSV 文件", dataTypes, typeof(TextAsset), false) as TextAsset;
                if (EditorGUI.EndChangeCheck() && dataTypes != null) OnDataTypeChanged();

                if (currentHeaders?.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox($"✓ Detected {currentHeaders.Count} columns", MessageType.None);
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.Space(5);
                defaultMaterial = EditorGUILayout.ObjectField("Material", defaultMaterial, typeof(Material), false) as Material;

                EditorGUILayout.Space(5);
                convertDataTypeFile = EditorGUILayout.ToggleLeft("仅转换此CSV文件?", convertDataTypeFile);
            });

            if (dataTypes != null && currentHeaders?.Count > 0)
            {
                EditorGUILayout.Space(10);

                // 2. 折叠配置区
                DrawFoldoutSection("Column 映射", ref showColumnMapping, () =>
                {
                    var headers = currentHeaders.ToArray();
                    dataConfig.PositionIndex = EditorGUILayout.Popup("Position", dataConfig.PositionIndex, headers);
                    dataConfig.NormalIndex = EditorGUILayout.Popup("Normal", dataConfig.NormalIndex, headers);
                    dataConfig.TangentIndex = EditorGUILayout.Popup("Tangent", dataConfig.TangentIndex, headers);
                    dataConfig.ColorIndex = EditorGUILayout.Popup("Color", dataConfig.ColorIndex, headers);
                    dataConfig.UVIndex = EditorGUILayout.Popup("UV", dataConfig.UVIndex, headers);
                });

                DrawFoldoutSection("Import Options", ref showImportOptions, () =>
                {
                    importSettings.ScaleFactor = EditorGUILayout.FloatField("Scale Factor", importSettings.ScaleFactor);
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    importSettings.RotationN90 = EditorGUILayout.ToggleLeft("Rotate -90°", importSettings.RotationN90, GUILayout.Width(100));
                    importSettings.ReverseUvY = EditorGUILayout.ToggleLeft("Reverse UV", importSettings.ReverseUvY, GUILayout.Width(100));
                    importSettings.ReverseTriangles = EditorGUILayout.ToggleLeft("Reverse Tris", importSettings.ReverseTriangles, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                });

                EditorGUILayout.Space(10);

                // 3. 文件列表区（仅当不勾选 "Also Convert This File" 时显示）
                if (!convertDataTypeFile)
                {
                    DrawSection("Files to Convert", () =>
                    {
                        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("csvFiles"), GUIContent.none);
                        EditorGUILayout.EndScrollView();
                    });
                }

                EditorGUILayout.Space(15);
                DrawActionButtons();
            }
            else
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Please select a valid CSV file to start.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 0, 10, 10)
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }
        }

        private void DrawSection(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            content();
            EditorGUILayout.EndVertical();
        }

        private void DrawFoldoutSection(string title, ref bool state, System.Action content)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            state = EditorGUILayout.Foldout(state, title, true, EditorStyles.foldoutHeader);
            if (state)
            {
                EditorGUILayout.Space(5);
                content();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            DrawColoredButton("Convert to Mesh", new Color(0.5f, 0.8f, 0.5f), 35, ConvertAllFiles);
            DrawColoredButton("Clear All", new Color(0.8f, 0.5f, 0.5f), 35, ClearAll, 100);
            EditorGUILayout.EndHorizontal();

            if (MeshExporter.GetExportedObjectCount() > 0)
                EditorGUILayout.HelpBox($"Exported objects in scene: {MeshExporter.GetExportedObjectCount()}", MessageType.Info);
        }

        private void DrawColoredButton(string label, Color color, int height, System.Action onClick, int width = -1)
        {
            GUI.backgroundColor = color;
            var options = width > 0 ? new[] { GUILayout.Height(height), GUILayout.Width(width) } : new[] { GUILayout.Height(height) };
            if (GUILayout.Button(label, options)) onClick();
            GUI.backgroundColor = Color.white;
        }

        private void OnDataTypeChanged()
        {
            currentHeaders = CSVParser.ParseHeaders(dataTypes);
            if (currentHeaders?.Count > 0)
            {
                dataConfig.ClampIndices(currentHeaders.Count - 1);
                Debug.Log($"Loaded {currentHeaders.Count} column headers.");
            }
            else Debug.LogError("Failed to parse headers.");
        }

        private void ConvertAllFiles()
        {
            // 收集要转换的文件
            var filesToConvert = new List<string>();

            // 如果勾选了 "Also Convert This File"，添加 dataTypes 文件
            if (convertDataTypeFile && dataTypes != null)
            {
                filesToConvert.Add(AssetDatabase.GetAssetPath(dataTypes));
            }

            // 添加 Files to Convert 列表中的文件
            if (csvFiles != null)
            {
                foreach (var file in csvFiles)
                {
                    if (file != null)
                    {
                        string path = AssetDatabase.GetAssetPath(file);
                        if (!filesToConvert.Contains(path)) // 避免重复
                            filesToConvert.Add(path);
                    }
                }
            }

            if (filesToConvert.Count == 0)
            {
                EditorUtility.DisplayDialog("No Files", "Please add CSV files to convert.", "OK");
                return;
            }

            int success = 0, fail = 0;
            foreach (var path in filesToConvert)
                if (ConvertSingleFile(path)) success++; else fail++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var msg = $"Conversion complete!\nSuccess: {success}, Failed: {fail}";
            EditorUtility.DisplayDialog("Result", msg, "OK");
            Debug.Log(msg);
        }

        private bool ConvertSingleFile(string csvPath)
        {
            try
            {
                var meshData = CSVParser.ParseMeshData(csvPath, dataConfig, importSettings);
                if (meshData?.IsValid() != true) return LogError($"Failed to parse: {csvPath}");

                var mesh = MeshBuilder.BuildMesh(meshData, importSettings.ReverseTriangles);
                if (mesh == null) return LogError($"Failed to build: {csvPath}");

                var obj = MeshExporter.ExportAndShow(mesh, csvPath, defaultMaterial);
                if (obj == null) return LogError($"Failed to export: {csvPath}");

                Debug.Log($"Success: {csvPath}\n{MeshBuilder.GetMeshInfo(mesh)}");
                return true;
            }
            catch (System.Exception e)
            {
                return LogError($"Error: {csvPath} - {e.Message}");
            }
        }

        private bool LogError(string msg)
        {
            Debug.LogError(msg);
            return false;
        }

        private void ClearAll()
        {
            if (EditorUtility.DisplayDialog("Clear All", "Clear file list and remove all exported objects?", "Yes", "Cancel"))
            {
                csvFiles.Clear();
                MeshExporter.ClearAllExportedObjects();
                Debug.Log("Cleared all.");
            }
        }
    }
}
