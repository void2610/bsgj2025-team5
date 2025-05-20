using UnityEngine;

public class FoxMesh : MonoBehaviour
{
	[SerializeField] private GameObject playerSphere;
	
    private void Update()
    {
        this.transform.position = playerSphere.transform.position;
        
        // 向く方向を変える
        var angle = Camera.main.transform.eulerAngles.y + 90f;
        this.transform.rotation = Quaternion.Euler(0, angle, 0);
    }
}
