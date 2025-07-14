using UnityEngine;
using Cysharp.Threading.Tasks;
using LitMotion;

/// <summary>
/// ゲーム開始時の演出シーケンスを管理するクラス
/// </summary>
public class GameStartSequence
{
    // 演出定数
    private const float INTRO_SEQUENCE_DURATION = 3.5f;      // カメラ演出の継続時間
    private const float FRONT_VIEW_DISTANCE = 6f;         // プレイヤー正面からの距離
    private const float FRONT_VIEW_HEIGHT_OFFSET = 1.25f;    // プレイヤー正面からの高さオフセット
    private const Ease INTRO_CAMERA_EASE = Ease.InOutQuart; // 演出のイージング

    private readonly Player _player;
    private readonly PlayerCamera _playerCamera;
    private readonly Canvas _uiCanvas;

    public GameStartSequence(Player player, PlayerCamera playerCamera, Canvas uiCanvas)
    {
        // 引数から参照を設定
        this._player = player;
        this._playerCamera = playerCamera;
        this._uiCanvas = uiCanvas;
    }
    
    /// <summary>
    /// ゲーム開始演出を実行する
    /// </summary>
    public async UniTask StartSequenceAsync()
    {
        SetupInitialCameraPosition();
        
        await IrisShot.StartIrisIn(_uiCanvas);
        await StartCameraIntroAsync();
        await UniTask.Delay(200);
        await StartUISlideAnimationsAsync();
        await UniTask.Delay(500);
    }
    
    /// <summary>
    /// カメラの演出部分を実行
    /// プレイヤーの正面から見るカットから通常のTPS視点に移行
    /// </summary>
    private async UniTask StartCameraIntroAsync()
    {
        // 演出開始時のプレイヤーの位置を取得
        var playerPosition = _player.transform.position;
        
        // 通常のTPSカメラの最終位置と回転を計算
        var finalCameraRotation = Quaternion.Euler(_playerCamera.GetCurrentPitch(), _playerCamera.GetCurrentYaw(), 0f);
        var finalPivot = playerPosition + Vector3.up * _playerCamera.height;
        var finalCameraPosition = finalPivot - finalCameraRotation * Vector3.forward * _playerCamera.distance;
        
        var startPosition = _playerCamera.transform.position;
        var startRotation = _playerCamera.transform.rotation;
        
        // 位置と回転のアニメーションを同時実行
        var positionTask = LMotion.Create(startPosition, finalCameraPosition, INTRO_SEQUENCE_DURATION)
            .WithEase(INTRO_CAMERA_EASE)
            .Bind(pos => _playerCamera.transform.position = pos)
            .ToUniTask();
            
        var rotationTask = LMotion.Create(startRotation, finalCameraRotation, INTRO_SEQUENCE_DURATION)
            .WithEase(INTRO_CAMERA_EASE)
            .Bind(rot => _playerCamera.transform.rotation = rot)
            .ToUniTask();
        
        // 両方のアニメーションが完了するまで待機
        await UniTask.WhenAll(positionTask, rotationTask);
        
        // カメラの通常更新を再開
        _playerCamera.SetIntroMode(false);
    }
    
    /// <summary>
    /// カメラを初期位置（プレイヤーの正面）に瞬間移動
    /// </summary>
    private void SetupInitialCameraPosition()
    {
        // 演出開始時のプレイヤーの位置と回転を取得
        var playerPosition = _player.transform.position;
        var playerForward = _player.transform.forward;
        
        // 正面カメラの位置と回転を計算
        var frontCameraPosition = playerPosition + playerForward * FRONT_VIEW_DISTANCE + Vector3.up * FRONT_VIEW_HEIGHT_OFFSET;
        var frontCameraRotation = Quaternion.LookRotation(-playerForward);
        
        // カメラを即座に移動
        _playerCamera.transform.position = frontCameraPosition;
        _playerCamera.transform.rotation = frontCameraRotation;
        
        // カメラの通常更新を停止
        _playerCamera.SetIntroMode(true);
    }
    
    /// <summary>
    /// UIスライドアニメーションを実行する
    /// </summary>
    private async UniTask StartUISlideAnimationsAsync()
    {
        // uiCanvas内の全てのUISlideAnimationコンポーネントを取得
        var slideAnimations = _uiCanvas.GetComponentsInChildren<UISlideAnimation>(true);
        
        if (slideAnimations.Length == 0)
        {
            // スライドアニメーションが無い場合は即座に完了
            return;
        }
        
        // 全てのスライドアニメーションを並行実行
        var slideTasks = new UniTask[slideAnimations.Length];
        for (int i = 0; i < slideAnimations.Length; i++)
        {
            slideTasks[i] = slideAnimations[i].StartSlideAnimationAsync();
        }
        
        // 全てのスライドアニメーションが完了するまで待機
        await UniTask.WhenAll(slideTasks);
    }
}