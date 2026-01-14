using UnityEngine;
using UnityEditor;
using VertexPainter.Core;

namespace VertexPainter.Logic
{
    /// <summary>
    /// 绘制工具接口
    /// </summary>
    public interface IPaintTool
    {
        void OnEnter(PainterContext context);
        void OnExit();
        void OnSceneGUI(SceneView sceneView);
        void OnInspectorGUI();
    }
}
