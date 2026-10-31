using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MenuPointerFocus : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    public Action OnFocus;
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Rebuilding or scrolling a keyboard-selected list must not let a
        // stationary pointer steal the selection from the keyboard.
        if (eventData.delta.sqrMagnitude > 0) OnFocus?.Invoke();
    }
    public void OnPointerDown(PointerEventData eventData) => OnFocus?.Invoke();
}
