using UnityEngine;
using R3;

namespace Izumi.Prototype
{
    public class RevealableObject : MonoBehaviour
    {
        [SerializeField, Range(0, 4)] private int revealSpeed = 0;

        private void OnChangePlayerSpeed(int s)
        {
            this.gameObject.SetActive(s >= revealSpeed);
        }
        
        private void Start()
        {
            GameManager.Instance.Player.PlayerSpeedInt.Subscribe(OnChangePlayerSpeed);
        }
    }
}
