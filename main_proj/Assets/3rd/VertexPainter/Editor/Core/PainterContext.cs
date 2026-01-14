using UnityEngine;
using System.Collections.Generic;

namespace VertexPainter.Core
{
    /// <summary>
    /// 绘制上下文 - 持有所有核心数据的引用
    /// </summary>
    public class PainterContext
    {
        public BrushData Brush { get; private set; }
        public PainterSettings Settings { get; private set; }
        public PaintingObject[] Objects { get; private set; }

        // 性能优化数据
        public Dictionary<PaintingObject, Tools.SpatialGrid> SpatialGrids { get; private set; }
        public Dictionary<PaintingObject, Tools.PerformanceMonitor.PerformanceLevel> PerfLevels { get; private set; }

        public PainterContext(BrushData brush, PainterSettings settings)
        {
            Brush = brush;
            Settings = settings;
            SpatialGrids = new Dictionary<PaintingObject, Tools.SpatialGrid>();
            PerfLevels = new Dictionary<PaintingObject, Tools.PerformanceMonitor.PerformanceLevel>();
        }

        public void SetPaintingObjects(PaintingObject[] objects)
        {
            Objects = objects;
            SpatialGrids.Clear();
            PerfLevels.Clear();

            if (objects == null) return;

            foreach (var obj in objects)
            {
                if (obj == null || obj.verts == null) continue;

                int vertexCount = obj.verts.Length;
                var perfLevel = Tools.PerformanceMonitor.EvaluatePerformance(vertexCount);
                PerfLevels[obj] = perfLevel;

                if (Tools.PerformanceMonitor.ShouldUseSpatialPartitioning(vertexCount))
                {
                    var grid = new Tools.SpatialGrid();
                    grid.BuildGrid(obj.verts, obj.renderer.bounds, Brush.Size);
                    SpatialGrids[obj] = grid;
                }
            }
        }
        
        public int GetTotalVertexCount()
        {
            if (Objects == null) return 0;
            int total = 0;
            foreach (var obj in Objects)
            {
                if (obj != null && obj.verts != null)
                    total += obj.verts.Length;
            }
            return total;
        }
    }
}
