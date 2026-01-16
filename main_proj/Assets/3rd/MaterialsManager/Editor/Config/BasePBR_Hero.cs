using UnityEngine;

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// 主角 PBR 材质配置
    /// </summary>
    [System.Serializable]
    public class BasePBR_Hero : MaterialConfig
    {
        [TextureSlot("_BaseMap", "_BASEMAP", 1024)]
        public Texture2D baseMap;

        [TextureSlot("_NormalMap", "_NORMALMAP", 512)]
        public Texture2D normalMap;

        [TextureSlot("_MixMap", "_MIXMAP", 512)]
        public Texture2D mixMap;

        public override string DisplayName => "测试/角色/PBR_主角";
        public override Shader GetShader() => Shader.Find("Universal Render Pipeline/Lit");
    }
}