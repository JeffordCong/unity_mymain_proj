using UnityEngine;
using UnityEditor;
using VertexPainter.Core;

namespace VertexPainter.UI
{
    /// <summary>
    /// Vertex Painter UI 绘制逻辑 - 扩展版本
    /// </summary>
    public static class VertexPainterUI
    {
        /// <summary>
        /// 绘制主功能选择界面 (UI 1)
        /// </summary>
        public static void DrawMainModeSelection(System.Action<VertexPainterWindow.PainterMode> onSelect)
        {
            EditorGUILayout.Space(20);
            GUILayout.Label("请选择工作模式", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 1. 绘制模式
            GUI.backgroundColor = new Color(0.7f, 1.0f, 0.7f); // 淡绿色
            if (GUILayout.Button("🎨 进入绘制模式\n(顶点颜色刷涂)", VertexPainterStyles.BigButtonStyle, GUILayout.Height(70)))
            {
                onSelect?.Invoke(VertexPainterWindow.PainterMode.Paint);
            }

            EditorGUILayout.Space(10);

            // 2. Debug 模式
            GUI.backgroundColor = new Color(0.7f, 0.85f, 1.0f); // 淡蓝色
            if (GUILayout.Button("🔍 进入 Debug 模式\n(查看顶点颜色材质)", VertexPainterStyles.BigButtonStyle, GUILayout.Height(70)))
            {
                onSelect?.Invoke(VertexPainterWindow.PainterMode.Debug);
            }

            EditorGUILayout.Space(10);

            // 3. 清理模式
            GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f); // 淡红色
            if (GUILayout.Button("🧹 进入清理模式\n(移除 Painting Data 组件)", VertexPainterStyles.BigButtonStyle, GUILayout.Height(70)))
            {
                onSelect?.Invoke(VertexPainterWindow.PainterMode.Cleanup);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("提示：在任何模式下都可以点击顶部的「返回主菜单」重新选择。", MessageType.None);
        }

        // --- 以下为原有的绘制方法，保持不变 ---

        public static void DrawModeToggle(PainterSettings settings, System.Action<bool> onToggle)
        {
            string text = settings.Enabled ? "● 绘制模式 (点击退出)" : "○ 启动绘制模式";
            GUI.backgroundColor = settings.Enabled ? VertexPainterStyles.EnabledColor : VertexPainterStyles.DisabledColor;
            if (GUILayout.Button(text, VertexPainterStyles.BigButtonStyle, GUILayout.Height(45))) onToggle?.Invoke(!settings.Enabled);
            GUI.backgroundColor = Color.white;
        }

        public static void DrawDisplayOptions(PainterSettings settings)
        {
            VertexPainterStyles.DrawSection("显示选项", () =>
            {
                settings.ShowPoints = EditorGUILayout.Toggle("显示顶点", settings.ShowPoints);
                settings.WeightMode = EditorGUILayout.Toggle("权重模式", settings.WeightMode);
            });
        }

        public static void DrawHelpBox()
        {
            VertexPainterStyles.DrawSection("快捷键", () =>
            {
                EditorGUILayout.HelpBox("• 滚轮: 大小 | Ctrl+滚轮: 强度 | Shift: 反向\n• 1~4: R/G/B/A 通道 | ~: 全通道", MessageType.None);
            });
        }

        public static void DrawChannelSelector(BrushData brush)
        {
            if (brush == null) return;
            VertexPainterStyles.DrawSection("颜色通道", () =>
            {
                brush.Channel = (BrushChannel)GUILayout.Toolbar((int)brush.Channel, ChannelConfig.GetChannelNames(), GUILayout.Height(25));
            });
        }

        public static void DrawColorPicker(BrushData brush)
        {
            if (brush == null) return;
            VertexPainterStyles.DrawSection("笔刷颜色", () =>
            {
                EditorGUILayout.BeginHorizontal();
                brush.Color = EditorGUILayout.ColorField(GUIContent.none, brush.Color, true, true, false, GUILayout.Height(30), GUILayout.Width(60));
                EditorGUILayout.LabelField($"R:{brush.Color.r:F2} G:{brush.Color.g:F2} B:{brush.Color.b:F2} A:{brush.Color.a:F2}");
                EditorGUILayout.EndHorizontal();
            });
        }

        public static void DrawBrushSettings(BrushData brush)
        {
            if (brush == null) return;
            VertexPainterStyles.DrawSection("笔刷参数", () =>
            {
                brush.Size = EditorGUILayout.Slider("大小 (Size)", brush.Size, 0.01f, 20.0f);
                brush.Flow = EditorGUILayout.Slider("强度 (Opacity)", brush.Flow, 0.1f, 2.0f);
                brush.Falloff = EditorGUILayout.Slider("衰减 (Falloff)", brush.Falloff, 0.1f, 3.5f);
            });
        }

        public static void DrawActionButtons(System.Action onFill, System.Action onSave)
        {
            VertexPainterStyles.AddSpace(8);
            if (onFill != null)
            {
                GUI.backgroundColor = VertexPainterStyles.FillButtonColor;
                if (GUILayout.Button("填充当前颜色到所有顶点", GUILayout.Height(35))) onFill.Invoke();
                GUI.backgroundColor = Color.white;
            }
            if (onSave != null)
            {
                VertexPainterStyles.AddSpace();
                GUI.backgroundColor = VertexPainterStyles.SaveButtonColor;
                if (GUILayout.Button("保存并导出 FBX", GUILayout.Height(35))) onSave.Invoke();
                GUI.backgroundColor = Color.white;
            }
        }

        public static void DrawPerformanceWarning(int totalVertexCount, Tools.PerformanceMonitor.PerformanceLevel perfLevel)
        {
            if (perfLevel == Tools.PerformanceMonitor.PerformanceLevel.Excellent) return;
            string message = Tools.PerformanceMonitor.GetPerformanceMessage(perfLevel, totalVertexCount);
            MessageType msgType = (perfLevel == Tools.PerformanceMonitor.PerformanceLevel.Critical) ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox(message, msgType);
        }

        public static void DrawNoSelectionUI(System.Action onSave)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("请选择一个包含 Mesh 的游戏对象以启动绘制", MessageType.Info);
            DrawActionButtons(null, onSave);
        }

        public static VertexPainterWindow.DebugChannel DrawDebugChannelSelector(VertexPainterWindow.DebugChannel current)
        {
            string[] names = { "RGB", "Red (R)", "Green (G)", "Blue (B)", "Alpha (A)" };

            // 使用 Toolbar 展现切换按钮
            return (VertexPainterWindow.DebugChannel)GUILayout.Toolbar(
                (int)current,
                names,
                GUILayout.Height(30)
            );
        }
    }
}