using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A list that scrolls up and down, inside pages that swipe left and right (PageSwiper).
// A normal ScrollRect would take EVERY drag, so you could no longer swipe pages while touching the list.
// This one looks at the start of each drag: if it goes the way this list scrolls, the list scrolls as usual,
// otherwise the drag goes to the parent. It works the other way round too: a sideways list (Carousel)
// inside an up-and-down list hands the up-and-down drags to that list.
//
// "override" replaces a method of ScrollRect, "base.X" still runs ScrollRect's own version.
public class NestedScrollRect : ScrollRect
{
    // "protected" = private, but classes that build on this one (Carousel) can read it too
    protected bool dragIsForParent;

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        PassToParent(eventData, ExecuteEvents.initializePotentialDrag);
        base.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        bool sideways = Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y);
        dragIsForParent = sideways ? !horizontal : !vertical; // the way we don't scroll belongs to the parent

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
