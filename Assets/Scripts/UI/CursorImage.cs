using UnityEngine;
using UnityEngine.UI;

public class CursorImage : MonoBehaviour
{
    [SerializeField] private float lerpSpeed = 10f; 
    [SerializeField] private Canvas canvas; 
    
    private RectTransform _mouseImage;
    private RectTransform _canvasRect;
    private void Awake() 
    { 
        _canvasRect = canvas.GetComponent<RectTransform>(); 
        _mouseImage = this.GetComponent<RectTransform>();
    }
    
    private void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect,
            Input.mousePosition, canvas.worldCamera, out var mousePos);

        var p = Vector2.Lerp(_mouseImage.anchoredPosition, mousePos, Time.unscaledDeltaTime * lerpSpeed);
        _mouseImage.anchoredPosition = p;
    }
}
