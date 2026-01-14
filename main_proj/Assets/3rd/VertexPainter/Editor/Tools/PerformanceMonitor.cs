using UnityEngine;

namespace VertexPainter.Tools
{
    /// <summary>
    /// 性能监控器 - 根据顶点数量评估性能等级并提供优化建议
    /// </summary>
    public static class PerformanceMonitor
    {
        /// <summary>
        /// 性能等级
        /// </summary>
        public enum PerformanceLevel
        {
            Excellent,  // < 2,000 顶点
            Good,       // 2,000 - 5,000
            Fair,       // 5,000 - 10,000
            Poor,       // 10,000 - 20,000
            Critical    // > 20,000
        }

        // 性能阈值配置
        public static class Thresholds
        {
            public const int Excellent = 2000;
            public const int Good = 5000;
            public const int Fair = 10000;
            public const int Poor = 20000;
        }

        /// <summary>
        /// 评估性能等级
        /// </summary>
        public static PerformanceLevel EvaluatePerformance(int vertexCount)
        {
            if (vertexCount < Thresholds.Excellent) return PerformanceLevel.Excellent;
            if (vertexCount < Thresholds.Good) return PerformanceLevel.Good;
            if (vertexCount < Thresholds.Fair) return PerformanceLevel.Fair;
            if (vertexCount < Thresholds.Poor) return PerformanceLevel.Poor;
            return PerformanceLevel.Critical;
        }

        /// <summary>
        /// 获取显示比例（降采样）
        /// </summary>
        public static float GetDisplayRatio(PerformanceLevel level)
        {
            switch (level)
            {
                case PerformanceLevel.Excellent:
                    return 1.0f;    // 100% 显示

                case PerformanceLevel.Good:
                    return 0.5f;    // 50% 显示

                case PerformanceLevel.Fair:
                    return 0.25f;   // 25% 显示

                case PerformanceLevel.Poor:
                    return 0.1f;    // 10% 显示

                case PerformanceLevel.Critical:
                    return 0.0f;    // 禁用显示

                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 获取性能提示消息
        /// </summary>
        public static string GetPerformanceMessage(PerformanceLevel level, int vertexCount)
        {
            switch (level)
            {
                case PerformanceLevel.Excellent:
                    return null;

                case PerformanceLevel.Good:
                    return $"顶点数 {vertexCount:N0}，已降采样至 50% 以提升性能";

                case PerformanceLevel.Fair:
                    return $"顶点数 {vertexCount:N0}，已降采样至 25% 以提升性能";

                case PerformanceLevel.Poor:
                    return $"顶点数 {vertexCount:N0}，性能较差，已降采样至 10%";

                case PerformanceLevel.Critical:
                    return $"顶点数 {vertexCount:N0} 过多，已禁用顶点显示\n建议：关闭「显示顶点」或使用更小的模型";

                default:
                    return null;
            }
        }

        /// <summary>
        /// 是否应该使用空间分区
        /// </summary>
        public static bool ShouldUseSpatialPartitioning(int vertexCount)
        {
            return vertexCount >= Thresholds.Good;
        }
    }
}
