using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using JetBrains.Annotations;
using UnityEngine.Localization;

public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private GameObject focusButtonPrefab;
    [SerializeField] private float ySpacing = 100; // ボタン間の間隔
    [SerializeField] private float buttonSize = 1; // ボタンのサイズ
    
    private List<FocusButton>_focusButtons = new ();

    private async void Start()
    {
        // ローカライズシステムの初期化を待つ
        await LocalizationSettings.InitializationOperation.Task;

        // UIを構築
        var index = 0;
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            var fb = Instantiate(focusButtonPrefab, this.transform).GetComponent<FocusButton>();
            _focusButtons.Add(fb);
            fb.transform.localPosition = new Vector3(0f, -(index * ySpacing), 0f);
            fb.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);
            
            fb.SetText(locale.LocaleName);
            fb.SetAction(() => LocalizationSettings.SelectedLocale = locale);
            _focusButtons.Add(fb);
            index++;
        }
    }
}

