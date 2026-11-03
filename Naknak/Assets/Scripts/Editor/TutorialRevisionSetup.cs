using System;
using System.Collections.Generic;
using System.Linq;
using Naknak.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit authoring operation; never runs automatically when the editor opens.
public static class TutorialRevisionSetup
{
    private const string Case = "Assets/Story/Tutorial/Case/";
    private const string T3 = "Tutorial_T3_VaseBreak";
    private const string T4 = "Tutorial_T4_BroochMissing";
    private const string PA = "Tutorial_PA_Alibi";
    private const string PB = "Tutorial_PB_Alibi";

    private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    private static SceneReference Reference(string name) => JsonUtility.FromJson<SceneReference>(
        "{\"path\":\"Assets/Scenes/" + name + ".unity\"}");
    private static Sprite Portrait(string name) => AssetDatabase.LoadAllAssetsAtPath(
        "Assets/Resources/Portraits/" + name + ".png").OfType<Sprite>().First();

    public static void PlaceOwnerBehindCounter(Scene scene)
    {
        var owner = Find(scene, "Owner");
        if (owner == null) return;
        var counter = Find(scene, "Depth - Counter");
        owner.transform.position = counter != null ? counter.transform.position + Vector3.up : new Vector3(-3, 3, 0);
        // Keep the talk target at the counter front, where the player can reach it.
        var collider = owner.GetComponent<BoxCollider2D>();
        if (collider != null) collider.offset = new Vector2(0, -1.5f);
        owner.GetComponent<WorldDepthSort>()?.Refresh();
    }
    private static StoryData Line(string path, string speaker, string text, string portrait)
    {
        var story = Load<StoryData>(path);
        if (story == null) { story = ScriptableObject.CreateInstance<StoryData>(); AssetDatabase.CreateAsset(story, path); }
        story.charName = speaker;
        story.text = text;
        story.portrait = portrait == null ? null : Portrait(portrait);
        story.activatedImage = portrait == null ? -1 : 0;
        story.doorSlam = false;
        story.autoAdvanceAfter = 0;
        story.isSelect = false;
        story.isEnd = true;
        story.nextData = null;
        story.nextSceneName = "";
        EditorUtility.SetDirty(story);
        return story;
    }
    private static ConditionID Flag(string name)
    {
        string path = Case + "Evidence/" + name + ".asset";
        var flag = Load<ConditionID>(path);
        if (flag == null) { flag = ScriptableObject.CreateInstance<ConditionID>(); AssetDatabase.CreateAsset(flag, path); }
        return flag;
    }
    private static void Ref(UnityEngine.Object obj, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(obj);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Bool(UnityEngine.Object obj, string field, bool value)
    {
        var so = new SerializedObject(obj);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void CompletionFlag(StoryData story, ConditionID flag)
    {
        var so = new SerializedObject(story);
        var actions = so.FindProperty("endActions");
        actions.arraySize = 1;
        var action = actions.GetArrayElementAtIndex(0);
        action.FindPropertyRelative("type").enumValueIndex = 0;
        action.FindPropertyRelative("id").objectReferenceValue = flag;
        action.FindPropertyRelative("value").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Naknak/Apply Tutorial Revision")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring scenes.");
        var flow = Load<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        var original = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            ApplyStories(flow);
            var s2 = Open("S2", opened);
            var t0 = Open("T0", opened);
            var t1 = Open("T1", opened);
            var t2 = Open("T2", opened);
            var t4 = Open(T4, opened);
            var t3 = Open(T3, opened);
            var pa = Open(PA, opened);
            var pb = Open(PB, opened);

            var marker = Find(s2, "Assistant Attention") ?? New(s2, "Assistant Attention");
            Ref(Get<TutorialAttentionMarker>(marker), "target", Find(s2, "assist").transform);
            Ref(Get<TutorialAttentionMarker>(marker), "icon", Load<Sprite>("Assets/Warning.png"));
            var room = All<TutorialRoom0>(t0).First();
            foreach (var name in new[] { "T0 Broach", "T0 Jewelry Box" })
            {
                var target = Find(t0, name);
                var attention = target.GetComponent<TutorialAttentionMarker>();
                if (attention != null) UnityEngine.Object.DestroyImmediate(attention);
                var renderer = target.GetComponent<SpriteRenderer>();
                renderer.sprite = Load<Sprite>("Assets/Warning.png");
                renderer.color = new Color(1, 0.85f, 0.35f);
                renderer.enabled = true;
            }
            var box = Find(t0, "T0 Jewelry Box");
            Ref(room, "sparkle", box.GetComponent<SpriteRenderer>());

            foreach (var scene in new[] { t1, t2, t3, t4 })
            {
                PlaceOwnerBehindCounter(scene);
                var staging = Get<TutorialWitnessStaging>(Find(scene, "Witness Staging") ?? New(scene, "Witness Staging"));
                Ref(staging, "witnessA", Find(scene, "A").transform);
                Ref(staging, "witnessB", Find(scene, "B").transform);
                Bool(staging, "walkAfterAppraisal", scene == t1);
                Bool(staging, "facePlayerUpOnEntry", scene == t1);
                Ref(staging, "appraisalLastLine", Load<StoryData>("Assets/Scripts/New Folder/Text_T1/T1_10.asset"));
                Find(scene, "B").transform.position = new Vector3(3, -4, 0);
                if (scene == t3 || scene == t4) Find(scene, "A").transform.position = new Vector3(-4, 1, 0);
            }

            foreach (var scene in new[] { t2, t4 }) ConfigureEvidence(scene);
            CopyEvidenceToDarkScene(t4, t3);
            ConfigureEvidence(t3);
            foreach (var scene in new[] { t1, t2, t3, t4 }) TutorialRouteSetup.Rebuild(scene);
            var cabinet = Find(t2, "Cabinet Number 2").GetComponent<InvestigationPoint>();
            var cs = new SerializedObject(cabinet);
            var rules = cs.FindProperty("rules");
            rules.arraySize = 1;
            var rule = rules.GetArrayElementAtIndex(0);
            rule.FindPropertyRelative("memo").stringValue = "사건 전: 평범한 진열장";
            foreach (var field in new[] { "conditions", "requiredItems", "anyItems", "rewards", "actions", "evidenceConditions" })
                rule.FindPropertyRelative(field).arraySize = 0;
            rule.FindPropertyRelative("dialogue").objectReferenceValue = Load<StoryData>(Case + "CabinetBeforeIncident.asset");
            rule.FindPropertyRelative("presentEvidence").boolValue = false;
            rule.FindPropertyRelative("connectionId").stringValue = "";
            cs.ApplyModifiedPropertiesWithoutUndo();

            foreach (var scene in new[] { pa, pb }) StageVerification(scene, s2);
            foreach (var scene in new[] { s2, t0, t1, t2, t3, t4, pa, pb })
                EditorSceneManager.SaveScene(scene);
            EditorUtility.SetDirty(flow);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            foreach (var scene in opened) if (scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ApplyStories(SceneFlowContainer flow)
    {
        string[] texts = { "저기요?", "저기요?", "이봐요! 듣고 있어요?", "네. 듣고 있습니다. ...그래서 뭘 찾으신다고요?",
            "... 만년필이요.", "그러시군요.", "...", ".......", ".............................", "...? 저기요?",
            "...아, 생각하고 있습니다.", "....", "아, 그리고 그건 곧 찾으실 수 있을 겁니다.", "무슨 헛소리를 하는겁니까",
            "", "...", "쳇 오늘도 허탕인가...", ".....그것이라도 팔아야 하나" };
        var client = new HashSet<int> { 0, 1, 2, 4, 9, 11, 13 };
        var prologue = new List<StoryData>();
        for (int i = 0; i < texts.Length; i++)
        {
            string portrait = client.Contains(i) || i == 14 ? null : i == 8 ? "탐정 잠" :
                i == 6 || i == 7 || i == 15 ? "탐정 눈감" : "탐정 기본";
            prologue.Add(Line("Assets/Story/Tutorial/Prologue/Prologue_" + (i + 1).ToString("00") + ".asset",
                i == 14 ? "" : client.Contains(i) ? "의뢰인" : "탐정", texts[i], portrait));
        }
        for (int i = 0; i < prologue.Count - 1; i++) { prologue[i].nextData = prologue[i + 1]; prologue[i].isEnd = false; }
        prologue[14].doorSlam = true;
        prologue[14].autoAdvanceAfter = 0.55f;
        var prologueContainer = flow.FindScene("Tutorial_Prologue").entryDialogs;
        prologueContainer.dialogSlots[0].memo = "S0: 의뢰인 대화, 졸음, 문 닫힘, 브로치 회상으로 이어지는 18개 구간";
        EditorUtility.SetDirty(prologueContainer);
        foreach (var c in flow.connections.Where(c => c.from.Name == "Tutorial_Prologue")) c.completedDialog = prologue.Last();

        var narration = Line("Assets/Story/Tutorial/Prologue/MemoryEntryNarration.asset", "조수",
            "저는 브로치를 팔려고 전당포 안으로 들어왔어요.", null);
        var s2Last = Load<StoryData>("Assets/Scripts/New Folder/Text_#2/Ait_4.asset");
        for (int i = 1; i <= 4; i++)
        {
            var line = Load<StoryData>("Assets/Scripts/New Folder/Text_#2/Ait_" + i + ".asset");
            line.charName = i % 2 == 1 ? "탐정" : "조수";
            EditorUtility.SetDirty(line);
        }
        s2Last.isEnd = true; s2Last.nextData = null; EditorUtility.SetDirty(s2Last);
        foreach (var c in flow.connections.Where(c => c.from.Name == "S2" && c.to.Name == "T0"))
        { c.completedDialog = s2Last; c.transitionDialog = narration; }
        Line("Assets/Story/T0/T0PackedMessage.asset", "조수", "미안해요. 팔 수밖에 없어요.", "조수 슬픔");
        Line(Case + "CabinetBeforeIncident.asset", "", "진열장이다.", null);

        var aFlag = Flag("EvidenceARouteComplete");
        var bFlag = Flag("EvidenceBRouteComplete");
        var aDone = Line(Case + "EvidenceAComplete.asset", "탐정", "A의 동선을 전부 파악했다.", "탐정 기본");
        var bDone = Line(Case + "EvidenceBComplete.asset", "탐정", "B의 동선을 전부 파악했다.", "탐정 기본");
        CompletionFlag(aDone, aFlag);
        CompletionFlag(bDone, bFlag);
        Line(Case + "AlibisVerified.asset", "탐정", "A와 B의 동선을 전부 파악했다.", "탐정 기본");
        Line(Case + "AHint.asset", "탐정", "A와 상호작용해 편지·시계 상자·전화기 사용 흔적을 제시하자.", "탐정 기본");
        Line(Case + "BHint.asset", "탐정", "B와 상호작용해 전당포증·장난감 부품·드라이버와 진열장 흔적을 제시하자.", "탐정 기본");

        var aSummary = Load<StoryData>(Case + "PA_01.asset");
        aSummary.text = "편지를 버리고, 시계를 판 뒤, 가족에게 전화를 걸었군요.";
        aSummary.nextData = Load<StoryData>(Case + "PA_05.asset");
        EditorUtility.SetDirty(aSummary);
        var bSummary = Load<StoryData>(Case + "PB_01.asset");
        bSummary.text = "전당포증을 버리고, 장난감으로 소란을 만든 뒤, 2번 진열장을 열려고 했군요.";
        bSummary.nextData = Load<StoryData>(Case + "PB_05.asset");
        EditorUtility.SetDirty(bSummary);
        flow.FindScene("T0").colorMemory = false;
        flow.FindScene("T1").playerPosition = new Vector2(1, -4);
        flow.FindScene(T3).memoryReplayEntry = true;
        flow.connections.First(c => c.from.Name == "Tutorial_Prologue" && c.to.Name == "S1").entranceFootsteps =
            Load<AudioClip>("Assets/Sound/freesound_community-running-14658.mp3");
        foreach (var name in new[] { PA, PB })
        {
            var entry = flow.FindScene(name);
            entry.memorySpace = false;
            entry.lockPlayerInput = true;
            entry.playerPosition = new Vector2(0, 0);
        }
        flow.connections.RemoveAll(c => c.from.Name == PA || c.from.Name == PB ||
            c.id.StartsWith("pdf-start-PA-") || c.id.StartsWith("pdf-start-PB-") ||
            c.id.StartsWith("evidence-summary-PA-") || c.id.StartsWith("evidence-complete-"));
        foreach (var name in new[] { "T2", T3, T4 })
            flow.connections.Insert(0, new SceneFlowContainer.Connection { id = "evidence-complete-" + name,
                from = Reference(name), to = Reference(PA), trigger = SceneConnectionTrigger.DialogFinished,
                completedDialog = Load<StoryData>(Case + "AlibisVerified.asset"), fade = true,
                conditions = new[] { "Evidence_A_LetterSeen", "Evidence_A_WatchSeen", "Evidence_A_PhoneSeen",
                    "Evidence_B_TicketSeen", "Evidence_B_ToySeen", "Evidence_B_CabinetSeen" }.Select(flag => new DialogCondition
                    { id = Load<ConditionID>(Case + "Evidence/" + flag + ".asset"), compare = CompareType.Equal, value = 1 }).ToList() });
        flow.connections.Insert(0, new SceneFlowContainer.Connection { id = "verified-A-to-B", from = Reference(PA), to = Reference(PB),
            trigger = SceneConnectionTrigger.DialogFinished, completedDialog = Load<StoryData>(Case + "PA_08.asset"), fade = true });
        flow.connections.Insert(0, new SceneFlowContainer.Connection { id = "verified-B-to-owner", from = Reference(PB), to = Reference("S#6"),
            trigger = SceneConnectionTrigger.DialogFinished, completedDialog = Load<StoryData>(Case + "PB_07.asset"), fade = true });
        // A player may also leave the dark memory before completing both routes.
        var exit = flow.connections.First(c => c.id == "pdf-verified-exit-" + T4);
        if (!flow.connections.Any(c => c.id == "pdf-verified-exit-" + T3))
        {
            var copy = JsonUtility.FromJson<SceneFlowContainer.Connection>(JsonUtility.ToJson(exit));
            copy.id = "pdf-verified-exit-" + T3;
            copy.from = Reference(T3);
            flow.connections.Add(copy);
        }
    }

    private static void ConfigureEvidence(Scene scene)
    {
        foreach (var actor in new[] { "A", "B" })
        {
            var reconstruction = Find(scene, actor).GetComponent<EvidenceReconstruction>();
            if (reconstruction != null) Ref(reconstruction, "completionDialog", Load<StoryData>(Case + "Evidence" + actor + "Complete.asset"));
        }
        foreach (var conclusion in All<TutorialEvidenceConclusion>(scene))
        {
            var so = new SerializedObject(conclusion);
            var witnesses = so.FindProperty("witnesses");
            for (int i = 0; i < witnesses.arraySize; i++)
            {
                var w = witnesses.GetArrayElementAtIndex(i);
                var dialog = w.FindPropertyRelative("completionDialog").objectReferenceValue as StoryData;
                w.FindPropertyRelative("completed").objectReferenceValue = Flag(dialog != null && dialog.name.Contains("BComplete") ?
                    "EvidenceBRouteComplete" : "EvidenceARouteComplete");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CopyEvidenceToDarkScene(Scene source, Scene target)
    {
        var map = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
        foreach (var root in source.GetRootGameObjects().Where(r => r.name.StartsWith("Evidence Routes")))
        {
            var existing = Find(target, root.name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var clone = UnityEngine.Object.Instantiate(root);
            clone.name = root.name;
            SceneManager.MoveGameObjectToScene(clone, target);
            var originalNodes = root.GetComponentsInChildren<Transform>(true);
            var copyNodes = clone.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < originalNodes.Length; i++)
            {
                map[originalNodes[i]] = copyNodes[i]; map[originalNodes[i].gameObject] = copyNodes[i].gameObject;
                var originalComponents = originalNodes[i].GetComponents<Component>();
                var copiedComponents = copyNodes[i].GetComponents<Component>();
                for (int j = 0; j < originalComponents.Length; j++) if (originalComponents[j] != null) map[originalComponents[j]] = copiedComponents[j];
            }
        }
        foreach (var actor in new[] { "A", "B" })
        {
            var original = Find(source, actor);
            var destination = Find(target, actor);
            EditorUtility.CopySerialized(original.GetComponent<InvestigationPoint>(), Get<InvestigationPoint>(destination));
            var copied = Get<EvidenceReconstruction>(destination);
            EditorUtility.CopySerialized(original.GetComponent<EvidenceReconstruction>(), copied);
            Remap(copied, map, target);
        }
        foreach (var conclusion in All<TutorialEvidenceConclusion>(source))
        {
            var destination = Get<TutorialEvidenceConclusion>(Find(target, "Evidence Completion") ?? New(target, "Evidence Completion"));
            EditorUtility.CopySerialized(conclusion, destination);
        }
        var exit = Find(target, "Memory Exit").GetComponent<InvestigationPoint>();
        var so = new SerializedObject(exit);
        var rules = so.FindProperty("rules");
        for (int i = 0; i < rules.arraySize; i++)
        {
            var property = rules.GetArrayElementAtIndex(i).FindPropertyRelative("connectionId");
            property.stringValue = property.stringValue.Replace(T4, T3);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Remap(Component component, Dictionary<UnityEngine.Object, UnityEngine.Object> map, Scene scene)
    {
        var so = new SerializedObject(component);
        var it = so.GetIterator();
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
            var value = it.objectReferenceValue;
            if (map.TryGetValue(value, out var copy)) { it.objectReferenceValue = copy; continue; }
            if (value is Component source && source.gameObject.scene.IsValid() && source.gameObject.scene != scene)
            {
                var target = Find(scene, source.name);
                if (target != null) it.objectReferenceValue = target.GetComponent(source.GetType());
            }
            else if (value is GameObject sourceObject && sourceObject.scene.IsValid() && sourceObject.scene != scene)
                it.objectReferenceValue = Find(scene, sourceObject.name);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void StageVerification(Scene scene, Scene shop)
    {
        foreach (var track in All<TutorialDeductionTrack>(scene)) UnityEngine.Object.DestroyImmediate(track);
        foreach (var name in new[] { "A", "B", "Owner", "assist" })
        {
            var actor = Find(scene, name);
            if (actor == null)
            {
                actor = UnityEngine.Object.Instantiate(Find(shop, name));
                actor.name = name;
                SceneManager.MoveGameObjectToScene(actor, scene);
            }
            foreach (var point in actor.GetComponents<InvestigationPoint>()) UnityEngine.Object.DestroyImmediate(point);
            foreach (var npc in actor.GetComponents<NPC>()) npc.enabled = false;
            actor.transform.position = name == "A" ? new Vector3(-1, 2, 0) : name == "B" ? new Vector3(2, 2, 0) :
                name == "assist" ? new Vector3(1, 0, 0) : new Vector3(-2, 3, 0);
        }
    }

    private static Scene Open(string name, List<Scene> opened)
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/" + name + ".unity");
        if (!scene.isLoaded) { scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity", OpenSceneMode.Additive); opened.Add(scene); }
        return scene;
    }
    private static IEnumerable<T> All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
    private static GameObject Find(Scene scene, string name) => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
        .FirstOrDefault(t => t.name == name)?.gameObject;
    private static GameObject New(Scene scene, string name)
    {
        var obj = new GameObject(name); SceneManager.MoveGameObjectToScene(obj, scene); return obj;
    }
    private static T Get<T>(GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();
}
