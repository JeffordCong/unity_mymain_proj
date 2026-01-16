using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// MaterialGenerate 批量管理窗口
    /// </summary>
    public class MaterialGenerateManagerWindow : EditorWindow
    {
        private DefaultAsset targetFolder;
        private List<MaterialGenerate> foundGenerates = new List<MaterialGenerate>();
        private Vector2 scrollPosition;

        [MenuItem("工具/材质管理器/批量管理窗口")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialGenerateManagerWindow>("材质生成器批量管理");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("MaterialGenerate 批量管理", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawFolderSelection();
            EditorGUILayout.Space(10);

            DrawGeneratesList();
            EditorGUILayout.Space(10);

            DrawBatchOperations();
        }

        /// <summary>
        /// 绘制文件夹选择区域
        /// </summary>
        private void DrawFolderSelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标文件夹", GUILayout.Width(80));

            var newFolder = EditorGUILayout.ObjectField(targetFolder, typeof(DefaultAsset), false) as DefaultAsset;
            if (newFolder != targetFolder)
            {
                targetFolder = newFolder;
                RefreshGeneratesList();
            }

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshGeneratesList();
            }

            EditorGUILayout.EndHorizontal();

            if (targetFolder == null)
            {
                EditorGUILayout.HelpBox("请选择包含 MaterialGenerate 资源的文件夹", MessageType.Info);
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(targetFolder);
                EditorGUILayout.LabelField($"路径: {path}", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 绘制找到的 MaterialGenerate 列表
        /// </summary>
        private void DrawGeneratesList()
        {
            EditorGUILayout.LabelField($"找到的材质生成器 ({foundGenerates.Count})", EditorStyles.boldLabel);

            if (foundGenerates.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到任何 MaterialGenerate 资源", MessageType.Warning);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var gen in foundGenerates)
            {
                if (gen == null) continue;

                EditorGUILayout.BeginHorizontal("box");

                // 资源名称和类型
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(gen.name, EditorStyles.boldLabel);
                string configType = gen.config != null ? gen.config.DisplayName : "未配置";
                EditorGUILayout.LabelField($"类型: {configType}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                // 状态标识
                string status = gen.targetMaterial != null ? "已生成" : "未生成";
                Color statusColor = gen.targetMaterial != null ? Color.green : Color.gray;
                GUI.color = statusColor;
                EditorGUILayout.LabelField(status, GUILayout.Width(60));
                GUI.color = Color.white;

                // 定位按钮
                if (GUILayout.Button("定位", GUILayout.Width(60)))
                {
                    Selection.activeObject = gen;
                    EditorGUIUtility.PingObject(gen);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制批量操作按钮
        /// </summary>
        private void DrawBatchOperations()
        {
            EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);

            // 批量更新按钮
            GUI.enabled = foundGenerates.Count > 0;
            if (GUILayout.Button("批量更新材质", GUILayout.Height(30)))
            {
                BatchUpdateMaterials();
            }
            GUI.enabled = true;

            // 统计信息
            int uninitializedCount = foundGenerates.Count(g => g.targetMaterial == null);
            if (uninitializedCount > 0)
            {
                EditorGUILayout.HelpBox($"有 {uninitializedCount} 个生成器尚未生成材质", MessageType.Info);
            }
        }

        /// <summary>
        /// 刷新 MaterialGenerate 列表
        /// </summary>
        private void RefreshGeneratesList()
        {
            foundGenerates.Clear();

            if (targetFolder == null)
                return;

            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"路径不存在: {folderPath}");
                return;
            }

            // 查找所有 MaterialGenerate 资源
            string[] guids = AssetDatabase.FindAssets("t:MaterialGenerate", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var gen = AssetDatabase.LoadAssetAtPath<MaterialGenerate>(path);
                if (gen != null)
                {
                    foundGenerates.Add(gen);
                }
            }

            foundGenerates = foundGenerates.OrderBy(g => g.name).ToList();
            Debug.Log($"在 {folderPath} 找到 {foundGenerates.Count} 个 MaterialGenerate 资源");
        }

        /// <summary>
        /// 批量更新材质
        /// </summary>
        private void BatchUpdateMaterials()
        {
            if (!EditorUtility.DisplayDialog("确认操作",
                $"将更新所有 {foundGenerates.Count} 个材质生成器的材质球。",
                "确定", "取消"))
            {
                return;
            }

            int successCount = 0;

            foreach (var gen in foundGenerates)
            {
                if (gen == null || gen.config == null)
                    continue;

                gen.Generate();
                EditorUtility.SetDirty(gen);

                if (gen.targetMaterial)
                {
                    EditorUtility.SetDirty(gen.targetMaterial);
                    successCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"批量更新完成: {successCount} 个材质已更新");
        }
    }
}
