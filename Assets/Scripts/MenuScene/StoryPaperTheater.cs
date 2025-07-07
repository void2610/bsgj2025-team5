using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class StoryPaperTheater : MonoBehaviour
{
    [SerializeField] private List<Sprite> storyPaperSprites;
    [SerializeField] private float displayTimePerImage = 2f;
    [SerializeField] private Image image;
    
    private CanvasGroup _canvasGroup;
    private CancellationTokenSource _cancellationTokenSource;
    
    private void Awake()
    {
        _canvasGroup = this.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
    
    private void OnDestroy()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
    
    public async UniTask StartStoryAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        
        try
        {
            foreach (var sprite in storyPaperSprites)
            {
                image.sprite = sprite;
                await UniTask.Delay((int)(displayTimePerImage * 1000), cancellationToken: _cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException) { }
    }
    
    public void StopStory()
    {
        _cancellationTokenSource?.Cancel();
    }
}
