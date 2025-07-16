using R3;
using UnityEngine;

public class FoxMesh : MonoBehaviour
{
	[Header("基本設定")]
	[Tooltip("プレイヤーのアニメーター")]
	[SerializeField] private Animator playerAnimator;
	[Tooltip("プレイヤーカメラ")]
	[SerializeField] private PlayerCamera playerCamera;
	[Tooltip("追従するプレイヤーの球体オブジェクト")]
	[SerializeField] private GameObject playerSphere;
	[SerializeField] private float offsetY = 50f;
	[SerializeField] private float offsetAngle = 0f;
	
	[Header("目の表情設定")]
	[Tooltip("左目のSkinnedMeshRenderer")]
	[SerializeField] private SkinnedMeshRenderer leftEyeRenderer;
	[Tooltip("右目のSkinnedMeshRenderer")]
	[SerializeField] private SkinnedMeshRenderer rightEyeRenderer;
	
	private const float EXPRESSION_NORMAL = 0f;
	private const float EXPRESSION_CONFUSE = 0.25f;
	private const float EXPRESSION_AWAKENING = 0.5f;
	private static readonly int _speed = Animator.StringToHash("Speed");
	
	private Material _eyeMaterialInstance;

	private void UpdateMesh()
	{
		var p = playerSphere.transform.position;
		p.y += offsetY;
		this.transform.position = p;
	    
		// 向く方向を変える
		var angle = playerCamera.transform.eulerAngles.y;
		this.transform.rotation = Quaternion.Euler(0, angle + offsetAngle, 0);
	}
	
	private void OnPlayerSpeedChanged(float speed)
	{
		playerAnimator.SetFloat(_speed, speed);
	}
	
	/// <summary>
	/// アイテム数が変更された時の処理
	/// </summary>
	private void OnItemCountChanged(int itemCount)
	{
		if (!_eyeMaterialInstance) return;
	    
		// アイテム数に応じて表情を選択
		var targetOffsetX = itemCount switch
		{
			>= 4 => EXPRESSION_AWAKENING,
			>= 2 => EXPRESSION_CONFUSE,
			_ => EXPRESSION_NORMAL
		};

		// マテリアルのテクスチャオフセットを変更
		var currentOffset = _eyeMaterialInstance.mainTextureOffset;
		_eyeMaterialInstance.mainTextureOffset = new Vector2(targetOffsetX, currentOffset.y);
	}

	private void Awake()
	{
		UpdateMesh();
		
		// 目のマテリアルインスタンスを作成
		if (leftEyeRenderer && rightEyeRenderer && leftEyeRenderer.sharedMaterial)
		{
			_eyeMaterialInstance = new Material(leftEyeRenderer.sharedMaterial);
			leftEyeRenderer.material = _eyeMaterialInstance;
			rightEyeRenderer.material = _eyeMaterialInstance;
		}
	}

	private void Start()
	{
		GameManager.Instance.Player.PlayerSpeed
			.Subscribe(OnPlayerSpeedChanged)
			.AddTo(this);
			
		// アイテム数の変化を購読して目の表情を変更
		GameManager.Instance.ItemCount
			.Subscribe(OnItemCountChanged)
			.AddTo(this);
	}
	
    private void Update()
    {
	    if (playerCamera.IsIntroMode) return;
	    UpdateMesh();
    }

    private void OnDestroy()
    {
	    // マテリアルインスタンスの破棄
	    if (_eyeMaterialInstance) Destroy(_eyeMaterialInstance);
    }
}
