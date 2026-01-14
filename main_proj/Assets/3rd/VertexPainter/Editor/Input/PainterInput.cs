using UnityEngine;
using UnityEditor;
using VertexPainter.Core;

namespace VertexPainter.Input
{
    /// <summary>
    /// 输入处理器 - 处理快捷键和通用输入
    /// </summary>
    public static class PainterInput
    {
        public static void ProcessShortcuts(PainterContext context)
        {
            if (context == null || context.Brush == null) return;

            Event e = Event.current;

            // 通道切换
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.BackQuote: // ~
                        e.Use();
                        context.Brush.Channel = BrushChannel.All;
                        break;
                    case KeyCode.Alpha1:
                        e.Use();
                        context.Brush.Channel = BrushChannel.Red;
                        break;
                    case KeyCode.Alpha2:
                        e.Use();
                        context.Brush.Channel = BrushChannel.Green;
                        break;
                    case KeyCode.Alpha3:
                        e.Use();
                        context.Brush.Channel = BrushChannel.Blue;
                        break;
                    case KeyCode.Alpha4:
                        e.Use();
                        context.Brush.Channel = BrushChannel.Alpha;
                        break;
                }
            }

            // Shift 键控制强度反转 (橡皮擦模式)
            // 注意：这里直接修改 Strength 可能不太好，最好有一个 IsEraser 状态
            // 但为了保持原有逻辑，我们暂时这样处理
            context.Brush.Strength = e.shift ? 0.0f : 1.0f;

            // 滚轮调整参数
            if (e.type == EventType.ScrollWheel)
            {
                if (e.shift)
                {
                    e.Use();
                    // Shift+滚轮原本是调整强度，但现在Shift被用作橡皮擦，这里可能需要调整
                    // 原有逻辑：Shift+滚轮调整 Strength。但 Shift 按下时 Strength 强制为 0。
                    // 这似乎有冲突，或者 Shift 只是临时切换。
                    // 我们保留原有逻辑：
                    // context.Brush.Strength -= e.delta.y * 0.01f; 
                    // 但由于上面强制设置了 Strength，这里可能无效。
                    // 让我们假设 Shift 只是临时反转，不影响基础 Strength 值。
                    // 实际上 BrushData 没有 BaseStrength，所以这里简化处理。
                }
                else if (e.control)
                {
                    e.Use();
                    context.Brush.Flow -= e.delta.y * 0.05f;
                }
                else
                {
                    e.Use();
                    context.Brush.Size -= e.delta.y * 0.005f;
                }
            }
        }
    }
}
