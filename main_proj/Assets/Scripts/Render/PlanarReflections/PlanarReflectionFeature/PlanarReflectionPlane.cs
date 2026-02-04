using UnityEngine;
using System.Collections.Generic;

namespace Render.PlanarReflectionFeature
{
    [ExecuteAlways]
    public class PlanarReflectionPlane : MonoBehaviour
    {

        public static readonly List<PlanarReflectionPlane> ActivePlanes = new List<PlanarReflectionPlane>();

        private void OnEnable()
        {
            ActivePlanes.Add(this);
        }

        private void OnDisable()
        {
            ActivePlanes.Remove(this);
        }

        [Header("反射平面设置")]
        [Tooltip("平面垂直偏移")]
        public float planeOffset = 0f;

        [Header("可选参数")]
        [Tooltip("可选的参考平面对像，如果为空则使用自身 Transform")]
        public Transform referencePlane;

        public Vector3 GetPlanePosition()
        {
            Transform refTransform = referencePlane != null ? referencePlane : transform;
            return refTransform.position + refTransform.up * planeOffset;
        }

        public Vector3 GetPlaneNormal()
        {
            Transform refTransform = referencePlane != null ? referencePlane : transform;
            return refTransform.up;
        }

        public float GetPlaneOffset()
        {
            return planeOffset;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = GetPlanePosition();
            Vector3 normal = GetPlaneNormal();

            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Vector3 right = Vector3.Cross(normal, Vector3.forward);
            if (right.magnitude < 0.1f)
                right = Vector3.Cross(normal, Vector3.up);
            right = right.normalized;
            Vector3 forward = Vector3.Cross(right, normal).normalized;

            float size = 5f;
            Vector3[] corners = new Vector3[4]
            {
            pos + right * size + forward * size,
            pos - right * size + forward * size,
            pos - right * size - forward * size,
            pos + right * size - forward * size
            };

            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(pos, normal * 2f);
        }
    }
}
