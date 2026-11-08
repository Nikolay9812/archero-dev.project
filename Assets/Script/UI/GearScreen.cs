using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The GEAR page of the main menu:
//   top    - the character, with the 7 gear slots around her and a MERGE button
//   middle - the total bonus of everything she wears
//   bottom - the inventory: every item she doesn't wear, plus her scrolls
// Tap an item (or a worn slot) to see it, wear it or level it up.
// MainMenu adds this component by code and calls Build(), so there is nothing to set up in the scene.
public class GearScreen : MonoBehaviour
{
    public Color slotColor = new Color(0.17f, 0.21f, 0.32f);
    public Color avatarBackground = new Color(0.2f, 0.25f, 0.37f);
    public Color windowColor = new Color(0.13f, 0.16f, 0.24f);
    public Color goodButtonColor = new Color(0.3f, 0.7f, 0.3f);
    public Color unavailableColor = new Color(0.4f, 0.4f, 0.45f);
    public Color mergeColor = new Color(0.55f, 0.35f, 0.8f);

    public const float CellSize = 170f;
    private const float TopHeight = 960f; // the part with the character, slots and stats

    // Where each slot sits around the character: 4 on the left, 3 on the right (MERGE is the 4th on the right)
    private static readonly GearSlot[] SlotOrder =
    {
        GearSlot.Head, GearSlot.Armor, GearSlot.Hands, GearSlot.Boots, // left, top to bottom
        GearSlot.Weapon, GearSlot.Ring, GearSlot.Amulet                 // right, top to bottom
    };
    private static readonly float[] RowY = { 340f, 140f, -60f, -260f };

    // The parts of one slot that change when something else is worn
    private class SlotView
    {
        public Image background;
        public Text title;
        public Text level;
    }

    private SlotView[] slots = new SlotView[GearCatalog.SlotCount]; // index = (int)GearSlot
    private Text statsText;
    private Text inventoryLabel;
    private ScrollRect inventoryList;
    private Transform overlayParent; // the whole canvas: popups cover the header and the tabs too
    private GameObject popup;
    private bool built;

    public void Build(Transform page, GameObject avatarModel, RuntimeAnimatorController avatarAnimator)
    {
        overlayParent = page.GetComponentInParent<Canvas>().transform;

        // A box stuck to the top of the page. Everything in it is measured from its middle.
        RectTransform top = UiBuilder.CreateBox("Top", page, Color.clear, Vector2.zero, new Vector2(1080f, TopHeight)).rectTransform;
        top.anchorMin = top.anchorMax = top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;

        gameObject.AddComponent<AvatarView>().Build(top, new Vector2(0f, 40f), new Vector2(480f, 680f), avatarModel, avatarAnimator, avatarBackground);

        for (int i = 0; i < SlotOrder.Length; i++)
        {
            bool left = i < 4;
            float x = left ? -390f : 390f;
            float y = RowY[left ? i : i - 4];
            slots[(int)SlotOrder[i]] = CreateSlot(top, SlotOrder[i], new Vector2(x, y));
        }

        Text merge = UiBuilder.CreateButton("MergeButton", top, "MERGE", new Vector2(390f, RowY[3]), new Vector2(CellSize, CellSize), mergeColor, OpenMerge);
        merge.fontSize = 38;

        statsText = UiBuilder.CreateText("Stats", top, "", 34, new Vector2(0f, -380f), new Vector2(1040f, 110f));
        statsText.color = new Color(1f, 0.8f, 0.15f);

        inventoryLabel = UiBuilder.CreateText("InventoryLabel", top, "", 44, new Vector2(0f, -460f), new Vector2(1000f, 60f));

        // The inventory fills the rest of the page, so taller phones see more rows
        inventoryList = UiBuilder.CreateScrollGrid("Inventory", page, new Vector2(CellSize, CellSize), 18f, 5);
        RectTransform listRect = inventoryList.GetComponent<RectTransform>();
        listRect.offsetMin = new Vector2(20f, 10f);
        listRect.offsetMax = new Vector2(-20f, -(TopHeight + 40f));

        built = true;
        Refresh();
    }

    void OnEnable()
    {
        Inventory.Changed += Refresh;
    }

    void OnDisable()
    {
        Inventory.Changed -= Refresh;
    }

    // Redraws everything from the save data. Simple and safe: called after every change.
    void Refresh()
    {
        if (!built) return;

        RefreshSlots();
        RefreshStats();
        RefreshInventory();
    }

