using UnityEngine;

public class TimeDestroy : MonoBehaviour
{
    [SerializeField]
    private float destroyTime = 10f;  // �C���X�y�N�^�[����ݒ�ł���
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, destroyTime);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
