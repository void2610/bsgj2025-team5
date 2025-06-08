using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [Tooltip("壊すために必要な最低速度レベル（0:停止〜4:最高速）")]
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