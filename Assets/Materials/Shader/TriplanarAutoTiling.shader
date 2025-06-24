// URP対応 トライプラナー自動タイリングシェーダー
// 3D オブジェクトの3軸（X、Y、Z）からテクスチャを投影し、法線に基づいてブレンドするシェーダー
// オブジェクトのスケールに応じてタイリングを自動調整し、影の描画にも対応
Shader "Universal Render Pipeline/Custom/TriplanarAutoTiling"
{
    // マテリアルのインスペクターで調整可能なプロパティ
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}              // メインテクスチャ
        _TileScale ("Tile Scale", Float) = 1.0             // タイリングスケール（値が大きいほどテクスチャが小さく表示される）
        _Color ("Color", Color) = (1,1,1,1)                // 色の乗算用（白色の場合は元のテクスチャ色がそのまま表示される）
        
        // URP用追加プロパティ
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }
    
    SubShader
    {
        // URPタグ設定
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // Forward Rendering Pass（メインの描画パス）
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // URP用のバリアント定義
            #pragma vertex vert
            #pragma fragment frag

            // URP機能のためのキーワード
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // URP用インクルード
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // 頂点シェーダーへの入力データ構造体
            struct Attributes
            {
                float4 positionOS : POSITION;      // オブジェクト空間での頂点位置
                float2 uv : TEXCOORD0;             // UV座標
                float3 normalOS : NORMAL;          // オブジェクト空間での法線
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 頂点シェーダーからフラグメントシェーダーに渡すデータ構造体
            struct Varyings
            {
                float4 positionCS : SV_POSITION;    // クリップ空間での位置
                float2 uv : TEXCOORD0;              // UV座標
                float3 positionWS : TEXCOORD1;      // ワールド空間での位置
                float3 normalWS : TEXCOORD2;        // ワールド空間での法線
                float4 shadowCoord : TEXCOORD3;     // 影の座標
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // プロパティと対応するシェーダー変数の宣言
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _TileScale;
                half4 _Color;
            CBUFFER_END

            // 頂点シェーダー
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 座標変換
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // 影の座標を計算
                output.shadowCoord = GetShadowCoord(vertexInput);

                return output;
            }

            // フラグメントシェーダー
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // トライプラナーマッピング用のUV座標を3軸分計算
                float2 uvX = input.positionWS.yz * _TileScale;  // X軸投影
                float2 uvY = input.positionWS.xz * _TileScale;  // Y軸投影
                float2 uvZ = input.positionWS.xy * _TileScale;  // Z軸投影

                // 3つの軸からテクスチャをサンプリング
                half4 texX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                half4 texY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                half4 texZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                // ブレンド重みを計算
                float3 blendWeights = abs(input.normalWS);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                // 3つのテクスチャをブレンド
                half4 albedo = texX * blendWeights.x + 
                              texY * blendWeights.y + 
                              texZ * blendWeights.z;
                
                albedo *= _Color;

                // URPのライティング計算
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;

                // 影の計算
                Light mainLight = GetMainLight(inputData.shadowCoord);
                half shadow = mainLight.shadowAttenuation;

                // シンプルなランバート照明
                half NdotL = saturate(dot(inputData.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * (NdotL * shadow);

                // 環境光を追加
                half3 ambient = SampleSH(inputData.normalWS);
                
                half3 finalColor = albedo.rgb * (lighting + ambient);
                
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // Shadow Caster Pass（影の描画パス） - 簡潔版
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Shadow Caster Pass用の構造体
            struct AttributesShadow
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsShadow
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Light direction - これらの変数はURP内部で定義されています
            float3 _LightDirection;
            float3 _LightPosition;

            // Shadow Caster 頂点シェーダー - 簡潔版
            VaryingsShadow ShadowPassVertex(AttributesShadow input)
            {
                VaryingsShadow output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // 基本的な座標変換のみ
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // シンプルなシャドウバイアス適用
                float4 positionCS = TransformWorldToHClip(positionWS);
                
                // 法線方向に少しオフセット（シャドウアクネ対策）
                float3 lightDir = normalize(_LightDirection);
                float bias = 0.005 * max(0, dot(normalWS, -lightDir));
                positionWS += normalWS * bias;
                
                output.positionCS = TransformWorldToHClip(positionWS);

                // クリップスペースのZ値制限
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            // Shadow Caster フラグメントシェーダー
            half4 ShadowPassFragment(VaryingsShadow input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }

        // Depth Only Pass（深度のみの描画パス）
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesDepth
            {
                float4 position : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsDepth DepthOnlyVertex(AttributesDepth input)
            {
                VaryingsDepth output = (VaryingsDepth)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half4 DepthOnlyFragment(VaryingsDepth input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                return 0;
            }
            ENDHLSL
        }
    }
    
    // フォールバック（URP用）
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    
    // カスタムエディター（必要に応じて）
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.BaseShaderGUI"
}