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
    
    [Header("クールタイム設定")]
    [Tooltip("アクション実行後のクールタイム（秒）")]
    [SerializeField] private float cooldownTime = 2f;

    private bool _isPointerOver;
    private float _focusTime;
    private float _cooldownTimer;
    private bool _isOnCooldown;
    private TextMeshProUGUI _text;
    private AudioSource _chargeAudioSource;
    private UIEffect _uiEffect;
    
    public void SetText(string text) => _text.text = text;
    public void SetAction(UnityAction a) => action.AddListener(a.Invoke);
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // クールタイム中は反応しない
        if (_isOnCooldown) return;
        
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
        // クールタイム処理
        if (_isOnCooldown)
        {
            _cooldownTimer -= Time.unscaledDeltaTime;
            if (_cooldownTimer <= 0f)
            {
                _isOnCooldown = false;
            }
            
            // クールタイム中は進行度を表示（クールタイムの進捗）
            var cooldownProgress = 1f - (_cooldownTimer / cooldownTime);
            _uiEffect.transitionRange = new MinMax01(cooldownProgress * 0.3f, 0f); // 薄く表示
            return;
        }
        
        if (_isPointerOver)
        {
            _focusTime += Time.unscaledDeltaTime;

            if (_focusTime >= requiredTime)
            {
                action?.Invoke();
                _focusTime = 0f;
                _isPointerOver = false; // ポインタオーバー状態をリセット
                
                // アクション実行時にチャージSEを停止
                if (_chargeAudioSource)
                {
                    SeManager.Instance.StopSe(_chargeAudioSource);
                    _chargeAudioSource = null;
                }
                
                // クールタイムを開始
                StartCooldown();
            }
        }
        
        // 通常時の進行度表示（クールタイム中でない場合のみ）
        if (!_isOnCooldown)
        {
            var normalizedTime = _focusTime / requiredTime;
            var curvedValue = progressCurve.Evaluate(normalizedTime);
            _uiEffect.transitionRange = new MinMax01(curvedValue, curvedValue - 0.1f);
        }
    }
    
    /// <summary>
    /// クールタイムを開始する
    /// </summary>
    private void StartCooldown()
    {
        _isOnCooldown = true;
        _cooldownTimer = cooldownTime;
    }
}
