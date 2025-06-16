using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;

        // カメラの方向にオブジェクトを向ける
        transform.LookAt(Camera.main.transform);
    }
}