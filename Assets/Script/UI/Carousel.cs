using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A row of cards you slide left and right, one card at a time (the chapter packs in the shop).
// After letting go it snaps to the nearest card, like PageSwiper does with the menu pages.
// Up-and-down drags still scroll the shop, because NestedScrollRect passes them on.
public class Carousel : NestedScrollRect
{
    public float snapSpeed = 4000f;    // canvas units per second
    public float swipeDistance = 0.15f; // drag this part of a card to go to the next one

    // The dots under the cards: the current card's dot is bright
    public Image[] dots;
    public Color dotColor = new Color(1f, 1f, 1f, 0.3f);
    public Color currentDotColor = Color.white;

    public int CurrentCard { get; private set; }

    private float cardWidth;
    private int cardCount;
    private int cardAtDragStart;
    private bool dragging;

    public void Setup(float width, int count, int startCard)
    {
        cardWidth = width;
        cardCount = count;
        CurrentCard = Mathf.Clamp(startCard, 0, count - 1);
        content.anchoredPosition = new Vector2(-CurrentCard * cardWidth, 0f);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        dragging = !dragIsForParent;
        cardAtDragStart = CurrentCard;
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (dragging)
        {
            // How many cards the finger moved: positive = towards the next card
            float moved = -content.anchoredPosition.x / cardWidth - cardAtDragStart;
            if (moved > swipeDistance) CurrentCard = cardAtDragStart + 1;
            else if (moved < -swipeDistance) CurrentCard = cardAtDragStart - 1;
            CurrentCard = Mathf.Clamp(CurrentCard, 0, cardCount - 1);
        }

        dragging = false;
        base.OnEndDrag(eventData);
    }

    void Update()
    {
        if (dots != null)
        {
            for (int i = 0; i < dots.Length; i++) dots[i].color = i == CurrentCard ? currentDotColor : dotColor;
        }

        if (dragging || cardWidth <= 0f) return;

        // Slide to the current card
        StopMovement();
        Vector2 target = new Vector2(-CurrentCard * cardWidth, 0f);
        content.anchoredPosition = Vector2.MoveTowards(content.anchoredPosition, target, snapSpeed * Time.unscaledDeltaTime);
    }
}
