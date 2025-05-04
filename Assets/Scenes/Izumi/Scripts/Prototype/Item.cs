using UnityEngine;

namespace Izumi.Prototype
{
    public class Item : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Player>(out _))
            {
                GameManager.Instance.AddItemCount();
                Destroy(gameObject);
            }
        }
    }
}
