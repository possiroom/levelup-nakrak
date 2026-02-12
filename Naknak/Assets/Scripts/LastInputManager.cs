using System.Reflection;
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
    private bool _wasIgnoring = false;
    private float ignoreInputTime = 0f;

    void Update()
    {
        if (ignoreInputTime > 0f)
        {
            ignoreInputTime -= Time.deltaTime;
            _wasIgnoring = true;
            lastHorizontal = 0f;
            lastVertical = 0f;
            return;
        }

        // 방금까지 무시 상태였다가, 이번 프레임에 풀린 경우
        if (_wasIgnoring)
        {
            _wasIgnoring = false;

            // 무시가 풀리는 시점에 "현재 눌림"을 1회 Down처럼 반영
            bool leftHeld  = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            bool rightHeld = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            bool downHeld  = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            bool upHeld    = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);

            // 가로: 동시에 눌리면(좌+우) 기존 로직상 마지막 입력이 뭐였는지 없으니 우선순위 하나 선택
            if (leftHeld && !rightHeld) lastHorizontal = -1f;
            else if (rightHeld && !leftHeld) lastHorizontal = 1f;

            // 세로
            if (downHeld && !upHeld) lastVertical = -1f;
            else if (upHeld && !downHeld) lastVertical = 1f;

            // 이 프레임에는 GetKeyDown/Up 처리까지 하면 중복될 수 있으니 종료
            return;
        }

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

    public bool GetKeyDownInteract()
    {
        return Input.GetKeyDown(KeyCode.Z);
    }

    public bool GetKeyDownJump()
    {
        if (ignoreInputTime > 0f) return false;
        return Input.GetKeyDown(KeyCode.Space);
    }

    public bool GetKeyRun()
    {
        if (ignoreInputTime > 0f) return false;
        return Input.GetKey(KeyCode.LeftShift);
    }

    public void IgnoreInput(float time)
    {
        ignoreInputTime = time;
    }
}