using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoomTransitionTestEffects : MonoBehaviour
{
    [Header("Blackout")]
    [SerializeField] private Image blackoutImage;
    [SerializeField] private int flickerCount = 3;
    [SerializeField] private float flickerAlpha = 0.55f;
    [SerializeField] private float flickerTime = 0.06f;
    [SerializeField] private float blackoutFadeInTime = 0.12f;
    [SerializeField] private float blackoutFadeOutTime = 0.25f;

    [SerializeField, Range(0f, 1f)] private float blackoutAlpha = 0.85f;

    [Tooltip("암전 상태에서 이 키를 누르면 다시 밝아짐")]
    [SerializeField] private KeyCode releaseBlackoutKey = KeyCode.Z;

    [Header("Moving Object Test")]
    [SerializeField] private Transform chairTarget;
    [SerializeField] private bool useLocalPosition = true;
    [SerializeField] private Vector3 chairMoveOffset = new Vector3(0.8f, 0f, 0f);
    [SerializeField] private float chairMoveTime = 0.25f;

    [SerializeField] private float chairMoveDelayAfterRoomEnter = 1f;

    [Header("Sound Test")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip runningSoundClip;
    [SerializeField] private float soundDelay = 0.02f;

    private Vector3 chairOriginalPosition;
    private bool chairMoved = false;
    private bool waitingForBlackoutRelease = false;

    private Coroutine chairRoutine;
    private Coroutine delayedChairRoutine;
    private Coroutine blackoutRoutine;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (chairTarget != null)
        {
            if (useLocalPosition)
                chairOriginalPosition = chairTarget.localPosition;
            else
                chairOriginalPosition = chairTarget.position;
        }

        SetBlackAlpha(0f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[RoomTransitionTestEffects] C 키 테스트: 의자 이동 실행");
            MoveChairTest();
        }

        if (waitingForBlackoutRelease && Input.GetKeyDown(releaseBlackoutKey))
        {
            waitingForBlackoutRelease = false;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeBlack(0f, blackoutFadeOutTime));

            Debug.Log("[RoomTransitionTestEffects] 암전 해제 키 입력: " + releaseBlackoutKey);
        }
    }

    public IEnumerator PlayBeforeRoomMove()
    {
        yield return null;
    }

    public void PlayAfterRoomMove(int fromRoomId, int toRoomId)
    {
        Debug.Log("[RoomTransitionTestEffects] 방 입장 후 테스트 연출 시작: " + fromRoomId + " → " + toRoomId);

        PlayRunningSound();

        if (delayedChairRoutine != null)
            StopCoroutine(delayedChairRoutine);

        delayedChairRoutine = StartCoroutine(MoveChairAfterRoomEnterDelay());

        if (blackoutRoutine != null)
            StopCoroutine(blackoutRoutine);

        blackoutRoutine = StartCoroutine(BlackoutAfterRoomEnterRoutine());
    }

    private IEnumerator BlackoutAfterRoomEnterRoutine()
    {
        if (blackoutImage == null)
            yield break;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        waitingForBlackoutRelease = false;

        SetBlackAlpha(0f);

        for (int i = 0; i < flickerCount; i++)
        {
            yield return FadeBlack(flickerAlpha, flickerTime);
            yield return FadeBlack(0f, flickerTime);
        }

        yield return FadeBlack(blackoutAlpha, blackoutFadeInTime);

        waitingForBlackoutRelease = true;

        Debug.Log("[RoomTransitionTestEffects] 반투명 암전 유지 중. 해제 키: " + releaseBlackoutKey);
    }

    private IEnumerator MoveChairAfterRoomEnterDelay()
    {
        if (chairMoveDelayAfterRoomEnter > 0f)
            yield return new WaitForSeconds(chairMoveDelayAfterRoomEnter);

        MoveChairTest();

        Debug.Log("[RoomTransitionTestEffects] 방 입장 후 지연 의자 이동 실행");
    }

    private void MoveChairTest()
    {
        if (chairTarget == null)
        {
            Debug.LogWarning("[RoomTransitionTestEffects] Chair Target이 비어 있습니다.");
            return;
        }

        if (chairRoutine != null)
            StopCoroutine(chairRoutine);

        Vector3 startPos = useLocalPosition ? chairTarget.localPosition : chairTarget.position;
        Vector3 targetPos;

        if (chairMoved)
            targetPos = chairOriginalPosition;
        else
            targetPos = chairOriginalPosition + chairMoveOffset;

        chairMoved = !chairMoved;

        Debug.Log("[RoomTransitionTestEffects] 의자 이동: " + startPos + " → " + targetPos);

        chairRoutine = StartCoroutine(MoveChairRoutine(startPos, targetPos));
    }

    private IEnumerator MoveChairRoutine(Vector3 startPos, Vector3 targetPos)
    {
        float elapsed = 0f;

        while (elapsed < chairMoveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / chairMoveTime);

            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t);

            if (useLocalPosition)
                chairTarget.localPosition = nextPos;
            else
                chairTarget.position = nextPos;

            yield return null;
        }

        if (useLocalPosition)
            chairTarget.localPosition = targetPos;
        else
            chairTarget.position = targetPos;
    }

    private void PlayRunningSound()
    {
        if (audioSource == null)
            return;

        StartCoroutine(PlayRunningSoundRoutine());
    }

    private IEnumerator PlayRunningSoundRoutine()
    {
        if (soundDelay > 0f)
            yield return new WaitForSeconds(soundDelay);

        if (runningSoundClip != null)
            audioSource.PlayOneShot(runningSoundClip);
        else
            audioSource.Play();
    }

    private IEnumerator FadeBlack(float targetAlpha, float duration)
    {
        if (blackoutImage == null)
            yield break;

        Color startColor = blackoutImage.color;
        float startAlpha = startColor.a;

        if (duration <= 0f)
        {
            SetBlackAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            SetBlackAlpha(alpha);

            yield return null;
        }

        SetBlackAlpha(targetAlpha);
    }

    private void SetBlackAlpha(float alpha)
    {
        if (blackoutImage == null)
            return;

        Color color = blackoutImage.color;
        color.a = alpha;
        blackoutImage.color = color;
    }
}