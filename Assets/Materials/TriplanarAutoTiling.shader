Shader "Custom/TriplanarAutoTiling"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TileScale ("Tile Scale", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _TileScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Triplanar UVs
                float2 uvX = i.worldPos.yz * _TileScale;
                float2 uvY = i.worldPos.xz * _TileScale;
                float2 uvZ = i.worldPos.xy * _TileScale;

                // Sample textures from 3 planes
                fixed4 texX = tex2D(_MainTex, uvX);
                fixed4 texY = tex2D(_MainTex, uvY);
                fixed4 texZ = tex2D(_MainTex, uvZ);

                // Calculate blend weights
                float3 blendWeights = abs(i.worldNormal);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                // Blend the textures
                fixed4 col = texX * blendWeights.x + texY * blendWeights.y + texZ * blendWeights.z;
                return col;
            }
            ENDCG
        }
    }
}
