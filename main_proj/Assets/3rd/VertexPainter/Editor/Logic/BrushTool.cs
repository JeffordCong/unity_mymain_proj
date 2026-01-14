using UnityEngine;
using UnityEditor;
using VertexPainter.Core;
using VertexPainter.Tools;

namespace VertexPainter.Logic
{
    /// <summary>
    /// 标准笔刷工具
    /// </summary>
    public class BrushTool : IPaintTool
    {
        private PainterContext context;
        private bool isPainting = false;
        private bool isDragging = false;
        private bool[] objectEdits;

        public void OnEnter(PainterContext context)
        {
            this.context = context;
            objectEdits = new bool[context.Objects != null ? context.Objects.Length : 0];
        }

        public void OnExit()
        {
            isPainting = false;
            isDragging = false;
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (context == null || context.Objects == null) return;

            HandleMouseEvents(sceneView);

            if (Event.current.type == EventType.Repaint || isPainting)
            {
                ProcessPainting(sceneView);
            }
        }

        public void OnInspectorGUI()
        {
            // 可以在这里绘制笔刷特有的设置
        }

        private void HandleMouseEvents(SceneView sceneView)
        {
            Event e = Event.current;

            if (e.rawType == EventType.MouseUp)
            {
                EndStroke();
                isDragging = false;
                isPainting = false;
            }

            if (e.isMouse && isPainting)
            {
                e.Use();
            }

            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
            }

            if (e.alt) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isDragging = true;
                isPainting = true;
                System.Array.Clear(objectEdits, 0, objectEdits.Length);
            }
        }

        private void ProcessPainting(SceneView sceneView)
        {
            RaycastHit hit;
            float distance = float.MaxValue;
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.forward;

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            // 查找最近的击中点
            for (int i = 0; i < context.Objects.Length; i++)
            {
                PaintingObject obj = context.Objects[i];
                if (obj == null || obj.meshFilter == null) continue;

                Bounds bounds = obj.renderer.bounds;
                bounds.Expand(context.Brush.Size * 2);
                if (!bounds.IntersectRay(ray)) continue;

                Matrix4x4 mtx = obj.meshFilter.transform.localToWorldMatrix;
                Mesh mesh = GetActiveMesh(obj);

                if (RayMesh.IntersectRayMesh(ray, mesh, mtx, out hit))
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        hitPoint = hit.point;
                        hitNormal = hit.normal;
                        
                        // 法线修正
                        if (hitNormal.magnitude < 0.1f)
                        {
                             RayMesh.IntersectRayMesh(ray, obj.meshFilter.sharedMesh, mtx, out hit);
                             hitNormal = hit.normal;
                        }
                    }
                }
            }

            // 如果没有击中任何物体，直接返回
            if (distance == float.MaxValue) return;

            // 应用绘制
            if (isPainting)
            {
                for (int i = 0; i < context.Objects.Length; i++)
                {
                    PaintingObject obj = context.Objects[i];
                    RegisterUndo(obj, i);
                    PaintVertices(obj, hitPoint);
                }
            }
            
            // 存储击中信息供 Visualizer 使用 (这里我们可以通过 Context 或者事件传递，
            // 但为了简化，我们假设 Visualizer 会自己做射线检测，或者我们将击中点存储在 Context 中)
            // 为了解耦，这里只负责逻辑。Visualizer 可以独立运行。
        }

        private Mesh GetActiveMesh(PaintingObject obj)
        {
            if (obj.HasStream())
            {
                ColorApplicator.InitializeColors(obj);
                return obj.stream.GetModifierMesh();
            }
            obj.EnforceStream();
            return obj.meshFilter.sharedMesh;
        }

        private void RegisterUndo(PaintingObject obj, int index)
        {
            if (!objectEdits[index])
            {
                objectEdits[index] = true;
                Undo.RegisterCompleteObjectUndo(obj.stream, "Vertex Painter Stroke");
            }
        }

        private void PaintVertices(PaintingObject obj, Vector3 point)
        {
            var mtx = obj.renderer.transform.localToWorldMatrix;
            Vector3 localPoint = obj.renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(point);

            float scale = 1.0f / Mathf.Abs(obj.renderer.transform.lossyScale.x);
            float brushSizeSquared = (scale * context.Brush.Size) * (scale * context.Brush.Size);
            
            bool affected = false;

            // 使用空间分区优化
            System.Collections.Generic.List<int> verticesToCheck;
            if (context.SpatialGrids.ContainsKey(obj))
            {
                verticesToCheck = context.SpatialGrids[obj].GetNearbyVertices(localPoint);
            }
            else
            {
                verticesToCheck = new System.Collections.Generic.List<int>(obj.verts.Length);
                for(int i=0; i<obj.verts.Length; i++) verticesToCheck.Add(i);
            }

            foreach (int i in verticesToCheck)
            {
                Vector3 vert = obj.verts[i];
                float distSquared = (localPoint - vert).sqrMagnitude;

                if (distSquared < brushSizeSquared)
                {
                    float pressure = Event.current.pressure > 0 ? Event.current.pressure : 1.0f;
                    float strength = 1.0f - distSquared / brushSizeSquared;
                    strength = Mathf.Pow(strength, context.Brush.Falloff);
                    float finalStrength = strength * Time.deltaTime * (context.Brush.Flow / 8) * pressure;

                    if (finalStrength > 0)
                    {
                        affected = true;
                        obj.stream.colors[i] = ColorApplicator.ApplyColor(
                            obj.stream.colors[i],
                            context.Brush.Color,
                            context.Brush.Channel,
                            finalStrength,
                            context.Settings.WeightMode);
                    }
                }
            }

            if (affected)
            {
                obj.stream.Apply();
            }
        }

        private void EndStroke()
        {
            foreach (var obj in context.Objects)
            {
                if (obj != null && obj.HasStream())
                {
                    EditorUtility.SetDirty(obj.stream);
                    EditorUtility.SetDirty(obj.stream.gameObject);
                }
            }
        }
    }
}
