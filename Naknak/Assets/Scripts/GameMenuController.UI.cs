using System;
using System.Collections.Generic;
using System.IO;
using Naknak.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class GameMenuController
{
    private RectTransform screen;
    private RectTransform panel;
    private TMP_FontAsset menuFont;
    private EventSystem ownEventSystem;
    private readonly List<Button> buttons = new List<Button>();
    private readonly List<Action<int>> adjustments = new List<Action<int>>();
    private readonly List<InventoryItem> visibleItems = new List<InventoryItem>();
    private TextMeshProUGUI description;
    private TextMeshProUGUI footer;
    private ScrollRect itemScroll;
    private int selected;
    private bool keyItems;
    private Page optionsOrigin;
    private Page loadOrigin;
    private bool loadManual;
    private Page confirmOrigin;
    private string confirmation;
    private Action confirmed;
    private bool noticeOnly;
    private bool rebuilding;
    private static readonly Color WindowColor = new Color(0.018f, 0.023f, 0.032f, 0.96f);
    private static readonly Color SelectedColor = new Color(0.19f, 0.21f, 0.24f, 1);

    private void BuildCanvas()
    {
        if (configuration.font != null)
            menuFont = TMP_FontAsset.CreateFontAsset(configuration.font, 42, 5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
        var canvasObject = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        screen = Box(canvasObject.transform, "Menu Overlay", new Color(0, 0, 0, 0.76f), Vector2.zero, Vector2.one);
        panel = Box(screen, "Window", WindowColor, new Vector2(0.035f, 0.045f), new Vector2(0.965f, 0.955f), true);
        var eventObject = new GameObject("Menu Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        eventObject.transform.SetParent(transform, false);
        ownEventSystem = eventObject.GetComponent<EventSystem>();
        ownEventSystem.sendNavigationEvents = false;
        eventObject.AddComponent<MenuEventSystemOwner>();
    }

    private void EnsureMenuEventSystem()
    {
        if (ownEventSystem != null) ownEventSystem.GetComponent<MenuEventSystemOwner>().EnsureSingle();
    }

    private void ClearPage()
    {
        rebuilding = true;
        foreach (Transform child in panel) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        buttons.Clear(); adjustments.Clear(); visibleItems.Clear();
        description = null; footer = null; itemScroll = null;
        screen.gameObject.SetActive(true);
        EnsureMenuEventSystem();
    }

    private void ShowPage(Page next)
    {
        int previousSelection = page == next ? selected : 0;
        page = next;
        if (next == Page.Closed)
        {
            screen.gameObject.SetActive(false);
            EnsureMenuEventSystem();
            return;
        }
        ClearPage();
        switch (next)
        {
            case Page.Title: DrawTitle(); break;
            case Page.Menu: DrawMenu(); break;
            case Page.Items: DrawItems(); break;
            case Page.Options: DrawOptions(); break;
            case Page.Save: DrawSave(); break;
            case Page.Load: DrawLoad(); break;
            case Page.Confirm: DrawConfirmation(); break;
        }
        rebuilding = false;
        Focus(Mathf.Clamp(previousSelection, 0, Mathf.Max(0, buttons.Count - 1)));
    }

    private void DrawTitle()
    {
        Label(panel, "NAKNAK  /  MYSTERY ADVENTURE", 17, new Vector2(0.1f, 0.79f), new Vector2(0.9f, 0.85f), TextAlignmentOptions.Center).color = new Color(0.6f, 0.64f, 0.7f);
        Label(panel, configuration.gameTitle, 76, new Vector2(0.1f, 0.57f), new Vector2(0.9f, 0.78f), TextAlignmentOptions.Center);
        string[] entries = { "새 게임", "이어하기", "옵션", "종료" };
        Action[] actions = { BeginNewGame, () => OpenLoad(Page.Title),
            () => { optionsOrigin = Page.Title; ShowPage(Page.Options); }, () => Confirm("게임을 종료할까요?", Quit) };
        bool anySave = false;
        for (int slot = 1; slot <= GameCheckpointStore.SlotCount; slot++)
            if (GameCheckpointStore.TryRead(slot, out _, out _) || GameCheckpointStore.TryReadManual(slot, out _, out _)) anySave = true;
        for (int i = 0; i < entries.Length; i++)
        {
            float top = 0.51f - i * 0.09f;
            var button = Command(entries[i], actions[i], new Vector2(0.33f, top - 0.072f), new Vector2(0.67f, top));
            if (i == 1 && !anySave) { button.interactable = false; button.GetComponentInChildren<TextMeshProUGUI>().color = Color.gray; }
        }
        Footer("↑↓ 선택    Z / Enter 확인");
    }

    private void DrawMenu()
    {
        Heading("메뉴", "잠시 숨을 고릅니다.");
        string[] entries = { "아이템", "옵션", "세이브", "로드", "타이틀로" };
        Action[] actions = { () => ShowPage(Page.Items), () => { optionsOrigin = Page.Menu; ShowPage(Page.Options); },
            () => ShowPage(Page.Save), () => OpenLoad(Page.Menu),
            () => Confirm("타이틀로 돌아갈까요?\n마지막 저장 이후의 진행은 사라집니다.",
                () => StartCoroutine(ChangeSession(configuration.titleScene, null))) };
        for (int i = 0; i < entries.Length; i++)
        {
            float top = 0.70f - i * 0.105f;
            Command(entries[i], actions[i], new Vector2(0.32f, top - 0.083f), new Vector2(0.68f, top));
        }
        Footer("↑↓ 선택    Z / Enter 확인    X / Esc 게임으로");
    }

    private void DrawItems()
    {
        var descriptionWindow = Box(panel, "Description", new Color(0, 0, 0, 0.25f), new Vector2(0.02f, 0.80f), new Vector2(0.98f, 0.98f), true);
        description = Label(descriptionWindow, itemSelectionPrompt ?? "소지한 아이템이 없습니다.", 25, new Vector2(0.025f, 0.08f), new Vector2(0.975f, 0.92f));
        // Tabs are mouse-clickable without becoming item-list navigation entries.
        Tab("아이템", false, 0.02f, 0.48f);
        Tab("중요 아이템", true, 0.52f, 0.98f);
        var viewport = Box(panel, "Item Viewport", Color.clear, new Vector2(0.02f, 0.13f), new Vector2(0.98f, 0.67f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        itemScroll = viewport.gameObject.AddComponent<ScrollRect>();
        itemScroll.viewport = viewport; itemScroll.content = content;
        itemScroll.horizontal = false; itemScroll.movementType = ScrollRect.MovementType.Clamped;
        itemScroll.scrollSensitivity = 35;
        visibleItems.AddRange(inventory.Items(keyItems));
        content.sizeDelta = new Vector2(0, Mathf.Max(70, Mathf.CeilToInt(visibleItems.Count / 2f) * 70));
        if (visibleItems.Count == 0)
            Label(viewport, "소지한 아이템이 없습니다.", 24, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.85f), TextAlignmentOptions.Center).color = Color.gray;
        for (int i = 0; i < visibleItems.Count; i++)
        {
            var item = visibleItems[i];
            int index = i;
            var button = Command("", () => ActivateInventoryItem(index), Vector2.zero, Vector2.one, content);
            var rect = (RectTransform)button.transform;
            int column = i % 2;
            rect.anchorMin = new Vector2(column * 0.5f, 1); rect.anchorMax = new Vector2((column + 1) * 0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1); rect.offsetMin = new Vector2(5, -(i / 2 + 1) * 70 + 5); rect.offsetMax = new Vector2(-5, -(i / 2) * 70 - 5);
            if (item.Icon != null)
            {
                var icon = Box(rect, "Icon", Color.white, new Vector2(0.02f, 0.12f), new Vector2(0.12f, 0.88f)).GetComponent<Image>();
                icon.sprite = item.Icon; icon.preserveAspect = true; icon.raycastTarget = false;
            }
            else
            {
                var placeholder = Label(rect, "◆", 25, new Vector2(0.02f, 0.12f), new Vector2(0.12f, 0.88f), TextAlignmentOptions.Center);
                placeholder.color = item.IsKeyItem ? new Color(0.78f, 0.7f, 0.45f) : new Color(0.64f, 0.7f, 0.76f);
            }
            Label(rect, item.DisplayName, 26, new Vector2(0.14f, 0.05f), new Vector2(0.82f, 0.95f));
            Label(rect, ": " + inventory.Count(item.Id), 25, new Vector2(0.83f, 0.05f), new Vector2(0.97f, 0.95f), TextAlignmentOptions.Right);
        }
        Footer(itemSelected != null ? "사용할 아이템 선택    Z / Enter 사용    X / Esc 취소" : "↑↓←→ 선택    Q / E / Tab 분류 변경    X / Esc 뒤로");
        Command("뒤로", ReturnFromItems, new Vector2(0.84f, 0.02f), new Vector2(0.98f, 0.09f));
        // Keep the back action in keyboard navigation too, including an empty inventory.
    }

    private void Tab(string label, bool key, float left, float right)
    {
        var rect = Box(panel, label + " Tab", keyItems == key ? SelectedColor : WindowColor,
            new Vector2(left, 0.70f), new Vector2(right, 0.78f), true);
        Label(rect, label, 25, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        rect.gameObject.AddComponent<Button>().onClick.AddListener(() => { keyItems = key; selected = 0; ShowPage(Page.Items); });
    }

    private void DrawOptions()
    {
        Heading("옵션", "설정은 타이틀과 게임에서 함께 사용합니다.");
        if (options == null)
        { Label(panel, "옵션 파일을 읽지 못했습니다.", 26, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f)); }
        else
        {
            Option("전체 음량", () => Mathf.RoundToInt(options.Volume * 100) + "%", options.AdjustVolume, 0.67f);
            Option("해상도", () => options.Resolutions[options.ResolutionIndex].x + " × " + options.Resolutions[options.ResolutionIndex].y, options.AdjustResolution, 0.55f);
            Option("전체 화면", () => options.Fullscreen ? "켬" : "끔", _ => options.ToggleFullscreen(), 0.43f);
            Command("기본값 복원", () => { options.Reset(); ShowPage(Page.Options); }, new Vector2(0.15f, 0.18f), new Vector2(0.47f, 0.27f));
        }
        Command("적용하고 돌아가기", SaveOptions, new Vector2(0.53f, 0.18f), new Vector2(0.85f, 0.27f));
        Footer("↑↓ 선택    ←→ 조절    X / Esc 적용하고 뒤로");
    }

    private void Option(string name, Func<string> value, Action<int> adjust, float top)
    {
        Command(name + "    ‹  " + value() + "  ›", () => { adjust(1); ShowPage(Page.Options); },
            new Vector2(0.15f, top - 0.09f), new Vector2(0.85f, top), null,
            step => { adjust(step); ShowPage(Page.Options); });
    }

    private void SaveOptions()
    {
        try { options?.Save(); ShowPage(optionsOrigin); }
        catch (Exception exception) { Debug.LogWarning(exception); Notice("설정을 저장하지 못했습니다. 저장 폴더를 확인해 주세요."); }
    }

    private void OpenLoad(Page origin)
    {
        loadOrigin = origin;
        loadManual = false;
        for (int slot = 1; slot <= GameCheckpointStore.SlotCount; slot++)
            if (GameCheckpointStore.TryReadManual(slot, out _, out _)) { loadManual = true; break; }
        ShowPage(Page.Load);
    }

    private void DrawSave()
    {
        Heading("세이브", "직접 저장한 기록은 자동 저장으로 덮어쓰지 않습니다.");
        DrawSaveSlots(true, true);
        Command("뒤로", () => ShowPage(Page.Menu), new Vector2(0.65f, 0.08f), new Vector2(0.90f, 0.16f));
        Footer("↑↓ 선택    Z / Enter 저장    X / Esc 뒤로");
    }

    private void DrawLoad()
    {
        Heading("로드", "직접 저장한 기록과 자동 저장 기록을 선택할 수 있습니다.");
        LoadTab("직접 저장", true, 0.10f, 0.48f);
        LoadTab("자동 저장", false, 0.52f, 0.90f);
        DrawSaveSlots(loadManual, false);
        Command("뒤로", () => ShowPage(loadOrigin), new Vector2(0.65f, 0.08f), new Vector2(0.90f, 0.16f));
        Footer("↑↓ 선택    Q / E / Tab 분류 변경    Z / Enter 로드    X / Esc 뒤로");
    }

    private void LoadTab(string label, bool manual, float left, float right)
    {
        var rect = Box(panel, label, loadManual == manual ? SelectedColor : WindowColor,
            new Vector2(left, 0.69f), new Vector2(right, 0.76f), true);
        Label(rect, label, 24, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        rect.gameObject.AddComponent<Button>().onClick.AddListener(() => { loadManual = manual; selected = 0; ShowPage(Page.Load); });
    }

    private void DrawSaveSlots(bool manual, bool saving)
    {
        for (int slot = 1; slot <= GameCheckpointStore.SlotCount; slot++)
        {
            int target = slot;
            GameCheckpoint data;
            string error;
            bool valid = manual ? GameCheckpointStore.TryReadManual(slot, out data, out error) : GameCheckpointStore.TryRead(slot, out data, out error);
            string text = (manual ? "슬롯 " : "자동 기록 ") + slot + "    ";
            if (valid)
            {
                DateTimeOffset.TryParse(data.savedAt, out var date);
                text += date.ToLocalTime().ToString("yyyy.MM.dd  HH:mm") + "\n" + data.scene + "    " + TimeSpan.FromSeconds(data.playSeconds).ToString(@"hh\:mm\:ss");
            }
            else text += error == null ? "비어 있음" : "읽을 수 없는 기록";
            float top = (saving ? 0.71f : 0.65f) - (slot - 1) * 0.155f;
            var button = Command(text, () => { if (saving) BeginSave(target); else BeginLoad(target, manual); },
                new Vector2(0.10f, top - 0.13f), new Vector2(0.90f, top));
            button.interactable = saving || valid;
            if (!button.interactable) button.GetComponentInChildren<TextMeshProUGUI>().color = Color.gray;
        }
    }

    private void Confirm(string text, Action action)
    {
        confirmOrigin = page; confirmation = text; confirmed = action; noticeOnly = false; ShowPage(Page.Confirm);
        Focus(1); // Destructive actions default to cancel.
    }

    private void Notice(string text)
    {
        if (page != Page.Confirm) confirmOrigin = page;
        confirmation = text; confirmed = null; noticeOnly = true; ShowPage(Page.Confirm);
    }

    private void DrawConfirmation()
    {
        Label(panel, confirmation, 28, new Vector2(0.12f, 0.45f), new Vector2(0.88f, 0.72f), TextAlignmentOptions.Center);
        if (!noticeOnly) Command("확인", () => confirmed?.Invoke(), new Vector2(0.22f, 0.25f), new Vector2(0.47f, 0.35f));
        Command(noticeOnly ? "돌아가기" : "취소", () => ShowPage(confirmOrigin), new Vector2(0.53f, 0.25f), new Vector2(0.78f, 0.35f));
        Footer("←→ 선택    Z / Enter 확인    X / Esc 취소");
    }

    private void HandleMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X) || (page == Page.Items && Input.GetKeyDown(KeyCode.I)))
        {
            switch (page)
            {
                case Page.Menu: Resume(); break;
                case Page.Items: ReturnFromItems(); break;
                case Page.Options: SaveOptions(); break;
                case Page.Save: ShowPage(Page.Menu); break;
                case Page.Load: ShowPage(loadOrigin); break;
                case Page.Confirm: ShowPage(confirmOrigin); break;
            }
            return;
        }
        if (page == Page.Items && (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab)))
        { keyItems = !keyItems; selected = 0; ShowPage(Page.Items); return; }
        if (page == Page.Load && (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab)))
        { loadManual = !loadManual; selected = 0; ShowPage(Page.Load); return; }
        int step = 0;
        if (Input.GetKeyDown(KeyCode.UpArrow)) step = page == Page.Items ? -2 : -1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) step = page == Page.Items ? 2 : 1;
        int horizontal = Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : Input.GetKeyDown(KeyCode.RightArrow) ? 1 : 0;
        if (horizontal != 0)
        {
            if (selected < adjustments.Count && adjustments[selected] != null) { adjustments[selected](horizontal); return; }
            if (page == Page.Items || page == Page.Confirm) step = horizontal;
        }
        if (step != 0) MoveFocus(step);
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            if (selected < buttons.Count && buttons[selected].interactable) buttons[selected].onClick.Invoke();
    }

    private void MoveFocus(int step)
    {
        if (buttons.Count == 0) return;
        int index = (selected + step % buttons.Count + buttons.Count) % buttons.Count;
        for (int attempts = 0; attempts < buttons.Count; attempts++)
        {
            if (buttons[index].interactable) { Focus(index); return; }
            index = (index + (step < 0 ? -1 : 1) + buttons.Count) % buttons.Count;
        }
    }

    private void Focus(int index)
    {
        if (rebuilding || buttons.Count == 0) return;
        selected = Mathf.Clamp(index, 0, buttons.Count - 1);
        if (!buttons[selected].interactable) { MoveFocus(1); return; }
        for (int i = 0; i < buttons.Count; i++)
        {
            var image = buttons[i].GetComponent<Image>();
            image.color = i == selected ? SelectedColor : new Color(0, 0, 0, 0.08f);
            var outline = buttons[i].GetComponent<Outline>();
            if (outline != null) outline.effectColor = i == selected ? Color.white : new Color(1, 1, 1, 0.22f);
        }
        if (page == Page.Items && selected < visibleItems.Count)
        {
            description.text = visibleItems[selected].Description;
            if (string.IsNullOrWhiteSpace(description.text)) description.text = visibleItems[selected].DisplayName;
            if (itemSelected != null) description.text = itemSelectionPrompt + "\n" + description.text;
            Canvas.ForceUpdateCanvases();
            int row = selected / 2;
            float height = itemScroll.viewport.rect.height;
            float maxScroll = Mathf.Max(0, itemScroll.content.rect.height - height);
            float offset = itemScroll.content.anchoredPosition.y;
            if (row * 70 < offset) offset = row * 70;
            else if ((row + 1) * 70 > offset + height) offset = (row + 1) * 70 - height;
            itemScroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, maxScroll));
        }
    }

    private Button Command(string text, Action action, Vector2 min, Vector2 max, Transform parent = null, Action<int> adjust = null)
    {
        var rect = Box(parent != null ? parent : panel, text.Length > 0 ? text : "Item", Color.clear, min, max, true);
        var label = Label(rect, text, 27, new Vector2(0.025f, 0.03f), new Vector2(0.975f, 0.97f), TextAlignmentOptions.Center);
        var button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => action());
        int index = buttons.Count;
        rect.gameObject.AddComponent<MenuPointerFocus>().OnFocus = () => Focus(index);
        buttons.Add(button); adjustments.Add(adjust);
        return button;
    }

    private void Heading(string title, string subtitle)
    {
        Label(panel, title, 38, new Vector2(0.055f, 0.84f), new Vector2(0.95f, 0.95f));
        Label(panel, subtitle, 20, new Vector2(0.055f, 0.77f), new Vector2(0.95f, 0.84f)).color = new Color(0.65f, 0.68f, 0.72f);
    }

    private void Footer(string text)
    {
        footer = Label(panel, text, 18, new Vector2(0.035f, 0.025f), new Vector2(0.81f, 0.085f));
        footer.color = new Color(0.65f, 0.68f, 0.72f);
    }

    private static RectTransform Box(Transform parent, string name, Color color, Vector2 min, Vector2 max, bool border = false)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.GetComponent<Image>().color = color;
        if (border) { var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0.84f, 0.87f, 0.91f, 0.85f); outline.effectDistance = new Vector2(1.5f, -1.5f); }
        return rect;
    }

    private TextMeshProUGUI Label(Transform parent, string text, int size, Vector2 min, Vector2 max, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var rect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (menuFont != null) label.font = menuFont;
        label.text = text; label.fontSize = size; label.color = new Color(0.91f, 0.92f, 0.94f);
        label.alignment = alignment; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
