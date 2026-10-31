using System;
using System.Collections;
using System.Linq;
using Naknak.Inventory;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Inspector-editable item-to-action reconstructions, independent of the final confession.</summary>
public sealed class EvidenceReconstruction : MonoBehaviour
{
    [Serializable] public sealed class Clip
    {
        public string label;
        public InventoryItem[] evidence = Array.Empty<InventoryItem>();
        public System.Collections.Generic.List<DialogCondition> conditions = new System.Collections.Generic.List<DialogCondition>();
        public ConditionID observed;
        public StoryData missingDialog;
        public Transform startPoint;
        public Vector2 startPosition;
        public TutorialDeductionTrack.Beat[] beats = Array.Empty<TutorialDeductionTrack.Beat>();
        public GameObject[] hideDuringReplay = Array.Empty<GameObject>();
        public GameObject[] showDuringReplay = Array.Empty<GameObject>();
        public Tilemap hideTilemap;
        public Vector3Int hideTileCell;
    }

    [SerializeField] private Clip[] clips = Array.Empty<Clip>();
    private TutorialDeductionTrack track;
    private bool replaying;
    private Vector3 originalPosition;
    private float originalFacing;
    private Animator animator;
    private Collider2D[] colliders;
    private bool[] colliderStates;
    private GameObject[] visuals;
    private bool[] visualStates;
    private string sourceScene;
    private Tilemap hiddenMap;
    private Vector3Int hiddenCell;
    private TileBase hiddenTile;
    private Matrix4x4 hiddenMatrix;
    private Color hiddenColor;
    private TileFlags hiddenFlags;

    public bool IsReplaying => replaying;
    public bool ReadyForConclusion => clips.Length > 0 && ConditionManager.Instance != null &&
        clips.All(c => c.observed != null && ConditionManager.Instance.Get(c.observed) != 0);

    public bool Recognizes(InventoryItem item) => item != null &&
        clips.Any(c => c.evidence.Any(e => e != null && e.Id == item.Id));

    public bool TryPlay(InventoryItem item, StoryData missingEvidence)
    {
        if (replaying || !Recognizes(item) || !SceneLoadManager.IsMemoryRevisit || SceneLoadManager.IsCutscene) return false;
        var clip = clips.First(c => c.evidence.Any(e => e != null && e.Id == item.Id));
        if (!SceneFlowContainer.Passes(clip.conditions))
        {
            var hint = clip.missingDialog != null ? clip.missingDialog : missingEvidence;
            if (hint != null) DialogManager.Instance.ShowDialog(hint);
            return true;
        }
        if (clip.observed == null || clip.beats.Length == 0 || clip.beats.Any(b => b.dialogue == null))
        { Debug.LogError("[Evidence] 아이템별 재현 대사와 완료 조건을 연결해주세요.", this); return true; }
        StartCoroutine(Replay(clip));
        return true;
    }

    private IEnumerator Replay(Clip clip)
    {
        replaying = true;
        sourceScene = SceneLoadManager.CurrentScenePath;
        originalPosition = transform.position;
        animator = GetComponent<Animator>();
        if (animator != null) originalFacing = animator.GetFloat("direction");
        colliders = GetComponentsInChildren<Collider2D>(true);
        colliderStates = colliders.Select(c => c.enabled).ToArray();
        visuals = clip.hideDuringReplay.Concat(clip.showDuringReplay).Where(o => o != null).Distinct().ToArray();
        visualStates = visuals.Select(o => o.activeSelf).ToArray();
        try
        {
            SceneLoadManager.Instance.SetPlayerInputLocked(true);
            foreach (var collider in colliders) collider.enabled = false;
            foreach (var obj in clip.hideDuringReplay) if (obj != null) obj.SetActive(false);
            foreach (var obj in clip.showDuringReplay) if (obj != null) obj.SetActive(true);
            hiddenMap = clip.hideTilemap;
            if (hiddenMap != null)
            {
                hiddenCell = clip.hideTileCell;
                hiddenTile = hiddenMap.GetTile(hiddenCell);
                hiddenMatrix = hiddenMap.GetTransformMatrix(hiddenCell);
                hiddenColor = hiddenMap.GetColor(hiddenCell);
                hiddenFlags = hiddenMap.GetTileFlags(hiddenCell);
                hiddenMap.SetTile(hiddenCell, null);
            }
            Vector2 start = clip.startPoint != null ? (Vector2)clip.startPoint.position : clip.startPosition;
            transform.position = new Vector3(start.x, start.y, originalPosition.z);
            track = TutorialDeductionTrack.BeginSegment(transform, clip.beats);
            DialogManager.Instance.ShowDialog(clip.beats[0].dialogue);
            yield return null;
            while (replaying && DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) yield return null;
            if (replaying && SceneLoadManager.CurrentScenePath == sourceScene && ConditionManager.Instance != null)
                ConditionManager.Instance.Set(clip.observed, 1);
        }
        finally { Restore(); }
    }

    private void OnDisable() { Restore(); }

    private void Restore()
    {
        if (!replaying) return;
        replaying = false;
        if (track != null) { track.enabled = false; Destroy(track.gameObject); track = null; }
        transform.position = originalPosition;
        if (animator != null) { animator.SetBool("isMoving", false); animator.SetFloat("direction", originalFacing); }
        if (colliders != null)
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
        if (visuals != null)
            for (int i = 0; i < visuals.Length; i++) if (visuals[i] != null) visuals[i].SetActive(visualStates[i]);
        if (hiddenMap != null)
        {
            hiddenMap.SetTile(hiddenCell, hiddenTile);
            hiddenMap.SetTileFlags(hiddenCell, TileFlags.None);
            hiddenMap.SetTransformMatrix(hiddenCell, hiddenMatrix);
            hiddenMap.SetColor(hiddenCell, hiddenColor);
            hiddenMap.SetTileFlags(hiddenCell, hiddenFlags);
            hiddenMap = null;
        }
        if (SceneLoadManager.Instance != null && SceneLoadManager.CurrentScenePath == sourceScene)
            SceneLoadManager.Instance.SetPlayerInputLocked(false);
    }
}
