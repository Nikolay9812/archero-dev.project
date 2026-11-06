using UnityEngine;
using UnityEngine.UI;

// The shop page of the main menu: one row per gem item, each with a button that shows the price.
// MainMenu adds this component by code and calls Build(), so there is nothing to set up in the scene.
public class ShopScreen : MonoBehaviour
{
    public Color buyColor = new Color(0.2f, 0.6f, 0.75f);
    public Color unavailableColor = new Color(0.4f, 0.4f, 0.45f); // too expensive or already owned

    // The parts of one row that change after buying something
    private class Row
    {
        public GemItem item;
        public Text price;
        public Image button;
    }

    private Row[] rows;

    public void Build(Transform page)
    {
        UiBuilder.CreateText("Title", page, "SHOP", 110, new Vector2(0f, 500f), new Vector2(1000f, 180f));

        rows = new Row[GemShop.All.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = CreateRow(page, GemShop.All[i], 250f - i * 220f);
        }

        Refresh();
    }

    void Buy(Row row)
    {
        if (GemShop.TryBuy(row.item)) Refresh();
    }

    // Writes the current prices on the buttons (the gems are in the menu's header)
    void Refresh()
    {
        foreach (Row row in rows)
        {
            bool owned = GemShop.Owns(row.item);
            int price = GemShop.GetPrice(row.item);

            row.price.text = owned ? "OWNED" : price + " gems";
            row.button.color = !owned && Wallet.Gems >= price ? buyColor : unavailableColor;
        }
    }

    Row CreateRow(Transform parent, GemItem item, float y)
    {
        Row row = new Row();
        row.item = item;

        string info = GemShop.GetTitle(item) + "\n" + GemShop.GetDescription(item); // \n = new line
        UiBuilder.CreateText(item + "Info", parent, info, 40, new Vector2(-200f, y), new Vector2(600f, 180f));

        // "() => Buy(row)" is a lambda: a tiny method with no name. Each button remembers its own row.
        row.price = UiBuilder.CreateButton(item + "Button", parent, "", new Vector2(320f, y), new Vector2(340f, 150f), buyColor, () => Buy(row));
        row.price.fontSize = 52;
        row.button = row.price.transform.parent.GetComponent<Image>(); // the label sits on top of the button's image

        return row;
    }
}
