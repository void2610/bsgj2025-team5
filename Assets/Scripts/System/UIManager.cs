using R3;
using TMPro;
using UnityEngine;

public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [Tooltip("アイテム取得数を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI itemCountText;
    
    [Tooltip("ポーズ画面のCanvasGroup")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    public bool IsPaused { get; private set; } = false;

    public void TogglePause() => SetPause(!IsPaused);
    
    public void SetPause(bool p)
    {
        IsPaused = p;
        
        Time.timeScale = IsPaused ? 0 : 1;
        pauseCanvasGroup.alpha = IsPaused ? 1 : 0;
        pauseCanvasGroup.interactable = IsPaused;
        pauseCanvasGroup.blocksRaycasts = IsPaused;
        
        Cursor.lockState = p ? CursorLockMode.None : CursorLockMode.Locked;
    }

    protected override void Awake()
    {
        base.Awake();
        
        SetPause(false);
    }
}