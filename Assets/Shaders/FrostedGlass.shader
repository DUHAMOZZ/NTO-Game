Shader "Custom/FrostedScreen_URP"
{
    Properties
    {
        [Header(Screen Content)]
        _MainTex ("Canvas Texture (Render Texture)", 2D) = "white" {}
        _EmissionPower ("Screen Brightness", Range(0.5, 5.0)) = 1.5

        [Header(Frosted Glass Effect)]
        _BlurAmount ("Blur / Frostiness", Range(0.0, 0.05)) = 0.012
        _NoiseStrength ("Grain / Roughness Noise", Range(0.0, 0.02)) = 0.003
        _GlassTint ("Glass Tint Color", Color) = (0.9, 0.95, 1.0, 1.0)

        [Header(Glass Reflections)]
        _FresnelPower ("Edge Glare (Fresnel)", Range(0.5, 5.0)) = 2.5
        _RimColor ("Glass Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }

    SubShader
    {
        Tags 
{ 
    "RenderType"="Opaque" 
    "Queue"="Geometry" 
    "RenderPipeline"="UniversalPipeline" 
}

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GlassTint;
                float4 _RimColor;
                float _BlurAmount;
                float _NoiseStrength;
                float _EmissionPower;
                float _FresnelPower;
            CBUFFER_END

            // Генератор псевдослучайного шума для зернистости стекла
            float rand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Добавляем микрошум к UV-координатам (эффект неровностей матового стекла)
                float noise = (rand(input.uv * 500.0) - 0.5) * _NoiseStrength;
                float2 frostedUV = input.uv + noise;

                // Сетка смещений (Poisson Disk) для мягкого размытия изображения
                const float2 offsets[12] = {
                    float2(-0.326, -0.406), float2(-0.840, -0.074),
                    float2(-0.696,  0.457), float2(-0.203,  0.621),
                    float2( 0.962, -0.195), float2( 0.473, -0.480),
                    float2( 0.519,  0.767), float2( 0.185, -0.893),
                    float2( 0.507,  0.064), float2( 0.896,  0.412),
                    float2(-0.322, -0.933), float2(-0.792, -0.598)
                };

                half3 blurredScreen = half3(0, 0, 0);

                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    float2 sampleUV = frostedUV + offsets[i] * _BlurAmount;
                    blurredScreen += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV).rgb;
                }
                blurredScreen /= 12.0;

                // Эффект Френеля (стеклянный отблеск по краям экрана телевизора)
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Итоговый цвет: размытый экран + оттенок стекла + свечение + блик по краям
                half3 finalColor = blurredScreen * _GlassTint.rgb * _EmissionPower;
                finalColor += fresnel * _RimColor.rgb;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}