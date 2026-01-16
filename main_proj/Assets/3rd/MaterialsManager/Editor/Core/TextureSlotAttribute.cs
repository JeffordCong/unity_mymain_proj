using System;

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// 贴图槽特性，用于标记 Texture2D 字段的材质属性信息
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TextureSlotAttribute : Attribute
    {
        /// <summary>
        /// Shader 属性名，如 _BaseMap
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// 关键字名（可选），如 _BASEMAP
        /// </summary>
        public string KeywordName { get; }

        /// <summary>
        /// 贴图最大分辨率
        /// </summary>
        public int MaxSize { get; }

        /// <summary>
        /// 定义贴图槽属性
        /// </summary>
        /// <param name="property">Shader 属性名</param>
        /// <param name="keyword">关键字名（可选）</param>
        /// <param name="maxSize">最大分辨率</param>
        public TextureSlotAttribute(string property, string keyword = "", int maxSize = 1024)
        {
            PropertyName = property;
            KeywordName = keyword;
            MaxSize = maxSize;
        }
    }
}
