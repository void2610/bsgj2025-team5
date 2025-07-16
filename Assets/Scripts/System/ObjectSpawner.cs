using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using R3;

/// <summary>
/// プレイヤーのアイテム個数に応じてオブジェクトを生成するコンポーネント
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("生成するプレハブ")]
    [SerializeField] private GameObject prefabToSpawn;
    
    [Tooltip("各PlayerItemCountレベルでの1秒あたりの生成数")]
    [SerializeField] private float[] spawnRatesPerLevel = { 0f, 0.5f, 1f, 1.5f, 2f };
    
    [Header("オプション")]
    [Tooltip("生成時にランダムな位置オフセットを追加")]
    [SerializeField] private Vector3 randomPositionRange = Vector3.zero;
    
    [Tooltip("生成したオブジェクトの親")]
    [SerializeField] private Transform parentTransform;
    
    private CancellationTokenSource _cts;
    private float _currentSpawnRate;
    
    private void Start()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("生成するプレハブが設定されていません", this);
            return;
        }
        
        // プレイヤーのアイテム数変化を購読
        GameManager.Instance.Player.PlayerItemCountInt
            .Subscribe(OnChangePlayerItemCount)
            .AddTo(this);
        
        // 初期生成レートを設定
        OnChangePlayerItemCount(GameManager.Instance.Player.PlayerItemCountInt.CurrentValue);
        
        StartSpawning();
    }
    
    private void OnEnable()
    {
        // 再度有効になったときは生成を再開
        if (prefabToSpawn != null)
        {
            StartSpawning();
        }
    }
    
    private void OnDisable()
    {
        StopSpawning();
    }
    
    private void OnDestroy()
    {
        StopSpawning();
    }
    
    /// <summary>
    /// 生成を開始
    /// </summary>
    public void StartSpawning()
    {
        StopSpawning(); // 既存の生成処理を停止
        _cts = new CancellationTokenSource();
        SpawnLoop(_cts.Token).Forget();
    }
    
    /// <summary>
    /// 生成を停止
    /// </summary>
    public void StopSpawning()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
    
    /// <summary>
    /// オブジェクトを即座に1つ生成
    /// </summary>
    public void SpawnImmediately()
    {
        SpawnObject();
    }
    
    /// <summary>
    /// プレイヤーアイテム数が変化した時の処理
    /// </summary>
    private void OnChangePlayerItemCount(int itemCount)
    {
        // アイテム数を配列のインデックスに変換（0-4の範囲）
        int levelIndex = Mathf.Clamp(itemCount, 0, spawnRatesPerLevel.Length - 1);
        _currentSpawnRate = spawnRatesPerLevel[levelIndex];
    }
    
    /// <summary>
    /// 生成レートを変更
    /// </summary>
    public void SetSpawnRate(float rate)
    {
        _currentSpawnRate = Mathf.Max(0f, rate);
    }
    
    /// <summary>
    /// 生成ループ
    /// </summary>
    private async UniTaskVoid SpawnLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 生成レートが0の場合は待機
                if (_currentSpawnRate <= 0f)
                {
                    await UniTask.Delay(100, cancellationToken: ct); // 100ms待機
                    continue;
                }
                
                // 生成間隔を計算（1秒 / 生成レート）
                float interval = 1f / _currentSpawnRate;
                
                SpawnObject();
                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);
            }
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルは正常な動作
        }
    }
    
    /// <summary>
    /// オブジェクトを生成
    /// </summary>
    private void SpawnObject()
    {
        if (prefabToSpawn == null) return;
        
        // 生成位置を計算
        Vector3 position = CalculateSpawnPosition();
        
        // オブジェクトを生成
        GameObject spawned = Instantiate(prefabToSpawn, position, transform.rotation);
        
        // 親を設定
        if (parentTransform != null)
        {
            spawned.transform.SetParent(parentTransform, true);
        }
    }
    
    /// <summary>
    /// 生成位置を計算
    /// </summary>
    private Vector3 CalculateSpawnPosition()
    {
        Vector3 basePosition = transform.position;
        
        // ランダムオフセットを追加
        if (randomPositionRange != Vector3.zero)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomPositionRange.x, randomPositionRange.x),
                Random.Range(-randomPositionRange.y, randomPositionRange.y),
                Random.Range(-randomPositionRange.z, randomPositionRange.z)
            );
            basePosition += randomOffset;
        }
        
        return basePosition;
    }
}