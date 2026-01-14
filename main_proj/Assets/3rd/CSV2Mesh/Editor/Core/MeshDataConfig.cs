using UnityEngine;

namespace CSV2Mesh.Core
{
    /// <summary>
    /// 网格数据列配置
    /// 管理 CSV 文件中各数据列的索引映射
    /// </summary>
    [System.Serializable]
    public class MeshDataConfig
    {
        [SerializeField] private int positionIndex = 3;
        [SerializeField] private int tangentIndex = 6;
        [SerializeField] private int normalIndex = 10;
        [SerializeField] private int colorIndex = 14;
        [SerializeField] private int uvIndex = 18;

        public int PositionIndex
        {
            get => positionIndex;
            set => positionIndex = value;
        }

        public int TangentIndex
        {
            get => tangentIndex;
            set => tangentIndex = value;
        }

        public int NormalIndex
        {
            get => normalIndex;
            set => normalIndex = value;
        }

        public int ColorIndex
        {
            get => colorIndex;
            set => colorIndex = value;
        }

        public int UVIndex
        {
            get => uvIndex;
            set => uvIndex = value;
        }

        /// <summary>
        /// 获取所有索引中的最大值
        /// </summary>
        public int GetMaxIndex()
        {
            return Mathf.Max(positionIndex, tangentIndex, normalIndex, colorIndex, uvIndex);
        }

        /// <summary>
        /// 验证所有索引是否在有效范围内
        /// </summary>
        /// <param name="headCount">表头列数</param>
        /// <returns>是否所有索引都有效</returns>
        public bool ValidateIndices(int headCount)
        {
            if (headCount <= 0) return false;
            return GetMaxIndex() < headCount;
        }

        /// <summary>
        /// 自动将超出范围的索引值 Clamp 到有效范围内
        /// </summary>
        /// <param name="maxValidIndex">最大有效索引值</param>
        public void ClampIndices(int maxValidIndex)
        {
            if (maxValidIndex < 0) return;

            positionIndex = Mathf.Clamp(positionIndex, 0, maxValidIndex);
            tangentIndex = Mathf.Clamp(tangentIndex, 0, maxValidIndex);
            normalIndex = Mathf.Clamp(normalIndex, 0, maxValidIndex);
            colorIndex = Mathf.Clamp(colorIndex, 0, maxValidIndex);
            uvIndex = Mathf.Clamp(uvIndex, 0, maxValidIndex);
        }

        /// <summary>
        /// 重置为默认索引值
        /// </summary>
        public void ResetToDefault()
        {
            positionIndex = 3;
            tangentIndex = 6;
            normalIndex = 10;
            colorIndex = 14;
            uvIndex = 18;
        }
    }
}
