using UnityEngine;

namespace Naknak.FlashlightSystem
{
    public class FlashlightController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private FlashlightFreeSightOverlay overlay;
        [SerializeField] private FlashlightWorldDesaturateEffect worldDesaturateEffect;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LastInputManager playerInput;
        [SerializeField] private bool lightsOffEnabled = true;
        [SerializeField] private bool flashlightEnabled = true;
        [SerializeField] private bool lockedToPlayer = false;
        [SerializeField] private KeyCode turnLightsOffKey = KeyCode.Alpha0;
        [SerializeField] private KeyCode toggleFlashlightKey = KeyCode.Tab;
        [SerializeField] private KeyCode recenterToPlayerKey = KeyCode.Y;
        [SerializeField] private bool allowKeyboardMove = true;
        [SerializeField] private bool allowMouseMove = true;
        [SerializeField] private bool blockPlayerMovementWhileFree = true;
        [SerializeField] private float keyboardMoveSpeed = 1.5f;
        [SerializeField] private float maxDistanceFromPlayer = 6f;

        private Vector3 sightCenter;

        public bool FlashlightEnabled
        {
            get => flashlightEnabled;
            set
            {
                flashlightEnabled = value;
                ApplyOverlayState();
            }
        }

        public bool LockedToPlayer => lockedToPlayer;
        public Vector3 SightCenter => sightCenter;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    player = playerObject.transform;
            }

            if (overlay == null)
                overlay = GetComponentInChildren<FlashlightFreeSightOverlay>();

            if (worldDesaturateEffect == null)
                worldDesaturateEffect = GetComponent<FlashlightWorldDesaturateEffect>();

            if (playerInput == null && player != null)
                playerInput = player.GetComponent<LastInputManager>();

            sightCenter = player != null ? player.position : transform.position;
            ApplyOverlayState();
        }

        private void OnDisable()
        {
            SetPlayerWasdSuppressed(false);
        }

        private void Update()
        {
            UpdateLightsOffInput();
            UpdateFlashlightToggle();

            if (!lightsOffEnabled)
            {
                ApplyOverlayState();
                SetPlayerWasdSuppressed(false);
                return;
            }

            SetPlayerWasdSuppressed(flashlightEnabled);

            if (flashlightEnabled)
                UpdatePlayerLock();

            if (flashlightEnabled && lockedToPlayer)
                FollowPlayer();
            else if (flashlightEnabled)
            {
                UpdateFreeSightCenter();
            }

            ClampSightCenter();
            UpdateOverlay();
        }

        private void FollowPlayer()
        {
            if (player != null)
                sightCenter = player.position;
        }

        private void UpdateLightsOffInput()
        {
            if (!Input.GetKeyDown(turnLightsOffKey))
                return;

            lightsOffEnabled = true;
            ApplyOverlayState();
        }

        private void UpdateFlashlightToggle()
        {
            if (!Input.GetKeyDown(toggleFlashlightKey))
                return;

            if (!lightsOffEnabled)
                lightsOffEnabled = true;

            flashlightEnabled = !flashlightEnabled;

            if (flashlightEnabled)
            {
                lockedToPlayer = false;
                if (player != null)
                    sightCenter = player.position;
            }

            ApplyOverlayState();
        }

        private void UpdatePlayerLock()
        {
            if (Input.GetKeyDown(recenterToPlayerKey))
                lockedToPlayer = !lockedToPlayer;
        }

        private void UpdateFreeSightCenter()
        {
            if (allowKeyboardMove)
            {
                Vector2 input = new Vector2(
                    GetWasdAxis(KeyCode.A, KeyCode.D),
                    GetWasdAxis(KeyCode.S, KeyCode.W));

                if (input.sqrMagnitude > 1f)
                    input.Normalize();

                sightCenter += (Vector3)(input * keyboardMoveSpeed * Time.deltaTime);
            }

            if (allowMouseMove && targetCamera != null && Input.GetMouseButton(1))
            {
                Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = sightCenter.z;
                sightCenter = mouseWorld;
            }
        }

        private void SetPlayerWasdSuppressed(bool suppress)
        {
            if (!blockPlayerMovementWhileFree || playerInput == null)
                return;

            playerInput.SetSuppressWasdMovement(suppress);
        }

        private void ClampSightCenter()
        {
            if (player == null || maxDistanceFromPlayer <= 0f)
                return;

            Vector3 offset = sightCenter - player.position;
            if (offset.magnitude > maxDistanceFromPlayer)
                sightCenter = player.position + offset.normalized * maxDistanceFromPlayer;
        }

        private static float GetWasdAxis(KeyCode negative, KeyCode positive)
        {
            float value = 0f;
            if (Input.GetKey(negative)) value -= 1f;
            if (Input.GetKey(positive)) value += 1f;
            return value;
        }

        private void UpdateOverlay()
        {
            if (overlay == null)
                return;

            overlay.SetActive(lightsOffEnabled);
            overlay.SetFlashlightVisible(flashlightEnabled);
            overlay.SetSightCenter(sightCenter);
        }

        private void ApplyOverlayState()
        {
            if (overlay != null)
            {
                overlay.SetActive(lightsOffEnabled);
                overlay.SetFlashlightVisible(flashlightEnabled);
            }

            if (worldDesaturateEffect == null)
                return;

            if (lightsOffEnabled)
                worldDesaturateEffect.Apply();
            else
                worldDesaturateEffect.Clear();
        }
    }
}
