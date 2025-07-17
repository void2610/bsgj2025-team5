using UnityEngine;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine.AddressableAssets;

/// <summary>
/// ゲームクリア時の演出シーケンスを管理するクラス
/// </summary>
public class GameClearSequence
{
    // 演出定数
    private const float CAMERA_ANIMATION_DURATION = 3.5f;    // カメラ演出の継続時間
    private const float CAMERA_DISTANCE = 3.5f;              // カメラとプレイヤーの距離
    private const float CAMERA_HEIGHT_OFFSET = 1f;           // カメラの高さオフセット
    private const float SHAKE_AMOUNT = 0.1f;                 // 振動の強度
    private const float SHAKE_DURATION = 1f;                 // 振動の継続時間
    private const float BASE_EXPLOSION_FORCE = 6f;           // ガシャ玉が飛ぶ力
    private const float UPWARD_FORCE_MULTIPLIER = 1.0f;     // 上方向への力の倍率
    private const float TORQUE_FORCE = 15f;                  // 回転力
    private const Ease CAMERA_EASE = Ease.InOutQuart;        // カメラ演出のイージング
    
    // Addressableキー定数
    private const string PARTICLE_ADDRESSABLE_KEY = "GameClearParticle";
    private const string SEPARATED_GASHA_ADDRESSABLE_KEY = "SeparatedGasha";
    private const string GAME_CLEAR_SE1_ADDRESSABLE_KEY = "GameClearSe1";
    private const string GAME_CLEAR_SE2_ADDRESSABLE_KEY = "GameClearSe2";
    
    private readonly Player _player;
    private readonly PlayerCamera _playerCamera;
    private readonly GameObject _foxGameObject;
    
    public GameClearSequence(
        Player player,
        PlayerCamera playerCamera,
        GameObject foxGameObject)
    {
        this._player = player;
        this._playerCamera = playerCamera;
        this._foxGameObject = foxGameObject;
    }
    
    /// <summary>
    /// ゲームクリア演出を実行する
    /// </summary>
    public async UniTask StartSequenceAsync()
    {
        // 必要なアセットを事前ロード
        var seData1Task = LoadSeDataAsync(GAME_CLEAR_SE1_ADDRESSABLE_KEY);
        var seData2Task = LoadSeDataAsync(GAME_CLEAR_SE2_ADDRESSABLE_KEY);
        var particleTask = Addressables.LoadAssetAsync<GameObject>(PARTICLE_ADDRESSABLE_KEY).ToUniTask();
        var separatedGashaTask = Addressables.LoadAssetAsync<GameObject>(SEPARATED_GASHA_ADDRESSABLE_KEY).ToUniTask();
        
        await UniTask.WhenAll(seData1Task, seData2Task, particleTask, separatedGashaTask);
        
        var gameClearSe1 = seData1Task.GetAwaiter().GetResult();
        var gameClearSe2 = seData2Task.GetAwaiter().GetResult();
        var particlePrefab = particleTask.GetAwaiter().GetResult();
        var separatedGashaPrefab = separatedGashaTask.GetAwaiter().GetResult();
        
        // プレイヤーの動きを完全に停止
        _player.StopMovement();
        await UniTask.Delay(100);
        
        var currentGashaPosition = _player.transform.position;
        
        // カメラをプレイヤーの正面に移動させる
        await MoveCameraToFrontAsync(_foxGameObject);
        await UniTask.Delay(500);
        
        // ガシャ玉振動と力溜めSE
        var powerChargeSeTask = SeManager.Instance.PlaySeAsync(gameClearSe1, pitch: 1.0f, important: true);
        var shakeMotion = StartGashaShake(_player.transform);
        
        // パーティクル再生
        var particleInstance = Object.Instantiate(particlePrefab, currentGashaPosition, Quaternion.identity);
        var particleSystem = particleInstance.GetComponent<ParticleSystem>();
        
        // プレイヤーのガシャ玉を非表示
        HidePlayerGasha();
        
        // 2つに分かれたガシャ玉を生成
        var separatedGashaInstance = Object.Instantiate(separatedGashaPrefab, currentGashaPosition, Quaternion.identity);
        shakeMotion.Cancel();
        
        // 新しいガシャ玉を振動開始
        shakeMotion = StartGashaShake(separatedGashaInstance.transform, frequency: 15, dampingRatio: 0.2f, seed: 456);
        
        await UniTask.Delay(500);
        
        // パーティクルを停止
        particleSystem.Stop();
        
        await UniTask.Delay(500);
        await powerChargeSeTask;
        
        shakeMotion.Cancel();
        
        // 2秒待機
        await UniTask.Delay(2000);
        
        // ガシャ玉を飛ばす
        ExplodeGashaPieces(separatedGashaInstance);
        
        // ガシャ玉が割れるSE再生
        await SeManager.Instance.PlaySeAsync(gameClearSe2, pitch: 1.0f, important: true);
        
        // 演出終了待機
        await UniTask.Delay(2500, cancellationToken: _player.GetCancellationTokenOnDestroy());
        
        // リソースを解放
        Addressables.Release(gameClearSe1);
        Addressables.Release(gameClearSe2);
        Addressables.Release(particlePrefab);
        Addressables.Release(separatedGashaPrefab);
    }
    
