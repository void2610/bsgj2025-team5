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
    [SerializeField] private List<Sprite> storyPaperSprites; // 紙芝居の画像リスト
    [SerializeField] private float displayTimePerImage = 2f; // 画像を表示する時間
    [SerializeField] private float transitionDuration = 0.5f; // 画像の切り替えにかかる時間
    [SerializeField] private Image frontImage; // 前面に表示する画像
    [SerializeField] private SeData slideSeData; // スライド時のSEデータ
    
    [Header("紙芝居アニメーション設定")]
    [SerializeField] private float slideInAngle = 30f; // 斜めに入ってくる角度
    [SerializeField] private float slideInDistance = 300f; // スライドする距離
    [SerializeField] private float rotationAmount = 15f; // 回転量
    [SerializeField] private float backImageDarkenAmount = 0.7f; // 背面画像の暗さ
    [SerializeField] private float backImageScale = 0.95f; // 背面画像のスケール
    [SerializeField] private Ease slideEase = Ease.OutExpo; // スライドのイージング
    
    [Header("画像サイズ設定")]
    [SerializeField, Range(0.1f, 2f)] private float globalImageScale = 1f; // 全体の画像スケール
    
    private CanvasGroup _canvasGroup;
    private CancellationTokenSource _cancellationTokenSource;
    private Image _backImage;
    private bool _isUsingFrontImage = true;
    
    private void Awake()
    {
        _canvasGroup = this.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        // 両方の画像を初期状態で透明に
        frontImage.color = new Color(1f, 1f, 1f, 0f);
        frontImage.rectTransform.localScale = Vector3.one * globalImageScale;
        
        var backGameObject = Instantiate(frontImage.gameObject, frontImage.transform.parent);
        backGameObject.name = "BackImage";
        backGameObject.transform.SetSiblingIndex(frontImage.transform.GetSiblingIndex());
        _backImage = backGameObject.GetComponent<Image>();
        _backImage.color = new Color(1f, 1f, 1f, 0f);
        _backImage.rectTransform.localScale = Vector3.one * globalImageScale;
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

            for (var i = 0; i < storyPaperSprites.Count; i++)
            {
                var sprite = storyPaperSprites[i];
                var isFirstImage = (i == 0);
                
                // 画像を紙芝居らしくアニメーション
                await ShowImageWithAnimationAsync(sprite, isFirstImage);
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
    
    private async UniTask ShowImageWithAnimationAsync(Sprite sprite, bool isFirstImage)
    {
        SeManager.Instance.PlaySe(slideSeData);
        // 現在の画像と次の画像を決定
        var currentImage = _isUsingFrontImage ? frontImage : _backImage;
        var previousImage = _isUsingFrontImage ? _backImage : frontImage;
        
        // 画像を設定
        currentImage.sprite = sprite;
        
        // 現在の画像を前面に
        currentImage.transform.SetAsLastSibling();
        
        // 初期状態設定（斜め上から登場）
        var rectTransform = currentImage.rectTransform;
        var originalPosition = Vector2.zero; // 中央に配置
        
        // 斜め上の開始位置を計算
        var angleRad = slideInAngle * Mathf.Deg2Rad;
        var startPosition = originalPosition + new Vector2(
            Mathf.Sin(angleRad) * slideInDistance,
            Mathf.Cos(angleRad) * slideInDistance
        );
        
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localRotation = Quaternion.Euler(0, 0, -rotationAmount);
        rectTransform.localScale = Vector3.one * globalImageScale * 1.1f; // グローバルスケールを適用して少し大きめから始める
        currentImage.color = new Color(1f, 1f, 1f, 0f);
        
        // アニメーションのモーション設定
        var positionMotion = LMotion.Create(startPosition, originalPosition, transitionDuration)
            .WithEase(slideEase)
            .BindToAnchoredPosition(rectTransform);
        
        var rotationMotion = LMotion.Create(-rotationAmount, 0f, transitionDuration)
            .WithEase(slideEase)
            .Bind(value => rectTransform.localRotation = Quaternion.Euler(0, 0, value));
        
        var scaleMotion = LMotion.Create(
            new Vector3(globalImageScale * 1.1f, globalImageScale * 1.1f, 1f), 
            Vector3.one * globalImageScale, 
            transitionDuration)
            .WithEase(slideEase)
            .BindToLocalScale(rectTransform);
        
        var fadeInMotion = LMotion.Create(0f, 1f, transitionDuration * 0.8f)
            .WithEase(Ease.OutQuart)
            .BindToColorA(currentImage);
        
        // 前の画像を背面に移動（最初の画像でない場合）
        if (!isFirstImage)
        {
            // 前の画像を暗くして少し小さくする
            var darkenMotion = LMotion.Create(1f, backImageDarkenAmount, transitionDuration)
                .WithEase(Ease.InOutQuad)
                .Bind(value => previousImage.color = new Color(value, value, value, 1f));
            
            var shrinkMotion = LMotion.Create(
                Vector3.one * globalImageScale, 
                Vector3.one * globalImageScale * backImageScale, 
                transitionDuration)
                .WithEase(Ease.InOutQuad)
                .BindToLocalScale(previousImage.rectTransform);
            
            // 全てのアニメーションを同時実行
            await UniTask.WhenAll(
                positionMotion.ToUniTask(_cancellationTokenSource.Token),
                rotationMotion.ToUniTask(_cancellationTokenSource.Token),
                scaleMotion.ToUniTask(_cancellationTokenSource.Token),
                fadeInMotion.ToUniTask(_cancellationTokenSource.Token),
                darkenMotion.ToUniTask(_cancellationTokenSource.Token),
                shrinkMotion.ToUniTask(_cancellationTokenSource.Token)
            );
        }
        else
        {
            // 最初の画像の場合は前の画像のアニメーションなし
            await UniTask.WhenAll(
                positionMotion.ToUniTask(_cancellationTokenSource.Token),
                rotationMotion.ToUniTask(_cancellationTokenSource.Token),
                scaleMotion.ToUniTask(_cancellationTokenSource.Token),
                fadeInMotion.ToUniTask(_cancellationTokenSource.Token)
            );
        }
        
        // 次回のために画像を切り替え
        _isUsingFrontImage = !_isUsingFrontImage;
    }
    
    public void SkipStory()
    {
        _cancellationTokenSource?.Cancel();
    }
}
