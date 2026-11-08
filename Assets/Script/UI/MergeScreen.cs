using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A full screen on top of the menu where 3 of the same item (same rarity) become 1 of the next rarity.
// Tap items in the list to put them in the 3 boxes at the top, tap a box to take it out again.
// Items that can't go with the ones already picked are shown dark.
// GearScreen adds this component by code when MERGE is tapped, and it removes itself on BACK.
public class MergeScreen : MonoBehaviour
{
    public Color backgroundColor = new Color(0.13f, 0.17f, 0.25f);
    public Color emptyBoxColor = new Color(0.17f, 0.21f, 0.32f);
    public Color mergeColor = new Color(0.55f, 0.35f, 0.8f);
    public Color unavailableColor = new Color(0.4f, 0.4f, 0.45f);
    public Color backColor = new Color(0.35f, 0.4f, 0.5f);

    private GameObject panel;
    private ScrollRect list;
    private readonly List<OwnedGear> picked = new List<OwnedGear>();
    private Image[] pickedBoxes = new Image[Inventory.MergeCount];
    private Text[] pickedTexts = new Text[Inventory.MergeCount];
    private Image resultBox;
    private Text resultText;
    private Text messageText;
    private Image mergeButton;

    public void Open(Transform canvas)
    {
        panel = UiBuilder.CreateFullScreenPanel("MergeScreen", canvas, backgroundColor);
        RectTransform safeArea = UiBuilder.CreateSafeArea(panel.transform);

        // The fixed part at the top. Everything in it is measured from its middle.
        RectTransform top = UiBuilder.CreateBox("Top", safeArea, Color.clear, Vector2.zero, new Vector2(1080f, 860f)).rectTransform;
        top.anchorMin = top.anchorMax = top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;

        UiBuilder.CreateText("Title", top, "MERGE", 100, new Vector2(0f, 340f), new Vector2(1000f, 150f));
        UiBuilder.CreateText("Help", top, "Pick 3 of the same item with the same rarity", 36, new Vector2(0f, 240f), new Vector2(1000f, 60f));

        // 3 boxes for the picked items, "=", and the result
        for (int i = 0; i < Inventory.MergeCount; i++)
        {
            int box = i; // a copy for the lambda
            pickedTexts[i] = UiBuilder.CreateButton("Picked" + i, top, "", new Vector2(-380f + i * 190f, 80f), new Vector2(GearScreen.CellSize, GearScreen.CellSize), emptyBoxColor, () => Unpick(box));
            pickedTexts[i].fontSize = 30;
            pickedBoxes[i] = pickedTexts[i].transform.parent.GetComponent<Image>();
        }

        UiBuilder.CreateText("Equals", top, "=", 90, new Vector2(165f, 80f), new Vector2(100f, 150f));

        resultBox = UiBuilder.CreateBox("Result", top, emptyBoxColor, new Vector2(340f, 80f), new Vector2(GearScreen.CellSize + 20f, GearScreen.CellSize + 20f));
        resultText = UiBuilder.CreateText("ResultText", resultBox.transform, "?", 30, Vector2.zero, new Vector2(GearScreen.CellSize, GearScreen.CellSize));

        messageText = UiBuilder.CreateText("Message", top, "", 44, new Vector2(0f, -80f), new Vector2(1000f, 80f));

        Vector2 buttonSize = new Vector2(440f, 150f);
        UiBuilder.CreateButton("Back", top, "BACK", new Vector2(-240f, -230f), buttonSize, backColor, Close);
        Text mergeLabel = UiBuilder.CreateButton("Merge", top, "MERGE", new Vector2(240f, -230f), buttonSize, mergeColor, Merge);
        mergeButton = mergeLabel.transform.parent.GetComponent<Image>();

        UiBuilder.CreateText("ListLabel", top, "ALL ITEMS", 44, new Vector2(0f, -380f), new Vector2(1000f, 60f));

        list = UiBuilder.CreateScrollGrid("Items", safeArea, new Vector2(GearScreen.CellSize, GearScreen.CellSize), 18f, 5);
        RectTransform listRect = list.GetComponent<RectTransform>();
        listRect.offsetMin = new Vector2(20f, 20f);
        listRect.offsetMax = new Vector2(-20f, -880f);

        Refresh();
    }

