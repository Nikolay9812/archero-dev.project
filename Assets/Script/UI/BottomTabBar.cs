using UnityEngine;
using UnityEngine.UI;

// The tabs at the bottom of the menu. The selected tab is wider, taller and yellow.
// All of that is worked out from the position of the pages, so the tabs change smoothly while you swipe.
// MainMenu adds this component by code and calls Build(), so there is nothing to set up in the scene.
public class BottomTabBar : MonoBehaviour
{
    public float tabHeight = 170f;
    public float selectedExtraHeight = 40f;
    public int labelSize = 34;
    public int selectedLabelSize = 46;
    public float labelPadding = 16f; // free space kept beside the text inside a tab
    public Color barColor = new Color(0.09f, 0.11f, 0.18f);
    public Color tabColor = new Color(0.17f, 0.21f, 0.32f);
    public Color selectedColor = new Color(1f, 0.8f, 0.15f);
    public Color labelColor = Color.white;
    public Color selectedLabelColor = new Color(0.25f, 0.18f, 0.05f);

    private PageSwiper swiper;
    private RectTransform bar;
    private RectTransform[] tabs;
    private Image[] tabImages;
    private Text[] labels;

    public void Build(Transform parent, string[] tabNames, PageSwiper pageSwiper)
    {
        swiper = pageSwiper;
        bar = UiBuilder.CreateEdgeBar("Footer", parent, false, tabHeight, barColor);

        tabs = new RectTransform[tabNames.Length];
        tabImages = new Image[tabNames.Length];
        labels = new Text[tabNames.Length];

        for (int i = 0; i < tabNames.Length; i++)
        {
            // A copy for the lambda below. Without it, every button would remember the same "i",
            // which is tabNames.Length by the time anyone taps a tab.
            int page = i;

            labels[i] = UiBuilder.CreateButton("Tab" + i, bar, tabNames[i], Vector2.zero, Vector2.zero, tabColor, () => swiper.GoToPage(page));
            labels[i].rectTransform.sizeDelta = new Vector2(200f, 100f);
            labels[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            labels[i].verticalOverflow = VerticalWrapMode.Overflow;

            tabs[i] = labels[i].transform.parent.GetComponent<RectTransform>();
            tabImages[i] = tabs[i].GetComponent<Image>();

            // Measure the tab from the bottom left corner of the bar
            tabs[i].anchorMin = tabs[i].anchorMax = tabs[i].pivot = Vector2.zero;
        }
    }

    void Update()
    {
        // The selected tab is as wide as two normal tabs. While swiping, two tabs share that extra width.
        float normalWidth = bar.rect.width / (tabs.Length + 1);
        float x = 0f;

        for (int i = 0; i < tabs.Length; i++)
        {
            // How selected is this tab? 1 = fully, 0 = not at all, 0.5 = halfway through a swipe
            float selected = Mathf.Clamp01(1f - Mathf.Abs(swiper.Position - i));

            float width = normalWidth * (1f + selected);
            tabs[i].anchoredPosition = new Vector2(x, 0f);
            tabs[i].sizeDelta = new Vector2(width, tabHeight + selectedExtraHeight * selected);
            x += width;

            tabImages[i].color = Color.Lerp(tabColor, selectedColor, selected);
            labels[i].color = Color.Lerp(labelColor, selectedLabelColor, selected);
            labels[i].fontSize = Mathf.RoundToInt(Mathf.Lerp(labelSize, selectedLabelSize, selected));

            // Long names like COLLECTIBLE would run into the next tab, so shrink them to fit
            float scale = Mathf.Min((width - labelPadding) / labels[i].preferredWidth, 1f);
            labels[i].rectTransform.localScale = Vector3.one * scale;
        }
    }
}
