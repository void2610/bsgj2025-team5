using UnityEngine;

public class FoxMesh : MonoBehaviour
{
	[Tooltip("プレイヤーカメラ")]
	[SerializeField] private PlayerCamera playerCamera;
	[Tooltip("追従するプレイヤーの球体オブジェクト")]
	[SerializeField] private GameObject playerSphere;
	[SerializeField] private float offsetY = 50f;
	[SerializeField] private float offsetAngle = 0f;

	private void UpdateMesh()
	{
		var p = playerSphere.transform.position;
		p.y += offsetY;
		this.transform.position = p;
	    
		// 向く方向を変える
		var angle = playerCamera.transform.eulerAngles.y;
		this.transform.rotation = Quaternion.Euler(0, angle + offsetAngle, 0);
	}

	private void Awake()
	{
		UpdateMesh();
	}
	
    private void Update()
    {
	    if (playerCamera.IsIntroMode) return;
	    UpdateMesh();
    }
}
