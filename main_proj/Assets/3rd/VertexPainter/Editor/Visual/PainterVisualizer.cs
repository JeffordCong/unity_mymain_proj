using UnityEngine;
using UnityEditor;
using VertexPainter.Core;
using VertexPainter.Tools;
using System.Collections.Generic;

namespace VertexPainter.Visual
{
    /// <summary>
    /// 可视化器 - 负责绘制 Scene 视图中的辅助显示
    /// </summary>
    public static class PainterVisualizer
    {
        public static void DrawSceneGUI(PainterContext context)
        {
            if (context == null || !context.Settings.Enabled) return;

            // 绘制笔刷光圈
            DrawBrushDisc(context);

            // 绘制顶点点
            if (context.Settings.ShowPoints)
            {
                DrawVertexPoints(context);
            }
        }

        private static void DrawBrushDisc(PainterContext context)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            // 简单的射线检测用于显示笔刷位置
            // 注意：这里为了性能，可能不需要精确检测所有 Mesh，或者复用 BrushTool 的检测结果
            // 但为了解耦，我们这里做一个简化的检测，或者只在有击中时绘制

            // 实际上，为了更好的体验，Visualizer 应该知道当前的击中点。
            // 由于我们没有共享状态，这里重新做一次射线检测可能会浪费性能。
            // 但考虑到 Editor 模式下的帧率要求不高，且我们有 SpatialGrid，可以接受。
            // 或者我们可以让 BrushTool 计算好位置存在 Context 中？
            // 让我们在 Context 中加一个 LastHitPoint 吧？
            // 为了保持 Context 纯净，我们还是在这里做检测。

            float distance = float.MaxValue;
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.forward;
            bool hasHit = false;

            if (context.Objects == null) return;

            foreach (var obj in context.Objects)
            {
                if (obj == null || obj.meshFilter == null) continue;

                Bounds bounds = obj.renderer.bounds;
                if (!bounds.IntersectRay(ray)) continue;

                // 使用 RayMesh 进行射线检测
                Matrix4x4 mtx = obj.renderer.transform.localToWorldMatrix;
                Mesh mesh = obj.meshFilter.sharedMesh;

                if (RayMesh.IntersectRayMesh(ray, mesh, mtx, out hit))
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        hitPoint = hit.point;
                        hitNormal = hit.normal;
                        hasHit = true;
                    }
                }
            }

            if (hasHit)
            {
                Color displayColor = context.Brush.GetDisplayColor();
                Handles.color = new Color(displayColor.r, displayColor.g, displayColor.b, 1.0f);

                float innerRadius = Mathf.Pow(0.5f, context.Brush.Falloff);
                Handles.DrawWireDisc(hitPoint, hitNormal, context.Brush.Size * innerRadius);
                Handles.DrawWireDisc(hitPoint, hitNormal, context.Brush.Size);

                Handles.color = Color.white;
                Handles.DrawLine(hitPoint, hitPoint + hitNormal * 0.2f);
            }
        }

        private static void DrawVertexPoints(PainterContext context)
        {
            if (context.Objects == null) return;

            // 获取鼠标位置用于范围剔除
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            // 这里我们只显示鼠标周围的顶点，或者显示所有？
            // 原有逻辑是 DrawVertexPoints(obj, hitPoint)，只显示笔刷范围内的。
            // 我们需要击中点。

            // 为了避免重复射线检测，我们这里简化：只在 BrushTool 激活时显示？
            // 或者我们确实需要一个共享状态。
            // 让我们简化逻辑：只显示笔刷范围内的点，且需要重新射线检测。
            // 考虑到代码量，我们把射线检测提取出来？

            // 重新检测一次
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit)) return; // 简化检测

            foreach (var obj in context.Objects)
            {
                if (obj == null) continue;

                // 性能检查
                if (!context.PerfLevels.ContainsKey(obj)) continue;
                var perfLevel = context.PerfLevels[obj];
                float displayRatio = Tools.PerformanceMonitor.GetDisplayRatio(perfLevel);
                if (displayRatio <= 0) continue;

                DrawPointsForObject(context, obj, hit.point, displayRatio);
            }
        }

        private static void DrawPointsForObject(PainterContext context, PaintingObject obj, Vector3 hitPoint, float displayRatio)
        {
            var mtx = obj.renderer.transform.localToWorldMatrix;
            Vector3 localHitPoint = obj.renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(hitPoint);

            float scale = 1.0f / Mathf.Abs(obj.renderer.transform.lossyScale.x);
            float brushSizeSquared = (scale * context.Brush.Size) * (scale * context.Brush.Size);

            List<int> verticesToCheck;
            if (context.SpatialGrids.ContainsKey(obj))
            {
                verticesToCheck = context.SpatialGrids[obj].GetNearbyVertices(localHitPoint);
            }
            else
            {
                // 如果没有空间分区且顶点数多，可能卡顿，但这里只在 Brush 范围内
                // 如果没有空间分区，我们就不显示了？或者全遍历？
                // 原有逻辑是全遍历。
                verticesToCheck = new List<int>(); // 应该缓存
                // 这里全遍历太慢，暂不实现全遍历显示，除非顶点少
                if (obj.verts.Length < 1000)
                {
                    for (int i = 0; i < obj.verts.Length; i++) verticesToCheck.Add(i);
                }
                else
                {
                    return;
                }
            }

            int step = Mathf.Max(1, (int)(1f / displayRatio));

            foreach (int i in verticesToCheck)
            {
                if (i % step != 0) continue;

                Vector3 vert = obj.verts[i];
                if ((localHitPoint - vert).sqrMagnitude < brushSizeSquared)
                {
                    Color c = GetVertexDisplayColor(obj, i, context.Brush.Channel);
                    Vector3 worldPos = mtx.MultiplyPoint(vert);
                    Handles.color = c;
                    Handles.DotHandleCap(0, worldPos, Quaternion.identity,
                        HandleUtility.GetHandleSize(worldPos) * 0.03f, EventType.Repaint);
                }
            }
        }

        private static Color GetVertexDisplayColor(PaintingObject obj, int index, BrushChannel channel)
        {
            if (obj.stream.colors == null || index >= obj.stream.colors.Length) return Color.white;
            Color c = obj.stream.colors[index];
            switch (channel)
            {
                case BrushChannel.Red: return new Color(c.r, 0, 0, 1);
                case BrushChannel.Green: return new Color(0, c.g, 0, 1);
                case BrushChannel.Blue: return new Color(0, 0, c.b, 1);
                case BrushChannel.Alpha: return new Color(c.a, c.a, c.a, 1);
                default: return c;
            }
        }
    }
}
