using System;
using UnityEngine;
using UnityEngine.EventSystems; 

public class Event : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject PopUpObject; 
    public void OnPointerEnter(PointerEventData eventData)
    {
        PopUpObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PopUpObject.SetActive(false);
    }
}
