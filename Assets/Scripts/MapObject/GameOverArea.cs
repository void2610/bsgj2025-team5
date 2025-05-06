using UnityEngine;

public class GameOverArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            Debug.Log("Entered!!");
            GameManager.Instance.GameOver();
        }
    }
}