    void RefreshSlots()
    {
        for (int i = 0; i < GearCatalog.SlotCount; i++)
        {
            GearSlot slot = (GearSlot)i;
            OwnedGear item = Inventory.GetEquipped(slot);
            SlotView view = slots[i];

            if (item == null)
            {
                view.background.color = slotColor;
                view.title.text = GearCatalog.SlotName(slot);
                view.level.text = "";
            }
            else
            {
                view.background.color = GearCatalog.RarityColor(item.rarity);
                view.title.text = Inventory.TypeOf(item).title;
                view.level.text = "Lv " + item.level;
            }
        }
    }

    void RefreshStats()
    {
        string text = "";
        // Go through every value of the GearStat enum
        foreach (GearStat stat in System.Enum.GetValues(typeof(GearStat)))
        {
            float bonus = Inventory.Bonus(stat);
            if (bonus <= 0f) continue; // "continue" skips to the next stat

            if (text != "") text += "     ";
            text += GearCatalog.FormatBonus(stat, bonus);
        }

        statsText.text = text != "" ? text : "Wear gear to get stronger";
    }

    void RefreshInventory()
    {
        Transform content = inventoryList.content;
        UiBuilder.ClearChildren(content);

        // Only what she doesn't wear, best first
        List<OwnedGear> items = new List<OwnedGear>();
        foreach (OwnedGear item in Inventory.All)
        {
            if (Inventory.TypeOf(item) != null && !Inventory.IsEquipped(item)) items.Add(item);
        }
        items.Sort(CompareItems);

        foreach (OwnedGear item in items)
        {
            OwnedGear tapped = item; // a copy for the lambda (same reason as "page" in BottomTabBar)
            CreateItemCell(content, item, () => ShowItem(tapped));
        }

        // Scrolls come after the gear, one cell per slot that has any
        int scrollCount = 0;
        for (int i = 0; i < GearCatalog.SlotCount; i++)
        {
            int amount = Inventory.GetScrolls((GearSlot)i);
            if (amount <= 0) continue;

            CreateScrollCell(content, (GearSlot)i, amount);
            scrollCount += amount;
        }

        inventoryLabel.text = "INVENTORY   " + items.Count + " items   " + scrollCount + " scrolls";
    }

    // Sorting rule for List.Sort: a negative number puts a first, a positive number puts b first
    static int CompareItems(OwnedGear a, OwnedGear b)
    {
        if (a.rarity != b.rarity) return b.rarity.CompareTo(a.rarity); // b before a = best rarity first

        GearSlot slotA = Inventory.TypeOf(a).slot;
        GearSlot slotB = Inventory.TypeOf(b).slot;
        if (slotA != slotB) return slotA.CompareTo(slotB);

        if (a.type != b.type) return string.CompareOrdinal(a.type, b.type);
        return b.level.CompareTo(a.level); // highest level first
    }

    // ---------- The item window ----------

    void ShowItem(OwnedGear item)
    {
        ClosePopup();

        GearType type = Inventory.TypeOf(item);
        Color rarityColor = GearCatalog.RarityColor(item.rarity);
        bool worn = Inventory.IsEquipped(item);

        // The dark full-screen panel blocks taps on the page behind it
        popup = UiBuilder.CreateFullScreenPanel("ItemPopup", overlayParent, new Color(0f, 0f, 0f, 0.75f));
        Transform window = UiBuilder.CreateBox("Window", popup.transform, windowColor, Vector2.zero, new Vector2(920f, 1060f)).transform;

        Image header = UiBuilder.CreateBox("Header", window, rarityColor, new Vector2(0f, 420f), new Vector2(920f, 220f));
        UiBuilder.CreateText("Title", header.transform, type.title.ToUpper(), 64, new Vector2(0f, 30f), new Vector2(900f, 90f));
        UiBuilder.CreateText("Rarity", header.transform, GearCatalog.RarityName(item.rarity) + "   " + GearCatalog.SlotName(type.slot), 36, new Vector2(0f, -55f), new Vector2(900f, 60f));

        int maxLevel = GearCatalog.MaxLevel(item.rarity);
        UiBuilder.CreateText("Level", window, "Level " + item.level + " / " + maxLevel, 48, new Vector2(0f, 230f), new Vector2(900f, 70f));

        Text bonus = UiBuilder.CreateText("Bonus", window, GearCatalog.FormatBonus(type.stat, Inventory.ValueOf(item)), 60, new Vector2(0f, 140f), new Vector2(900f, 80f));
        bonus.color = rarityColor;

        bool canLevelUp = false;
        if (Inventory.IsMaxLevel(item))
        {
            UiBuilder.CreateText("Max", window, "MAX LEVEL\nMerge to raise the limit", 40, new Vector2(0f, 0f), new Vector2(900f, 130f));
        }
        else
        {
            float nextValue = GearCatalog.Value(type, item.rarity, item.level + 1);
            UiBuilder.CreateText("Next", window, "Next level: " + GearCatalog.FormatBonus(type.stat, nextValue), 38, new Vector2(0f, 50f), new Vector2(900f, 60f));

            int scrollCost = GearCatalog.ScrollCost(item.level);
            int coinCost = GearCatalog.CoinCost(item.level);
            int scrolls = Inventory.GetScrolls(type.slot);
            string cost = "Needs " + scrollCost + " " + GearCatalog.SlotName(type.slot) + " scroll(s) (you have " + scrolls + ")\nand " + coinCost + " coins";
            UiBuilder.CreateText("Cost", window, cost, 34, new Vector2(0f, -50f), new Vector2(900f, 110f));

            canLevelUp = scrolls >= scrollCost && Wallet.Coins >= coinCost;
        }

        Vector2 buttonSize = new Vector2(400f, 150f);
        if (worn) UiBuilder.CreateButton("Unequip", window, "REMOVE", new Vector2(-220f, -250f), buttonSize, unavailableColor, () => Unequip(type.slot));
        else UiBuilder.CreateButton("Equip", window, "WEAR", new Vector2(-220f, -250f), buttonSize, goodButtonColor, () => Equip(item));

        Text levelUp = UiBuilder.CreateButton("LevelUp", window, "LEVEL UP", new Vector2(220f, -250f), buttonSize, canLevelUp ? goodButtonColor : unavailableColor, () => LevelUp(item));
        levelUp.fontSize = 56;

        UiBuilder.CreateButton("Close", window, "CLOSE", new Vector2(0f, -430f), new Vector2(500f, 120f), unavailableColor, ClosePopup);
    }

