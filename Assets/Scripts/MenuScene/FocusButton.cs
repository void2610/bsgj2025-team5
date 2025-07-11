using Coffee.UIEffectInternal;
using Coffee.UIEffects;
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
    [SerializeField] private SeData chargeSe;
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isPointerOver;
    private float _focusTime;
    private TextMeshProUGUI _text;
    private AudioSource _chargeAudioSource;
    private UIEffect _uiEffect;
    
    public void SetText(string text) => _text.text = text;
    public void SetAction(UnityAction a) => action.AddListener(a.Invoke);
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        
        // チャージSEを再生開始
        _chargeAudioSource = SeManager.Instance.PlaySeLoop(chargeSe);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        _focusTime = 0f;
        
        // チャージSEを停止
        if (_chargeAudioSource)
        {
            SeManager.Instance.StopSe(_chargeAudioSource);
            _chargeAudioSource = null;
        }
    }

    private void Awake()
    {
        _text = this.GetComponentInChildren<TextMeshProUGUI>();
        _uiEffect = this.transform.GetComponentInChildren<UIEffect>();
    }
    
    private void OnDisable()
    {
        // オブジェクトが無効化されたときにチャージSEを停止
        if (_chargeAudioSource)
        {
            SeManager.Instance.StopSe(_chargeAudioSource);
            _chargeAudioSource = null;
        }
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
                
                // アクション実行時にチャージSEを停止
                if (_chargeAudioSource)
                {
                    SeManager.Instance.StopSe(_chargeAudioSource);
                    _chargeAudioSource = null;
                }
            }
        }
        
        // カーブを使用して進行度を計算
        var normalizedTime = _focusTime / requiredTime;
        var curvedValue = progressCurve.Evaluate(normalizedTime);
        _uiEffect.transitionRange = new MinMax01(curvedValue, curvedValue - 0.1f);
    }
}
