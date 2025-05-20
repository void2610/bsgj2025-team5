/// <summary>
/// 敵が侵入できないエリア（オブジェクト）を付与する
/// </summary>
/// 
/// 進捗
/// 05/20:作成
/// 
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Coliderに対して侵入禁止を設定できる
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyBlockObject : MonoBehaviour
{
    private NavMeshObstacle obstacle;   // NavMeshObstacleを使って侵入禁止を設定する
    void Start()
    {

        obstacle = gameObject.GetComponent<NavMeshObstacle>();
        // NavMeshObstacleがなければ追加する
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }

        // 細かい設定をする

        obstacle.carving = true;
        obstacle.carvingMoveThreshold = 0.5f;
        obstacle.carvingTimeToStationary = 2f;

        obstacle.shape = NavMeshObstacleShape.Box;              // Boxに対して適用する
        obstacle.size = GetComponent<Collider>().bounds.size * 0.1f;   // サイズ設定
    }
}
