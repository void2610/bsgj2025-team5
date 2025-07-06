using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// 3Dオブジェクトにアウトラインを追加するコンポーネント
/// </summary>
[System.Serializable]
public class OutlineSettings
{
    [Header("アウトライン設定")]
    public Color outlineColor = Color.black;
    [Range(0.0f, 1f)]
    public float outlineWidth = 0.01f;
    [Range(0.0f, 1.0f)]
    public float outlineAlpha = 1.0f;
    
    [Header("アウトラインモード")]
    public OutlineMode mode = OutlineMode.OutlineOnly;
    
    [Header("レンダリング設定")]
    public bool enableOutline = true;
    public LayerMask outlineLayer = 1;
}

public enum OutlineMode
{
    OutlineOnly,     // アウトラインのみ
    ToonOutline      // トゥーンレンダリング + アウトライン
}

public class OutlineObject : MonoBehaviour
{
    [SerializeField] private OutlineSettings outlineSettings = new OutlineSettings();
    
    private Material outlineMaterial;
    private Material toonOutlineMaterial;
    private Renderer[] renderers;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    // 全てのアウトラインオブジェクトを管理
    private static List<OutlineObject> allOutlineObjects = new List<OutlineObject>();
    
    public OutlineSettings Settings => outlineSettings;
    public bool IsOutlineEnabled => outlineSettings.enableOutline;
    
    void Awake()
    {
        InitializeOutline();
    }
    
    void OnEnable()
    {
        if (!allOutlineObjects.Contains(this))
        {
            allOutlineObjects.Add(this);
        }
        
        if (outlineSettings.enableOutline)
        {
            EnableOutline();
        }
    }
    
    void OnDisable()
    {
        allOutlineObjects.Remove(this);
        DisableOutline();
    }
    
    void OnDestroy()
    {
        if (outlineMaterial != null)
        {
            DestroyImmediate(outlineMaterial);
        }
        if (toonOutlineMaterial != null)
        {
            DestroyImmediate(toonOutlineMaterial);
        }
    }
    
    /// <summary>
    /// アウトラインシステムの初期化
    /// </summary>
    private void InitializeOutline()
    {
        renderers = GetComponentsInChildren<Renderer>();
        
        // 元のマテリアルを保存
        foreach (var renderer in renderers)
        {
            originalMaterials[renderer] = renderer.materials;
        }
        
        CreateOutlineMaterials();
    }
    
    /// <summary>
    /// アウトライン用マテリアルの作成
    /// </summary>
    private void CreateOutlineMaterials()
    {
        var outlineShader = Shader.Find("Custom/OutlineOnly");
        var toonOutlineShader = Shader.Find("Custom/ToonOutline");
        
        if (outlineShader != null)
        {
            outlineMaterial = new Material(outlineShader);
            UpdateOutlineMaterial(outlineMaterial);
        }
        else
        {
            Debug.LogError("OutlineOnly shader not found!");
        }
        
        if (toonOutlineShader != null)
        {
            toonOutlineMaterial = new Material(toonOutlineShader);
            UpdateOutlineMaterial(toonOutlineMaterial);
        }
        else
        {
            Debug.LogError("ToonOutline shader not found!");
        }
    }
    
