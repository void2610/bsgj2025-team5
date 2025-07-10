Shader "Custom/URP/TransparentOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.01
        _OutlineAlpha ("Outline Alpha", Range(0.0, 1.0)) = 1.0
        [Toggle] _ScaleWithDistance ("Scale With Distance", Float) = 1
        _MinDistance ("Min Distance", Float) = 1.0
        _MaxDistance ("Max Distance", Float) = 10.0
        [Header(Silhouette Settings)]
        _SilhouetteThreshold ("Silhouette Threshold", Range(0.0, 1.0)) = 0.3
        _SilhouetteSoftness ("Silhouette Softness", Range(0.0, 0.5)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"  // 透明オブジェクトの後に描画
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
            ZWrite Off  // 透明オブジェクトなのでZWriteをOff
            ZTest LEqual
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha  // アルファブレンディング

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
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineAlpha;
                float _ScaleWithDistance;
                float _MinDistance;
                float _MaxDistance;
                float _SilhouetteThreshold;
                float _SilhouetteSoftness;
            CBUFFER_END

            VertexOutput OutlineVertexProgram(VertexInput v)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // ワールド空間での頂点位置
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                
                // ワールド空間での法線
                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                worldNormal = normalize(worldNormal);
                
                // アウトライン用に出力
                o.worldNormal = worldNormal;
                o.worldPos = worldPos;
                
                // カメラからの距離を計算
                float distance = length(_WorldSpaceCameraPos - worldPos);
                
                // 距離に基づくスケール調整
                float outlineScale = 1.0;
                if (_ScaleWithDistance > 0.5)
                {
                    outlineScale = smoothstep(_MinDistance, _MaxDistance, distance);
                    outlineScale = lerp(0.5, 2.0, outlineScale);
                }
                
                // ビュー空間での法線を計算（より安定したアウトライン）
                float3 viewNormal = TransformWorldToViewDir(worldNormal);
                viewNormal = normalize(viewNormal);
                
                // ビュー空間での頂点位置
                float4 viewPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0));
                
                // ビュー空間でアウトライン用に頂点を拡張
                viewPos.xy += viewNormal.xy * _OutlineWidth * outlineScale;
                
                // クリップ空間に変換
                o.clipPos = mul(UNITY_MATRIX_P, viewPos);
                
                // フォグ計算
                o.fogCoord = ComputeFogFactor(o.clipPos.z);
                
                return o;
            }

            half4 OutlineFragmentProgram(VertexOutput i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // カメラへの方向ベクトル
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                
                // 法線とカメラ方向の内積（シルエットエッジの検出）
                float NdotV = abs(dot(normalize(i.worldNormal), viewDir));
                
                // シルエットエッジの計算
                // NdotVが小さい（0に近い）ほど、法線とカメラが垂直（輪郭部分）
                float silhouette = 1.0 - smoothstep(_SilhouetteThreshold - _SilhouetteSoftness, 
                                                    _SilhouetteThreshold + _SilhouetteSoftness, 
                                                    NdotV);
                
                // アウトラインの色とアルファ
                half4 color = half4(_OutlineColor.rgb, _OutlineAlpha * _OutlineColor.a * silhouette);
                
                // シルエット強度が低い場合は完全に透明にする
                if (silhouette < 0.01)
                {
                    discard;
                }
                
                // フォグ適用
                color.rgb = MixFog(color.rgb, i.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}