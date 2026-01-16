using MyEditor.MaterialSystem;
using UnityEditor;
using UnityEngine;

public static class TextureHelper
{
    /// <summary>
    /// 检查贴图是否在Common目录下
    /// Common目录通常用于存放通用的贴图资源，避免重复导入
    /// </summary>
    public static bool IsInCommonDirectory(Texture2D tex)
    {
        if (tex == null) return false;
        string path = AssetDatabase.GetAssetPath(tex).Replace("\\", "/");
        return path.Contains("/Common/");
    }


    /// <summary>
    /// 自动批量设置贴图的多平台参数（包括所有通用参数和平台特殊参数）
    /// </summary>
    public static void ApplyImportSettings(Texture2D tex, TextureImportConfig cfg)
    {
        if (tex == null) return;
        string path = AssetDatabase.GetAssetPath(tex);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        // 1. 应用默认平台参数（全局基础设置）
        if (!string.IsNullOrEmpty(cfg.defaultSettings.platformName))
            SetPlatform(importer, cfg.defaultSettings);

        // 2. 平台专用参数覆盖
        if (cfg.pcSettings.HasValue)
            SetPlatform(importer, cfg.pcSettings.Value);
        if (cfg.androidSettings.HasValue)
            SetPlatform(importer, cfg.androidSettings.Value);
        if (cfg.iosSettings.HasValue)
            SetPlatform(importer, cfg.iosSettings.Value);
        if (cfg.webglSettings.HasValue)
            SetPlatform(importer, cfg.webglSettings.Value);

        importer.SaveAndReimport();
    }


    /// <summary>
    /// 检查后缀（可选自动重命名），再设置贴图导入参数
    /// </summary>
    public static void ApplyImportSettings(
        Texture2D tex,
        TextureImportConfig cfg,
        string requiredSuffix,
        bool autoRename = false,
        bool showWarning = true)
    {
        if (tex == null) return;
        // 1. 检查/自动重命名
        CheckAndRenameTextureSuffix(tex, requiredSuffix, autoRename, showWarning);
        // 2. 设置导入参数
        ApplyImportSettings(tex, cfg);
    }


    /// <summary>
    /// 只做命名规范检查与重命名
    /// </summary>
    public static string CheckAndRenameTextureSuffix(
        Texture2D tex,
        string requiredSuffix,
        bool autoRename = false,
        bool showWarning = true)
    {
        if (tex == null || string.IsNullOrEmpty(requiredSuffix)) return AssetDatabase.GetAssetPath(tex);
        string path = AssetDatabase.GetAssetPath(tex);
        string filename = System.IO.Path.GetFileNameWithoutExtension(path);

        if (filename.EndsWith(requiredSuffix, System.StringComparison.OrdinalIgnoreCase)) return path;

        if (autoRename)
        {
            string dir = System.IO.Path.GetDirectoryName(path);
            string ext = System.IO.Path.GetExtension(path);
            string newFilename = filename + requiredSuffix + ext;
            string newPath = System.IO.Path.Combine(dir, newFilename);
            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

            string result = AssetDatabase.RenameAsset(path, System.IO.Path.GetFileNameWithoutExtension(newPath));
            if (string.IsNullOrEmpty(result))
            {
                if (showWarning)
                    Debug.LogWarning($"已自动为贴图 {filename}{ext} 加上后缀，重命名为: {System.IO.Path.GetFileName(newPath)}", tex);
                return newPath;
            }
            else
            {
                Debug.LogError($"贴图自动加后缀重命名失败: {result}  原路径: {path}");
            }
        }
        else if (showWarning)
        {
            Debug.LogWarning($"【贴图命名不规范】 {filename} 应以 {requiredSuffix} 结尾，路径: {path}", tex);
        }

        return path;
    }

    /// <summary>
    /// 应用单个平台的参数（包含所有通用参数和平台专用参数）
    /// </summary>
    private static void SetPlatform(TextureImporter importer, PlatformTextureSettings settings)
    {
        // 统一设置所有通用参数
        importer.sRGBTexture = settings.sRGB;
        importer.textureCompression = settings.compression;
        importer.mipmapEnabled = settings.mipmap;
        importer.isReadable = settings.isReadable;
        importer.textureType = settings.type;

        // 设置平台参数
        var pts = new TextureImporterPlatformSettings
        {
            name = settings.platformName,
            overridden = settings.platformName != "Default",
            maxTextureSize = settings.maxSize,
            format = settings.format,
            compressionQuality = settings.compressionQuality
        };
        importer.SetPlatformTextureSettings(pts);
    }
}