    /// <summary>
    /// アウトラインマテリアルの更新
    /// </summary>
    private void UpdateOutlineMaterial(Material material)
    {
        if (material == null) return;
        
        // 基本アウトラインプロパティの設定
        material.SetColor("_OutlineColor", outlineSettings.outlineColor);
        material.SetFloat("_OutlineWidth", outlineSettings.outlineWidth);
        material.SetFloat("_OutlineAlpha", outlineSettings.outlineAlpha);
        
        // 透明オブジェクトの検出
        bool isTransparent = IsObjectTransparent();
        
        // アウトラインモードに応じた設定
        if (outlineSettings.mode == OutlineMode.ToonOutline)
        {
            // トゥーンモードの場合、元のマテリアルの色を使用
            Color originalColor = GetOriginalObjectColor();
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", originalColor);
            }
            // メインテクスチャを元のマテリアルからコピー
            CopyMainTextureFromOriginal(material);
        }
        else
        {
            // アウトラインオンリーモードの場合、アウトライン色を使用
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", outlineSettings.outlineColor);
            }
        }
        
        // カリング設定の調整
        if (material.HasProperty("_Cull"))
        {
            if (isTransparent)
            {
                if (outlineSettings.mode == OutlineMode.ToonOutline)
                {
                    // ToonOutlineモードの透明オブジェクト: 元のカリング設定を保持
                    // シェーダーが適切に処理する
                }
                else
                {
                    // OutlineOnlyモードの透明オブジェクト: 裏面カリングで裏側を隠す
                    material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
                }
            }
            else
            {
                // 不透明オブジェクト: カリング無効でアウトラインを全周表示
                material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            }
        }
    }
    
    /// <summary>
    /// アウトラインを有効にする
    /// </summary>
    public void EnableOutline()
    {
        if (renderers == null || renderers.Length == 0) return;
        
        Material targetMaterial = GetTargetMaterial();
        if (targetMaterial == null) return;
        
        // 透明オブジェクトの検出
        bool isTransparent = IsObjectTransparent();
        
        foreach (var renderer in renderers)
        {
            if (outlineSettings.mode == OutlineMode.ToonOutline)
            {
                // トゥーンアウトラインモード: 元のマテリアルを置き換え
                var materials = new Material[1] { targetMaterial };
                renderer.materials = materials;
            }
            else
            {
                // アウトラインのみモード: 元のマテリアルに追加
                var originalMats = originalMaterials[renderer];
                Material[] materials;
                
                if (isTransparent)
                {
                    // 透明オブジェクトの場合、アウトラインを先に描画
                    materials = new Material[originalMats.Length + 1];
                    materials[0] = targetMaterial; // アウトラインを最初に
                    for (int i = 0; i < originalMats.Length; i++)
                    {
                        materials[i + 1] = originalMats[i];
                    }
                }
                else
                {
                    // 不透明オブジェクトの場合、アウトラインを後に描画
                    materials = new Material[originalMats.Length + 1];
                    for (int i = 0; i < originalMats.Length; i++)
                    {
                        materials[i] = originalMats[i];
                    }
                    materials[originalMats.Length] = targetMaterial; // アウトラインを最後に
                }
                
                renderer.materials = materials;
            }
        }
        
        // レイヤーの設定
        SetLayerRecursively(gameObject, outlineSettings.outlineLayer);
    }
    
    /// <summary>
    /// アウトラインを無効にする
    /// </summary>
    public void DisableOutline()
    {
        if (renderers == null || renderers.Length == 0) return;
        
        foreach (var renderer in renderers)
        {
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.materials = originalMaterials[renderer];
            }
        }
    }
    
    /// <summary>
    /// 使用するマテリアルを取得
    /// </summary>
    private Material GetTargetMaterial()
    {
        switch (outlineSettings.mode)
        {
            case OutlineMode.OutlineOnly:
                return outlineMaterial;
            case OutlineMode.ToonOutline:
                return toonOutlineMaterial;
            default:
                return outlineMaterial;
        }
    }
    
    /// <summary>
    /// 再帰的にレイヤーを設定
    /// </summary>
    private void SetLayerRecursively(GameObject obj, LayerMask layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    /// <summary>
    /// ランタイムでアウトライン設定を更新
    /// </summary>
    public void UpdateOutlineSettings()
    {
        UpdateOutlineMaterial(outlineMaterial);
        UpdateOutlineMaterial(toonOutlineMaterial);
        
        if (outlineSettings.enableOutline)
        {
            EnableOutline();
        }
        else
        {
            DisableOutline();
        }
    }
    
    /// <summary>
    /// アウトラインの色を設定
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        outlineSettings.outlineColor = color;
        UpdateOutlineSettings();
    }
    
    /// <summary>
    /// アウトラインの幅を設定
    /// </summary>
    public void SetOutlineWidth(float width)
    {
        outlineSettings.outlineWidth = Mathf.Clamp(width, 0.0f, 0.1f);
        UpdateOutlineSettings();
    }
    
    /// <summary>
    /// アウトラインの不透明度を設定
    /// </summary>
    public void SetOutlineAlpha(float alpha)
    {
        outlineSettings.outlineAlpha = Mathf.Clamp01(alpha);
        UpdateOutlineSettings();
    }
    
    /// <summary>
    /// アウトラインモードを設定
    /// </summary>
    public void SetOutlineMode(OutlineMode mode)
    {
        outlineSettings.mode = mode;
        UpdateOutlineSettings();
    }
    
    /// <summary>
    /// アウトラインのオン/オフを切り替え
    /// </summary>
    public void ToggleOutline()
    {
        outlineSettings.enableOutline = !outlineSettings.enableOutline;
        UpdateOutlineSettings();
    }
    
    /// <summary>
    /// 全てのアウトラインオブジェクトを取得
    /// </summary>
    public static List<OutlineObject> GetAllOutlineObjects()
    {
        return new List<OutlineObject>(allOutlineObjects);
    }
    
    /// <summary>
    /// 全てのアウトラインを一括で有効/無効にする
    /// </summary>
    public static void SetAllOutlinesEnabled(bool enabled)
    {
        foreach (var outline in allOutlineObjects)
        {
            outline.outlineSettings.enableOutline = enabled;
            outline.UpdateOutlineSettings();
        }
    }
    
    /// <summary>
    /// オブジェクトが透明かどうかを取得
    /// </summary>
    public bool IsTransparent()
    {
        return IsObjectTransparent();
    }
    
    /// <summary>
    /// オブジェクトが透明かどうかを判定
    /// </summary>
    private bool IsObjectTransparent()
    {
        if (renderers == null || renderers.Length == 0) return false;
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                
                // マテリアルのレンダリングモードをチェック
                if (material.HasProperty("_Mode"))
                {
                    var mode = material.GetFloat("_Mode");
                    if (mode == 2 || mode == 3) // Fade or Transparent
                    {
                        return true;
                    }
                }
                
                // アルファ値をチェック
                if (material.HasProperty("_Color"))
                {
                    var color = material.GetColor("_Color");
                    if (color.a < 1.0f)
                    {
                        return true;
                    }
                }
                
                // シェーダーのレンダーキューをチェック
                if (material.renderQueue >= 3000) // Transparent queue
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 元のオブジェクトの色を取得
    /// </summary>
    private Color GetOriginalObjectColor()
    {
        if (renderers == null || renderers.Length == 0) return Color.white;
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            if (originalMaterials.ContainsKey(renderer))
            {
                var materials = originalMaterials[renderer];
                foreach (var material in materials)
                {
                    if (material != null && material.HasProperty("_Color"))
                    {
                        return material.GetColor("_Color");
                    }
                }
            }
        }
        
        return Color.white;
    }
    
    /// <summary>
    /// 元のマテリアルからメインテクスチャをコピー
    /// </summary>
    private void CopyMainTextureFromOriginal(Material targetMaterial)
    {
        if (targetMaterial == null || renderers == null || renderers.Length == 0) return;
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            if (originalMaterials.ContainsKey(renderer))
            {
                var materials = originalMaterials[renderer];
                foreach (var material in materials)
                {
                    if (material != null && material.HasProperty("_MainTex"))
                    {
                        var mainTexture = material.GetTexture("_MainTex");
                        if (mainTexture != null && targetMaterial.HasProperty("_MainTex"))
                        {
                            targetMaterial.SetTexture("_MainTex", mainTexture);
                            return;
                        }
                    }
                }
            }
        }
    }
}