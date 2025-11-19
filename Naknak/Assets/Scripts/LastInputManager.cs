using UnityEngine;

/// <summary>
/// 가로와 세로 입력의 마지막 입력을 처리하는 클래스
/// GetAxisRaw("Horizontal") or GetAxizRaw("Vertical")
/// </summary>
public class LastInputManager : MonoBehaviour
{
    private float _lastHorizontal = 0f;
    private float lastHorizontal
    {
        set
        {
            if (value != 0f) lastInputAxis = LastInputAxis.Horizontal;
            else if (_lastVertical != 0f) lastInputAxis = LastInputAxis.Vertical;
            _lastHorizontal = value;
        }
        get { return _lastHorizontal; }
    }

    private float _lastVertical = 0f;
    private float lastVertical
    {
        set
        {
            if (value != 0f) lastInputAxis = LastInputAxis.Vertical;
            else if (_lastHorizontal != 0f) lastInputAxis = LastInputAxis.Horizontal;
            _lastVertical = value;
        }
        get { return _lastVertical; }
    }
    enum LastInputAxis { Horizontal, Vertical }

    private LastInputAxis lastInputAxis = LastInputAxis.Horizontal;

    void Update()
    {
        // 가로 입력 처리
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) lastHorizontal = -1f;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) lastHorizontal = 1f;

        // Key Up
        if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.A))
        {
            // 만약 반대 키가 눌려있다면, 그 방향으로 전환
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) lastHorizontal = 1f;
            else lastHorizontal = 0f;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) lastHorizontal = -1f;
            else lastHorizontal = 0f;
        }

        // 세로 입력 처리
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) lastVertical = -1f;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) lastVertical = 1f;

        // Key Up
        if (Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.S))
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) lastVertical = 1f;
            else lastVertical = 0f;
        }
        if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W))
        {
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) lastVertical = -1f;
            else lastVertical = 0f;
        }
    }

    public float GetAxisRaw(string axisName)
    {
        if (axisName == "Horizontal")
        {
            return lastHorizontal;
        }
        else if (axisName == "Vertical")
        {
            return lastVertical;
        }
        return 0f;
    }

    public string GetLastInputAxis()
    {
        return lastInputAxis.ToString();
    }
}