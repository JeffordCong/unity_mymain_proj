using UnityEngine;

namespace VertexPainter.Core
{
    /// <summary>
    /// 颜色应用器 - 负责顶点颜色计算和应用
    /// </summary>
    public static class ColorApplicator
    {
        /// <summary>
        /// 应用颜色到单个顶点
        /// </summary>
        public static Color ApplyColor(
            Color currentColor,
            Color brushColor,
            BrushChannel channel,
            float strength,
            bool weightMode)
        {
            Color targetColor;

            switch (channel)
            {
                case BrushChannel.All:
                    targetColor = brushColor;
                    break;

                case BrushChannel.Red:
                    targetColor = currentColor;
                    targetColor.r = brushColor.r;
                    if (weightMode)
                        ApplyWeightMode(ref targetColor, targetColor.r);
                    break;

                case BrushChannel.Green:
                    targetColor = currentColor;
                    targetColor.g = brushColor.g;
                    if (weightMode)
                        ApplyWeightMode(ref targetColor, targetColor.g);
                    break;

                case BrushChannel.Blue:
                    targetColor = currentColor;
                    targetColor.b = brushColor.b;
                    if (weightMode)
                        ApplyWeightMode(ref targetColor, targetColor.b);
                    break;

                case BrushChannel.Alpha:
                    targetColor = currentColor;
                    targetColor.a = brushColor.a;
                    if (weightMode)
                        ApplyWeightMode(ref targetColor, targetColor.a);
                    break;

                default:
                    targetColor = brushColor;
                    break;
            }

            return Color.Lerp(currentColor, targetColor, strength);
        }

        /// <summary>
        /// 应用权重模式（反色）
        /// </summary>
        private static void ApplyWeightMode(ref Color color, float channelValue)
        {
            float inverted = 1.0f - channelValue;
            color.r = inverted;
            color.g = inverted;
            color.b = inverted;
            color.a = inverted;
        }

        /// <summary>
        /// 填充所有顶点
        /// </summary>
        public static void FillAll(
            PaintingObject paintingObject,
            Color brushColor,
            BrushChannel channel,
            bool weightMode)
        {
            if (paintingObject == null || paintingObject.stream == null)
                return;

            Color[] colors = paintingObject.stream.colors;
            if (colors == null) return;

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = ApplyColor(colors[i], brushColor, channel, 1f, weightMode);
            }

            paintingObject.stream.Apply();
        }

        /// <summary>
        /// 初始化顶点颜色（如果不存在）
        /// </summary>
        public static void InitializeColors(PaintingObject paintingObject)
        {
            if (paintingObject.stream.colors != null && 
                paintingObject.stream.colors.Length == paintingObject.verts.Length)
                return;

            Color[] originalColors = paintingObject.meshFilter.sharedMesh.colors;

            if (originalColors != null && originalColors.Length > 0)
            {
                paintingObject.stream.colors = originalColors;
            }
            else
            {
                paintingObject.stream.SetColor(Color.white, paintingObject.verts.Length);
            }
        }
    }
}
