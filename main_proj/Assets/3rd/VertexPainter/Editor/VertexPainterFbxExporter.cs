using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Autodesk.Fbx;
using VertexPainter.Tools;

/// <summary>
/// FBX 顶点颜色导出器 (重构版)
/// 采用 "读取 -> 修改 -> 保存" 流程，保留原始 FBX 信息
/// </summary>
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Autodesk.Fbx;
using VertexPainter.Tools;

/// <summary>
/// FBX 顶点颜色导出器 (重构版)
/// 采用 "读取 -> 修改 -> 保存" 流程，保留原始 FBX 信息
/// </summary>
public static class VertexPainterFbxExporter
{
    /// <summary>
    /// 导出带顶点颜色的 FBX 文件 (直接修改原文件)
    /// </summary>
    /// <param name="paintingObjects">绘制对象数组</param>
    /// <param name="invertX">手动反转 X 轴</param>
    /// <param name="invertY">手动反转 Y 轴</param>
    /// <param name="invertZ">手动反转 Z 轴</param>
    public static void ExportToFBX(PaintingObject[] paintingObjects, bool invertX = false, bool invertY = false, bool invertZ = false)
    {
        if (paintingObjects == null || paintingObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有可导出的对象！", "确定");
            return;
        }

        // 1. 按 FBX 文件路径分组对象
        var fileGroups = new Dictionary<string, List<PaintingObject>>();
        foreach (var obj in paintingObjects)
        {
            if (obj == null || obj.meshFilter == null || obj.meshFilter.sharedMesh == null) continue;

            string assetPath = AssetDatabase.GetAssetPath(obj.meshFilter.sharedMesh);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.ToLower().EndsWith(".fbx"))
            {
                Debug.LogWarning($"跳过非 FBX 对象: {obj.meshFilter.name} ({assetPath})");
                continue;
            }

            if (!fileGroups.ContainsKey(assetPath))
            {
                fileGroups[assetPath] = new List<PaintingObject>();
            }
            fileGroups[assetPath].Add(obj);
        }

        if (fileGroups.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到可导出的 FBX 关联对象。\n请确保绘制的对象是 FBX 模型的实例。", "确定");
            return;
        }

        // 2. 逐个文件处理
        int successCount = 0;
        int totalFiles = fileGroups.Count;

        Vector3 manualSigns = new Vector3(invertX ? -1 : 1, invertY ? -1 : 1, invertZ ? -1 : 1);

        foreach (var kvp in fileGroups)
        {
            string assetPath = kvp.Key;
            List<PaintingObject> objects = kvp.Value;

            if (ProcessFbxFile(assetPath, objects, manualSigns))
            {
                successCount++;
            }
        }

        // 3. 刷新资源
        foreach (var kvp in fileGroups)
        {
            // 强制重新导入以确保 Unity 读取最新的 FBX 数据
            AssetDatabase.ImportAsset(kvp.Key, ImportAssetOptions.ForceUpdate);
        }

