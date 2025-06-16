/// <sammary>
/// ゲームオーバー判定をするクラス
/// </sammary>
/// 作業進捗
/// 05/06:作成
/// 06/07:敵も判定するように変更

using UnityEngine;

public class GameOverArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            Debug.Log("Entered!!");
            // GameManager.Instance.GameOver();
            GameManager.Instance.Fall();
        }
        else if (other.TryGetComponent<Enemy>(out _))
        {
            Debug.Log("Enemy Falled!!");
        }
    }
}