    /// <summary>
    /// カメラを狐の正面に移動させる
    /// </summary>
    private async UniTask MoveCameraToFrontAsync(GameObject foxGameObjectPrefab)
    {
        // カメラの通常更新を停止
        _playerCamera.SetIntroMode(true);
        
        // 狐の位置と向きを取得
        var foxPosition = foxGameObjectPrefab.transform.position;
        var foxForward = foxGameObjectPrefab.transform.forward;
        
        // 正面の位置と回転を計算
        var finalCameraPosition = foxPosition - foxForward * CAMERA_DISTANCE + Vector3.up * CAMERA_HEIGHT_OFFSET;
        var finalCameraRotation = Quaternion.LookRotation(foxPosition - finalCameraPosition);
        
        var startPosition = _playerCamera.transform.position;
        var startRotation = _playerCamera.transform.rotation;
        
        // 位置と回転のアニメーションを同時実行
        var positionTask = LMotion.Create(startPosition, finalCameraPosition, CAMERA_ANIMATION_DURATION)
            .WithEase(CAMERA_EASE)
            .Bind(pos => _playerCamera.transform.position = pos)
            .ToUniTask();
            
        var rotationTask = LMotion.Create(startRotation, finalCameraRotation, CAMERA_ANIMATION_DURATION)
            .WithEase(CAMERA_EASE)
            .Bind(rot => _playerCamera.transform.rotation = rot)
            .ToUniTask();
        
        await UniTask.WhenAll(positionTask, rotationTask);
    }
    
    /// <summary>
    /// ガシャ玉の振動を開始する
    /// </summary>
    private MotionHandle StartGashaShake(Transform target, int frequency = 20, float dampingRatio = 0.1f, uint seed = 123)
    {
        return LMotion.Shake.Create(SHAKE_AMOUNT, SHAKE_AMOUNT, SHAKE_DURATION)
            .WithLoops(-1, LoopType.Flip)
            .WithFrequency(frequency)
            .WithDampingRatio(dampingRatio)
            .WithRandomSeed(seed)
            .BindToPositionX(target);
    }
    
    /// <summary>
    /// プレイヤーのガシャ玉を非表示にする
    /// </summary>
    private void HidePlayerGasha()
    {
        if (_player)
        {
            var playerMeshRenderer = _player.GetComponent<MeshRenderer>();
            if (playerMeshRenderer)
            {
                playerMeshRenderer.enabled = false;
            }
        }
    }
    
    /// <summary>
    /// 分離したガシャ玉のピースを飛ばす
    /// </summary>
    private void ExplodeGashaPieces(GameObject separatedGasha)
    {
        foreach (Transform child in separatedGasha.transform)
        {
            var childRb = child.GetComponent<Rigidbody>();
            
            childRb.isKinematic = false;
            childRb.useGravity = true;
            
            // ランダムな方向ベクトルを生成
            var randomHorizontal = new Vector3(
                Random.Range(-1f, 1f), 
                0, 
                Random.Range(-1f, 1f)
            ).normalized;
            
            var forceDirection = (randomHorizontal + Vector3.up * UPWARD_FORCE_MULTIPLIER).normalized;
            
            // 力を加える
            childRb.AddForce(forceDirection * BASE_EXPLOSION_FORCE, ForceMode.Impulse);
            childRb.AddTorque(Random.insideUnitSphere * TORQUE_FORCE, ForceMode.Impulse);
        }
    }
    
    /// <summary>
    /// SeDataをAddressableから読み込む
    /// </summary>
    private async UniTask<SeData> LoadSeDataAsync(string addressableKey)
    {
        return await Addressables.LoadAssetAsync<SeData>(addressableKey).ToUniTask();
    }
}