    void Tap(OwnedGear item)
    {
        if (picked.Contains(item)) picked.Remove(item);
        else if (CanPick(item)) picked.Add(item);

        messageText.text = "";
        Refresh();
    }

    void Unpick(int box)
    {
        if (box < picked.Count) picked.RemoveAt(box);
        Refresh();
    }

    bool CanPick(OwnedGear item)
    {
        if (picked.Count >= Inventory.MergeCount) return false;
        if (item.rarity == Rarity.Legendary) return false; // already the best
        return picked.Count == 0 || Inventory.CanMergeWith(picked[0], item);
    }

    void Merge()
    {
        if (picked.Count < Inventory.MergeCount) return;

        OwnedGear result = Inventory.Merge(picked);
        picked.Clear();
        if (result == null) return; // should not happen: only matching items can be picked

        messageText.text = "You made: " + GearCatalog.RarityName(result.rarity) + " " + Inventory.TypeOf(result).title + "!";
        messageText.color = GearCatalog.RarityColor(result.rarity);
        SoundManager.Play(Sound.LevelUp);
        Refresh();
    }

    void Close()
    {
        Destroy(panel);
        Destroy(this); // removes only this component, not the menu
    }

    void Refresh()
    {
        // The 3 boxes at the top
        for (int i = 0; i < Inventory.MergeCount; i++)
        {
            bool filled = i < picked.Count;
            pickedBoxes[i].color = filled ? GearCatalog.RarityColor(picked[i].rarity) : emptyBoxColor;
            pickedTexts[i].text = filled ? Inventory.TypeOf(picked[i]).title + "\nLv " + picked[i].level : "";
        }

        // The result: the same item, one rarity better
        if (picked.Count > 0)
        {
            Rarity next = picked[0].rarity + 1;
            resultBox.color = GearCatalog.RarityColor(next);
            resultText.text = GearCatalog.RarityName(next) + "\n" + Inventory.TypeOf(picked[0]).title;
        }
        else
        {
            resultBox.color = emptyBoxColor;
            resultText.text = "?";
        }

        mergeButton.color = picked.Count == Inventory.MergeCount ? mergeColor : unavailableColor;

        RefreshList();
    }

    void RefreshList()
    {
        UiBuilder.ClearChildren(list.content);

        // Sorted so that copies of the same item sit next to each other
        List<OwnedGear> items = new List<OwnedGear>();
        foreach (OwnedGear item in Inventory.All)
        {
            if (Inventory.TypeOf(item) != null) items.Add(item);
        }
        items.Sort((a, b) =>
        {
            if (a.type != b.type) return string.CompareOrdinal(a.type, b.type);
            if (a.rarity != b.rarity) return b.rarity.CompareTo(a.rarity);
            return b.level.CompareTo(a.level);
        });

        foreach (OwnedGear item in items)
        {
            OwnedGear tapped = item; // a copy for the lambda
            Image cell = GearScreen.CreateItemCell(list.content, item, () => Tap(tapped));

            bool isPicked = picked.Contains(item);
            if (isPicked || !CanPick(item))
            {
                cell.color = Color.Lerp(cell.color, Color.black, 0.65f); // dark = can't be picked (or already is)
            }

            if (isPicked) AddTag(cell.transform, "PICKED");
            else if (Inventory.IsEquipped(item)) AddTag(cell.transform, "WORN");
        }
    }

    // A small word across the top of a cell
    static void AddTag(Transform cell, string word)
    {
        Text tag = UiBuilder.CreateText("Tag", cell, word, 24, new Vector2(0f, 68f), new Vector2(GearScreen.CellSize, 34f));
        tag.color = new Color(1f, 0.8f, 0.15f);
    }
}
