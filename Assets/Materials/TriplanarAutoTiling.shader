// トライプラナー自動タイリングシェーダー
// 3D オブジェクトの3軸（X、Y、Z）からテクスチャを投影し、法線に基づいてブレンドするシェーダー
// オブジェクトのスケールに応じてタイリングを自動調整し、自然な見た目を実現する
Shader "Custom/TriplanarAutoTiling"
{
    // マテリアルのインスペクターで調整可能なプロパティ
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}              // メインテクスチャ
        _TileScale ("Tile Scale", Float) = 1.0             // タイリングスケール（値が大きいほどテクスチャが小さく表示される）
        _Color ("Color", Color) = (1,1,1,1)             // 色の乗算用（白色の場合は元のテクスチャ色がそのまま表示される）
    }
    SubShader
    {
        // レンダリングタイプを不透明に設定
        Tags { "RenderType"="Opaque" }
        LOD 100  // Level of Detail（詳細度レベル）

        Pass
        {
            CGPROGRAM
            // 頂点シェーダーとフラグメントシェーダーを指定
            #pragma vertex vert
            #pragma fragment frag

            // Unity の標準的な関数やマクロを使用するためのインクルード
            #include "UnityCG.cginc"

            // 頂点シェーダーへの入力データ構造体
            struct appdata
            {
                float4 vertex : POSITION;     // 頂点位置（ローカル座標）
                float2 uv : TEXCOORD0;        // UV座標（テクスチャ座標）
                float3 normal : NORMAL;       // 法線ベクトル（ローカル座標）
            };

            // 頂点シェーダーからフラグメントシェーダーに渡すデータ構造体
            struct v2f
            {
                float2 uv : TEXCOORD0;           // UV座標
                float3 worldPos : TEXCOORD1;     // ワールド座標での頂点位置
                float3 worldNormal : TEXCOORD2;  // ワールド座標での法線ベクトル
                float4 vertex : SV_POSITION;     // クリップ空間での頂点位置（最終的な画面位置）
            };

            // プロパティと対応するシェーダー変数の宣言
            sampler2D _MainTex;    // メインテクスチャのサンプラー
            float4 _MainTex_ST;    // テクスチャのスケールとオフセット情報
            float _TileScale;      // タイリングスケール値
            fixed4 _Color;         // 色の乗算用カラー

            // 頂点シェーダー：各頂点に対して実行される
            v2f vert (appdata v)
            {
                v2f o;  // 出力用のv2f構造体を初期化
                
                // ローカル座標からクリップ空間座標に変換（画面上の最終位置を計算）
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // UV座標にテクスチャのスケールとオフセットを適用
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // ローカル座標からワールド座標に変換（トライプラナーマッピングで使用）
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // ローカル法線をワールド法線に変換（ブレンド重みの計算で使用）
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                return o;  // フラグメントシェーダーに渡すデータを返す
            }

            // フラグメントシェーダー：各ピクセルに対して実行される
            fixed4 frag (v2f i) : SV_Target
            {
                // トライプラナーマッピング用のUV座標を3軸分計算
                // X軸投影：YZ平面にテクスチャを投影（正面・背面）
                float2 uvX = i.worldPos.yz * _TileScale;
                // Y軸投影：XZ平面にテクスチャを投影（上面・下面）
                float2 uvY = i.worldPos.xz * _TileScale;
                // Z軸投影：XY平面にテクスチャを投影（左面・右面）
                float2 uvZ = i.worldPos.xy * _TileScale;

                // 3つの軸からテクスチャをサンプリング
                fixed4 texX = tex2D(_MainTex, uvX);  // X軸投影のテクスチャ
                fixed4 texY = tex2D(_MainTex, uvY);  // Y軸投影のテクスチャ
                fixed4 texZ = tex2D(_MainTex, uvZ);  // Z軸投影のテクスチャ

                // ブレンド重みを計算：法線の各軸成分の絶対値を使用
                // 法線がX軸に近いほどtexXの重みが大きくなる
                float3 blendWeights = abs(i.worldNormal);
                
                // 重みを正規化（合計が1になるように調整）
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                // 3つのテクスチャを重みに基づいてブレンド
                fixed4 col = texX * blendWeights.x +    // X軸投影テクスチャ × X軸重み
                            texY * blendWeights.y +     // Y軸投影テクスチャ × Y軸重み
                            texZ * blendWeights.z;      // Z軸投影テクスチャ × Z軸重み
                
                // カラープロパティを乗算（色調整）
                col *= _Color;
                
                return col;  // 最終的なピクセル色を返す
            }
            ENDCG
        }
    }
}
