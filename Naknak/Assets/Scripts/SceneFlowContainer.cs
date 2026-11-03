using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SceneReference
{
    [SerializeField] private string path;
    public string Path => path;
    public string Name => string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileNameWithoutExtension(path);
}

public enum SceneConnectionTrigger { Manual, DialogFinished, SceneCompleted }

[CreateAssetMenu(menuName = "Naknak/Scene Flow Container", fileName = "SceneFlow")]
public sealed class SceneFlowContainer : ScriptableObject
{
    [Serializable]
    public sealed class SceneEntry
    {
        public SceneReference scene = new SceneReference();
        public StoryData entryDialog;
        public DialogContainer entryDialogs;
        public bool memorySpace;
        public bool colorMemory;
        [Tooltip("Entering this scene from reality starts a replay, including older saves without a visit counter.")]
        public bool memoryReplayEntry;
        [Tooltip("Item held when entering this memory from reality. T0 grants its own brooch.")]
        public Naknak.Inventory.InventoryItem memoryEntryItem;
        public bool hidePlayer;
        [Tooltip("플레이어 모습은 유지하고 이동/메뉴만 잠급니다.")]
        public bool lockPlayerInput;
        [Tooltip("PrototypeScene의 기존 플레이어 추적 카메라를 사용합니다.")]
        public bool useSharedCamera;
        public bool placePlayer = true;
        public Vector2 playerPosition;
    }

    [Serializable]
    public sealed class Connection
    {
        [HideInInspector] public string id = Guid.NewGuid().ToString("N");
        public string memo;
        public SceneReference from = new SceneReference();
        public SceneReference to = new SceneReference();
        public SceneConnectionTrigger trigger;
        public bool fade;
        public StoryData transitionDialog;
        [Tooltip("Play approaching footsteps and a shop door bell during the black transition.")]
        public AudioClip entranceFootsteps;
        [Tooltip("DialogFinished: select the LAST dialogue asset in the conversation.")]
        public StoryData completedDialog;
        public List<DialogCondition> conditions = new List<DialogCondition>();
    }

    public List<SceneEntry> scenes = new List<SceneEntry>();
    public List<Connection> connections = new List<Connection>();
    public ConditionID memoryVisitCount;
    [Tooltip("첫 기억은 조수, 현실과 재탐색은 탐정으로 조작합니다.")]
    public RuntimeAnimatorController assistantPlayerController;
    public RuntimeAnimatorController detectivePlayerController;
    public List<ConditionID> memoryRunConditions = new List<ConditionID>();

    private void OnValidate()
    {
        var ids = new HashSet<string>();
        foreach (var row in connections)
            if (row != null && (string.IsNullOrEmpty(row.id) || !ids.Add(row.id)))
            { row.id = Guid.NewGuid().ToString("N"); ids.Add(row.id); }
    }

    public SceneEntry FindScene(string pathOrName) => scenes.Find(s => s != null &&
        (s.scene.Path == pathOrName || s.scene.Name == pathOrName));

    // First matching row wins, just like DialogContainer.
    public Connection FindConnection(string source, SceneConnectionTrigger trigger, StoryData dialog = null)
    {
        foreach (var row in connections)
        {
            if (row == null || row.from.Path != source && row.from.Name != source || row.trigger != trigger) continue;
            if (trigger == SceneConnectionTrigger.DialogFinished && (row.completedDialog == null || row.completedDialog != dialog)) continue;
            if (!Passes(row.conditions)) continue;
            return row;
        }
        return null;
    }

    public static bool Passes(List<DialogCondition> conditions)
    {
        if (conditions == null || conditions.Count == 0) return true;
        if (ConditionManager.Instance == null || conditions.Exists(c => c.id == null)) return false;
        return ConditionManager.Instance.CheckConditions(conditions);
    }
}
