using UnityEditor;

namespace MyEditor.MaterialSystem
{
  
    /// 贴图导入设置
    public struct PlatformTextureSettings
    {
        public string platformName; // 平台名称（如 Default、Standalone、Android 等）
        public int maxSize; // 最大分辨率
        public TextureImporterFormat format; // 贴图格式
        public int compressionQuality; // 压缩质量
        public bool sRGB; // 是否为sRGB贴图
        public TextureImporterCompression compression; // 压缩类型
        public bool mipmap; // 是否生成mipmap
        public bool isReadable; // 是否可读
        public TextureImporterType type; // 贴图类型
    }


    /// 多平台贴图导入配置
    public struct TextureImportConfig
    {
        public PlatformTextureSettings defaultSettings; // 默认平台设置
        public PlatformTextureSettings? pcSettings; // PC平台设置
        public PlatformTextureSettings? androidSettings; // Android平台设置
        public PlatformTextureSettings? iosSettings; // iOS平台设置
        public PlatformTextureSettings? webglSettings; // WebGL平台设置
    }


    /// 贴图平台参数工厂与预设
    public static class TexturePlatformSettings
    {
 
        /// 创建单个平台的贴图导入设置
        public static PlatformTextureSettings CreatePlatformSettings(
            string platformName,
            int maxSize,
            TextureImporterFormat format,
            int compressionQuality,
            bool sRGB = true,
            TextureImporterCompression compression = TextureImporterCompression.Compressed,
            bool mipmap = true,
            bool isReadable = false,
            TextureImporterType type = TextureImporterType.Default)
        {
            return new PlatformTextureSettings
            {
                platformName = platformName,
                maxSize = maxSize,
                format = format,
                compressionQuality = compressionQuality,
                sRGB = sRGB,
                compression = compression,
                mipmap = mipmap,
                isReadable = isReadable,
                type = type
            };
        }

        /// <summary>
        /// 重新设置分辨率
        /// </summary>
        public static TextureImportConfig OverrideAllPlatformMaxSize(TextureImportConfig src, int maxSize)
        {
            var config = src;
            config.defaultSettings.maxSize = maxSize;
            if (config.pcSettings.HasValue)
            {
                var pc = config.pcSettings.Value;
                pc.maxSize = maxSize;
                config.pcSettings = pc;
            }

            if (config.androidSettings.HasValue)
            {
                var android = config.androidSettings.Value;
                android.maxSize = maxSize;
                config.androidSettings = android;
            }

            if (config.iosSettings.HasValue)
            {
                var ios = config.iosSettings.Value;
                ios.maxSize = maxSize;
                config.iosSettings = ios;
            }

            if (config.webglSettings.HasValue)
            {
                var webgl = config.webglSettings.Value;
                webgl.maxSize = maxSize;
                config.webglSettings = webgl;
            }

            return config;
        }

        // 工厂方法快速定义所有平台参数
        /// BaseMap类型的多平台贴图导入配置预设

        public static readonly TextureImportConfig BaseMap = new TextureImportConfig
        {
            defaultSettings = CreatePlatformSettings("Default", 1024, TextureImporterFormat.Automatic, 50),
            pcSettings = CreatePlatformSettings("Standalone", 1024, TextureImporterFormat.DXT5, 50),
            androidSettings = CreatePlatformSettings("Android", 1024, TextureImporterFormat.ASTC_6x6, 50),
            iosSettings = CreatePlatformSettings("iPhone", 1024, TextureImporterFormat.ASTC_6x6, 50),
            webglSettings = CreatePlatformSettings("WebGL", 1024, TextureImporterFormat.DXT5, 50)
        };


    }
}