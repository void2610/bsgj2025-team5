Shader "Custom/URP/ChromaKey"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _KeyColor ("Key Color", Color) = (0, 1, 0, 1)
        _Threshold ("Threshold", Range(0, 1)) = 0.1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _EdgeSharpness ("Edge Sharpness", Range(1, 10)) = 2.0
        _ColorSpillRemoval ("Color Spill Removal", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "ChromaKey"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
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
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _KeyColor;
                float _Threshold;
                float _Smoothness;
                float _EdgeSharpness;
                float _ColorSpillRemoval;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            
            // RGBからYCbCr色空間に変換
            float3 RGBToYCbCr(float3 rgb)
            {
                float y = 0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b;
                float cb = 0.564 * (rgb.b - y);
                float cr = 0.713 * (rgb.r - y);
                return float3(y, cb, cr);
            }
            
            // HSV色空間への変換（より正確な色検出用）
            float3 RGBToHSV(float3 rgb)
            {
                float maxValue = max(rgb.r, max(rgb.g, rgb.b));
                float minValue = min(rgb.r, min(rgb.g, rgb.b));
                float delta = maxValue - minValue;
                
                float h = 0;
                float s = 0;
                float v = maxValue;
                
                if (delta != 0)
                {
                    s = delta / maxValue;
                    
                    if (rgb.r == maxValue)
                        h = (rgb.g - rgb.b) / delta;
                    else if (rgb.g == maxValue)
                        h = 2.0 + (rgb.b - rgb.r) / delta;
                    else
                        h = 4.0 + (rgb.r - rgb.g) / delta;
                    
                    h /= 6.0;
                    if (h < 0) h += 1.0;
                }
                
                return float3(h, s, v);
            }
            
            // 色の距離を計算（改善版）
            float ColorDistance(float3 color1, float3 color2)
            {
                // RGB距離（基本）
                float rgbDist = distance(color1, color2);
                
                // HSV距離（色相を重視）
                float3 hsv1 = RGBToHSV(color1);
                float3 hsv2 = RGBToHSV(color2);
                
                // 色相の差（循環を考慮）
                float hueDiff = abs(hsv1.x - hsv2.x);
                if (hueDiff > 0.5) hueDiff = 1.0 - hueDiff;
                
                // 彩度と明度の差
                float satDiff = abs(hsv1.y - hsv2.y);
                float valDiff = abs(hsv1.z - hsv2.z);
                
                // 重み付けして最終的な距離を計算
                float dist = hueDiff * 2.0 + satDiff * 0.5 + valDiff * 0.5 + rgbDist * 0.3;
                
                return dist;
            }
            
            // カラースピル除去
            float3 RemoveColorSpill(float3 color, float3 keyColor, float amount)
            {
                float3 newColor = color;
                
                // キー色の成分を減らす
                if (keyColor.g > keyColor.r && keyColor.g > keyColor.b) // 緑スクリーン
                {
                    float spillAmount = max(0, color.g - max(color.r, color.b));
                    newColor.g -= spillAmount * amount;
                }
                else if (keyColor.b > keyColor.r && keyColor.b > keyColor.g) // 青スクリーン
                {
                    float spillAmount = max(0, color.b - max(color.r, color.g));
                    newColor.b -= spillAmount * amount;
                }
                
                return newColor;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 元のアルファ値を保存
                float originalAlpha = color.a;
                
                // キー色との距離を計算
                float dist = ColorDistance(color.rgb, _KeyColor.rgb);
                
                // アルファ値を計算（しきい値とスムージングを適用）
                float chromaAlpha = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, dist);
                
                // エッジシャープネスを適用（より鋭いエッジを作成）
                chromaAlpha = saturate(pow(chromaAlpha, _EdgeSharpness));
                
                // カラースピル除去を適用（アルファ値に基づいて強度を調整）
                color.rgb = RemoveColorSpill(color.rgb, _KeyColor.rgb, _ColorSpillRemoval * (1.0 - chromaAlpha));
                
                // フォグを適用
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                // アルファを設定（元のアルファ値を考慮し、クロマキー部分のみ透過）
                color.a = originalAlpha * chromaAlpha;
                
                // デバッグ用：完全に不透明にすべき部分を確実に不透明にする
                if (dist > _Threshold + _Smoothness)
                {
                    color.a = originalAlpha;
                }
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

// ===== Shader Graph実装ガイド =====

/* 
Shader Graphでクロマキーシェーダーを作成する手順：

1. 新しいShader Graphを作成
   - Create > Shader Graph > URP > Lit Shader Graph

2. Graph Settingsで以下を設定
   - Surface Type: Transparent
   - Blending Mode: Alpha
   - Render Face: Both Sides

3. 必要なプロパティを追加
   - Texture2D: MainTexture
   - Color: KeyColor (デフォルト: 緑 0,1,0,1)
   - Float: Threshold (デフォルト: 0.1, Range: 0-1)
   - Float: Smoothness (デフォルト: 0.1, Range: 0-1)

4. ノード構成
   a. Sample Texture 2D ノードでMainTextureをサンプリング
   
   b. 色距離の計算：
      - Subtract ノード: SampledColor - KeyColor
      - Length ノード: 色の差のベクトル長を計算
   
   c. アルファ計算：
      - Smoothstep ノード:
        - Edge1: Threshold - Smoothness
        - Edge2: Threshold + Smoothness
        - In: 色距離
   
   d. 最終出力：
      - Base Color: サンプリングしたテクスチャのRGB
      - Alpha: Smoothstepの結果

5. より高度な実装（オプション）
   - Custom Function ノードを使用して、上記HLSLコードの
     RGBToYCbCr関数とColorDistance関数を実装
   - これによりより正確な色検出が可能になります

使用方法：
1. マテリアルを作成し、このシェーダーを適用
2. Key Colorに透過したい色を設定（通常は緑: 0,1,0）
3. Thresholdで透過する色の範囲を調整
4. Smoothnessでエッジのソフトさを調整
5. Color Spill Removalで色かぶりを除去

注意事項：
- カメラのRendering > Post Processingで
  「Opaque Texture」と「Depth Texture」を有効にすると
  より良い結果が得られる場合があります
- ライティングの影響を受けたくない場合は、
  Unlit Shader Graphを使用してください
*/