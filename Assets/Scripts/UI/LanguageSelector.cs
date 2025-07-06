using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private GameObject focusButtonPrefab;
    [SerializeField] private float ySpacing = 100; // ボタン間の間隔
    [SerializeField] private float buttonSize = 1; // ボタンのサイズ
    
    private string GetLocaleName(Locale locale)
    {
        // 言語コードに基づいてネイティブな表示名を返す
        return locale.Identifier.Code switch
        {
            "en" => "English",
            "ja" => "日本語",
            _ => locale.ToString()
        };
    }

    private async void Start()
    {
        // ローカライズシステムの初期化を待つ
        await LocalizationSettings.InitializationOperation.Task;

        // UIを構築
        var index = 0;
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            var fb = Instantiate(focusButtonPrefab, this.transform).GetComponent<FocusButton>();
            fb.transform.localPosition = new Vector3(0f, -(index * ySpacing), 0f);
            fb.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);
            
            fb.SetText(GetLocaleName(locale));
            fb.SetAction(() => LocalizationSettings.SelectedLocale = locale);
            index++;
        }
    }
}

