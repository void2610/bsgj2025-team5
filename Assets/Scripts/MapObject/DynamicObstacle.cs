/// <sammary>
/// Enemyが障害物として認識するようにするスクリプト
/// </sammary>
/// 開発進捗
/// 06/07:作成

using R3;
using UnityEngine;

public class DynamicObstacle : MonoBehaviour
{
    // NavMeshObstacleを使う
    private UnityEngine.AI.NavMeshObstacle _navMeshObstacle;
    // 必要な速度
    [SerializeField, Range(0, 4)] private int requiredSpeed = 0;

    /*
    *   NavMeshObstacleがアタッチされているか確認する
    */
    void Awake()
    {
        _navMeshObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (_navMeshObstacle == null)
        {
            Debug.Log("NavMeshObstacleコンポーネントが見つかりません。床オブジェクトに追加してください。", this);
        }
    }

    /*
    *   初期状態を強制的に同期させる
    */
    void Start()
    {
        GameManager.Instance.Player.PlayerItemCountInt.Subscribe(ToggleObstacle).AddTo(this);
    }

    /* 
    *   NavMeshObstacleをアクティブ・非アクティブに切り替えるメソッド
    *   @param int s プレイヤーの速度
    */
    private void ToggleObstacle(int s)
    {
        if (_navMeshObstacle == null) return;

        // if (s >= requiredSpeed)
        if (s < requiredSpeed)
        {
            if (_navMeshObstacle.enabled)
            {
                _navMeshObstacle.enabled = false;
                Debug.Log("障害物を非アクティブ化し、NavMeshの切り抜きを解除しました。");
            }
        }
        else
        {
            if (!_navMeshObstacle.enabled)
            {
                _navMeshObstacle.enabled = true;
                Debug.Log("障害物をアクティブ化し、NavMeshの切り抜きを有効にしました。");
            }
        }
    }

}