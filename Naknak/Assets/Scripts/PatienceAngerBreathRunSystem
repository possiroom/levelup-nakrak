using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class PatienceAngerBreathRunSystem : MonoBehaviour
{
    public static PatienceAngerBreathRunSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerMoveController3 playerCtrl;
    [SerializeField] private LastInputManager inputManager;

    [Header("Phase")]
    [SerializeField] private int currentPhase = 5;

    [Header("Stamina")]
    [SerializeField] private float staminaMax = 10f;

    // 뛰는 중 초당 감소량
    [SerializeField] private float staminaRunUse = 1f;

    // 걷는 중 초당 회복량
    [SerializeField] private float staminaWalkRecovery = 1f;

    // 멈춘 상태 초당 회복량
    [SerializeField] private float staminaIdleRecovery = 2f;

    [Header("Breath")]
    [SerializeField] private float breathTriggerTime = 2f;
    [SerializeField] private float breathReducePerSecond = 1f;

    [Header("Vision Debuff Test")]
    [SerializeField] private float visionDebuffTime = 0f;
    [SerializeField] private bool debugKeyTest = true;

    [Header("Stamina UI")]
    [SerializeField] private Slider staminaSlider;

    [SerializeField] private Image staminaBackgroundImage;

    [Header("Stamina Warning Glow")]
    [SerializeField]
    private Color warningColor =
        new Color(1f, 0f, 0f, 1f);

    [SerializeField] private float warningBlinkTime = 0.6f;
    [SerializeField] private float warningBlinkSpeed = 14f;

    [Header("Glow Size")]

    [SerializeField] private float borderSize = 1.3f;

    [SerializeField] private float outerGlowSize = 3f;

    [SerializeField] private float borderMaxAlpha = 0.75f;
    [SerializeField] private float outerGlowMaxAlpha = 0.22f;

    private float stamina;
    private float idleTimer;
    private float warningTimer;

    private float originalRunDuration;

    private Image borderGlowImage;
    private Image outerGlowImage;

    private Outline borderOutline;
    private Outline outerGlowOutline;

    public bool IsRunning { get; private set; }
    public bool IsBreathing { get; private set; }

    public float Stamina => stamina;
    public float VisionDebuffTime => visionDebuffTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (playerCtrl == null)
        {
            playerCtrl = GetComponent<PlayerMoveController3>();
        }

        if (inputManager == null)
        {
            inputManager = GetComponent<LastInputManager>();
        }

        if (playerCtrl != null)
        {
            originalRunDuration = playerCtrl.runDuration;
        }

        stamina = staminaMax;

        FindStaminaBackground();

        CreateExactBorderGlow();

        UpdateStaminaUI();
    }

    private void Update()
    {
        if (playerCtrl == null || inputManager == null)
        {
            return;
        }

        if (debugKeyTest)
        {
            DebugTestInput();
        }

        UpdateRunSpeed();
        UpdateStamina();
        UpdateBreath();
        UpdateStaminaUI();
        UpdateWarningGlow();
    }

    // 달리기 속도 적용
    private void UpdateRunSpeed()
    {
        bool runKey = inputManager.GetKeyRun();
        bool isJumping = playerCtrl.GetIsJumping();

        bool canUseRunSpeed =
            currentPhase >= 5 &&
            runKey &&
            !isJumping &&
            stamina > 0f;

        if (canUseRunSpeed)
        {
            playerCtrl.runDuration = originalRunDuration;
        }
        else
        {
            playerCtrl.runDuration = playerCtrl.moveDuration;
        }
    }

    // 스태미나 감소 및 회복
    private void UpdateStamina()
    {
        bool runKey = inputManager.GetKeyRun();
        bool isMoving = playerCtrl.isMoving;
        bool isJumping = playerCtrl.GetIsJumping();

        bool staminaEmpty = stamina <= 0.001f;

        IsRunning =
            currentPhase >= 5 &&
            runKey &&
            isMoving &&
            !isJumping &&
            stamina > 0f;

        // 뛰는 중
        if (IsRunning)
        {
            // 초당 1 감소
            stamina -= staminaRunUse * Time.deltaTime;

            if (stamina <= 0f)
            {
                stamina = 0f;

                IsRunning = false;

                // 스태미나가 모두 소모되면 걷기 속도
                playerCtrl.runDuration =
                    playerCtrl.moveDuration;

                StartWarningGlow();
            }
        }
        else
        {
            // 스태미나가 없는데 Shift를 누르며 이동 중
            bool failedRunInput =
                currentPhase >= 5 &&
                runKey &&
                isMoving &&
                !isJumping &&
                staminaEmpty;

            if (!failedRunInput)
            {
                if (isMoving)
                {
                    // 걷는 중 회복
                    stamina +=
                        staminaWalkRecovery *
                        Time.deltaTime;
                }
                else
                {
                    // 멈춘 상태 회복
                    stamina +=
                        staminaIdleRecovery *
                        Time.deltaTime;
                }
            }
        }

        stamina =
            Mathf.Clamp(
                stamina,
                0f,
                staminaMax
            );

        // 스태미나가 없는 상태에서 Shift 입력
        if (currentPhase >= 5 &&
            runKey &&
            staminaEmpty)
        {
            StartWarningGlow();
        }
    }

    // 숨고르기
    private void UpdateBreath()
    {
        bool isMoving = playerCtrl.isMoving;
        bool isJumping =
            playerCtrl.GetIsJumping();

        // 시야 감소 디버프 기본 시간 감소
        if (visionDebuffTime > 0f)
        {
            visionDebuffTime -=
                Time.deltaTime;
        }

        // 숨고르기 사용 불가
        if (currentPhase < 3 ||
            isMoving ||
            isJumping)
        {
            idleTimer = 0f;
            IsBreathing = false;

            ClampVisionDebuff();

            return;
        }

        // 정지 시간 측정
        idleTimer += Time.deltaTime;

        // 2초 이상 멈춘 경우
        if (idleTimer >= breathTriggerTime &&
            visionDebuffTime > 0f)
        {
            IsBreathing = true;

            // 디버프 시간을 추가 감소
            visionDebuffTime -=
                breathReducePerSecond *
                Time.deltaTime;
        }
        else
        {
            IsBreathing = false;
        }

        ClampVisionDebuff();
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider == null)
        {
            return;
        }

        staminaSlider.minValue = 0f;
        staminaSlider.maxValue = staminaMax;
        staminaSlider.value = stamina;
    }

    private void FindStaminaBackground()
    {
        if (staminaBackgroundImage != null)
        {
            return;
        }

        if (staminaSlider == null)
        {
            return;
        }

        Transform background =
            staminaSlider.transform.Find(
                "Background"
            );

        if (background != null)
        {
            staminaBackgroundImage =
                background.GetComponent<Image>();
        }

        if (staminaBackgroundImage != null)
        {
            return;
        }

        Image[] images =
            staminaSlider.GetComponentsInChildren
            <Image>(true);

        foreach (Image image in images)
        {
            if (image.name
                .ToLower()
                .Contains("background"))
            {
                staminaBackgroundImage = image;
                break;
            }
        }
    }

    private void CreateExactBorderGlow()
    {
        if (staminaBackgroundImage == null)
        {
            Debug.LogWarning(
                "[BreathRun] Stamina Background Image가 연결되지 않았습니다."
            );

            return;
        }

        RemoveOldGlow();

        CreateGlowLayer(
            "StaminaOuterGlow",
            outerGlowSize,
            out outerGlowImage,
            out outerGlowOutline
        );

        CreateGlowLayer(
            "StaminaBorderGlow",
            borderSize,
            out borderGlowImage,
            out borderOutline
        );

        SetGlowActive(false);
    }

    private void CreateGlowLayer(
        string objectName,
        float outlineSize,
        out Image glowImage,
        out Outline glowOutline)
    {
        RectTransform backgroundRect =
            staminaBackgroundImage
            .rectTransform;

        GameObject glowObject =
            new GameObject(objectName);

        glowObject.transform.SetParent(
            backgroundRect.parent,
            false
        );

        glowObject.transform.SetSiblingIndex(
            backgroundRect.GetSiblingIndex()
        );

        RectTransform glowRect =
            glowObject.AddComponent
            <RectTransform>();

        glowRect.anchorMin =
            backgroundRect.anchorMin;

        glowRect.anchorMax =
            backgroundRect.anchorMax;

        glowRect.pivot =
            backgroundRect.pivot;

        glowRect.anchoredPosition =
            backgroundRect.anchoredPosition;

        glowRect.sizeDelta =
            backgroundRect.sizeDelta;

        glowRect.localRotation =
            backgroundRect.localRotation;

        glowRect.localScale =
            backgroundRect.localScale;

        glowImage =
            glowObject.AddComponent<Image>();

        glowImage.sprite =
            staminaBackgroundImage.sprite;

        glowImage.type =
            staminaBackgroundImage.type;

        glowImage.preserveAspect =
            staminaBackgroundImage
            .preserveAspect;

        glowImage.fillCenter =
            staminaBackgroundImage
            .fillCenter;

        glowImage
            .pixelsPerUnitMultiplier =
            staminaBackgroundImage
            .pixelsPerUnitMultiplier;

        glowImage.raycastTarget = false;

        glowImage.color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );

        glowOutline =
            glowObject.AddComponent
            <Outline>();

        glowOutline.useGraphicAlpha = false;

        glowOutline.effectDistance =
            new Vector2(
                outlineSize,
                -outlineSize
            );

        Color color = warningColor;
        color.a = 0f;

        glowOutline.effectColor = color;
    }

    private void StartWarningGlow()
    {
        warningTimer =
            warningBlinkTime;
    }

    // 붉은 경고 애니메이션
    private void UpdateWarningGlow()
    {
        if (borderOutline == null ||
            outerGlowOutline == null)
        {
            return;
        }

        if (warningTimer <= 0f)
        {
            SetGlowActive(false);
            return;
        }

        warningTimer -=
            Time.deltaTime;

        SetGlowActive(true);

        float wave =
            Mathf.Sin(
                Time.unscaledTime *
                warningBlinkSpeed
            );

        float pulse =
            (wave + 1f) * 0.5f;

        float borderAlpha =
            Mathf.Lerp(
                0.15f,
                borderMaxAlpha,
                pulse
            );

        float outerAlpha =
            Mathf.Lerp(
                0.03f,
                outerGlowMaxAlpha,
                pulse
            );

        Color borderColor =
            warningColor;

        borderColor.a =
            borderAlpha;

        borderOutline.effectColor =
            borderColor;

        Color outerColor =
            warningColor;

        outerColor.a =
            outerAlpha;

        outerGlowOutline.effectColor =
            outerColor;
    }

    private void SetGlowActive(bool active)
    {
        if (borderGlowImage != null)
        {
            borderGlowImage
                .gameObject
                .SetActive(active);
        }

        if (outerGlowImage != null)
        {
            outerGlowImage
                .gameObject
                .SetActive(active);
        }
    }

    private void RemoveOldGlow()
    {
        string[] oldNames =
        {
            "StaminaWarningBorder",
            "StaminaLackGlow",
            "StaminaSmallEdgeWarning",
            "StaminaRoundedRedGlow",
            "StaminaBorderGlow",
            "StaminaOuterGlow"
        };

        Transform parent =
            staminaBackgroundImage
            .transform
            .parent;

        foreach (string oldName in oldNames)
        {
            Transform oldObject =
                parent.Find(oldName);

            if (oldObject != null)
            {
                Destroy(
                    oldObject.gameObject
                );
            }
        }
    }

    private void ClampVisionDebuff()
    {
        visionDebuffTime =
            Mathf.Max(
                0f,
                visionDebuffTime
            );
    }

    // 시야 감소 기능 테스트
    private void DebugTestInput()
    {
        // F: 화재 시야 감소 7초
        if (Input.GetKeyDown(KeyCode.F))
        {
            TriggerFireEncounter();

            Debug.Log(
                "[BreathRun] 화재 디버프: 7초"
            );
        }

        // G: 추격자 시야 감소 4초
        if (Input.GetKeyDown(KeyCode.G))
        {
            TriggerAlterEncounter();

            Debug.Log(
                "[BreathRun] 추격자 디버프: 4초"
            );
        }
    }

    public void SetPhase(int phase)
    {
        currentPhase = phase;
    }

    public void TriggerAlterEncounter()
    {
        visionDebuffTime = 4f;
    }

    public void TriggerFireEncounter()
    {
        visionDebuffTime = 7f;
    }

    public void SetVisionDebuffTime(
        float time)
    {
        visionDebuffTime =
            Mathf.Max(
                0f,
                time
            );
    }

    public void SetStamina(
        float value)
    {
        stamina =
            Mathf.Clamp(
                value,
                0f,
                staminaMax
            );

        UpdateStaminaUI();
    }

    private void OnDisable()
    {
        if (playerCtrl != null)
        {
            playerCtrl.runDuration =
                originalRunDuration;
        }

        SetGlowActive(false);
    }
}
