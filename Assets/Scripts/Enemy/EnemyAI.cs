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
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Transform player;

    // 巡回する場所
    [SerializeField] public Transform[] patrolPoints;

    // 現在の状態（Patrol,Chase）
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;
    
    // プレイヤーを視認する距離
    [SerializeField] public float sightRange = 10f;

    // アイテム数に応じた速度設定
    [SerializeField] private float[] speed;

    // 攻撃範囲
    [SerializeField] private float attackRange = 1f;

    // NavMeshAgentから敵AIを使わせてもらう
    private NavMeshAgent _agent;

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


    /// <summary>
    /// 初期化時に一度NavMeshAgentを取得（なければ作成する）
    /// </summary>
    private void Awake()
    {
        // エージェントの初期化
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.Log("NavMeshAgentを作成");
            _agent = gameObject.AddComponent<NavMeshAgent>();
        }
        // ステートの初期化
        currentState = EnemyState.Patrol;
    }

    /// <summary>
    /// アイテム取得後の処理を読み込む
    /// </summary>
    private void Start()
    {
        // 巡回場所の初期化
        SetInitialPatrolDestination();
        
        // アイテム取得数後の処理
        GameManager.Instance.ItemCount.Subscribe(newValue =>
        {
            // アイテム取得後は速度を変更する
            _itemCount = newValue;
            SetSpeed(_itemCount);

            Debug.Log("アイテム"+ _itemCount + "個ゲットたぜ！");

            //　強制追跡状態にする
            _isForcedChase = true;
            // 時間制限のリセット
            _chaseTimer = _forcedChaseDuration;
        });
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
    /// <param name="item">アイテム取得数</param>
    private void Patrol()
    {
        Debug.Log("Patrolling" + _patrolIndex); //デバッグ用

        if (_itemCount >= 3)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // ある程度目的地に着いた場合、到着したと判断して次のポイントへ移動させる
        if (!_agent.pathPending && _agent.remainingDistance < 0.001f)
            GoToNextPatrolPoint();

        if (CanSeePlayer())
            currentState = EnemyState.Chase;
    }

    /// <summary>
    /// 強制追跡状態の場合、追跡状態の場合にプレイヤーを追跡する
    /// アイテム数が3以上から常に追跡状態
    /// </summary>
    /// <param name="item">アイテム取得数</param>
    private void Chase()
    {
        Debug.Log("Chasing"); //デバッグ用

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
    /// 巡回場所の初期化
    /// </summary>
    private void SetInitialPatrolDestination()
    {
        // 巡回場所がセットされてない場合
        if (patrolPoints.Length == 0)
        {
            Debug.LogError("巡回場所がセットされていません!");
            return;
        }

        //巡回場所を初期化
        _agent.destination = patrolPoints[_patrolIndex].position;
    }

    /// <summary>
    /// 巡回場所を順にセットしていく
    /// </summary>
    private void GoToNextPatrolPoint()
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
    /// 取得アイテム数に応じた速度の変更
    /// </summary>
    /// <param name="item">アイテム取得数</param>
    private void SetSpeed(int item)
    {
        if (speed == null || item < 0 || item >= speed.Length)
        {
            Debug.LogWarning($"速度設定に失敗：_speed[{item}] は無効です");
            return;
        }

        _agent.speed = speed[item];
    }
}
