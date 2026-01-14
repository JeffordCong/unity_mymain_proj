Shader "Hidden/VertexPainter"
{
    Properties
    {
        [KeywordEnum(RGB, R, G, B, A)] _DebugMode ("DebugMode", Int) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }


        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _DEBUGMODE_RGB _DEBUGMODE_R _DEBUGMODE_G _DEBUGMODE_B _DEBUGMODE_A

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END


            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;

                o.positionCS = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1.0)));
                o.color = v.color;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = 1;
                #if defined(_DEBUGMODE_RGB)
                    c = i.color.rgba;
                #elif (_DEBUGMODE_R)
                    c = i.color.rrrr;
                #elif (_DEBUGMODE_G)
                    c = i.color.gggg;
                #elif (_DEBUGMODE_B)
                    c = i.color.bbbb;
                #elif (_DEBUGMODE_A)
                    c = i.color.aaaa;
                #endif


                return c;
            }
            ENDHLSL
        }
    }
}