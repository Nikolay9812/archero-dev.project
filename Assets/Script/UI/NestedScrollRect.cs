using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A list that scrolls up and down, inside pages that swipe left and right (PageSwiper).
// A normal ScrollRect would take EVERY drag, so you could no longer swipe pages while touching the list.
// This one looks at the start of each drag: mostly sideways -> hand the drag to the parent (the swiper),
// mostly up or down -> scroll the list as usual.
//
// "override" replaces a method of ScrollRect, "base.X" still runs ScrollRect's own version.
public class NestedScrollRect : ScrollRect
{
    private bool dragIsForParent;

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        PassToParent(eventData, ExecuteEvents.initializePotentialDrag);
        base.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        dragIsForParent = Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y);

        if (dragIsForParent) PassToParent(eventData, ExecuteEvents.beginDragHandler);
        else base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (dragIsForParent) PassToParent(eventData, ExecuteEvents.dragHandler);
        else base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (dragIsForParent) PassToParent(eventData, ExecuteEvents.endDragHandler);
        else base.OnEndDrag(eventData);

        dragIsForParent = false;
    }

    // Sends the event to the first object above us that handles it (no one = nothing happens).
    // <T> means "any kind of event handler": the same method works for begin, drag and end.
    void PassToParent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> function) where T : IEventSystemHandler
    {
        if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, function);
    }
}
