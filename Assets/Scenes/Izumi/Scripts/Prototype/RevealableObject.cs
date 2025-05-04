using UnityEngine;
using R3;

namespace Izumi.Prototype
{
    public class RevealableObject : MonoBehaviour
    {
        [SerializeField, Range(0, 4)] private int requiredSpeed = 0;

        private void OnChangePlayerSpeed(int s)
        {
            this.gameObject.SetActive(s >= requiredSpeed);
        }
        
        private void Start()
        {
            GameManager.Instance.Player.PlayerSpeedInt.Subscribe(OnChangePlayerSpeed).AddTo(this);
        }
    }
}
