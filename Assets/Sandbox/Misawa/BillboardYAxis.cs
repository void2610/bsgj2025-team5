using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;

        // �J�����̕����ɃI�u�W�F�N�g��������
        transform.LookAt(Camera.main.transform);
    }
}