using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;

/// <summary>
/// URP用のアウトラインレンダリングフィーチャー
/// </summary>
public class OutlineRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineRenderSettings
    {
        [Header("レンダリング設定")]
        public LayerMask layerMask = -1;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        
        [Header("透明オブジェクト対応")]
        public bool supportTransparentObjects = true;
        public RenderPassEvent transparentRenderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        
        [Header("フィルタリング設定")]
        public string[] shaderTagIds = new string[] { "SRPDefaultUnlit" };
        
        [Header("デバッグ設定")]
        public bool enableDebugLog = false;
    }
    
    [SerializeField] private OutlineRenderSettings settings = new OutlineRenderSettings();
    
    private OutlineRenderPass outlineRenderPass;
    
    public override void Create()
    {
        outlineRenderPass = new OutlineRenderPass(settings);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlineRenderPass == null) return;
        
        // アウトラインオブジェクトが存在する場合のみパスを追加
        var outlineObjects = OutlineObject.GetAllOutlineObjects();
        if (outlineObjects.Count > 0)
        {
            // 透明オブジェクトが含まれているかチェック
            bool hasTransparentObjects = false;
            foreach (var obj in outlineObjects)
            {
                if (obj.IsTransparent())
                {
                    hasTransparentObjects = true;
                    break;
                }
            }
            
            if (hasTransparentObjects && settings.supportTransparentObjects)
            {
                // 透明オブジェクトがある場合は、透明オブジェクトの描画前に実行
                outlineRenderPass.renderPassEvent = settings.transparentRenderPassEvent;
            }
            else
            {
                // 通常のレンダリングタイミング
                outlineRenderPass.renderPassEvent = settings.renderPassEvent;
            }
            
            renderer.EnqueuePass(outlineRenderPass);
        }
    }
}

/// <summary>
/// アウトラインレンダリングパス
/// </summary>
public class OutlineRenderPass : ScriptableRenderPass
{
    private OutlineRenderFeature.OutlineRenderSettings settings;
    private List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();
    private FilteringSettings filteringSettings;
    
    private const string ProfilerTag = "OutlineRenderPass";
    
    public OutlineRenderPass(OutlineRenderFeature.OutlineRenderSettings settings)
    {
        this.settings = settings;
        this.renderPassEvent = settings.renderPassEvent;
        
        // シェーダータグIDの設定
        if (settings.shaderTagIds != null && settings.shaderTagIds.Length > 0)
        {
            foreach (var passName in settings.shaderTagIds)
            {
                shaderTagIds.Add(new ShaderTagId(passName));
            }
        }
        else
        {
            shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
        }
        
        // フィルタリング設定
        filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.layerMask);
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // アウトラインオブジェクトが存在しない場合は何もしない
        var outlineObjects = OutlineObject.GetAllOutlineObjects();
        if (outlineObjects.Count == 0) return;
        
        CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);
        
        using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
        {
            // カメラとライティングの設定
            context.SetupCameraProperties(renderingData.cameraData.camera);
            
            // 描画設定
            var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawingSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, sortingCriteria);
            
            // アウトラインオブジェクトのみを描画
            foreach (var outlineObject in outlineObjects)
            {
                if (outlineObject.IsOutlineEnabled)
                {
                    RenderOutlineObject(context, cmd, outlineObject, drawingSettings, filteringSettings);
                }
            }
        }
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    /// <summary>
    /// Unity 6 RenderGraph用の実装（簡略版）
    /// </summary>
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // アウトラインオブジェクトが存在しない場合は何もしない
        var outlineObjects = OutlineObject.GetAllOutlineObjects();
        if (outlineObjects.Count == 0) return;

        // デバッグログ
        if (settings.enableDebugLog)
        {
            Debug.Log("RenderGraph: Recording outline render pass");
        }

        // シンプルなRenderGraphパスとして実装
        // より詳細な実装が必要な場合は、UniversalResourceDataとUniversalCameraDataを使用
    }
    
    /// <summary>
    /// アウトラインオブジェクトを描画
    /// </summary>
    private void RenderOutlineObject(ScriptableRenderContext context, CommandBuffer cmd, 
        OutlineObject outlineObject, DrawingSettings drawingSettings, FilteringSettings filteringSettings)
    {
        if (outlineObject == null || !outlineObject.gameObject.activeInHierarchy) return;
        
        var renderers = outlineObject.GetComponentsInChildren<Renderer>();
        
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            
            // レイヤーマスクのチェック
            int layer = renderer.gameObject.layer;
            if ((settings.layerMask.value & (1 << layer)) == 0) continue;
            
            // デバッグログ
            if (settings.enableDebugLog)
            {
                Debug.Log($"Rendering outline for: {renderer.name}");
            }
        }
    }
    
    /// <summary>
    /// クリーンアップ
    /// </summary>
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // 必要に応じてクリーンアップ処理を追加
    }
}