using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using LitMotion;
using LitMotion.Extensions;

public class StoryPaperTheater : MonoBehaviour
{
    [SerializeField] private List<Sprite> storyPaperSprites;
    [SerializeField] private float displayTimePerImage = 2f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Image image;
    
    private CanvasGroup _canvasGroup;
    private CancellationTokenSource _cancellationTokenSource;
    
    private void Awake()
    {
        _canvasGroup = this.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        image.color = new Color(1f, 1f, 1f, 0f); // 初期状態は透明
    }
    
    private void OnDestroy()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
    
    public async UniTask StartStoryAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // 紙芝居全体をフェードイン
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            await LMotion.Create(0f, 1f, 0.5f)
                .WithEase(Ease.OutQuart)
                .Bind(value => _canvasGroup.alpha = value)
                .ToUniTask(_cancellationTokenSource.Token);
            
            await UniTask.Delay(1000, cancellationToken: _cancellationTokenSource.Token);
            image.color = Color.white;

            foreach (var sprite in storyPaperSprites)
            {
                // 画像を紙芝居らしくアニメーション
                await ShowImageWithAnimationAsync(sprite);
                await UniTask.Delay((int)(displayTimePerImage * 1000), cancellationToken: _cancellationTokenSource.Token);
            }
            
            await UniTask.Delay(1000, cancellationToken: _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
    
    private async UniTask ShowImageWithAnimationAsync(Sprite sprite)
    {
        // 画像を設定
        image.sprite = sprite;
        
        // 初期状態設定（右側から登場）
        var rectTransform = image.rectTransform;
        var originalPosition = rectTransform.anchoredPosition;
        var startPosition = originalPosition + Vector2.right * 100f;
        
        rectTransform.anchoredPosition = startPosition;
        image.color = new Color(1f, 1f, 1f, 0f);
        
        // スライドイン + フェードインのアニメーション
        var slideMotion = LMotion.Create(startPosition, originalPosition, transitionDuration)
            .WithEase(Ease.OutBack)
            .BindToAnchoredPosition(rectTransform);
            
        var fadeMotion = LMotion.Create(0f, 1f, transitionDuration)
            .WithEase(Ease.OutQuart)
            .BindToColorA(image);
        
        await UniTask.WhenAll(
            slideMotion.ToUniTask(_cancellationTokenSource.Token),
            fadeMotion.ToUniTask(_cancellationTokenSource.Token)
        );
    }
    
    public void SkipStory()
    {
        _cancellationTokenSource?.Cancel();
    }
}
