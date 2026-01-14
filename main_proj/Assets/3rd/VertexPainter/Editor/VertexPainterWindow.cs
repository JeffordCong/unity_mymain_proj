using UnityEngine;
using UnityEditor;
using VertexPainter.Core;
using VertexPainter.UI;
using VertexPainter.Logic;
using VertexPainter.Input;
using VertexPainter.Visual;
using System.Collections.Generic;

/// <summary>
/// Vertex Painter 主窗口 - 模块化重构版本
/// </summary>
public class VertexPainterWindow : EditorWindow
{
    // 定义三大功能模式
    public enum PainterMode { None, Paint, Debug, Cleanup }

    public enum DebugChannel { RGB = 0, R = 1, G = 2, B = 3, A = 4 }
    private DebugChannel currentDebugChannel = DebugChannel.RGB;


    #region 状态数据
    private PainterMode currentMode = PainterMode.None;
    private PainterContext context;
    private BrushData brush = new BrushData();
    private PainterSettings settings = new PainterSettings();

    // 工具接口
    private IPaintTool brushTool;
    private IPaintTool fillTool;
    private IPaintTool currentTool;
    private Tool lastTool = Tool.None;

    // 导出与配置
    private bool showExportSettings = false;
    private bool invertX, invertY, invertZ = false;
    private const string DEBUG_MAT_PATH = "Assets/3rd/VertexPainter/Debug/VetexColor Debug.mat";

    // 存储原始材质以便退出 Debug 模式时还原
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();

    // 锁定绘制模式下的选择
    private GameObject[] lockedSelection;
    #endregion

    [MenuItem("Tools/顶点色绘制工具", false, 1002)]
    public static void ShowWindow()
    {
        var window = GetWindow<VertexPainterWindow>();
        window.titleContent = new GUIContent("VertexPainter");
        window.Show();
    }

    void OnEnable()
    {
        brushTool = new BrushTool();
        fillTool = new FillTool();
        if (context == null) context = new PainterContext(brush, settings);
    }

    void OnGUI()
    {
        // 模式选择主界面 (UI 1)
        if (currentMode == PainterMode.None)
        {
            VertexPainterUI.DrawMainModeSelection(mode =>
            {
                currentMode = mode;
                if (mode == PainterMode.Paint) OnModeToggle(true);
                else if (mode == PainterMode.Debug) OnModeToggle(true); // Added for Debug
            });
            return;
        }

        // 顶部返回按钮
        if (GUILayout.Button("◀ 返回主菜单", GUILayout.Height(30)))
        {
            if (currentMode == PainterMode.Paint || currentMode == PainterMode.Debug) OnModeToggle(false);
            currentMode = PainterMode.None;
            return;
        }

        VertexPainterStyles.AddSpace(5);
        VertexPainterStyles.DrawSeparator();
        VertexPainterStyles.AddSpace(5);

        // 功能模块界面 (UI 2)
        switch (currentMode)
        {
            case PainterMode.Paint: DrawPaintModeGUI(); break;
            case PainterMode.Debug: DrawDebugModeGUI(); break;
            case PainterMode.Cleanup: DrawCleanupModeGUI(); break;
        }
    }

    #region 模块界面逻辑

    private void DrawPaintModeGUI()
    {
        if (Selection.activeGameObject == null)
        {
            VertexPainterUI.DrawNoSelectionUI(OnSave);
            return;
        }

        // 绘制模式特有的 UI 元素
        VertexPainterUI.DrawModeToggle(settings, OnModeToggle);

        if (settings.Enabled && context != null)
        {
            int totalVerts = context.GetTotalVertexCount();
            VertexPainterUI.DrawPerformanceWarning(totalVerts, VertexPainter.Tools.PerformanceMonitor.EvaluatePerformance(totalVerts));
        }

        VertexPainterUI.DrawDisplayOptions(settings);

        EditorGUI.BeginChangeCheck();
        settings.EnableRealtimePreview = EditorGUILayout.Toggle("启用实时预览", settings.EnableRealtimePreview);
        if (EditorGUI.EndChangeCheck() && context.Objects != null)
        {
            foreach (var obj in context.Objects) if (obj?._stream != null) obj._stream.SetPreviewEnabled(settings.EnableRealtimePreview);
        }

        VertexPainterUI.DrawHelpBox();
        if (settings.Enabled)
        {
            VertexPainterUI.DrawChannelSelector(brush);
            VertexPainterUI.DrawColorPicker(brush);
        }
        VertexPainterUI.DrawBrushSettings(brush);

        // 导出设置
        showExportSettings = EditorGUILayout.Foldout(showExportSettings, "导出设置 (手动修正坐标)", true);
        if (showExportSettings)
        {
            EditorGUI.indentLevel++;
            invertX = EditorGUILayout.Toggle("反转 X", invertX);
            invertY = EditorGUILayout.Toggle("反转 Y", invertY);
            invertZ = EditorGUILayout.Toggle("反转 Z", invertZ);
            EditorGUI.indentLevel--;
        }

        VertexPainterUI.DrawActionButtons(OnFill, OnSave);
    }

