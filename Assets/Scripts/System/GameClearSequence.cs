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
    private const float FRONT_VIEW_DISTANCE = 6f;            // プレイヤー正面からの距離
    private const float FRONT_VIEW_HEIGHT_OFFSET = 1.25f;    // プレイヤー正面からの高さオフセット
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
    private readonly Canvas _uiCanvas;
    private readonly FoxMesh _foxMesh;
    
    public GameClearSequence(
        Player player,
        PlayerCamera playerCamera,
        Canvas uiCanvas,
        FoxMesh foxMesh)
    {
        this._player = player;
        this._playerCamera = playerCamera;
        this._uiCanvas = uiCanvas;
        this._foxMesh = foxMesh;
    }
    
    /// <summary>
    /// ゲームクリア演出を実行する
    /// </summary>
    public async UniTask StartSequenceAsync()
    {
        _player.StopMovement();
        HideUISlideAnimationsAsync();
        
        // プレイヤーの回転を初期状態に戻す
        await ResetPlayerRotationAsync();
        
        // 必要なアセットを事前ロード
        var seData1Task = LoadSeDataAsync(GAME_CLEAR_SE1_ADDRESSABLE_KEY);
        var seData2Task = LoadSeDataAsync(GAME_CLEAR_SE2_ADDRESSABLE_KEY);
        var particleTask = Addressables.LoadAssetAsync<GameObject>(PARTICLE_ADDRESSABLE_KEY).ToUniTask();
        var separatedGashaTask = Addressables.LoadAssetAsync<GameObject>(SEPARATED_GASHA_ADDRESSABLE_KEY).ToUniTask();
        
        // 並行してアセットを読み込み
        var gameClearSe1 = await seData1Task;
        var gameClearSe2 = await seData2Task;
        var particlePrefab = await particleTask;
        var separatedGashaPrefab = await separatedGashaTask;
        
        await UniTask.Delay(100);
        
        BGMManager.Instance.FadeOutBGM(0.3f, 1.5f);
        await MoveCameraToFrontAsync();
        await UniTask.Delay(500);
        
        // ガシャ玉振動と力溜めSE
        var shakeMotion = StartGashaShake(_player.transform);
        var powerChargeSeTask = SeManager.Instance.PlaySeAsync(gameClearSe1, pitch: 1.0f, important: true);
        
        // パーティクル再生
        var particleInstance = Object.Instantiate(particlePrefab, _player.transform.position, Quaternion.identity);
        var particleSystem = particleInstance.GetComponent<ParticleSystem>();
        
        await UniTask.Delay(1300);
        
        // 割れるガシャ玉に切り替え
        shakeMotion.Cancel();
        HidePlayerGasha();
        var separatedGashaInstance = Object.Instantiate(separatedGashaPrefab, _player.transform.position, Quaternion.identity);
        separatedGashaInstance.transform.rotation = _player.transform.rotation;
        shakeMotion = StartGashaShake(separatedGashaInstance.transform);
        
        await UniTask.Delay(500);
        
        particleSystem.Stop();
        
        await UniTask.Delay(500);
        await powerChargeSeTask;
        
        shakeMotion.Cancel();
        
        await UniTask.Delay(2000);
        
        // ガシャ玉を飛ばす
        ExplodeGashaPieces(separatedGashaInstance);
        _playerCamera.ShakeCamera(0.5f, 0.3f);
        await SeManager.Instance.PlaySeAsync(gameClearSe2, pitch: 1.0f, important: true);
        // 演出終了待機
        await UniTask.Delay(2500);
        
        // リソースを解放
        Addressables.Release(gameClearSe1);
        Addressables.Release(gameClearSe2);
        Addressables.Release(particlePrefab);
        Addressables.Release(separatedGashaPrefab);
    }
    
    private void HideUISlideAnimationsAsync()
    {
        var uiAnimations = _uiCanvas.GetComponentsInChildren<UISlideAnimation>();
        foreach (var animation in uiAnimations)
        {
            animation.StartSlideOutAnimationAsync().Forget();
        }
    }
    
    /// <summary>
    /// カメラを狐の正面に移動させる
    /// </summary>
    private async UniTask MoveCameraToFrontAsync()
    {
        // カメラの通常更新を停止
        _playerCamera.SetIntroMode(true);
        
        // 現在のTPSカメラの位置と回転を開始位置とする
        var startPosition = _playerCamera.transform.position;
        var startRotation = _playerCamera.transform.rotation;
        
        // 狐の位置と向きを取得
        var foxPosition = _foxMesh.transform.position;
        var foxForward = _foxMesh.transform.forward;
        
        // 正面カメラの位置と回転を計算（狐の正面から撮影）
        var finalCameraPosition = foxPosition - foxForward * FRONT_VIEW_DISTANCE + Vector3.up * FRONT_VIEW_HEIGHT_OFFSET;
        var lookAtDirection = foxPosition - finalCameraPosition;
        var finalCameraRotation = Quaternion.LookRotation(lookAtDirection);
        
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
    /// プレイヤーの回転を初期状態（前方向）に戻す
    /// </summary>
    private async UniTask ResetPlayerRotationAsync()
    {
        var currentRotation = _player.transform.rotation;
        var targetRotation = Quaternion.identity; // 初期回転（前方向）
        
        await LMotion.Create(currentRotation, targetRotation, 1f)
            .WithEase(Ease.OutQuart)
            .Bind(rot => _player.transform.rotation = rot)
            .ToUniTask();
    }
    
    /// <summary>
    /// ガシャ玉の振動を開始する（元の位置と振動位置を足し算）
    /// </summary>
    private MotionHandle StartGashaShake(Transform target)
    {
        var originalPosition = target.position;
        
        return LMotion.Shake.Create(Vector3.one * 0.3f, Vector3.one * 0.3f, 1f)
            .WithLoops(-1, LoopType.Flip)
            .WithFrequency(25)
            .WithDampingRatio(0.2f)
            .Bind(shakeOffset => 
            {
                target.position = originalPosition + shakeOffset;
            });
    }
    
    /// <summary>
    /// プレイヤーのガシャ玉を非表示にする
    /// </summary>
    private void HidePlayerGasha()
    {
        _player.GetComponent<MeshRenderer>().enabled = false;
        _player.GetComponent<Collider>().enabled = false;
        _player.GetComponent<Rigidbody>().isKinematic = true;
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