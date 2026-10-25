using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MenuPointerFocus : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    public Action OnFocus;
    public void OnPointerEnter(PointerEventData eventData) => OnFocus?.Invoke();
    public void OnPointerDown(PointerEventData eventData) => OnFocus?.Invoke();
}
