using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// MaterialGenerate 自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(MaterialGenerate))]
    public class MaterialGenerateEditor : Editor
    {
        private Editor configEditor;
        private List<Type> _typeList;
        private List<string> _nameList;

        private void OnEnable()
        {
            _typeList = MaterialGenerate.AllConfigTypes;
            _nameList = MaterialGenerate.AllConfigNames;
        }

        public override void OnInspectorGUI()
        {
            var gen = (MaterialGenerate)target;
            serializedObject.Update();

            DrawHeader(gen);

            EditorGUILayout.Space(4);
            DrawConfigSection(gen);

            EditorGUILayout.Space(6);
            DrawMaterialSection(gen);

            EditorGUILayout.Space(6);
            DrawActionsSection(gen);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(MaterialGenerate gen)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("材质生成器", EditorStyles.boldLabel);
            string status = gen.targetMaterial != null ? "就绪" : "缺少材质";
            EditorGUILayout.LabelField($"状态: {status}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigSection(MaterialGenerate gen)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);
            DrawConfigSelector(gen);
            DrawConfigInspector(gen);
            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialSection(MaterialGenerate gen)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("材质", EditorStyles.boldLabel);
            DrawMaterialField(gen);
            EditorGUILayout.EndVertical();
        }

        private void DrawActionsSection(MaterialGenerate gen)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            DrawGenerateButton(gen);
            DrawSyncNameButton(gen);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制配置类型下拉选择框
        /// </summary>
        private void DrawConfigSelector(MaterialGenerate gen)
        {
            if (_typeList == null || _typeList.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到任何 MaterialConfig 类型", MessageType.Warning);
                return;
            }

            int curIndex = gen.config != null
                ? _typeList.FindIndex(t => t == gen.config.GetType())
                : -1;

            int newIndex = EditorGUILayout.Popup("材质类型", curIndex, _nameList.ToArray());

            if (newIndex != curIndex)
            {
                SwitchConfigType(gen, newIndex);
            }
        }

        /// <summary>
        /// 切换配置类型
        /// </summary>
        private void SwitchConfigType(MaterialGenerate gen, int newIndex)
        {
            if (gen.config != null)
                DestroyImmediate(gen.config, true);

            if (newIndex >= 0)
            {
                var newConfig = ScriptableObject.CreateInstance(_typeList[newIndex]) as MaterialConfig;
                newConfig.name = "config";
                AssetDatabase.AddObjectToAsset(newConfig, gen);
                gen.config = newConfig;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(gen));
                AssetDatabase.SaveAssets();
            }

            if (configEditor != null)
            {
                DestroyImmediate(configEditor);
                configEditor = null;
            }
        }

        /// <summary>
        /// 绘制材质球字段
        /// </summary>
        private void DrawMaterialField(MaterialGenerate gen)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("目标材质球", gen.targetMaterial, typeof(Material), false);
            EditorGUI.EndDisabledGroup();

            if (gen.targetMaterial == null)
            {
                EditorGUILayout.HelpBox("点击下方按钮自动创建材质球", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"自动创建: {gen.isAutoCreatedMaterial}", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 绘制配置参数的嵌套 Inspector
        /// </summary>
        private void DrawConfigInspector(MaterialGenerate gen)
        {
            if (gen.config != null)
            {
                if (configEditor == null || configEditor.target != gen.config)
                {
                    if (configEditor != null)
                        DestroyImmediate(configEditor);
                    configEditor = CreateEditor(gen.config);
                }

                if (configEditor != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("材质配置参数", EditorStyles.boldLabel);
                    configEditor.OnInspectorGUI();
                }
            }
            else
            {
                if (configEditor != null)
                {
                    DestroyImmediate(configEditor);
                    configEditor = null;
                }
                EditorGUILayout.HelpBox("请先选择材质类型", MessageType.Info);
            }
        }

        /// <summary>
        /// 绘制生成按钮
        /// </summary>
        private void DrawGenerateButton(MaterialGenerate gen)
        {
            if (GUILayout.Button("一键生成/更新材质"))
            {
                string error;
                if (!AssetDirectoryChecker.CheckMaterialGenerateDirectory(gen, out error))
                {
                    EditorUtility.DisplayDialog("目录规范检查未通过", error, "终止");
                    return;
                }

                if (gen.targetMaterial == null)
                {
                    gen.AutoCreateAndAssignMaterial();
                    EditorUtility.SetDirty(gen);
                    AssetDatabase.SaveAssets();
                }

                gen.Generate();
                if (gen.targetMaterial)
                {
                    EditorUtility.SetDirty(gen.targetMaterial);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        /// <summary>
        /// 绘制命名同步按钮
        /// </summary>
        private void DrawSyncNameButton(MaterialGenerate gen)
        {
            if (GUILayout.Button("同步材质球命名为本资源名"))
            {
                gen.SyncMaterialNameWithGenerate();
                EditorUtility.SetDirty(gen);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnDisable()
        {
            if (configEditor != null)
            {
                DestroyImmediate(configEditor);
                configEditor = null;
            }
        }
    }
}
