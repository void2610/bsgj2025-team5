using UnityEngine;

public class RotateYAxis : MonoBehaviour
{
    public float rotationSpeed = 90f; // 1�b�Ԃ�90�x��]

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
