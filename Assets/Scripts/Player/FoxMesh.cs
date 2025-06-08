using UnityEngine;

public class FoxMesh : MonoBehaviour
{
	[Tooltip("追従するプレイヤーの球体オブジェクト")]
	[SerializeField] private GameObject playerSphere;
	[SerializeField] private float offsetY = 50f;
	[SerializeField] private float offsetAngle = 0f;
    private void Update()
    {
        var p = playerSphere.transform.position;
		p.y += offsetY;
		this.transform.position = p;
	    
        
        // 向く方向を変える
        var angle = Camera.main.transform.eulerAngles.y;
        this.transform.rotation = Quaternion.Euler(0, angle + offsetAngle, 0);
    }
}
