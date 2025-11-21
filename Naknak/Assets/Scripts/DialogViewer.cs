using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogViewer : MonoBehaviour
{
    [SerializeField] Image character1;
    [SerializeField] Image character2;
    [SerializeField] TextMeshProUGUI storyText;
    [SerializeField] TextMeshProUGUI speakerName;

    [SerializeField] float darker = 0.5f;


    public void ChangeStoryText(string text)
    {
        storyText.text = text;
    }

    public void ChangeName(string name)
    {
        speakerName.text = name;
    }

    public void ChangeActivateImage(int code)
    {
        switch (code)
        {
            case 1:
                character1.color = new Color(1, 1, 1, 1);
                character2.color = new Color(darker, darker, darker, 1);
                break;

            case 2:
                character1.color = new Color(darker, darker, darker, 1);
                character2.color = new Color(1, 1, 1, 1);
                break;

            default: // 0 or other
                character1.color = new Color(darker, darker, darker, 1);
                character2.color = new Color(darker, darker, darker, 1);
                break;
        }
    }
}