    private void DrawDebugModeGUI()
    {
        VertexPainterStyles.DrawSection("Debug 模式设置", () =>
        {
            EditorGUILayout.HelpBox("当前处于 Debug 模式。\n选中物体将自动应用 Debug 材质。\n选择下方通道可实时预览。", MessageType.Info);

            // 监听 UI 变化
            EditorGUI.BeginChangeCheck();
            currentDebugChannel = VertexPainterUI.DrawDebugChannelSelector(currentDebugChannel);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyDebugSettings(); // 实时应用变体切换
            }
        });
    }

    private void ApplyDebugSettings()
    {
        Material debugMat = AssetDatabase.LoadAssetAtPath<Material>(DEBUG_MAT_PATH);
        if (debugMat == null) return;

        // 记录撤销
        Undo.RecordObject(debugMat, "Change Debug Channel");

        // 设置属性值 (用于 Inspector 显示)
        debugMat.SetInt("_DebugMode", (int)currentDebugChannel);

        // 手动处理 Keyword (代码中修改必须显式调用)
        debugMat.DisableKeyword("_DEBUGMODE_RGB");
        debugMat.DisableKeyword("_DEBUGMODE_R");
        debugMat.DisableKeyword("_DEBUGMODE_G");
        debugMat.DisableKeyword("_DEBUGMODE_B");
        debugMat.DisableKeyword("_DEBUGMODE_A");

        switch (currentDebugChannel)
        {
            case DebugChannel.RGB: debugMat.EnableKeyword("_DEBUGMODE_RGB"); break;
            case DebugChannel.R: debugMat.EnableKeyword("_DEBUGMODE_R"); break;
            case DebugChannel.G: debugMat.EnableKeyword("_DEBUGMODE_G"); break;
            case DebugChannel.B: debugMat.EnableKeyword("_DEBUGMODE_B"); break;
            case DebugChannel.A: debugMat.EnableKeyword("_DEBUGMODE_A"); break;
        }

        // 强制 Scene 视图刷新
        SceneView.RepaintAll();
    }

    private void DrawCleanupModeGUI()
    {
        VertexPainterStyles.DrawSection("清理模式", () =>
        {
            EditorGUILayout.HelpBox("清理选中对象及其子层级中的 PaintingData 组件。", MessageType.Warning);
            if (GUILayout.Button("执行深度清理", GUILayout.Height(40))) CleanupPaintingData();
        });
    }

    #endregion

    #region 底层功能实现

    private void ApplyDebugMaterial()
    {
        if (currentMode != PainterMode.Debug) return;

        Material debugMat = AssetDatabase.LoadAssetAtPath<Material>(DEBUG_MAT_PATH);
        if (debugMat == null)
        {
            Debug.LogError($"[VertexPainter] 未找到材质: {DEBUG_MAT_PATH}");
            return;
        }

        foreach (var go in Selection.gameObjects)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != debugMat)
            {
                // 记录原始材质
                if (!originalMaterials.ContainsKey(r))
                {
                    originalMaterials[r] = r.sharedMaterial;
                }

                Undo.RecordObject(r, "Apply Debug Material");
                r.sharedMaterial = debugMat;
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                Undo.RecordObject(kvp.Key, "Restore Original Material");
                kvp.Key.sharedMaterial = kvp.Value;
            }
        }
        originalMaterials.Clear();
    }

    private void CleanupPaintingData()
    {
        GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Deep);
        int count = 0;
        foreach (var go in selected)
        {
            // 使用组件名称或类型查找
            Component data = go.GetComponent("PaintingData");
            if (data != null)
            {
                Undo.DestroyObjectImmediate(data);
                count++;
            }
        }
        Debug.Log($"[VertexPainter] 清理完成，移除组件数量: {count}");
    }

    private void RemovePaintingDataComponents()
    {
        if (context?.Objects == null) return;

        foreach (var obj in context.Objects)
        {
            if (obj != null && obj._stream != null)
            {
                Undo.DestroyObjectImmediate(obj._stream);
            }
        }
    }

    private bool ValidateMeshReadability()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0) return true;

        List<string> unreadablePaths = new List<string>();

        foreach (var go in Selection.gameObjects)
        {
            Mesh mesh = null;
            var filter = go.GetComponent<MeshFilter>();
            if (filter != null) mesh = filter.sharedMesh;
            else
            {
                var skinned = go.GetComponent<SkinnedMeshRenderer>();
                if (skinned != null) mesh = skinned.sharedMesh;
            }

            if (mesh != null && !mesh.isReadable)
            {
                string path = AssetDatabase.GetAssetPath(mesh);
                if (!string.IsNullOrEmpty(path))
                {
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        if (!unreadablePaths.Contains(path))
                        {
                            unreadablePaths.Add(path);
                        }
                    }
                }
            }
        }

        if (unreadablePaths.Count > 0)
        {
            string message = "以下模型的 'Read/Write Enabled' 未开启，无法进行绘制：\n\n";
            foreach (var p in unreadablePaths) message += $"- {p}\n";
            message += "\n是否自动开启并重新导入？";

            if (EditorUtility.DisplayDialog("提示", message, "开启并重导", "取消"))
            {
                foreach (var path in unreadablePaths)
                {
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private void InitializePainter()
    {
        if (context.Objects != null) MeshManager.Cleanup(context.Objects);
        var objects = MeshManager.InitializeFromSelection();
        context.SetPaintingObjects(objects);
        SwitchTool(brushTool);
    }

    private void SwitchTool(IPaintTool newTool)
    {
        if (currentTool != null) currentTool.OnExit();
        currentTool = newTool;
        if (currentTool != null) currentTool.OnEnter(context);
    }

    private void OnModeToggle(bool enabled)
    {
        settings.Enabled = enabled;
        if (enabled)
        {
            if (currentMode == PainterMode.Paint)
            {
                // 检查模型读写权限
                if (!ValidateMeshReadability())
                {
                    settings.Enabled = false;
                    currentMode = PainterMode.None;
                    return;
                }

                lockedSelection = Selection.gameObjects; // 锁定选择
                InitializePainter();
                lastTool = Tools.current;
                Tools.current = Tool.None;
            }
            else if (currentMode == PainterMode.Debug)
            {
                ApplyDebugMaterial();
            }
        }
        else
        {
            // 退出 Debug 模式时还原材质
            if (currentMode == PainterMode.Debug)
            {
                RestoreOriginalMaterials();
            }
            // 退出 Paint 模式时清理组件
            else if (currentMode == PainterMode.Paint)
            {
                lockedSelection = null; // 解除锁定
                RemovePaintingDataComponents();
            }

            Tools.current = lastTool;
            if (currentTool != null) { currentTool.OnExit(); currentTool = null; }
        }
    }

    private void OnFill() { if (fillTool is FillTool fill) { fill.OnEnter(context); fill.FillAll(); } }

    private void OnSave() { if (context?.Objects != null) VertexPainterFbxExporter.ExportToFBX(context.Objects, invertX, invertY, invertZ); }

    private void OnUndo() { if (context.Objects == null) return; foreach (var job in context.Objects) if (job?.stream != null) job.stream.Apply(false); }

    void OnSceneGUI(SceneView sceneView)
    {
        if (currentMode != PainterMode.Paint || !settings.Enabled || context?.Objects == null) return;
        if (Tools.current != Tool.None) { lastTool = Tools.current; Tools.current = Tool.None; }
        PainterInput.ProcessShortcuts(context);
        if (currentTool != null) currentTool.OnSceneGUI(sceneView);
        PainterVisualizer.DrawSceneGUI(context);
    }

    void OnFocus() { SceneView.duringSceneGui -= OnSceneGUI; SceneView.duringSceneGui += OnSceneGUI; Undo.undoRedoPerformed -= OnUndo; Undo.undoRedoPerformed += OnUndo; }
    void OnDestroy()
    {
        if (context != null)
        {
            MeshManager.Cleanup(context.Objects);
            RemovePaintingDataComponents(); // 窗口关闭时自动清理
        }
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndo;
    }
    void OnSelectionChange()
    {
        if (currentMode == PainterMode.Paint)
        {
            // 如果锁定的选择丢失（例如编译后），则重新锁定当前选择
            if (lockedSelection == null)
            {
                lockedSelection = Selection.gameObjects;
            }

            if (lockedSelection != null)
            {
                // 检查选择是否发生变化
                bool selectionChanged = false;
                if (Selection.gameObjects.Length != lockedSelection.Length)
                {
                    selectionChanged = true;
                }
                else
                {
                    for (int i = 0; i < lockedSelection.Length; i++)
                    {
                        if (!System.Array.Exists(Selection.gameObjects, x => x == lockedSelection[i]))
                        {
                            selectionChanged = true;
                            break;
                        }
                    }
                }

                if (selectionChanged)
                {
                    // 强制还原选择
                    // 使用 delayCall 避免在当前事件处理中被覆盖
                    var restore = lockedSelection;
                    EditorApplication.delayCall += () =>
                    {
                        if (restore != null) Selection.objects = restore;
                    };
                    return;
                }
            }
            InitializePainter();
        }
        else if (currentMode == PainterMode.Debug) ApplyDebugMaterial();
        Repaint();
    }
    #endregion
}