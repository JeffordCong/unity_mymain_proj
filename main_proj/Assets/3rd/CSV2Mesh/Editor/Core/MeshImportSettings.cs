using UnityEngine;

namespace CSV2Mesh.Core
{
    /// <summary>
    /// 网格导入设置
    /// 管理所有导入过程中的转换选项
    /// </summary>
    [System.Serializable]
    public class MeshImportSettings
    {
        [SerializeField] private float scaleFactor = 0.01f;
        [SerializeField] private bool rotationN90 = false;
        [SerializeField] private bool reverseUvY = false;
        [SerializeField] private bool reverseTriangles = false;

        public float ScaleFactor
        {
            get => scaleFactor;
            set => scaleFactor = value;
        }

        public bool RotationN90
        {
            get => rotationN90;
            set => rotationN90 = value;
        }

        public bool ReverseUvY
        {
            get => reverseUvY;
            set => reverseUvY = value;
        }

        public bool ReverseTriangles
        {
            get => reverseTriangles;
            set => reverseTriangles = value;
        }

        /// <summary>
        /// 获取旋转四元数
        /// </summary>
        public Quaternion GetRotation()
        {
            return rotationN90 ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
        }

        /// <summary>
        /// 应用缩放到向量
        /// </summary>
        public Vector3 ApplyScale(Vector3 vector)
        {
            return vector * scaleFactor;
        }

        /// <summary>
        /// 处理 UV Y 坐标
        /// </summary>
        public float ProcessUvY(float y)
        {
            return reverseUvY ? 1 - y : y;
        }
    }
}
