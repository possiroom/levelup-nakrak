using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogViewer : MonoBehaviour
{
    [SerializeField] Image character1;
    [SerializeField] TextMeshProUGUI storyText;
    [SerializeField] TextMeshProUGUI speakerName;
    [SerializeField] Image endmark;

    // 
    [SerializeField] private float darker = 0.5f;
    [SerializeField] private float blinkSpeed = 0.5f;

    private float elapedTime;
    private bool portraitHiddenForTextOnly;
    private bool portraitEnabledBeforeTextOnly;
    private readonly Dictionary<string, Sprite> speakerPortraits = new Dictionary<string, Sprite>();
    [Header("Portrait Layout")]
    [SerializeField, Range(0.3f, 0.8f)] private float portraitScreenHeight = 0.56f;
    [SerializeField, Range(0.15f, 0.4f)] private float portraitScreenWidth = 0.28f;
    private Vector2 portraitLayoutSize;
    private Sprite portraitLayoutSprite;

    public void ChangePortrait(StoryData story)
    {
        if (character1 == null) return;
        Sprite portrait = story.portrait;
        if (portrait == null && story.activatedImage >= 0)
        {
            string resource = null;
            switch (story.charName)
            {
                case "조수": resource = "조수 기본"; break;
                case "탐정":
                case "주인공": resource = "탐정 기본"; break;
                case "주인":
                case "전당포 주인":
                case "C": resource = "전당포주인"; break;
                case "A": resource = "A"; break;
                case "B": resource = "B"; break;
            }
            if (resource != null && !speakerPortraits.TryGetValue(resource, out portrait))
            {
                var sprites = Resources.LoadAll<Sprite>("Portraits/" + resource);
                portrait = sprites.Length > 0 ? sprites[0] : null;
                speakerPortraits[resource] = portrait;
            }
        }

        // Narration and unknown speakers must not keep the previous speaker's face.
        character1.sprite = portrait;
        character1.type = Image.Type.Simple;
        character1.enabled = story.activatedImage >= 0 && portrait != null;
        character1.preserveAspect = true;
        character1.raycastTarget = false;
        character1.color = Color.white;
        LayoutPortrait();
    }

    private void LayoutPortrait()
    {
        if (character1 == null || character1.sprite == null) return;
        var parent = character1.rectTransform.parent as RectTransform;
        if (parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
        Vector2 size = parent.rect.size;
        if (size == portraitLayoutSize && portraitLayoutSprite == character1.sprite) return;
        portraitLayoutSize = size;
        portraitLayoutSprite = character1.sprite;

        // The old width-driven fitter reduced a full portrait to about 100 pixels.
        var fitter = character1.GetComponent<AspectRatioFitter>();
        if (fitter != null) fitter.enabled = false;
        var rect = character1.rectTransform;
        float aspect = character1.sprite.rect.width / character1.sprite.rect.height;
        float height = Mathf.Min(size.y * portraitScreenHeight, size.x * portraitScreenWidth / aspect);
        rect.anchorMin = rect.anchorMax = new Vector2(0.15f, 0.015f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(height * aspect, height);
        rect.localScale = Vector3.one;

        // Reserve the left side for the portrait so it cannot cover dialogue text.
        if (storyText != null)
        {
            var panel = storyText.rectTransform;
            while (panel != null && panel.parent != null && panel.parent != transform)
                panel = panel.parent as RectTransform;
            if (panel != null && panel.parent == transform && panel != transform)
            {
                panel.anchorMin = new Vector2(0.30f, panel.anchorMin.y);
                panel.anchorMax = new Vector2(0.98f, panel.anchorMax.y);
            }
        }
    }

    private void LateUpdate() => LayoutPortrait();


    public void ChangeStoryText(string text)
    {
        storyText.text = text;
    }

    public void ChangeName(string name)
    {
        speakerName.text = name;
    }

    // 추후 이미지로 선택으로 변경 (darker가 될 필요가 없어보임)
    public void ChangeActivateImage(int code)
    {
        // Negative portrait codes are text-only lines (for example the opening prologue).
        if (code < 0)
        {
            if (!portraitHiddenForTextOnly) portraitEnabledBeforeTextOnly = character1.enabled;
            portraitHiddenForTextOnly = true;
            character1.enabled = false;
            return;
        }
        if (portraitHiddenForTextOnly)
        {
            character1.enabled = portraitEnabledBeforeTextOnly;
            portraitHiddenForTextOnly = false;
        }
        switch (code)
        {
            case 1:
                character1.color = new Color(1, 1, 1, 1);
                break;

            case 2:
                character1.color = new Color(darker, darker, darker, 1);
                break;

            default: // 0 or other
                character1.color = new Color(darker, darker, darker, 1);
                break;
        }
    }

    public void ActivateEndMark(bool activate)
    {
        endmark.enabled = activate;
        elapedTime = 0f;
        endmark.color = new Color(1, 1, 1, 1);
    }

    private void Start()
    {
        elapedTime = 0f;
    }

    private void OnEnable()
    {
        ChangeName("");
        ChangeStoryText("");
        ChangeActivateImage(0);
        ActivateEndMark(false);
    }

    private void Update()
    {
        elapedTime += Time.deltaTime;
        if (elapedTime >= 2 * blinkSpeed)
        {
            elapedTime -= 2 * blinkSpeed;
            endmark.color = new Color(1, 1, 1, 1);
        } 
        else if (elapedTime >= blinkSpeed) 
        {
            if (endmark.color.a == 1) endmark.color = new Color(1, 1, 1, 0);
        }
    }
}