    void Equip(OwnedGear item)
    {
        Inventory.Equip(item);
        ClosePopup();
    }

    void Unequip(GearSlot slot)
    {
        Inventory.Unequip(slot);
        ClosePopup();
    }

    void LevelUp(OwnedGear item)
    {
        if (!Inventory.TryLevelUp(item)) return;

        SoundManager.Play(Sound.LevelUp);
        ShowItem(item); // build the window again with the new numbers
    }

    void ClosePopup()
    {
        if (popup != null) Destroy(popup);
    }

    void OpenMerge()
    {
        ClosePopup();
        gameObject.AddComponent<MergeScreen>().Open(overlayParent);
    }

    // ---------- Building blocks ----------

    SlotView CreateSlot(Transform parent, GearSlot slot, Vector2 position)
    {
        SlotView view = new SlotView();

        view.title = UiBuilder.CreateButton(slot + "Slot", parent, "", position, new Vector2(CellSize, CellSize), slotColor, () => TapSlot(slot));
        StyleCellText(view.title);
        view.background = view.title.transform.parent.GetComponent<Image>();
        view.level = CreateLevelText(view.background.transform);

        return view;
    }

    void TapSlot(GearSlot slot)
    {
        OwnedGear item = Inventory.GetEquipped(slot);
        if (item != null) ShowItem(item);
    }

    // One item in a grid: a square in its rarity color with its name and level.
    // "public static" so MergeScreen can use it too. Returns the square, so the caller can recolor it.
    public static Image CreateItemCell(Transform parent, OwnedGear item, UnityAction onTap)
    {
        Text title = UiBuilder.CreateButton("Item" + item.id, parent, Inventory.TypeOf(item).title, Vector2.zero, new Vector2(CellSize, CellSize), GearCatalog.RarityColor(item.rarity), onTap);
        StyleCellText(title);

        Image cell = title.transform.parent.GetComponent<Image>();
        CreateLevelText(cell.transform).text = "Lv " + item.level;
        return cell;
    }

    static void CreateScrollCell(Transform parent, GearSlot slot, int amount)
    {
        Image cell = UiBuilder.CreateBox(slot + "Scrolls", parent, ScrollPickup.ScrollColor, Vector2.zero, new Vector2(CellSize, CellSize));
        Color ink = new Color(0.35f, 0.22f, 0.1f);

        Text title = UiBuilder.CreateText("Title", cell.transform, GearCatalog.SlotName(slot) + "\nSCROLL", 28, new Vector2(0f, 20f), new Vector2(CellSize - 10f, 90f));
        title.color = ink;
        Text count = CreateLevelText(cell.transform);
        count.text = "x" + amount;
        count.color = ink;
    }

    // The item name: smaller than a normal button label, in the upper part of the square
    static void StyleCellText(Text text)
    {
        text.fontSize = 30;
        text.rectTransform.sizeDelta = new Vector2(CellSize - 10f, 100f);
        text.rectTransform.anchoredPosition = new Vector2(0f, 20f);
    }

    static Text CreateLevelText(Transform cell)
    {
        return UiBuilder.CreateText("Level", cell, "", 30, new Vector2(0f, -55f), new Vector2(CellSize - 10f, 50f));
    }
}
