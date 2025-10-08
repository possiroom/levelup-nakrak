using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedTest : MonoBehaviour
{
    public TextMeshProUGUI text;

    public PlayerMoveController2 player;

    void Start()
    {
        text.text = player.moveDuration.ToString(); // 소숫점 2째짜리 까지만 반올림
    }

    public void OnClickUp()
    {
        player.moveDuration += 0.01f;
        text.text = player.moveDuration.ToString();
    }

    public void OnClickDown()
    {
        player.moveDuration -= 0.01f;
        text.text = player.moveDuration.ToString();
    }
}
