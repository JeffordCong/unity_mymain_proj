using UnityEngine;
using UnityEditor;
using VertexPainter.Core;

namespace VertexPainter.Logic
{
    /// <summary>
    /// 填充工具
    /// </summary>
    public class FillTool : IPaintTool
    {
        private PainterContext context;

        public void OnEnter(PainterContext context)
        {
            this.context = context;
        }

        public void OnExit()
        {
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            // 填充工具不需要 SceneGUI 交互，它通常通过按钮触发
        }

        public void OnInspectorGUI()
        {
            if (GUILayout.Button("填充所有顶点", GUILayout.Height(30)))
            {
                FillAll();
            }
        }

        public void FillAll()
        {
            if (context == null || context.Objects == null) return;

            foreach (var obj in context.Objects)
            {
                Undo.RecordObject(obj.stream, "Fill Vertex Color");
                ColorApplicator.FillAll(obj, context.Brush.Color, context.Brush.Channel, context.Settings.WeightMode);
                EditorUtility.SetDirty(obj.stream);
            }
        }
    }
}
