using UnityEngine;

public class EnemyDirectionIndicator : MonoBehaviour
{
    [SerializeField] private Transform enemy;            // 敵の Transform
    [SerializeField] private Transform player;           // プレイヤーの Transform
    [SerializeField] private Camera mainCamera;          // プレイヤーカメラ
    [SerializeField] private RectTransform canvasRect;   // UI Canvas の RectTransform
    [SerializeField] private float yawOffset = 180f;     // アイコンの向き補正
    [SerializeField] private float screenEdgeBuffer = 50f;

    [Header("Indicator Size by Distance")]
    [SerializeField] private float minScale = 0.5f;      // 最も遠いときのサイズ
    [SerializeField] private float maxScale = 1.5f;      // 最も近いときのサイズ
    [SerializeField] private float minDistance = 5f;     // 最も近い距離（ここより近いと最大サイズ）
    [SerializeField] private float maxDistance = 30f;    // 最も遠い距離（ここより遠いと最小サイズ）

    private RectTransform _indicator;

    private void Awake()
    {
        _indicator = this.GetComponent<RectTransform>();
    }
    
    private void Update()
    {
        var screenPos = mainCamera.WorldToScreenPoint(enemy.position);

        // Z座標が負の場合、カメラの反対側にいるので、スクリーン座標を反転
        if (screenPos.z < 0) screenPos *= -1;

        // 画面内かどうか判定
        var isOnScreen = screenPos.x >= 0 && screenPos.x <= Screen.width &&
                         screenPos.y >= 0 && screenPos.y <= Screen.height;

        if (isOnScreen)
        {
            // 画面内にいる場合は非表示
            _indicator.anchoredPosition = new Vector2(9999, 9999); 
        }
        else
        {
            var center = new Vector2(Screen.width, Screen.height) / 2;
            var dir = ((Vector2)screenPos - center).normalized;

            var halfW = canvasRect.sizeDelta.x / 2 - screenEdgeBuffer;
            var halfH = canvasRect.sizeDelta.y / 2 - screenEdgeBuffer;

            var edgePos = new Vector2(
                Mathf.Clamp(dir.x * halfW, -halfW, halfW),
                Mathf.Clamp(dir.y * halfH, -halfH, halfH)
            );

            _indicator.anchoredPosition = edgePos;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _indicator.rotation = Quaternion.Euler(0, 0, angle - yawOffset);

            // 距離に応じてスケーリング
            var distance = Vector3.Distance(player.position, enemy.position);
            var t = Mathf.InverseLerp(maxDistance, minDistance, distance); // 遠いほど0、近いほど1
            var scale = Mathf.Lerp(minScale, maxScale, t);
            _indicator.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
