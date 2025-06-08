/// <summary>
/// 敵AIの管理をするクラス
/// 状態遷移によって管理して
/// アイテム数のReactivePropertyをSubscribeして
/// 状態更新する
/// </summary>
/// 
/// 開発進捗
/// 05/18:作成、状態を3つで、それぞれの状態についての行動を簡単に記述
/// 05/19:Enemy.csを使わずに単体で敵が状態遷移に応じた行動をするようにした
///       探索状態を削除して２つの状態に変更した
///       モードを四つにして、アイテム取得数に応じてスピードや行動を変更するようにした
/// 05/20:以下の問題点を修正
///      ・privateな変数名をアンダーバーから始めるように
///      ・コメントの位置を統一
///      ・メソッドにアクセス修飾子（private）をつける
///      ・NavMeshAgentをAwakeで作成する
///      ・巡回場所をAwakeで初期化させる
///      ・速度をインスペクトから設定するように
/// 05/23:Agentを取得する前にBakeする必要があるため、Start()で取得するようにした
/// 06/01:新たにコメントを追加
/// 06/07:落下状態を常に維持するようにRigidBody設定を変更した
/// 06/08: 床の非アクティブ状態をチェックし、床が非アクティブになったら、NavMeshAgentを無効化し、物理演算で落下させる処理を追加
///        落下状態(Fall)を追加
///        現在の床のレイヤーから落下するかどうかを判定する処理を追加
///        UniTaskによって落下から一定秒を計測し、ランダムなリスポーン地点からリスポーンするように処理を追加した

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using R3;
using System.Threading;
using Cysharp.Threading.Tasks;
using Random = UnityEngine.Random;

/// <summary>
/// 状態を巡回（Patrol）、追跡（Chase）、落下（Fall）の2つを遷移させる
/// </summary>
public enum EnemyState { Patrol, Chase, Fall }


