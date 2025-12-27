using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선택지 UI View
/// </summary>
public class DialogSelectViewer : MonoBehaviour
{
    private static readonly Color ActivateColor = new Color(1, 1, 1, 1);
    private static readonly Color DeActivateColor = new Color(0.5f, 0.5f, 0.5f, 1);

    private int selectionCount;

    [SerializeField] private GameObject selectBox1;
    [SerializeField] private GameObject selectBox2;
    [SerializeField] private GameObject selectBox3;
    [SerializeField] private GameObject selectBox4;

    private List<GameObject> selectBoxes = new List<GameObject>();

    private List<Image> images = new List<Image>();
    private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();


    void Awake()
    {
        Debug.Log("[DialogSelectViewer] Initailized.");

        selectBoxes.Clear();
        images.Clear();
        texts.Clear();

        selectBoxes.Add(selectBox1);
        selectBoxes.Add(selectBox2);
        selectBoxes.Add(selectBox3);
        selectBoxes.Add(selectBox4);

        selectionCount = 0;

        foreach (var box in selectBoxes)
        {
            images.Add(box.GetComponent<Image>());
            texts.Add(box.transform.Find("Select_Text").GetComponent<TextMeshProUGUI>());
        }

        for (int i = 0; i < 4; i++)
        {
            selectBoxes[i].SetActive(false);
            texts[i].text = "";
        }
    }

    public void SelectActivate(List<StorySelectData> selects)
    {
        selectionCount = selects.Count;

        for (int i = 0; i < selectionCount; i++)
        {
            selectBoxes[i].SetActive(true);
            texts[i].text = selects[selectionCount - 1 - i].text;
        }
    }

    public void ChangeSelectIndex(int index)
    {
        // index는 1부터 시작, 화면 위에서 아래 순서
        if (index > selectionCount || index <= 0)
        {
            Debug.LogError("[DialogSelectViewer] invaild index");
            return;
        }

        int boxIndex = selectionCount - index + 1;
        OnlyActivateBox(boxIndex);
    }

    public void SelectDeactivate()
    {
        for (int i = 0; i < 4; i++)
        {
            selectBoxes[i].SetActive(false);
            texts[i].text = "";
        }
    }

    private void OnlyActivateBox(int index)
    {
        if (index > selectBoxes.Count || index <= 0)
        {
            Debug.LogWarning("[DialogSelectViewer] invaild index");
            return;
        }

        for (int i = 0; i < selectBoxes.Count; i++)
        {
            ActivateBox(i + 1, i + 1 == index);
        }
    }

    private void ActivateBox(int index, bool activate)
    {
        if (index > selectBoxes.Count || index <= 0)
        {
            Debug.LogError("[DialogSelectViewer] invaild index");
            return;
        }

        images[index - 1].color = activate ? ActivateColor : DeActivateColor;
        texts[index - 1].color = activate ? ActivateColor : DeActivateColor;
    }
}
