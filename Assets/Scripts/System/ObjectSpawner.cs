using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// 定期的に指定したオブジェクトを生成するコンポーネント
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("生成するプレハブ")]
    [SerializeField] private GameObject prefabToSpawn;
    
    [Tooltip("生成間隔（秒）")]
    [SerializeField] private float spawnInterval = 1f;
    
    [Header("オプション")]
    [Tooltip("生成時にランダムな位置オフセットを追加")]
    [SerializeField] private Vector3 randomPositionRange = Vector3.zero;
    
    [Tooltip("生成したオブジェクトの親")]
    [SerializeField] private Transform parentTransform;
    
    private CancellationTokenSource _cts;
    
    private void Start()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("生成するプレハブが設定されていません", this);
            return;
        }
        
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
    /// 生成間隔を変更
    /// </summary>
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.1f, interval);
    }
    
    /// <summary>
    /// 生成ループ
    /// </summary>
    private async UniTaskVoid SpawnLoop(CancellationToken ct)
    {
        try
        {
            // 最初は待機してから生成開始
            await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: ct);
            
            while (!ct.IsCancellationRequested)
            {
                SpawnObject();
                await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: ct);
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