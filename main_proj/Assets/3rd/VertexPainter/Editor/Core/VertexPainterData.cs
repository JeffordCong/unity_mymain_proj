using UnityEngine;
using System.Collections.Generic;

namespace VertexPainter.Core
{
    /// <summary>
    /// 笔刷通道枚举
    /// </summary>
    public enum BrushChannel
    {
        All = 0,
        Red,
        Green,
        Blue,
        Alpha
    }

    /// <summary>
    /// 笔刷数据
    /// </summary>
    public class BrushData
    {
        public float Size = 1f;
        public float Flow = 0.5f;
        public float Falloff = 1f;
        public float Strength = 1f;
        public Color Color = Color.white;
        public BrushChannel Channel = BrushChannel.Red;

        public Color GetDisplayColor()
        {
            return ChannelConfig.GetChannelInfo(Channel).DisplayColor;
        }
    }

    /// <summary>
    /// 工具设置
    /// </summary>
    public class PainterSettings
    {
        public bool ShowPoints = true;
        public bool WeightMode = false;
        public bool Enabled = false;
        public bool EnableRealtimePreview = true;  // 默认开启实时预览
    }

    /// <summary>
    /// 通道配置信息
    /// </summary>
    public class ChannelInfo
    {
        public string Name { get; set; }
        public string Hotkey { get; set; }
        public Color DisplayColor { get; set; }

        public ChannelInfo(string name, string hotkey, Color color)
        {
            Name = name;
            Hotkey = hotkey;
            DisplayColor = color;
        }
    }

    /// <summary>
    /// 通道配置静态类
    /// </summary>
    public static class ChannelConfig
    {
        private static readonly Dictionary<BrushChannel, ChannelInfo> _channels;

        static ChannelConfig()
        {
            _channels = new Dictionary<BrushChannel, ChannelInfo>
            {
                { BrushChannel.All, new ChannelInfo("All", "~", Color.white) },
                { BrushChannel.Red, new ChannelInfo("Red", "1", Color.red) },
                { BrushChannel.Green, new ChannelInfo("Green", "2", Color.green) },
                { BrushChannel.Blue, new ChannelInfo("Blue", "3", Color.blue) },
                { BrushChannel.Alpha, new ChannelInfo("Alpha", "4", Color.gray) }
            };
        }

        public static ChannelInfo GetChannelInfo(BrushChannel channel)
        {
            return _channels.ContainsKey(channel) ? _channels[channel] : _channels[BrushChannel.All];
        }

        public static string[] GetChannelNames()
        {
            return new[]
            {
                $"{_channels[BrushChannel.All].Name}:{_channels[BrushChannel.All].Hotkey}",
                $"{_channels[BrushChannel.Red].Name}:{_channels[BrushChannel.Red].Hotkey}",
                $"{_channels[BrushChannel.Green].Name}:{_channels[BrushChannel.Green].Hotkey}",
                $"{_channels[BrushChannel.Blue].Name}:{_channels[BrushChannel.Blue].Hotkey}",
                $"{_channels[BrushChannel.Alpha].Name}:{_channels[BrushChannel.Alpha].Hotkey}"
            };
        }
    }
}
