using UnityEngine;
using R3;

public class ItemDirectionIndicator : MonoBehaviour
{
    [Header("必須参照")]
    [SerializeField] private Transform player;           // プレイヤーの Transform
    [SerializeField] private Camera mainCamera;          // プレイヤーカメラ
    [SerializeField] private RectTransform canvasRect;   // UI Canvas の RectTransform
    [SerializeField] private float yawOffset = 0f;       // アイコンの向き補正
    
    [Header("Indicator Display Range")]
    [Tooltip("中央からの最大距離（ピクセル）")]
    [SerializeField] private float indicatorRadius = 300f;

    [Header("Indicator Size by Distance")]
    [SerializeField] private float minScale = 0.8f;      // 最も遠いときのサイズ
    [SerializeField] private float maxScale = 1.5f;      // 最も近いときのサイズ
    [SerializeField] private float minDistance = 5f;     // 最も近い距離（ここより近いと最大サイズ）
    [SerializeField] private float maxDistance = 50f;    // 最も遠い距離（ここより遠いと最小サイズ）
    
    [Header("アニメーション設定")]
    [Tooltip("アイコンの脈動スピード")]
    [SerializeField] private float pulseSpeed = 2f;
    [Tooltip("アイコンの脈動の大きさ")]
    [SerializeField] private float pulseAmount = 0.1f;

    private RectTransform _indicator;
    private Transform _currentTarget;
    private float _baseScale = 1f;

    private void Awake()
    {
        _indicator = this.GetComponent<RectTransform>();
    }

    private void Start()
    {
        // アイテム数が変更されたときに次のターゲットを更新
        GameManager.Instance.ItemCount
            .Subscribe(_ => UpdateTargetItem())
            .AddTo(this);
        
        // 初期ターゲットを設定
        UpdateTargetItem();
    }
    
    private void UpdateTargetItem()
    {
        _currentTarget = GameManager.Instance.GetCurrentTargetItem();
        
        // ターゲットがない場合（全アイテム取得済み）は非表示
        if (_currentTarget == null)
        {
            _indicator.anchoredPosition = new Vector2(9999, 9999);
        }
    }
    
    private void Update()
    {
        if (_currentTarget == null) return;
        
        var screenPos = mainCamera.WorldToScreenPoint(_currentTarget.position);

        // Z座標が負の場合、カメラの反対側にいるので、スクリーン座標を反転
        if (screenPos.z < 0) screenPos *= -1;

        // 画面内かどうか判定
        var isOnScreen = screenPos.x >= 0 && screenPos.x <= Screen.width &&
                         screenPos.y >= 0 && screenPos.y <= Screen.height &&
                         screenPos.z > 0;

        if (isOnScreen)
        {
            // 画面内にいる場合は非表示
            _indicator.anchoredPosition = new Vector2(9999, 9999); 
        }
        else
        {
            var center = new Vector2(Screen.width, Screen.height) / 2;
            var dir = ((Vector2)screenPos - center).normalized;

            // 16:9のアスペクト比を考慮した楕円の境界上に配置
            var radiusX = indicatorRadius;
            var radiusY = indicatorRadius * (9f / 16f); // 16:9のアスペクト比
            
            // 楕円上の点を計算
            var ellipseAngle = Mathf.Atan2(dir.y * radiusX, dir.x * radiusY);
            var edgePos = new Vector2(
                radiusX * Mathf.Cos(ellipseAngle),
                radiusY * Mathf.Sin(ellipseAngle)
            );

            _indicator.anchoredPosition = edgePos;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _indicator.rotation = Quaternion.Euler(0, 0, angle - yawOffset);

            // 距離に応じてスケーリング
            var distance = Vector3.Distance(player.position, _currentTarget.position);
            var t = Mathf.InverseLerp(maxDistance, minDistance, distance); // 遠いほど0、近いほど1
            _baseScale = Mathf.Lerp(minScale, maxScale, t);
            
            // 脈動アニメーション
            var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            var finalScale = _baseScale * pulse;
            _indicator.localScale = new Vector3(finalScale, finalScale, 1f);
        }
    }
}