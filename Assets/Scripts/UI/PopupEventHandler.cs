using System;
using UnityEngine;
using UnityEngine.EventSystems; 

public class PopupEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject popupObject; 
    public void OnPointerEnter(PointerEventData eventData)
    {
        popupObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        popupObject.SetActive(false);
    }
}
