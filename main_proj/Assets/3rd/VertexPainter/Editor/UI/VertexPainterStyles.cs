using UnityEngine;
using UnityEditor;

namespace VertexPainter.UI
{
    /// <summary>
    /// UI 样式定义
    /// </summary>
    public static class VertexPainterStyles
    {
        // 颜色定义
        public static readonly Color EnabledColor = new Color(0.4f, 0.8f, 0.4f);
        public static readonly Color DisabledColor = new Color(0.8f, 0.4f, 0.4f);
        public static readonly Color FillButtonColor = new Color(0.6f, 0.8f, 1.0f);
        public static readonly Color SaveButtonColor = new Color(0.8f, 0.8f, 0.8f);

        private static GUIStyle _bigButtonStyle;

        /// <summary>
        /// 大按钮样式
        /// </summary>
        public static GUIStyle BigButtonStyle
        {
            get
            {
                if (_bigButtonStyle == null)
                {
                    _bigButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 14,
                        fontStyle = FontStyle.Bold
                    };
                }
                return _bigButtonStyle;
            }
        }

        /// <summary>
        /// 绘制分隔线
        /// </summary>
        public static void DrawSeparator()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        /// <summary>
        /// 添加间距
        /// </summary>
        public static void AddSpace(float pixels = 5f)
        {
            EditorGUILayout.Space(pixels);
        }

        /// <summary>
        /// 绘制区块（带标题）
        /// </summary>
        public static void DrawSection(string title, System.Action content)
        {
            if (!string.IsNullOrEmpty(title))
                GUILayout.Label(title, EditorStyles.boldLabel);

            content?.Invoke();

            AddSpace();
            DrawSeparator();
            AddSpace();
        }
    }
}
