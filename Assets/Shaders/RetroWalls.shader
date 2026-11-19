Shader "DeltaTime/RetroWalls"
{
    Properties
    {
        _UpperColor ("Цвет верха (побелка/штукатурка)", Color) = (0.35, 0.36, 0.33, 1)
        _LowerColor ("Цвет низа (масляная краска)", Color) = (0.11, 0.22, 0.19, 1)
        _StripeColor ("Цвет разделительной полосы", Color) = (0.06, 0.08, 0.07, 1)
        _GrimeColor ("Цвет грязи и потеков", Color) = (0.04, 0.04, 0.03, 1)
        
        [Header(Height Setup)]
        _FloorY ("Уровень пола по Y (сдвиг)", Float) = 0.0
        _SplitHeight ("Высота покраски (метры)", Float) = 1.25
        _StripeThickness ("Толщина полосы", Range(0.005, 0.1)) = 0.035

        [Header(Surface)]
        _LowerSmoothness ("Глянец нижней краски", Range(0, 1)) = 0.55
        _UpperSmoothness ("Глянец верхней штукатурки", Range(0, 1)) = 0.08
        _DirtStrength ("Шероховатость и грязь", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _UpperColor;
                half4 _LowerColor;
                half4 _StripeColor;
                half4 _GrimeColor;
                float _FloorY;
                float _SplitHeight;
                float _StripeThickness;
                half _LowerSmoothness;
                half _UpperSmoothness;
                float _DirtStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), f.x), f.y);
            }

            float FBM(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    v += a * ValueNoise(p);
                    p *= 2.05;
                    a *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = abs(normalize(IN.normalWS));
                // Автоматический подбор горизонтальной оси для любой из 4-х стен (Трипланар)
                float horizCoord = (n.x > n.z) ? IN.positionWS.z : IN.positionWS.x;
                float h = IN.positionWS.y - _FloorY;

                // Разделение на масляную краску и побелку
                float lowerMask = 1.0 - smoothstep(_SplitHeight - 0.005, _SplitHeight + 0.005, h);
                float distToLine = abs(h - _SplitHeight);
                float stripeMask = 1.0 - smoothstep(_StripeThickness * 0.5, _StripeThickness * 0.5 + 0.005, distToLine);

                half3 paintColor = lerp(_UpperColor.rgb, _LowerColor.rgb, lowerMask);
                paintColor = lerp(paintColor, _StripeColor.rgb, stripeMask);

                // Фактура штукатурки и вертикальные потеки сырости
                float plasterNoise = FBM(float2(horizCoord * 2.5, h * 2.5));
                float verticalStreaks = FBM(float2(horizCoord * 4.5, h * 0.4));
                float grain = ValueNoise(float2(horizCoord * 28.0, h * 28.0)) * 0.06;

                // Естественное затемнение у самого пола (грязь у плинтуса)
                float floorAO = 1.0 - smoothstep(0.0, 0.35, h);

                float totalDirt = saturate((plasterNoise * 0.4 + verticalStreaks * 0.6) * _DirtStrength + floorAO * 0.65);

                half3 albedo = paintColor - grain;
                albedo = lerp(albedo, _GrimeColor.rgb, totalDirt * 0.65);

                // Нижняя масляная краска слегка отражает свечение мониторов
                half smoothness = lerp(_UpperSmoothness, _LowerSmoothness, lowerMask);
                smoothness *= (1.0 - totalDirt * 0.7);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(IN.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}