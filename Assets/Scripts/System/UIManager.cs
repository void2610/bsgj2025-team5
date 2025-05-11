using R3;
using TMPro;
using UnityEngine;

public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private TextMeshProUGUI playerSpeedText;
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    private bool _isPaused = false;
    
    public void TogglePause() => SetPause(!_isPaused);
    
    public void SetPause(bool p)
    {
        _isPaused = p;
        
        Time.timeScale = _isPaused ? 0 : 1;
        pauseCanvasGroup.alpha = _isPaused ? 1 : 0;
        pauseCanvasGroup.interactable = _isPaused;
        pauseCanvasGroup.blocksRaycasts = _isPaused;
        
        Cursor.lockState = p ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = p;
    }

    protected override void Awake()
    {
        base.Awake();
        
        SetPause(false);
    }
    
    private void Start()
    {
        GameManager.Instance.ItemCount.Subscribe(v => itemCountText.text = $"Item: {v}/5").AddTo(this);
        GameManager.Instance.Player.PlayerSpeedInt.Subscribe(v => playerSpeedText.text = $"Speed: {v}/4").AddTo(this);
    }
}