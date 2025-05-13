using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float requiredTime = 1f;
    [SerializeField] private Image fillImage;
    [SerializeField] private UnityEvent action;

    private bool _isPointerOver;
    private float _focusTime;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        _focusTime = 0f;
    }
    
    private void Update()
    {
        if (_isPointerOver)
        {
            _focusTime += Time.deltaTime;

            if (_focusTime >= requiredTime)
            {
                action?.Invoke();
                _focusTime = 0f;
            }
        }
        
        // fillAmountの値をなめらかに補完して更新
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, _focusTime / requiredTime, Time.deltaTime * 10f);
    }
}
