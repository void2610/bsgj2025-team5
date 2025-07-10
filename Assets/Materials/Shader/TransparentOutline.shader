Shader "Custom/URP/TransparentOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 1.0)) = 0.01
        _OutlineAlpha ("Outline Alpha", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent-50"  // 2950: パーティクルより前に描画
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // アウトラインパス
        Pass
        {
            Name "Outline"
            Tags 
            { 
                "LightMode" = "SRPDefaultUnlit" 
            }

            Cull Front
            ZWrite Off
            ZTest LEqual
            ColorMask RGBA
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex OutlineVertexProgram
            #pragma fragment OutlineFragmentProgram
            #pragma multi_compile_fog

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
                float fogCoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
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

                // オブジェクト空間での法線を正規化
                float3 normalOS = normalize(v.normal);
                
                // ワールド空間での頂点位置と法線
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(normalOS);
                worldNormal = normalize(worldNormal);
                
                // カメラからの距離を計算（距離に応じた太さ調整用）
                float3 viewDir = _WorldSpaceCameraPos - worldPos;
                float distance = length(viewDir);
                
                // 距離に基づくスケール（1-10メートルの範囲で調整）
                float distanceScale = saturate(distance / 10.0);
                distanceScale = lerp(0.7, 1.5, distanceScale);
                
                // ビュー空間での法線計算（より安定したアウトライン）
                float4 viewPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0));
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
                viewNormal.z = -0.5; // Z成分を調整してアウトラインを均一に
                viewNormal = normalize(viewNormal);
                
                // ビュー空間でアウトラインの太さを適用
                viewPos.xy += viewNormal.xy * _OutlineWidth * distanceScale;
                
                // プロジェクション変換
                o.clipPos = mul(UNITY_MATRIX_P, viewPos);
                o.worldPos = worldPos;
                
                // フォグ計算
                o.fogCoord = ComputeFogFactor(o.clipPos.z);
                
                return o;
            }

            half4 OutlineFragmentProgram(VertexOutput i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // アウトラインカラーとアルファを適用
                half4 color = half4(_OutlineColor.rgb, _OutlineColor.a * _OutlineAlpha);
                
                // フォグ適用
                color.rgb = MixFog(color.rgb, i.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}