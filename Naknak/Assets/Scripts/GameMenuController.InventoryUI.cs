using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class GameMenuController
{
    private const int InventoryColumns = 4;
    private const float InventoryRowHeight = 142;
    private int inventoryCategory;
    private int lastInventorySelection;
    private Image itemPreview;
    private TextMeshProUGUI itemHeading;
    private bool IsInventoryPage => page == Page.Menu || page == Page.Items;

    private void DrawInventoryMenu()
    {
        bool presenting = itemSelected != null;
        var sidebar = Box(panel, "Menu Sidebar", new Color(0, 0, 0, .22f),
            new Vector2(.015f, .10f), new Vector2(.25f, .97f), true);
        Label(sidebar, presenting ? "증거 제시" : "메뉴", 34, new Vector2(.08f, .85f), new Vector2(.92f, .96f));
        Label(sidebar, presenting ? "제시할 아이템을 선택하세요." : "잠시 숨을 고릅니다.", 18,
            new Vector2(.08f, .75f), new Vector2(.92f, .85f));

        var detail = Box(panel, "Selected Item", new Color(0, 0, 0, .24f),
            new Vector2(.28f, .70f), new Vector2(.975f, .97f), true);
        itemPreview = Box(detail, "Item Preview", Color.clear, new Vector2(.025f, .12f), new Vector2(.22f, .88f)).GetComponent<Image>();
        itemPreview.preserveAspect = true;
        itemPreview.raycastTarget = false;
        itemHeading = Label(detail, "아이템", 29, new Vector2(.26f, .65f), new Vector2(.97f, .94f));
        description = Label(detail, "보유한 아이템이 없습니다.", 22, new Vector2(.26f, .10f), new Vector2(.97f, .65f));

        string[] categories = { "전체", "일반 아이템", "중요 아이템" };
        for (int i = 0; i < categories.Length; i++)
        {
            int category = i;
            var tab = Box(panel, "Category " + i, inventoryCategory == i ? SelectedColor : WindowColor,
                new Vector2(.28f + i * .232f, .62f), new Vector2(.50f + i * .232f, .68f), true);
            Label(tab, categories[i], 21, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            tab.gameObject.AddComponent<Button>().onClick.AddListener(() => ChangeInventoryCategory(category));
        }
        if (inventoryCategory != 2) visibleItems.AddRange(inventory.Items(false));
        if (inventoryCategory != 1) visibleItems.AddRange(inventory.Items(true));

        var viewport = Box(panel, "Item Viewport", Color.clear, new Vector2(.28f, .12f), new Vector2(.975f, .60f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = new GameObject("Inventory Grid", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
        int slots = Mathf.Max(8, Mathf.CeilToInt(visibleItems.Count / 4f) * 4);
        content.sizeDelta = new Vector2(0, slots / 4 * InventoryRowHeight);
        itemScroll = viewport.gameObject.AddComponent<ScrollRect>();
        itemScroll.viewport = viewport; itemScroll.content = content;
        itemScroll.horizontal = false; itemScroll.movementType = ScrollRect.MovementType.Clamped;
        itemScroll.scrollSensitivity = 45;
        for (int i = 0; i < slots; i++)
        {
            RectTransform slot;
            if (i < visibleItems.Count)
            {
                int index = i;
                var item = visibleItems[i];
                slot = (RectTransform)Command("", () => ActivateInventoryItem(index), Vector2.zero, Vector2.one, content).transform;
                slot.name = "Inventory Slot " + item.Id;
                var icon = Box(slot, "Icon", item.Icon != null ? Color.white : Color.clear,
                    new Vector2(.19f, .29f), new Vector2(.81f, .93f)).GetComponent<Image>();
                icon.sprite = item.Icon; icon.preserveAspect = true; icon.raycastTarget = false;
                if (item.Icon == null && !item.HideIconPlaceholder) Label(slot, "◇", 32, new Vector2(.2f, .3f), new Vector2(.8f, .9f), TextAlignmentOptions.Center);
                Label(slot, item.DisplayName, 18, new Vector2(.04f, .03f), new Vector2(.96f, .27f), TextAlignmentOptions.Center);
                Label(slot, "×" + inventory.Count(item.Id), 17, new Vector2(.65f, .74f), new Vector2(.94f, .96f), TextAlignmentOptions.TopRight);
            }
            else slot = Box(content, "Empty Slot", new Color(0, 0, 0, .12f), Vector2.zero, Vector2.one, true);
            int column = i % InventoryColumns, row = i / InventoryColumns;
            slot.anchorMin = new Vector2(column / 4f, 1); slot.anchorMax = new Vector2((column + 1) / 4f, 1);
            slot.pivot = new Vector2(.5f, 1);
            slot.offsetMin = new Vector2(7, -(row + 1) * InventoryRowHeight + 7);
            slot.offsetMax = new Vector2(-7, -row * InventoryRowHeight - 7);
        }
        if (visibleItems.Count > 0) UpdateItemDetails(0);

        string[] entries = presenting ? new[] { "제시 취소" } : new[] { "옵션", "로드", "저장", "타이틀로" };
        Action[] actions = presenting ? new Action[] { ReturnFromItems } : new Action[] {
            () => { optionsOrigin = Page.Menu; ShowPage(Page.Options); },
            () => OpenLoad(Page.Menu), () => ShowPage(Page.Save),
            () => Confirm("타이틀로 돌아갈까요?\n마지막 저장 이후의 진행은 사라집니다.",
                () => StartCoroutine(ChangeSession(configuration.titleScene, null))) };
        for (int i = 0; i < entries.Length; i++)
        {
            float top = .69f - i * .14f;
            Command(entries[i], actions[i], new Vector2(.08f, top - .105f), new Vector2(.92f, top), sidebar);
        }
        Footer(presenting ? "방향키: 선택    Z / Enter: 제시    Esc / X: 취소" :
            "방향키: 선택    Z / Enter: 확인    Q / E: 분류    Esc / X: 닫기");
    }

    private void ChangeInventoryCategory(int category)
    {
        inventoryCategory = (category + 3) % 3;
        selected = 0; lastInventorySelection = 0;
        ShowPage(page);
    }

    private void UpdateItemDetails(int index)
    {
        if (index < 0 || index >= visibleItems.Count) return;
        lastInventorySelection = index;
        var item = visibleItems[index];
        itemHeading.text = item.DisplayName + "  ×" + inventory.Count(item.Id);
        description.text = (itemSelected != null ? itemSelectionPrompt + "\n" : "") + item.Description;
        itemPreview.sprite = item.Icon;
        itemPreview.color = item.Icon != null ? Color.white : Color.clear;
        Canvas.ForceUpdateCanvases();
        int row = index / InventoryColumns;
        float height = itemScroll.viewport.rect.height;
        float maxScroll = Mathf.Max(0, itemScroll.content.rect.height - height);
        float offset = itemScroll.content.anchoredPosition.y;
        if (row * InventoryRowHeight < offset) offset = row * InventoryRowHeight;
        else if ((row + 1) * InventoryRowHeight > offset + height) offset = (row + 1) * InventoryRowHeight - height;
        itemScroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, maxScroll));
    }

    private void NavigateInventory(int horizontal, int vertical)
    {
        int count = visibleItems.Count;
        if (selected >= count)
        {
            if (horizontal > 0 && count > 0) Focus(Mathf.Clamp(lastInventorySelection, 0, count - 1));
            else if (vertical != 0) Focus(count + (selected - count + vertical + buttons.Count - count) % (buttons.Count - count));
            return;
        }
        if (horizontal < 0 && selected % InventoryColumns == 0) { Focus(count); return; }
        int target = selected + horizontal + vertical * InventoryColumns;
        if (horizontal != 0 && target / InventoryColumns != selected / InventoryColumns) return;
        Focus(Mathf.Clamp(target, 0, count - 1));
    }
}
