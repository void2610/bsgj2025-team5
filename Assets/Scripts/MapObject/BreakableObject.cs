using System;
using UnityEngine;
using R3;

namespace Izumi.Prototype
{
    public class BreakableObject : MonoBehaviour
    {
        [SerializeField, Range(0, 4)] private int requiredSpeed = 0;

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.TryGetComponent(out Player player))
            {
                if (player.PlayerSpeedInt.CurrentValue >= requiredSpeed)
                {
                    Destroy(this.gameObject);
                }
            }
        }
    }
}
