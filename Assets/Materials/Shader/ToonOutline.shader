Shader "Custom/ToonOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.01
        _OutlineAlpha ("Outline Alpha", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // アウトラインパス（最初に描画）
        Pass
        {
            Name "Outline"
            Tags 
            { 
                "LightMode" = "SRPDefaultUnlit" 
            }

            Cull Front
            ZWrite On
            ZTest LEqual
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex OutlineVertexProgram
            #pragma fragment OutlineFragmentProgram

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 clipPos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineAlpha;
            CBUFFER_END

            VertexOutput OutlineVertexProgram(VertexInput v)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // ワールド空間での法線を計算
                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                
                // ワールド空間での頂点位置
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                
                // アウトライン用に法線方向に頂点を拡張
                worldPos += worldNormal * _OutlineWidth;
                
                o.clipPos = TransformWorldToHClip(worldPos);
                
                return o;
            }

            half4 OutlineFragmentProgram(VertexOutput i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                return half4(_OutlineColor.rgb, _OutlineAlpha);
            }
            ENDHLSL
        }

        // メインパス（通常の描画）
        Pass
        {
            Name "ForwardLit"
            Tags 
            { 
                "LightMode" = "UniversalForward" 
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineAlpha;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // テクスチャサンプリング
                half4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 color = baseMap.rgb * _Color.rgb;

                // ライティング計算（トゥーンシェーディング風）
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                
                float NdotL = dot(normalWS, mainLight.direction);
                float lightIntensity = smoothstep(0.0, 0.01, NdotL);
                
                half3 lighting = lightIntensity * mainLight.color;
                lighting = max(lighting, 0.2); // 最小環境光

                color *= lighting;

                return half4(color, baseMap.a * _Color.a);
            }
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}