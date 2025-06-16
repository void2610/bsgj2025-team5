using UnityEngine;

public class BillboardYAxis : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;

        Vector3 targetPos = Camera.main.transform.position;
        targetPos.y = transform.position.y; // YŽ²‚Ì‰ñ“]‚¾‚¯‚ð‹–‰Â
        transform.LookAt(targetPos);
    }
}