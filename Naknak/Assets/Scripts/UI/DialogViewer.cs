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