/// <summary>
/// 敵AIの状態ごとのアルゴリズムを記述
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    // プレイヤーをバインド
    [SerializeField] private Transform player;
    // 巡回する場所
    [SerializeField] public Transform[] patrolPoints;
    // 現在の状態（Patrol,Chase）
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;
    // プレイヤーを視認する距離
    [SerializeField] public float sightRange = 10f;
    // ベースの速度
    [SerializeField] private float baseSpeed = 1f;
    // アイテム数に応じた速度設定
    [SerializeField] private List<float> speeds = new List<float> { 1f, 1.5f, 2f, 2.5f, 3f };
    // 攻撃範囲
    [SerializeField] private float attackRange = 15f;
    // NavMeshAgentから敵AIを使わせてもらう
    private NavMeshAgent _agent;
    // Rigidbodyも追加
    private Rigidbody _rb; 
    // カメラのセット
    private Camera _camera;
    // パトロールスポットの巡回
    private int _patrolIndex = 0;
    // プレイヤーのアイテムカウント
    private int _itemCount = 0;
    //　追跡タイム計算用変数
    private float _chaseTimer = 0f;
    // 強制追跡時間
    private readonly float _forcedChaseDuration = 5f;
    // 強制追跡状態かどうか
    private bool _isForcedChase = false;

    // 複数の再スポーン地点の配列に変更
    [SerializeField] private Transform[] respawnPoints;
    // 落下から再スポーンまでの時間
    [SerializeField] private float fallRespawnDelay = 3f;
    // UniTaskをキャンセルするためのトークン
    private CancellationTokenSource _fallCancellationTokenSource;
    // 選択したリスポーン地点からのNavMesh検索範囲
    [SerializeField] private float respawnSearchRadius = 2f;
    // 常にアクティブな床のレイヤー
    [SerializeField] private LayerMask FloorLayer; 
    [SerializeField] private LayerMask disappearFloorLayer; // 消える床のレイヤー


    private void Awake()
    {
        // 巡回場所がセットされてない場合
        if (patrolPoints.Length == 0) throw new ArgumentNullException(nameof(patrolPoints), "巡回場所がセットされていません!");

        _camera = Camera.main;

        // NavMeshAgentの取得
        if (!TryGetComponent(out _agent))
        {
            Debug.Log("NavMeshAgentを作成");
            _agent = this.gameObject.AddComponent<NavMeshAgent>();
        }

        // Rigidbodyの取得と初期設定
        if (!TryGetComponent(out _rb))
        {
            Debug.Log("Rigidbodyを作成");
            _rb = this.gameObject.AddComponent<Rigidbody>();
        }
        _rb.useGravity = true;  // 重力を有効にする
        _rb.isKinematic = true; // 最初はNavMeshAgentが制御するためKinematicにする
    }

    private void Start()
    {
        // エージェントの初期化
        InitializeAgentState();

        // アイテム取得数後の処理をSubscribe
        GameManager.Instance.ItemCount.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }

    /// <summary>
    /// GameObjectが破壊される時にUniTaskをキャンセルする
    /// </summary>
    private void OnDestroy()
    {
        // GameObjectが破棄されるときにUniTaskをキャンセルする
        _fallCancellationTokenSource?.Cancel();
        _fallCancellationTokenSource?.Dispose();
    }

    /// <summary>
    /// カメラの調整をしつつ、状態応じてそれぞれの処理へ遷移
    /// </summary>
    private void Update()
    {
        // カメラ方向にビルボードを向ける (これは常に実行して良い)
        var lookPos = player.position - _camera.transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.LookRotation(lookPos);

        // 強制追跡タイマーのリセット
        if (_isForcedChase)
        {
            _chaseTimer -= Time.deltaTime; // Time.fixedDeltaTime から Time.deltaTime に変更
            if (_chaseTimer <= 0f)
            {
                _isForcedChase = false;
            }
        }

        // ここから状態遷移のロジック
        if (currentState != EnemyState.Fall) // Fall状態でない場合のみ、床の状態をチェック
        {
            // NavMeshAgentが有効な場合のみ、NavMesh上と足元の床のアクティブ状態を確認
            if (_agent.enabled)
            {
                bool shouldFall = false;
                // NavMeshAgentがNavMeshから離れた（落下した）場合
                if (!_agent.isOnNavMesh)
                {
                    Debug.Log("NavMeshから離れました。Fall状態に切り替えます。");
                    shouldFall = true;
                }
                else
                {
                    // 足元の床の状態をRaycastでチェック
                    RaycastHit hit;
                    // 両方の床レイヤーを対象にするレイヤーマスクを作成
                    int combinedFloorLayers = FloorLayer.value | disappearFloorLayer.value;

                    // Rayの開始位置と長さは、キャラクターの足元と床の距離に合わせて調整
                    // isTriggerのColliderも検出するためQueryTriggerInteraction.Collideを指定
                    if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 20.0f, combinedFloorLayers, QueryTriggerInteraction.Collide))
                    {
                        // Raycastが何らかの床のColliderをヒットした
                        if (((1 << hit.collider.gameObject.layer) & disappearFloorLayer) != 0)
                        {
                            // ヒットしたのが「消える床」だった場合
                            if (hit.collider.isTrigger)
                            {
                                Debug.Log("足元の消える床がisTriggerになりました（通過可能）。Fall状態に切り替えます。");
                                shouldFall = true;
                            }
                            // else: 消える床だがisTriggerではない（表示状態）なら落下しない
                        }
                        else if (((1 << hit.collider.gameObject.layer) & FloorLayer) != 0)
                        {
                            // ヒットしたのが「常にアクティブな床」だった場合
                            // この場合、isTriggerは通常falseなので、落下しない
                            Debug.Log("足元に常にアクティブな床があります。");
                            shouldFall = false; // 明示的に落下しないことを示す
                        }
                        else
                        {
                            // 想定外のレイヤーの床にヒットした場合 (念のため)
                            Debug.LogWarning("足元に想定外のレイヤーの床にヒットしました。");
                            // この場合は落下しない、または別の判定ロジックを入れる
                            shouldFall = false;
                        }
                    }
                    else
                        // Raycastが何もヒットしなかった場合も落下とみなす
                    {
                        Debug.Log("足元に何もヒットしませんでした。Fall状態に切り替えます。");
                        shouldFall = true;
                    }
                }


                if (shouldFall)
                {
                    EnterFallState(); // Fall状態に遷移
                }
            }
            else // NavMeshAgentが使えない場合もFall状態に
            {
                if (currentState != EnemyState.Fall)
                {
                    Debug.Log("NavMeshAgentが無効になりました。Fall状態に切り替えます。");
                    EnterFallState(); // Fall状態遷移
                }
            }
        }
        else // currentState が Fall の場合
        {
            // Fall状態では物理演算が有効になっていることを確認
            if (_rb.isKinematic)
            {
                _rb.isKinematic = false;
            }
            // Fall状態での特殊処理があればここに記述
        }

        // 状態に応じた処理
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                Chase();
                break;
        }
    }

    /// <summary>
    /// アイテム数が0~2の場合のみ巡回をする
    /// プレイヤーが視界内に入ると追跡状態に遷移
    /// </summary>
    private void Patrol()
    {
        // Debug.Log("Patrolling" + _patrolIndex); //デバッグ用

        if (_itemCount >= 3)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // 仮
        var distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange)
        {
            GameManager.Instance.GameOver();
        }

        // ある程度目的地に着いた場合、到着したと判断して次のポイントへ移動させる
        if (!_agent.pathPending && _agent.remainingDistance < 0.001f && _agent.remainingDistance != float.PositiveInfinity) // 追加: remainingDistanceが無限大でないことを確認
            SetNextPatrolPoint();

        if (CanSeePlayer())
            currentState = EnemyState.Chase;
    }

    /// <summary>
    /// 強制追跡状態の場合、追跡状態の場合にプレイヤーを追跡する
    /// アイテム数が3以上から常に追跡状態
    /// </summary>
    private void Chase()
    {
        // Debug.Log("Chasing"); //デバッグ用

        if (_itemCount <= 2 && !CanSeePlayer() && !_isForcedChase)
        {
            // 探索状態に遷移させる
            currentState = EnemyState.Patrol;
            return;
        }

        // プレイヤーの位置を目的地にして追跡する
        _agent.destination = player.transform.position;

        // ターゲットとの距離を計算してゲームオーバー判定をする
        var distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange)
        {
            GameManager.Instance.GameOver();
        }
    }

    /// <summary>
    /// 敵が落下状態に入った際の初期処理
    /// </summary>
    private void EnterFallState()
    {
        if (currentState == EnemyState.Fall) return; // 既にFall状態なら何もしない

        currentState = EnemyState.Fall;
        _agent.enabled = false;
        _rb.isKinematic = false; // Rigidbodyを物理演算の影響下に戻す

        // 以前のUniTaskが存在すればキャンセルする
        _fallCancellationTokenSource?.Cancel();
        _fallCancellationTokenSource?.Dispose();
        _fallCancellationTokenSource = new CancellationTokenSource();

        // UniTaskによる落下後の待機と再スポーン処理を開始
        FallAndRespawnAsync(_fallCancellationTokenSource.Token).Forget();
    }

    /// <summary>
    /// UniTaskを使用した落下待機と再スポーン処理
    /// </summary>
    private async UniTask FallAndRespawnAsync(CancellationToken token)
    {
        try
        {
            Debug.Log($"Falling... Respawn in: {fallRespawnDelay:F2} seconds");
            await UniTask.Delay(TimeSpan.FromSeconds(fallRespawnDelay), ignoreTimeScale: false, cancellationToken: token);

            // キャンセルされていないか確認
            token.ThrowIfCancellationRequested();

            Respawn(); // 再スポーン処理
        }
        catch (OperationCanceledException)
        {
            Debug.Log("FallAndRespawnAsync was cancelled.");
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred during FallAndRespawnAsync: {e.Message}");
        }
    }

    /// <summary>
    /// 巡回場所を順にセットしていく
    /// </summary>
    private void SetNextPatrolPoint()
    {
        // 巡回する場所を更新
        _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
        // 巡回する順序を進める
        _agent.destination = patrolPoints[_patrolIndex].position;
    }

    /// <summary>
    /// プレイヤーが視界内に入ったかどうかを判定する
    /// </summary>
    /// <returns></returns>
    private bool CanSeePlayer()
    {
        // 距離が視認距離以内ならTrue
        return Vector3.Distance(transform.position, player.position) <= sightRange;
    }

    /// <summary>
    /// 速度を変更する
    /// </summary>
    /// <param name="v">変更後の速度</param>
    private void OnChangePlayerSpeed(int v)
    {
        // アイテム取得後は速度を変更する
        _itemCount = v;

        if (speeds == null || _itemCount < 0)
        {
            Debug.LogWarning($"速度設定に失敗：_speed[{_itemCount}] は無効です");
            return;
        }

        // indexが設定した数より大きいと、最大値で固定にする
        var newSpeed = _itemCount >= speeds.Count ? speeds[^1] : speeds[_itemCount];
        _agent.speed = baseSpeed * newSpeed;

        // Debug.Log($"アイテムを{_itemCount}個ゲット!"); // デバッグ用

         //　強制追跡状態にする
        _isForcedChase = true;
        // 時間制限のリセット
        _chaseTimer = _forcedChaseDuration;
    }

    /// <summary>
    /// 敵を再スポーンさせ、状態を初期化する
    /// </summary>
    private void Respawn()
    {
        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogError("Respawn Pointsが設定されていません！再スポーンできません。");
            // 最悪の場合、ゲームオーバーにするか、現在の位置で復帰させるかなど、フォールバック処理を記述
            InitializeAgentState(); // 状態だけは初期化しておく
            _agent.enabled = true;
            _rb.isKinematic = true;
            return;
        }

        Debug.Log("Respawning Enemy...");

        // ランダムにリスポーン地点を選択
        int randomIndex = Random.Range(0, respawnPoints.Length);
        Vector3 chosenRespawnPoint = respawnPoints[randomIndex].position;


        NavMeshHit hit;
        Vector3 finalSpawnPosition = chosenRespawnPoint;

        // 選択したリスポーン地点からNavMesh上で最も近い有効な位置を探す
        if (NavMesh.SamplePosition(chosenRespawnPoint, out hit, respawnSearchRadius, NavMesh.AllAreas))
        {
            finalSpawnPosition = hit.position; // 有効なNavMesh上の位置を取得
            Debug.Log($"Found NavMesh spawn position from chosen point: {finalSpawnPosition}");
        }
        else
        {
            // NavMesh.SamplePositionに失敗しても、選んだポイントにそのままスポーンさせる
            Debug.LogWarning($"Could not find valid NavMesh position near chosen respawn point ({chosenRespawnPoint}) within {respawnSearchRadius}. Spawning at chosen point directly (may be off-NavMesh).");
        }

        // 位置を最終的な再スポーン地点に設定
        transform.position = finalSpawnPosition;
        // 速度をリセット（落下中の慣性をなくす）
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // NavMeshAgentを再有効化し、RigidbodyをKinematicに戻す
        _agent.enabled = true;
        _rb.isKinematic = true;

        // 状態を初期化（Patrolに）
        InitializeAgentState(); 
    }

    /// <summary>
    /// エージェントの状態を初期化する共通処理
    /// </summary>
    private void InitializeAgentState()
    {
        currentState = EnemyState.Patrol;
        _patrolIndex = 0; // 巡回開始地点をリセット
        _agent.destination = patrolPoints[_patrolIndex].position;
        _isForcedChase = false; // 強制追跡状態もリセット
        _chaseTimer = 0f; // チェイスタイマーもリセット
    }
}