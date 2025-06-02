using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FocusButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float requiredTime = 1f;
    [SerializeField] private Image fillImage;
    [SerializeField] private UnityEvent action;

    private bool _isPointerOver;
    private float _focusTime;
    private TextMeshProUGUI _text;
    
    public void SetText(string text) => _text.text = text;
    public void SetAction(UnityAction a) => action.AddListener(a.Invoke);
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        _focusTime = 0f;
    }

    private void Awake()
    {
        _text = this.GetComponentInChildren<TextMeshProUGUI>();
    }
    
    private void Update()
    {
        if (_isPointerOver)
        {
            _focusTime += Time.unscaledDeltaTime;

            if (_focusTime >= requiredTime)
            {
                action?.Invoke();
                _focusTime = 0f;
            }
        }
        
        // fillAmountの値をなめらかに補完して更新
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, _focusTime / requiredTime, Time.unscaledDeltaTime * 10f);
    }
}
