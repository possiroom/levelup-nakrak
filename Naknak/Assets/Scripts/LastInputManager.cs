using UnityEngine;

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

    private enum LastInputAxis { Horizontal, Vertical }

    private LastInputAxis lastInputAxis = LastInputAxis.Horizontal;
    private bool _wasIgnoring = false;
    private float ignoreInputTime = 0f;
    private bool suppressWasdMovement = false;

    private void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay)
        {
            lastHorizontal = 0f;
            lastVertical = 0f;
            return;
        }

        if (ignoreInputTime > 0f)
        {
            ignoreInputTime -= Time.deltaTime;
            _wasIgnoring = true;
            lastHorizontal = 0f;
            lastVertical = 0f;
            return;
        }

        if (_wasIgnoring)
        {
            _wasIgnoring = false;
            RefreshDirectionalStateFromHeldKeys();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || IsWasdKeyDown(KeyCode.A)) lastHorizontal = -1f;
        if (Input.GetKeyDown(KeyCode.RightArrow) || IsWasdKeyDown(KeyCode.D)) lastHorizontal = 1f;

        if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.A))
        {
            if (IsRightHeld()) lastHorizontal = 1f;
            else lastHorizontal = 0f;
        }

        if (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.D))
        {
            if (IsLeftHeld()) lastHorizontal = -1f;
            else lastHorizontal = 0f;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || IsWasdKeyDown(KeyCode.S)) lastVertical = -1f;
        if (Input.GetKeyDown(KeyCode.UpArrow) || IsWasdKeyDown(KeyCode.W)) lastVertical = 1f;

        if (Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.S))
        {
            if (IsUpHeld()) lastVertical = 1f;
            else lastVertical = 0f;
        }

        if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W))
        {
            if (IsDownHeld()) lastVertical = -1f;
            else lastVertical = 0f;
        }
    }

    public float GetAxisRaw(string axisName)
    {
        if (axisName == "Horizontal")
            return lastHorizontal;

        if (axisName == "Vertical")
            return lastVertical;

        return 0f;
    }

    public string GetLastInputAxis()
    {
        return lastInputAxis.ToString();
    }

    public bool GetKeyDownInteract()
    {
        if (ignoreInputTime > 0f) return false;
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
        ignoreInputTime = Mathf.Max(ignoreInputTime, time);
    }

    public void SetSuppressWasdMovement(bool suppress)
    {
        if (suppressWasdMovement == suppress)
            return;

        suppressWasdMovement = suppress;
        RefreshDirectionalStateFromHeldKeys();
    }

    private bool IsWasdKeyDown(KeyCode key)
    {
        return !suppressWasdMovement && Input.GetKeyDown(key);
    }

    private bool IsWasdKeyHeld(KeyCode key)
    {
        return !suppressWasdMovement && Input.GetKey(key);
    }

    private bool IsLeftHeld()
    {
        return Input.GetKey(KeyCode.LeftArrow) || IsWasdKeyHeld(KeyCode.A);
    }

    private bool IsRightHeld()
    {
        return Input.GetKey(KeyCode.RightArrow) || IsWasdKeyHeld(KeyCode.D);
    }

    private bool IsDownHeld()
    {
        return Input.GetKey(KeyCode.DownArrow) || IsWasdKeyHeld(KeyCode.S);
    }

    private bool IsUpHeld()
    {
        return Input.GetKey(KeyCode.UpArrow) || IsWasdKeyHeld(KeyCode.W);
    }

    private void RefreshDirectionalStateFromHeldKeys()
    {
        bool leftHeld = IsLeftHeld();
        bool rightHeld = IsRightHeld();
        bool downHeld = IsDownHeld();
        bool upHeld = IsUpHeld();

        if (leftHeld && !rightHeld) lastHorizontal = -1f;
        else if (rightHeld && !leftHeld) lastHorizontal = 1f;
        else lastHorizontal = 0f;

        if (downHeld && !upHeld) lastVertical = -1f;
        else if (upHeld && !downHeld) lastVertical = 1f;
        else lastVertical = 0f;
    }
}
