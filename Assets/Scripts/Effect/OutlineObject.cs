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
        
        material.SetColor("_OutlineColor", outlineSettings.outlineColor);
        material.SetFloat("_OutlineWidth", outlineSettings.outlineWidth);
        material.SetFloat("_OutlineAlpha", outlineSettings.outlineAlpha);
        
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
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
                var materials = new Material[originalMats.Length + 1];
                
                // 元のマテリアルをコピー
                for (int i = 0; i < originalMats.Length; i++)
                {
                    materials[i] = originalMats[i];
                }
                
                // アウトラインマテリアルを追加
                materials[originalMats.Length] = targetMaterial;
                
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
}