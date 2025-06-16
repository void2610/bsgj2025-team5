using UnityEngine;

public class ScalePulse : MonoBehaviour
{
    public float scaleAmount = 0.2f;  // �ǂꂾ���傫���Ȃ邩
    public float speed = 1f;          // 1�b�����ŕω�������

    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        float scaleOffset = Mathf.Sin(Time.time * Mathf.PI * 2 * speed) * scaleAmount;
        transform.localScale = initialScale + Vector3.one * scaleOffset;
    }
}
