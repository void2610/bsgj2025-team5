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
// 
//  



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
    [SerializeField] private GameManager gameManager;   // アイテム獲得数を取得するためにインスタンスを持っておく
    [SerializeField] private Transform player;          // プレイヤー
    [SerializeField] public Transform[] patrolPoints;   // 巡回する場所
    [SerializeField] public float sightRange = 10f;     // プレイヤーを視認する距離
    [SerializeField] private EnemySpeedData speedData;  // スピードデータ
    [SerializeField] private float attackRange = 1f;    // 攻撃範囲


    private NavMeshAgent agent;                             // NavMeshAgentから敵AIを使わせてもらう
    private int patrolIndex = 0;                            // パトロールスポットの巡回
    private int _itemCount = 0;                             // プレイヤーのアイテムカウント

    private float chaseTimer = 0f;  //追跡タイム計算用変数
    private readonly float forcedChaseDuration = 5f;        // 強制追跡状態になる時間
    private bool isForcedChase = false;                     // 強制追跡状態かどうか

    public EnemyState currentState = EnemyState.Patrol;     // 現在の状態（Patrol,Chase）

    /// <summary>
    /// 初期化時に一度NavMeshAgentを取得
    /// </summary>
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        GoToNextPatrolPoint();
        currentState = EnemyState.Patrol;

        // アイテム取得数後の処理
        gameManager.ItemCount.Subscribe(newValue =>
        {
            // アイテム取得後は速度を変更する
            _itemCount = newValue;
            SetSpeed(newValue);

            //　強制追跡状態にする
            isForcedChase = true;
            chaseTimer = forcedChaseDuration;
            currentState = EnemyState.Chase;
        });
    }

    /// <summary>
    /// 状態応じてそれぞれの処理へ遷移
    /// プレイヤーがアイテムを取得すると強制機に5秒間Chaseする
    /// </summary>
    void Update()
    {
        // 強制追跡タイマーのリセット
        if (isForcedChase)
        {
            chaseTimer -= Time.fixedDeltaTime;
            if (chaseTimer <= 0f)
            {
                isForcedChase = false;
            }
        }


        // 状態に応じた処理
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol(_itemCount);
                break;
            case EnemyState.Chase:
                Chase(_itemCount);
                break;
        }
    }

    /// <summary>
    /// アイテム数が0~2の場合のみ巡回をする
    /// プレイヤーが視界内に入ると追跡状態に遷移
    /// </summary>
    /// <param name="item"></param>
    void Patrol(int item)
    {
        Debug.Log("Patroling"); //デバッグ用

        if (item >= 3)
        {
            currentState = EnemyState.Chase;
            return;
        }

        /// <summary>
        /// ある程度目的地に着いた場合、到着したと判断して次のポイントへ移動させる
        /// </summary>
        if (!agent.pathPending && agent.remainingDistance < 0.001f)
            GoToNextPatrolPoint();

        if (CanSeePlayer())
            currentState = EnemyState.Chase;
    }

    /// <summary>
    /// 強制追跡状態の場合、追跡状態の場合にプレイヤーを追跡する
    /// アイテム数が3以上から常に追跡状態
    /// </summary>
    /// <param name="item"></param>
    void Chase(int item)
    {
        Debug.Log("Chasing"); //デバッグ用

        if (item <= 2 && !CanSeePlayer() && !isForcedChase)
        {
            currentState = EnemyState.Patrol;           // 探索状態に遷移
            return;
        }

        Vector3 position = player.transform.position;   // プレイヤーの位置を目的地に
        agent.destination = position;                   // プレイヤーを追跡


        /// <summary>
        /// ターゲットとの距離を計算してゲームオーバー判定をする
        /// </summary>
        var distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange)
        {
            GameManager.Instance.GameOver();
        }

    }

    /// <summary>
    /// 巡回場所を順にセットしていく
    /// </summary>
    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0)   // 巡回場所がセットされてない場合
        {
            Debug.LogError("巡回場所がセットされていません!");
            return;
        }

        agent.destination = patrolPoints[patrolIndex].position; // 巡回する場所をセット
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;  // 巡回する順序を進める
    }

    /// <summary>
    /// プレイヤーが視界内に入ったかどうかを判定する
    /// </summary>
    /// <returns></returns>
    bool CanSeePlayer()
    {
        return Vector3.Distance(transform.position, player.position) <= sightRange;  // 距離が視認距離以内ならTrue
    }

    /// <summary>
    /// 取得アイテム数に応じた速度の変更
    /// </summary>
    /// <param name="item"></param>
    void SetSpeed(int item)
    {
        agent.speed = speedData.GetSpeed(item);
    }
}
