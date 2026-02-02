
Shader "URP/PlanarReflections"
{
    Properties
    {
        _BaseColor ("Base Color", color) = (1, 1, 1, 1)
        _BaseMap ("BaseMap", 2D) = "white" { }
    }

    SubShader
    {

        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);

            TEXTURE2D(_PlanarReflectionTexture);
            SAMPLER(sampler_PlanarReflectionTexture);


            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;

                float4 screenPos : TEXCOORD2;
            };




            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);

                o.screenPos = ComputeScreenPos(o.positionCS);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 screenPos = ComputeScreenPos(i.positionCS);
                float2 screenUV = screenPos.xy / screenPos.w;
                screenUV = i.positionCS / _ScaledScreenParams.xy;
                // screenUV = i.screenPos.xy / i.screenPos.w;

                float4 reflection = SAMPLE_TEXTURE2D(_PlanarReflectionTexture, sampler_PlanarReflectionTexture, screenUV);
                return half4(reflection.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}

