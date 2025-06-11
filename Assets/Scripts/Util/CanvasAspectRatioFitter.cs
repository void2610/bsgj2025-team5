using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
public class CanvasAspectRatioFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float aspectWidth = 16.0f;
    [SerializeField] private float aspectHeight = 9.0f;
    
    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private RectTransform _rectTransform;
    private float _targetAspect;
    private float _lastScreenWidth;
    private float _lastScreenHeight;
    
    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvasScaler = GetComponent<CanvasScaler>();
        _rectTransform = GetComponent<RectTransform>();
        _targetAspect = aspectWidth / aspectHeight;
        
        // Find camera if not assigned
        if (targetCamera == null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            targetCamera = Camera.main;
        }
        
        AdjustCanvas();
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }
    
    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            AdjustCanvas();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }
    }
    
    private void AdjustCanvas()
    {
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            // For Screen Space - Camera mode, the camera's viewport rect already handles this
            return;
        }
        
        var windowAspect = (float)Screen.width / (float)Screen.height;
        var scaleHeight = windowAspect / _targetAspect;
        
        // Reset to default first
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.sizeDelta = Vector2.zero;
        
        if (scaleHeight < 1.0f)
        {
            // Letterbox (black bars on top and bottom)
            // Canvas height should be scaled down to match the camera's viewport
            var scaledHeight = Screen.height * scaleHeight;
            var yOffset = (Screen.height - scaledHeight) * 0.5f;
            
            _rectTransform.offsetMin = new Vector2(0, yOffset);
            _rectTransform.offsetMax = new Vector2(0, -yOffset);
        }
        else
        {
            // Pillarbox (black bars on left and right)
            // Canvas width should be scaled down to match the camera's viewport
            var scaleWidth = 1.0f / scaleHeight;
            var scaledWidth = Screen.width * scaleWidth;
            var xOffset = (Screen.width - scaledWidth) * 0.5f;
            
            _rectTransform.offsetMin = new Vector2(xOffset, 0);
            _rectTransform.offsetMax = new Vector2(-xOffset, 0);
        }
        
        // Update Canvas Scaler to maintain proper UI scaling
        if (_canvasScaler != null && _canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // Adjust match width/height based on aspect ratio
            if (scaleHeight < 1.0f)
            {
                // When letterboxed, prioritize width matching
                _canvasScaler.matchWidthOrHeight = 0f;
            }
            else
            {
                // When pillarboxed, prioritize height matching
                _canvasScaler.matchWidthOrHeight = 1f;
            }
        }
    }
}