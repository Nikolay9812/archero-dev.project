using UnityEngine;
using UnityEngine.EventSystems;

// Pages side by side that you can slide with a finger (or the mouse), then they snap into place.
// The interfaces below are how Unity tells us about dragging: it calls OnBeginDrag, OnDrag and OnEndDrag.
// A drag that starts on a button in a page also ends up here, and that button then doesn't count as tapped.
public class PageSwiper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float slideSpeed = 5f;      // pages per second when snapping
    public float swipeDistance = 0.2f; // drag this part of a page (0.2 = 20%) to go to the next one

    // Where the pages are right now, in pages: 2 = on page 2, 2.5 = halfway to page 3.
    // BottomTabBar reads this, so the tabs follow the finger.
    public float Position { get; private set; }

    private RectTransform viewport; // the area where the pages are shown
    private RectTransform content;  // holds all the pages and is the part that moves
    private Canvas canvas;
    private int pageCount;
    private int targetPage;
    private int pageAtDragStart;
    private bool dragging;

    public void Setup(int numberOfPages, int startPage)
    {
        viewport = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        pageCount = numberOfPages;
        targetPage = startPage;
        Position = startPage;

        GameObject contentObject = new GameObject("Pages", typeof(RectTransform));
        contentObject.transform.SetParent(transform, false);
        content = contentObject.GetComponent<RectTransform>();
        MoveContent();
    }

    // Anchors are measured in "parts of the parent": 0 = left edge, 1 = right edge, 2 = one more parent to the right.
    // So page 3 runs from 3 to 4, and it is exactly one screen wide on every phone.
    public Transform CreatePage(int index)
    {
        GameObject page = new GameObject("Page" + index, typeof(RectTransform));
        page.transform.SetParent(content, false);

        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(index, 0f);
        rect.anchorMax = new Vector2(index + 1, 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        return page.transform;
    }

    public void GoToPage(int index)
    {
        targetPage = Mathf.Clamp(index, 0, pageCount - 1);
    }

    void Update()
    {
        if (dragging) return;

        Position = Mathf.MoveTowards(Position, targetPage, slideSpeed * Time.deltaTime);
        MoveContent();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        pageAtDragStart = Mathf.RoundToInt(Position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // delta = how far the finger moved since the last frame, in screen pixels.
        // Pixels -> canvas units -> pages. Finger to the left = towards the next page, hence the minus.
        float canvasUnits = eventData.delta.x / canvas.scaleFactor;
        Position -= canvasUnits / viewport.rect.width;
        Position = Mathf.Clamp(Position, 0f, pageCount - 1);
        MoveContent();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;

        float moved = Position - pageAtDragStart;
        if (Mathf.Abs(moved) < swipeDistance) GoToPage(pageAtDragStart); // too small: go back
        else if (moved > 0f) GoToPage(Mathf.CeilToInt(Position));
        else GoToPage(Mathf.FloorToInt(Position));
    }

    // Slide everything one screen to the left for each page
    void MoveContent()
    {
        content.anchorMin = new Vector2(-Position, 0f);
        content.anchorMax = new Vector2(1f - Position, 1f);
        content.offsetMin = content.offsetMax = Vector2.zero;
    }
}
