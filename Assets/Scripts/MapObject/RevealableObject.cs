using R3;
using UnityEngine;

public class RevealableObject : MonoBehaviour
{
    [Tooltip("オブジェクトが表示される最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed = 0;
    
    [Tooltip("ONの場合、指定速度以下で表示、OFFの場合、指定速度以上で表示")]
    [SerializeField] private bool invertBehavior = false;
    
    [Tooltip("パーティクルのプレハブ（オプション）")]
    [SerializeField] private GameObject particlePrefab;

    private void OnChangePlayerSpeed(int s)
    {
        bool shouldBeActive = invertBehavior ? s <= requiredSpeed : s >= requiredSpeed;
        this.gameObject.SetActive(shouldBeActive);
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
    }
        
    private void Start()
    {
        GameManager.Instance.Player.PlayerSpeedInt.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }
}