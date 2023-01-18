using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SmoothScrollRect: MonoBehaviour, IEndDragHandler
{
    LoopScrollRect loopScrollRect;
    ScrollRect scrollRect;
    public void Start()
    {
        loopScrollRect = GetComponent<LoopScrollRect>();
        scrollRect = GetComponent<ScrollRect>();
    }

    public void OnEndDrag(PointerEventData _)
    {
        if(loopScrollRect != null) UIManager.Instance.ScrollRectSmoothScroll(loopScrollRect);
        else if(scrollRect != null) UIManager.Instance.ScrollRectSmoothScroll(scrollRect);
    }
}