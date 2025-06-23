using UnityEngine;

public class RotateYAxis : MonoBehaviour
{
    public float rotationSpeed = 90f; // 1•bŠÔ‚É90“x‰ñ“]

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
