using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

public class TimeBonusItem : MonoBehaviour
{
    [Header("アイテム設定")]
    [Tooltip("取得時に増加する時間（秒）")]
    [SerializeField] private float timeBonus = 10f;
    
    [Tooltip("取得時のパーティクルエフェクト（オプション）")]
    [SerializeField] private GameObject particlePrefab;
    
    [Header("アニメーション設定")]
    [Tooltip("アイテムの回転速度")]
    [SerializeField] private float rotationSpeed = 90f;
    
    [Tooltip("アイテムの上下運動の高さ")]
    [SerializeField] private float floatHeight = 0.3f;
    
    [Tooltip("アイテムの上下運動の速度")]
    [SerializeField] private float floatSpeed = 2f;
    
    private Vector3 _startPosition;
    
    // BreakableObjectから呼び出される初期化メソッド
    public void Initialize()
    {
        _startPosition = this.transform.position;
        StartAnimations();
    }
    
    private void StartAnimations()
    {
        // 回転アニメーション（無限ループ）
        LMotion.Create(0f, 360f, 360f / rotationSpeed)
            .WithEase(Ease.Linear)
            .WithLoops(-1)
            .Bind(rotationY => transform.rotation = Quaternion.Euler(0, rotationY, 0))
            .AddTo(this);
        
        // 上下浮遊アニメーション（無限ループ）
        LMotion.Create(_startPosition.y, _startPosition.y + floatHeight, 1f / floatSpeed)
            .WithEase(Ease.InOutSine)
            .WithLoops(-1, LoopType.Yoyo)
            .BindToPositionY(this.transform)
            .AddTo(this);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            GameManager.Instance.IncreaseTime(timeBonus);
            
            if (particlePrefab)
            {
                Instantiate(particlePrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
}