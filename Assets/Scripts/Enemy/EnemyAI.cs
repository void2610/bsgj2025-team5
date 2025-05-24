/// <summary>
/// 敵AIの管理をするクラス
/// 状態遷移によって管理して
/// アイテム数のReactivePropertyをSubscribeして
/// 状態更新する
/// </summary>
// 
// 開発進捗
// 05/18:作成、状態を3つで、それぞれの状態についての行動を簡単に記述
// 05/19:Enemy.csを使わずに単体で敵が状態遷移に応じた行動をするようにした
//       探索状態を削除して２つの状態に変更した
//       モードを四つにして、アイテム取得数に応じてスピードや行動を変更するようにした
// 05/20:以下の問題点を修正
//      ・privateな変数名をアンダーバーから始めるように
//      ・コメントの位置を統一
//      ・メソッドにアクセス修飾子（private）をつける
//      ・NavMeshAgentをAwakeで作成する
//      ・巡回場所をAwakeで初期化させる
//      ・速度をインスペクトから設定するように
// 05/23:Agentを取得する前にBakeする必要があるため、Start()で取得するようにした


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using R3;

/// <summary>
/// 状態を巡回（Patrol）,追跡（Chase）の2つを遷移させる
/// </summary>
public enum EnemyState { Patrol, Chase }


/// <summary>
/// 敵AIの状態ごとのアルゴリズムを記述
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
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

            Debug.Log($"アイテムを{_itemCount}個ゲット!"); // デバッグ用

            //　強制追跡状態にする
            _isForcedChase = true;
            // 時間制限のリセット
            _chaseTimer = _forcedChaseDuration;
    }

    private void Awake()
    {
        // 巡回場所がセットされてない場合
        if (patrolPoints.Length == 0) throw new ArgumentNullException(nameof(patrolPoints), "巡回場所がセットされていません!");
        
        _camera = Camera.main;
        if (!TryGetComponent(out _agent))
        {
            Debug.Log("NavMeshAgentを作成");
            _agent = this.gameObject.AddComponent<NavMeshAgent>();
        }
    }

    private void Start()
    {
        // エージェントの初期化
        // ステートの初期化
        currentState = EnemyState.Patrol;
        //巡回場所を初期化
        _agent.destination = patrolPoints[_patrolIndex].position;

        // アイテム取得数後の処理
        GameManager.Instance.ItemCount.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }

    /// <summary>
    /// 状態応じてそれぞれの処理へ遷移
    /// </summary>
    private void Update()
    {
        // 強制追跡タイマーのリセット
        if (_isForcedChase)
        {
            _chaseTimer -= Time.fixedDeltaTime;
            if (_chaseTimer <= 0f)
            {
                _isForcedChase = false;
            }
        }
        
        // カメラ方向にビルボードを向ける
        var lookPos = player.position - _camera.transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.LookRotation(lookPos);

        // 状態に応じた処理
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                Chase();
                break;
            default:
                throw new ArgumentOutOfRangeException();
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
        if (!_agent.pathPending && _agent.remainingDistance < 0.001f)
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
}
