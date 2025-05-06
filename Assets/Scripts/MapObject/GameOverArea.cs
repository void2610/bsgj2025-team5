using UnityEngine;

namespace Uchiyama.Prototype
{
    public class GameOverArea : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Izumi.Prototype.Player>(out _))
            {
                Debug.Log("Entered!!");
                Izumi.Prototype.GameManager.Instance.GameOver();
            }
        }
    }
}