        EditorUtility.DisplayDialog("导出完成", $"成功处理 {successCount}/{totalFiles} 个 FBX 文件。\n请检查 Console 获取详细日志。", "确定");
    }

    private static bool ProcessFbxFile(string assetPath, List<PaintingObject> objects, Vector3 manualSigns)
    {
        FbxManager fbxManager = null;
        FbxImporter fbxImporter = null;
        FbxExporter fbxExporter = null;

        string fullPath = Path.GetFullPath(assetPath);
        string tempPath = fullPath + ".tmp.fbx";

        try
        {
            // 初始化 Manager
            fbxManager = FbxManager.Create();
            FbxIOSettings ioSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);
            fbxManager.SetIOSettings(ioSettings);

            // 导入原文件
            fbxImporter = FbxImporter.Create(fbxManager, "");
            if (!fbxImporter.Initialize(fullPath, -1, ioSettings))
            {
                Debug.LogError($"无法初始化 FBX 导入器: {fbxImporter.GetStatus().GetErrorString()}");
                return false;
            }

            FbxScene fbxScene = FbxScene.Create(fbxManager, "Scene");
            fbxImporter.Import(fbxScene);
            fbxImporter.Destroy();
            fbxImporter = null;

            // 修改场景中的 Mesh
            int modifiedCount = 0;
            foreach (var obj in objects)
            {
                if (ModifyMeshInScene(fbxScene, obj, manualSigns))
                {
                    modifiedCount++;
                }
            }

            if (modifiedCount == 0)
            {
                Debug.LogWarning($"文件 {assetPath} 中没有匹配的 Mesh 被修改。");
                return false;
            }

            // 导出到临时文件
            fbxExporter = FbxExporter.Create(fbxManager, "");
            // 使用 -1 让 SDK 自动检测格式 (通常保持原格式)
            int fileFormat = -1;

            if (!fbxExporter.Initialize(tempPath, fileFormat, ioSettings))
            {
                Debug.LogError($"无法初始化 FBX 导出器: {fbxExporter.GetStatus().GetErrorString()}");
                return false;
            }

            fbxExporter.Export(fbxScene);
            fbxExporter.Destroy();
            fbxExporter = null;

            // 替换原文件
            fbxManager.Destroy();
            fbxManager = null;

            // 确保资源释放后再操作文件
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            File.Move(tempPath, fullPath);

            Debug.Log($"[VertexPainter] 成功更新 FBX 文件: {assetPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"处理 FBX {assetPath} 失败: {e.Message}\n{e.StackTrace}");
            // 清理临时文件
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return false;
        }
        finally
        {
            if (fbxImporter != null) fbxImporter.Destroy();
            if (fbxExporter != null) fbxExporter.Destroy();
            if (fbxManager != null) fbxManager.Destroy();
        }
    }

    private static bool ModifyMeshInScene(FbxScene scene, PaintingObject obj, Vector3 manualSigns)
    {
        if (obj == null || obj.meshFilter == null || obj._stream == null) return false;

        // 查找对应的 FbxNode
        string targetNodeName = obj.meshFilter.sharedMesh.name;
        // 去除 " Instance" 后缀（如果存在）
        if (targetNodeName.EndsWith(" Instance"))
        {
            targetNodeName = targetNodeName.Substring(0, targetNodeName.Length - 9);
        }

        FbxNode targetNode = FindNodeByName(scene.GetRootNode(), targetNodeName);
        if (targetNode == null)
        {
            Debug.LogWarning($"[VertexPainter] 在 FBX 中未找到节点: {targetNodeName} (Unity Mesh Name: {obj.meshFilter.sharedMesh.name})");
            return false;
        }

        FbxMesh fbxMesh = targetNode.GetMesh();
        if (fbxMesh == null)
        {
            Debug.LogWarning($"[VertexPainter] 节点 {targetNodeName} 没有 Mesh 属性");
            return false;
        }

        // 应用顶点颜色
        ApplyVertexColorsToFbxMesh(fbxMesh, obj, manualSigns);
        return true;
    }

    private static FbxNode FindNodeByName(FbxNode node, string name)
    {
        if (node.GetName() == name) return node;

        for (int i = 0; i < node.GetChildCount(); i++)
        {
            var result = FindNodeByName(node.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private static void ApplyVertexColorsToFbxMesh(FbxMesh fbxMesh, PaintingObject obj, Vector3 manualSigns)
    {
        Mesh unityMesh = obj.meshFilter.sharedMesh;
        Color[] colors = obj._stream.colors;
        Vector3[] vertices = unityMesh.vertices;
        Vector3[] normals = unityMesh.normals; // 获取 Unity 法线

        if (colors == null || colors.Length == 0)
        {
            Debug.LogWarning($"[VertexPainter] 对象 {obj.meshFilter.name} 没有颜色数据");
            return;
        }

        int controlPointsCount = fbxMesh.GetControlPointsCount();
        if (controlPointsCount == 0) return;

        // 1. 构建空间索引
        SpatialGrid grid = new SpatialGrid();
        grid.BuildGrid(vertices, unityMesh.bounds, 0.05f);

        // 2. 自动计算最佳变换矩阵 (Scale + Axis + Sign)
        // 传入 Unity 法线以辅助判断
        Matrix4x4 bestTransform = AutoDetectTransform(fbxMesh, unityMesh, grid, vertices, normals, manualSigns);

        // 3. 准备 FBX 颜色层
        FbxLayerElementVertexColor colorElement = null;
        if (fbxMesh.GetLayerCount() > 0)
        {
            var layer = fbxMesh.GetLayer(0);
            colorElement = layer.GetVertexColors();
            if (colorElement == null)
            {
                colorElement = FbxLayerElementVertexColor.Create(fbxMesh, "VertexColors");
                layer.SetVertexColors(colorElement);
            }
        }
        else
        {
            fbxMesh.CreateLayer();
            colorElement = FbxLayerElementVertexColor.Create(fbxMesh, "VertexColors");
            fbxMesh.GetLayer(0).SetVertexColors(colorElement);
        }

        colorElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        colorElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

        var colorArray = colorElement.GetDirectArray();
        colorArray.SetCount(controlPointsCount);

        int matchCount = 0;
        int failCount = 0;

        // 4. 应用颜色
        for (int i = 0; i < controlPointsCount; i++)
        {
            FbxVector4 fbxPosRaw = fbxMesh.GetControlPointAt(i);
            Vector3 fbxPos = new Vector3((float)fbxPosRaw.X, (float)fbxPosRaw.Y, (float)fbxPosRaw.Z);

            // 应用变换
            Vector3 unityPos = bestTransform.MultiplyPoint3x4(fbxPos);

            // 查找最近点
            var nearbyIndices = grid.GetNearbyVertices(unityPos);

            Color targetColor = Color.white;
            float minDistSq = float.MaxValue;
            bool found = false;

            // Grid 搜索
            foreach (int idx in nearbyIndices)
            {
                float distSq = (vertices[idx] - unityPos).sqrMagnitude;
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    if (distSq < 0.001f) // 1mm 容差
                    {
                        if (idx < colors.Length)
                        {
                            targetColor = colors[idx];
                            found = true;
                        }
                    }
                }
            }

            // 暴力搜索兜底
            if (!found)
            {
                if (vertices.Length < 10000)
                {
                    // 简单的全量遍历
                    int bestIdx = -1;
                    float bestGlobalDistSq = float.MaxValue;

                    for (int v = 0; v < vertices.Length; v++)
                    {
                        float d = (vertices[v] - unityPos).sqrMagnitude;
                        if (d < bestGlobalDistSq)
                        {
                            bestGlobalDistSq = d;
                            bestIdx = v;
                        }
                    }

                    if (bestIdx != -1 && bestGlobalDistSq < 0.04f) // 20cm 容差
                    {
                        targetColor = colors[bestIdx];
                        found = true;
                    }
                }
            }

            if (found) matchCount++;
            else failCount++;

            colorArray.SetAt(i, new FbxColor(targetColor.r, targetColor.g, targetColor.b, targetColor.a));
        }

        Debug.Log($"[VertexPainter] 应用颜色到 {fbxMesh.GetName()}: 匹配 {matchCount}/{controlPointsCount}, 失败 {failCount}. Transform: \n{bestTransform}");
    }

    private static Matrix4x4 AutoDetectTransform(FbxMesh fbxMesh, Mesh unityMesh, SpatialGrid grid, Vector3[] unityVertices, Vector3[] unityNormals, Vector3 manualSigns)
    {
        // 1. 计算包围盒
        Bounds unityBounds = unityMesh.bounds;

        Vector3 fbxMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 fbxMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        int count = fbxMesh.GetControlPointsCount();
        for (int i = 0; i < count; i++)
        {
            var p = fbxMesh.GetControlPointAt(i);
            float x = (float)p.X; float y = (float)p.Y; float z = (float)p.Z;
            if (x < fbxMin.x) fbxMin.x = x; if (x > fbxMax.x) fbxMax.x = x;
            if (y < fbxMin.y) fbxMin.y = y; if (y > fbxMax.y) fbxMax.y = y;
            if (z < fbxMin.z) fbxMin.z = z; if (z > fbxMax.z) fbxMax.z = z;
        }
        Vector3 fbxSize = fbxMax - fbxMin;
        Vector3 unitySize = unityBounds.size;

        // 2. 确定缩放 (Scale)
        float maxFbxDim = Mathf.Max(fbxSize.x, Mathf.Max(fbxSize.y, fbxSize.z));
        float maxUnityDim = Mathf.Max(unitySize.x, Mathf.Max(unitySize.y, unitySize.z));

        float scale = 1.0f;
        if (maxUnityDim > 0.0001f)
        {
            float ratio = maxFbxDim / maxUnityDim;
            if (ratio > 50) scale = 0.01f;
            else if (ratio > 500) scale = 0.001f;
            else if (ratio < 0.05f) scale = 100.0f;
        }

        // 3. 确定轴映射 (Axis Mapping)
        var fbxDims = new List<KeyValuePair<int, float>> {
            new KeyValuePair<int, float>(0, fbxSize.x),
            new KeyValuePair<int, float>(1, fbxSize.y),
            new KeyValuePair<int, float>(2, fbxSize.z)
        };
        var unityDims = new List<KeyValuePair<int, float>> {
            new KeyValuePair<int, float>(0, unitySize.x),
            new KeyValuePair<int, float>(1, unitySize.y),
            new KeyValuePair<int, float>(2, unitySize.z)
        };

        fbxDims.Sort((a, b) => b.Value.CompareTo(a.Value));
        unityDims.Sort((a, b) => b.Value.CompareTo(a.Value));

        int[] axisMap = new int[3];
        for (int i = 0; i < 3; i++)
        {
            axisMap[unityDims[i].Key] = fbxDims[i].Key;
        }

        // 4. 确定符号 (Sign) - 使用法线辅助
        // 采样点：不仅需要位置，还需要法线
        List<(Vector3 pos, Vector3 normal)> samples = new List<(Vector3, Vector3)>();

        // 尝试获取 FBX 法线
        FbxLayerElementNormal fbxNormals = null;
        if (fbxMesh.GetLayerCount() > 0) fbxNormals = fbxMesh.GetLayer(0).GetNormals();

        // 随机采样 50 个点 (或者遍历前 N 个)
        int sampleTarget = 50;
        int polyCount = fbxMesh.GetPolygonCount();
        int vertexIdCounter = 0;

        for (int p = 0; p < polyCount && samples.Count < sampleTarget; p++)
        {
            int polySize = fbxMesh.GetPolygonSize(p);
            for (int v = 0; v < polySize; v++)
            {
                // 每隔几个点采一个，避免过于集中
                if ((vertexIdCounter++ % 5) != 0 && samples.Count > 5) continue;

                int cpIndex = fbxMesh.GetPolygonVertex(p, v);
                var rawPos = fbxMesh.GetControlPointAt(cpIndex);
                Vector3 pos = new Vector3((float)rawPos.X, (float)rawPos.Y, (float)rawPos.Z);

                Vector3 normal = Vector3.up; // 默认
                if (fbxNormals != null)
                {
                    var mapMode = fbxNormals.GetMappingMode();
                    var refMode = fbxNormals.GetReferenceMode();
                    int normalIndex = -1;

                    if (mapMode == FbxLayerElement.EMappingMode.eByControlPoint)
                    {
                        normalIndex = cpIndex;
                    }
                    else if (mapMode == FbxLayerElement.EMappingMode.eByPolygonVertex)
                    {
                        normalIndex = vertexIdCounter - 1; // 当前是第几个 PolygonVertex
                    }

                    if (normalIndex != -1)
                    {
                        if (refMode == FbxLayerElement.EReferenceMode.eDirect)
                        {
                            var rawN = fbxNormals.GetDirectArray().GetAt(normalIndex);
                            normal = new Vector3((float)rawN.X, (float)rawN.Y, (float)rawN.Z);
                        }
                        else if (refMode == FbxLayerElement.EReferenceMode.eIndexToDirect)
                        {
                            int id = fbxNormals.GetIndexArray().GetAt(normalIndex);
                            var rawN = fbxNormals.GetDirectArray().GetAt(id);
                            normal = new Vector3((float)rawN.X, (float)rawN.Y, (float)rawN.Z);
                        }
                    }
                }

                samples.Add((pos, normal));
                if (samples.Count >= sampleTarget) break;
            }
        }

        Vector3 bestSigns = Vector3.one;
        float bestScore = float.MaxValue;

        Vector3[] signCombs = new Vector3[] {
            new Vector3(1,1,1), new Vector3(1,1,-1), new Vector3(1,-1,1), new Vector3(1,-1,-1),
            new Vector3(-1,1,1), new Vector3(-1,1,-1), new Vector3(-1,-1,1), new Vector3(-1,-1,-1)
        };

        foreach (var signs in signCombs)
        {
            float totalScore = 0;
            int validSamples = 0;

            foreach (var sample in samples)
            {
                // 变换位置
                Vector3 uPos = Vector3.zero;
                uPos.x = sample.pos[axisMap[0]] * scale * signs.x;
                uPos.y = sample.pos[axisMap[1]] * scale * signs.y;
                uPos.z = sample.pos[axisMap[2]] * scale * signs.z;

                // 变换法线 (仅旋转/镜像，不缩放)
                Vector3 uNorm = Vector3.zero;
                uNorm.x = sample.normal[axisMap[0]] * signs.x;
                uNorm.y = sample.normal[axisMap[1]] * signs.y;
                uNorm.z = sample.normal[axisMap[2]] * signs.z;
                uNorm.Normalize();

                // 找最近点
                var indices = grid.GetNearbyVertices(uPos);
                float minDistSq = float.MaxValue;
                int bestIdx = -1;

                foreach (int idx in indices)
                {
                    float d = (unityVertices[idx] - uPos).sqrMagnitude;
                    if (d < minDistSq)
                    {
                        minDistSq = d;
                        bestIdx = idx;
                    }
                }

                if (bestIdx != -1)
                {
                    // 距离得分
                    float distScore = minDistSq;

                    // 法线得分 (Dot: 1=同向, -1=反向)
                    // 我们希望同向，所以 (1 - Dot) 越小越好
                    // 权重：距离误差通常在 0.001 级别。法线误差在 0~2 级别。
                    // 如果是对称模型，距离误差几乎为 0，主要靠法线区分。
                    // 给法线一个适当的权重，例如 0.1 (相当于 10cm 的距离误差)

                    Vector3 targetNorm = unityNormals[bestIdx];
                    float dot = Vector3.Dot(uNorm, targetNorm);
                    float normalScore = (1.0f - dot) * 0.1f;

                    totalScore += distScore + normalScore;
                    validSamples++;
                }
                else
                {
                    totalScore += 100.0f; // 惩罚未匹配
                }
            }

            if (validSamples > 0 && totalScore < bestScore)
            {
                bestScore = totalScore;
                bestSigns = signs;
            }
        }

        // 应用手动反转
        // manualSigns: 1=默认, -1=反转
        // 我们直接乘到 bestSigns 上
        bestSigns.x *= manualSigns.x;
        bestSigns.y *= manualSigns.y;
        bestSigns.z *= manualSigns.z;

        // 5. 构建最终矩阵
        Matrix4x4 mat = Matrix4x4.identity;
        mat.m00 = 0; mat.m01 = 0; mat.m02 = 0; mat.m03 = 0;
        mat.m10 = 0; mat.m11 = 0; mat.m12 = 0; mat.m13 = 0;
        mat.m20 = 0; mat.m21 = 0; mat.m22 = 0; mat.m23 = 0;
        mat.m33 = 1;

        if (axisMap[0] == 0) mat.m00 = scale * bestSigns.x;
        else if (axisMap[0] == 1) mat.m01 = scale * bestSigns.x;
        else if (axisMap[0] == 2) mat.m02 = scale * bestSigns.x;

        if (axisMap[1] == 0) mat.m10 = scale * bestSigns.y;
        else if (axisMap[1] == 1) mat.m11 = scale * bestSigns.y;
        else if (axisMap[1] == 2) mat.m12 = scale * bestSigns.y;

        if (axisMap[2] == 0) mat.m20 = scale * bestSigns.z;
        else if (axisMap[2] == 1) mat.m21 = scale * bestSigns.z;
        else if (axisMap[2] == 2) mat.m22 = scale * bestSigns.z;

        Debug.Log($"[VertexPainter] Auto-Detected Transform (Manual Override: {manualSigns}):\nScale: {scale}\nAxisMap: X<-{axisMap[0]}, Y<-{axisMap[1]}, Z<-{axisMap[2]}\nSigns: {bestSigns}\nMatrix:\n{mat}");
        return mat;
    }
}
