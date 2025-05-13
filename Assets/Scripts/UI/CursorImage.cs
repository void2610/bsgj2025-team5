using UnityEngine;
using UnityEngine.UI;

public class CursorImage : MonoBehaviour
{
    [SerializeField] private float lerpSpeed = 10f; 
    [SerializeField] private Image mouseImage; 
    [SerializeField] private Canvas canvas; 
    
    private RectTransform _canvasRect;
    private void Awake() 
    { 
        _canvasRect = canvas.GetComponent<RectTransform>(); 
    }
    
    private void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect,
            Input.mousePosition, canvas.worldCamera, out var mousePos);

        var p = Vector2.Lerp(mouseImage.GetComponent<RectTransform>().anchoredPosition, mousePos, Time.deltaTime * lerpSpeed);
        mouseImage.GetComponent<RectTransform>().anchoredPosition = p;
    }
}
