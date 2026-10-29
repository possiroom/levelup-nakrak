using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Naknak.Inventory;
using Naknak.FlashlightSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Explicit, repeatable wiring of the PDF tutorial using the project's existing maps and dialogue.</summary>
public static class TutorialCaseBuilder
{
    const string Folder = "Assets/Story/Tutorial/Case";
    const string T3 = "Tutorial_T3_VaseBreak", T4 = "Tutorial_T4_BroochMissing";
    const string T35 = "Tutorial_T35_BlackoutSearch", Reality = "S#6";
    static SceneFlowContainer flow;
    static readonly Dictionary<string, ConditionID> flags = new Dictionary<string, ConditionID>();
    static readonly Dictionary<string, InventoryItem> items = new Dictionary<string, InventoryItem>();
    static readonly Dictionary<string, Scene> opened = new Dictionary<string, Scene>();
    static readonly List<Scene> ownedScenes = new List<Scene>();
    static readonly HashSet<Object> changed = new HashSet<Object>();
    static Dictionary<string, StoryData> stories;
    static GameObject actorA, actorB, actorAssistant;
    static Sprite sparkle;

    [MenuItem("Tools/Naknak/Tutorial/Apply PDF Story Flow")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        flags.Clear(); items.Clear(); opened.Clear(); ownedScenes.Clear(); changed.Clear();
        var active = SceneManager.GetActiveScene();
        try
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            stories = AssetDatabase.FindAssets("t:StoryData").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<StoryData>).Where(s => s != null)
                .GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First());
            flow = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
            if (flow == null) throw new InvalidOperationException("Missing existing TutorialSceneFlow.");
            foreach (string name in new[] { "MemoryReturnUnlocked", "S3Seen", "PACompleted", "PBCompleted", "OwnerSearchRequested", "OpenBoxFound", "OwnerConfessed", "AQuestioned", "WatchBoxSeenEmpty", "WatchBoxSeenFull", "FinalBoxEvidence", "CaseSolved", "WatchCollected", "LetterCollected", "ABoxCollected", "DriverCollected", "ToyCollected", "PhoneInspected" }) Flag(name);
            var source = Open("S3");
            actorA = Actor(source, "A"); actorB = Actor(source, "B"); actorAssistant = Actor(source, "assist");
            sparkle = AssetDatabase.FindAssets("TX Props t:Texture2D").Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Sprite>().First(s => s.name == "TX Props_4");
            BuildItems(); BuildStories(); ConfigureFlow();
            foreach (string scene in new[] { "T1", "T2", T3, T4 }) BuildMemory(scene);
            BuildDeduction("PA", actorA); BuildDeduction("PB", actorB);
            BuildReality(); BuildDarkMemory(); BuildObjectives();
            var build = EditorBuildSettings.scenes.ToList();
            foreach (string scene in new[] { "Tutorial_PA_Alibi", "Tutorial_PB_Alibi" })
                if (!build.Any(s => s.path == PathFor(scene))) build.Add(new EditorBuildSettingsScene(PathFor(scene), true));
            EditorBuildSettings.scenes = build.ToArray();
            foreach (var asset in changed) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
            foreach (var scene in opened.Values) if (scene.isDirty) EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Build/PdfImplementation");
            File.WriteAllText("Build/PdfImplementation/wiring.json", JsonUtility.ToJson(new Report {
                scenes = flow.scenes.Select(s => s.scene.Path).ToArray(), routes = flow.connections.Count,
                evidence = items.Values.Select(i => i.Id).ToArray(),
                notes = "PDF 1-36: existing S1/T0/T1/T2/T3/T4; evidence/inspection; PA/PB; owner search T3.5; S6/S7/End. T0 stays assistant room. Reentry uses T1 per page 17. Camera follows player per user. Portrait expressions deferred per user."
            }, true));
            Debug.Log("[TutorialCaseBuilder] PDF story flow applied. " + flow.connections.Count + " routes.");
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            foreach (var scene in ownedScenes) if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Serializable] sealed class Report { public string[] scenes, evidence; public int routes; public string notes; }
    static string PathFor(string name) => "Assets/Scenes/" + name + ".unity";
    static SceneReference Ref(string name) => JsonUtility.FromJson<SceneReference>("{\"path\":\"" + PathFor(name) + "\"}");
    static void Set(Object obj, string field, object value)
    {
        var info = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (info == null) throw new MissingFieldException(obj.GetType().Name, field);
        info.SetValue(obj, value); EditorUtility.SetDirty(obj);
        if (EditorUtility.IsPersistent(obj)) changed.Add(obj);
    }
    static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); }
        changed.Add(value); return value;
    }
    static ConditionID Flag(string name)
    {
        if (flags.TryGetValue(name, out var value)) return value;
        string path = AssetDatabase.FindAssets(name + " t:ConditionID").Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == name);
        value = path != null ? AssetDatabase.LoadAssetAtPath<ConditionID>(path) : Asset<ConditionID>(name);
        flags.Add(name, value); return value;
    }
    static DialogCondition C(string name, int value = 1) => new DialogCondition { id = Flag(name), compare = CompareType.Equal, value = value };
    static DialogAction A(string name, int value = 1) => new DialogAction { id = Flag(name), type = ActionType.Set, value = value };
    static StoryData Existing(string name)
    {
        if (!stories.TryGetValue(name, out var value)) throw new InvalidOperationException("Missing existing dialogue: " + name);
        return value;
    }
    static StoryData Last(StoryData story)
    {
        var visited = new HashSet<StoryData>();
        while (story != null && !story.isEnd && !story.isSelect && story.nextData != null && visited.Add(story)) story = story.nextData;
        return story;
    }
    static void Action(StoryData story, string flag)
    {
        if (story.endActions == null) story.endActions = new List<DialogAction>();
        story.endActions.RemoveAll(a => a.id == Flag(flag)); story.endActions.Add(A(flag)); changed.Add(story);
    }
    static StoryData Line(string key, string text, string speaker = "")
    {
        var story = Asset<StoryData>(key); story.charName = speaker; story.text = text; story.isEnd = true;
        story.isSelect = false; story.nextData = null; story.nextSceneName = ""; story.selects = new List<StorySelectData>();
        story.endActions = new List<DialogAction>(); stories[key] = story; return story;
    }
    static StoryData Chain(string prefix, params string[] lines)
    {
        StoryData next = null;
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            var split = lines[i].Split(new[] { '|' }, 2);
            var story = Line(prefix + "_" + (i + 1).ToString("00"), split.Length == 2 ? split[1] : split[0], split.Length == 2 ? split[0] : "");
            story.nextData = next; story.isEnd = next == null; next = story;
        }
        return next;
    }
    static DialogContainer Container(string name, params DialogContainer.DialogSlot[] slots)
    { var value = Asset<DialogContainer>(name); value.dialogSlots = slots.ToList(); return value; }
    static DialogContainer.DialogSlot Slot(StoryData story, params DialogCondition[] conditions) =>
        new DialogContainer.DialogSlot { memo = story.name, headStoryData = story, conditions = conditions.ToList() };
    static Scene Open(string name)
    {
        if (opened.TryGetValue(name, out var scene)) return scene;
        scene = SceneManager.GetSceneByPath(PathFor(name));
        if (scene.IsValid() && scene.isLoaded)
        { if (scene.isDirty) throw new InvalidOperationException("Save your scene before applying: " + name); }
        else { scene = EditorSceneManager.OpenScene(PathFor(name), OpenSceneMode.Additive); ownedScenes.Add(scene); }
        opened.Add(name, scene); return scene;
    }
    static IEnumerable<GameObject> Objects(Scene scene) => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);
    static GameObject Actor(Scene scene, string name)
    {
        var actor = Objects(scene).FirstOrDefault(o => string.Equals(o.name, name, StringComparison.OrdinalIgnoreCase));
        if (actor == null) throw new InvalidOperationException("Missing actor " + name + " in " + scene.name);
        return actor;
    }
    static GameObject Root(Scene scene, string name)
    {
        var old = scene.GetRootGameObjects().FirstOrDefault(r => r.name == name); if (old != null) Object.DestroyImmediate(old);
        var result = new GameObject(name); SceneManager.MoveGameObjectToScene(result, scene); return result;
    }
    static GameObject CloneActor(GameObject template, Transform parent, string name, Vector2 position)
    {
        var actor = Object.Instantiate(template, parent); actor.name = name; actor.transform.position = position; actor.SetActive(true);
        foreach (var npc in actor.GetComponentsInChildren<NPC>(true)) Object.DestroyImmediate(npc);
        foreach (var point in actor.GetComponentsInChildren<InvestigationPoint>(true)) Object.DestroyImmediate(point);
        actor.tag = "Interactable"; actor.layer = LayerMask.NameToLayer("Col 1F"); return actor;
    }

    static void BuildItems()
    {
        Item("PocketWatch", "evidence-pocket-watch", "회중시계", "오래된 회중시계. 가치가 높아 보이지는 않는다. 안쪽에 무언가 들어 있다.");
        Item("ReturnedLetter", "evidence-returned-letter", "반송된 편지", "구겨진 편지 봉투이다. 안에 편지가 들어 있다.");
        Item("CrumpledLetter", "evidence-crumpled-letter", "구겨진 편지", "가족에게 보냈으나 반송된 편지. A의 사정을 보여 준다.");
        Item("ExpiredPawnTicket", "evidence-expired-ticket", "기한이 지난 전당포증", "기한이 지난 전당포증. 숫자 2가 크게 적혀 있다.");
        Item("WatchBox", "evidence-watch-box", "시계 상자", "시계 제조사의 로고가 박혀 있다. 상태가 좋지 않은 빈 상자이다.");
        Item("FlatheadDriver", "evidence-flathead-driver", "일자 드라이버", "태엽 장난감 같은 작은 물건을 조작할 수 있는 튼튼한 드라이버.");
        Item("ClockworkToy", "evidence-clockwork-toy", "태엽 장난감", "꽃병 안에 숨겨져 있던 장난감. 태엽과 나사로 움직인다.");
        Item("OpenJewelryBox", "evidence-open-jewelry-box", "떨어진 보석함", "정전 당시 카운터 뒤 바닥에 떨어져 있던 열린 보석함. 브로치는 없다.");
        items["Spring"] = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Resources/InventoryItems/WindingSpring.asset");
        items["Screw"] = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Resources/InventoryItems/FlatheadScrew.asset");
        var config = AssetDatabase.LoadAssetAtPath<GameMenuConfiguration>("Assets/Resources/NaknakMenu.asset");
        config.items = config.items.Concat(items.Values).Where(i => i != null).Distinct().ToArray(); changed.Add(config);
    }
    static void Item(string key, string id, string name, string description)
    {
        var item = Asset<InventoryItem>(key); items[key] = item;
        Set(item, "id", id); Set(item, "displayName", name); Set(item, "description", description);
        Set(item, "keyItem", true); Set(item, "memoryOnly", false); Set(item, "maxStack", 1);
    }
    static void BuildStories()
    {
        Line("WatchPhoto", "시계를 열자 안쪽에서 사진이 보인다. 사진 속 사람들의 얼굴은 지워져 있다.");
        Set(items["PocketWatch"], "inspectionDialogs", Container("WatchInspect", Slot(Existing("WatchPhoto"))));
        var letter = Line("LetterRead", "가족에게 보낸 편지가 반송되어 있다. 편지를 펼쳐 내용을 확인했다.");
        Set(items["ReturnedLetter"], "inspectionDialogs", Container("LetterInspect", Slot(letter)));
        Set(items["ReturnedLetter"], "inspectionResult", items["CrumpledLetter"]);
        var empty = Line("WatchBoxEmpty", "빈 시계 상자다. 제조사의 로고가 보인다."); Action(empty, "WatchBoxSeenEmpty");
        var heavy = Line("WatchBoxHeavy", "분명 비어 있던 시계 상자가 무거워졌다. 정전 전후로 안의 내용물이 달라진 것 같다."); Action(heavy, "WatchBoxSeenFull");
        var final = Chain("WatchBoxFinal", "주인공|빈 시계 상자가 무거워졌다. 바뀐 시점은 불이 꺼지고 꽃병이 깨진 직후야.", "주인공|A가 아무것도 하지 않았다는 말은 거짓말이었어. 현실에서 확인해 보자."); Action(Last(final), "FinalBoxEvidence");
        Set(items["WatchBox"], "descriptionCondition", Flag("T3Occurred")); Set(items["WatchBox"], "changedDescription", "시계 제조사의 로고가 박힌 상자. 전보다 묵직하다. 안에 무언가 들어 있다.");
        Set(items["WatchBox"], "inspectionDialogs", Container("WatchBoxInspect", Slot(final, C("T3Occurred"), C("AQuestioned"), C("WatchBoxSeenEmpty")), Slot(heavy, C("T3Occurred")), Slot(empty)));
        Line("ExitYes", "기억에서 빠져나간다."); Line("ExitNo", "조금 더 둘러보자."); Line("ExitBlocked", "아직 나갈 때가 아닌 것 같다.");
        var exit = Line("ExitPrompt", "기억 공간에서 나가시겠습니까?"); exit.isSelect = true; exit.isEnd = false;
        exit.selects.Add(new StorySelectData { text = "예", nextData = Existing("ExitYes") }); exit.selects.Add(new StorySelectData { text = "아니오", nextData = Existing("ExitNo") });
        var blocked = Line("ExitPromptBlocked", exit.text); blocked.isSelect = true; blocked.isEnd = false;
        blocked.selects.Add(new StorySelectData { text = "예", nextData = Existing("ExitBlocked") }); blocked.selects.Add(new StorySelectData { text = "아니오", nextData = Existing("ExitNo") });
        Line("UnknownSpace", "이곳은 기억의 주인이 가 보지 않은 장소야. 기억에 없는 공간은 볼 수 없어.", "주인공");
        Line("WatchFound", "카운터 뒤에서 오래된 회중시계를 발견했다.");
        Line("TrashFound", "쓰레기통에서 반송된 편지와 숫자 2가 적힌 전당포증을 발견했다.");
        Line("WatchBoxFound", "A가 들고 있는 빈 시계 상자를 조사했다. 시계 제조사의 로고가 보인다."); Action(Existing("WatchBoxFound"), "WatchBoxSeenEmpty");
        Line("WatchBoxFoundHeavy", "A가 들고 있는 시계 상자를 조사했다. 제조사의 로고가 보이고, 묵직한 무게가 느껴진다."); Action(Existing("WatchBoxFoundHeavy"), "WatchBoxSeenFull");
        Line("DriverFound", "B가 가진 일자 드라이버를 조사했다. 작은 물건을 열기에 충분히 튼튼하다.");
        Line("ToyFound", "꽃병 안에서 태엽 장난감을 발견했다.");
        Line("AlreadyChecked", "이미 조사한 곳이다. 다른 단서를 찾아보자.");
        Line("PhoneClue", "전화기가 비스듬하게 놓여 있다. 누군가 전화기를 사용한 흔적이다.");
        Line("CabinetClue", "2번 진열장의 잠금장치에 억지로 비틀어 연 흔적이 있다.");
        Line("AHint", "A의 이야기를 검증하려면 회중시계, 시계 상자, 반송된 편지와 전화기의 흔적을 확인해야 한다.", "주인공");
        Line("BHint", "B의 동선을 확인하려면 드라이버, 전당포증, 꽃병 속 태엽과 나사, 2번 진열장의 흔적이 필요하다.", "주인공");
        Line("ATruthRepeat", "가족 이야기는 거짓이었지만, 그것만으로 브로치를 훔쳤다고 할 수는 없다.", "주인공");
        Line("BTruthRepeat", "B가 노린 것은 자신의 전당품이었다. 브로치와는 다른 일이다.", "주인공");
        var pa = Chain("PA", "주인공|시계를 팔러 오기 전, A는 쓰레기통에 편지를 버렸다.", "주인공|반송된 편지였다. 시계 안쪽의 사진에 있던 가족에게 보낸 것이겠지.", "주인공|카운터에서 시계를 판 뒤에는 전화기 쪽으로 향했다.", "주인공|비스듬한 전화기는 그때 사용한 흔적이야.", "A|가족이 팔아 달라고 부탁한 게 아닙니다. 제게 남은 것을 팔러 온 거예요.", "A|편지는 돌아오고 연락도 되지 않아서… 마지막으로 전화를 걸었습니다.", "주인공|가족이 부탁했다는 말은 거짓이었군요.", "주인공|하지만 이 거짓말만으로 브로치 도난의 범인이라고 단정할 수는 없어."); Action(Last(pa), "PACompleted");
        var pb = Chain("PB", "주인공|B는 기한이 지난 전당포증을 쓰레기통에 버렸다.", "주인공|전당포증에 적힌 숫자는 2. B가 서 있던 진열장 번호와 같아.", "주인공|꽃병 속 장난감의 태엽과 나사는 정전 뒤에 발견됐다.", "주인공|꽃병이 깨진 소리로 시선을 돌리고, 드라이버로 2번 진열장을 열려 했군.", "B|맞습니다. 기한을 놓쳐서 돌려받을 수 없게 된 제 물건을 가져가려 했어요.", "B|하지만 그 브로치에는 손대지 않았습니다!", "주인공|아무것도 하지 않았다는 말은 거짓이었어. 다만 노린 물건은 브로치가 아니야."); Action(Last(pb), "PBCompleted");
        Action(Last(Existing("S")), "S3Seen"); Action(Existing("T4_Missing_08"), "MemoryReturnUnlocked");
        Action(Existing("S6_09 F"), "OwnerSearchRequested"); Action(Existing("S6_19 B"), "OwnerConfessed");
        Action(Existing("S7_13 A"), "AQuestioned"); Action(Existing("End_56"), "CaseSolved");
        if (stories.TryGetValue("S7_04 A", out var red) && red.text.Contains("<color=red>") && !red.text.Contains("</color>")) { red.text += "</color>"; changed.Add(red); }
        Line("LateMemory", "다시 기억으로 들어가 볼게요. 불이 꺼지기 전과 후의 시계 상자를 확인해 주세요.", "조수");
        Line("LateAHint", "정전 전후의 시계 상자가 어떻게 달라졌는지 확인해 보자.", "주인공");
    }

    static SceneFlowContainer.SceneEntry Entry(string name)
    {
        var entry = flow.FindScene(PathFor(name));
        if (entry == null) { entry = new SceneFlowContainer.SceneEntry { scene = Ref(name) }; flow.scenes.Add(entry); }
        return entry;
    }
    static string Route(string key, string from, string to, SceneConnectionTrigger trigger, StoryData last = null, params DialogCondition[] conditions)
    {
        string id = "pdf-" + key;
        flow.connections.RemoveAll(r => r.id == id);
        flow.connections.Add(new SceneFlowContainer.Connection { id = id, memo = "PDF: " + key, from = Ref(from), to = Ref(to), trigger = trigger, completedDialog = last, conditions = conditions.ToList(), fade = from != to });
        return id;
    }
    static void ConfigureFlow()
    {
        // Replace only the obsolete shortcut and unconditional exits; existing T0/T1/T2/T3 connections stay intact.
        flow.connections.RemoveAll(r => r.id.StartsWith("pdf-") || r.id == "b9abfe1d87bf4ebbad8299b016b6f659" || r.id == "676c86577f5d43c1a9960dda5be18db2" || r.id == "d06f52a18a3543599682f1973545a377");
        Entry("S2").entryDialog = Existing("Ait_1"); Entry("S2").lockPlayerInput = true;
        Entry("S2").hidePlayer = false;
        foreach (string name in new[] { "T0", "S3", Reality }) { Entry(name).useSharedCamera = true; Entry(name).hidePlayer = false; }
        Entry("S3").entryDialog = null;
        Entry("S3").entryDialogs = Container("S3Entry", Slot(Existing("S"), C("S3Seen", 0)));
        var late = Entry(Reality); late.lockPlayerInput = false; late.entryDialog = null;
        late.playerPosition = new Vector2(0, -1);
        late.entryDialogs = Container("OwnerEntry", Slot(Existing("S6_01 B"), C("OpenBoxFound"), C("OwnerConfessed", 0)),
            Slot(Existing("S6_01 F"), C("PACompleted"), C("PBCompleted"), C("OpenBoxFound", 0)));
        Route("owner-dark-search", Reality, T35, SceneConnectionTrigger.DialogFinished, Existing("S6_09 F"), C("OwnerSearchRequested"), C("PACompleted"), C("PBCompleted"), C("OpenBoxFound", 0));
        Route("found-open-box", T35, Reality, SceneConnectionTrigger.SceneCompleted, null, C("OpenBoxFound"));
        Route("late-reentry", Reality, "T1", SceneConnectionTrigger.DialogFinished, Existing("LateMemory"), C("AQuestioned"));
        foreach (string name in new[] { "T1", "T2", T3, T4 })
        {
            Route("exit-late-" + name, name, Reality, SceneConnectionTrigger.DialogFinished, Existing("ExitYes"), C("MemoryReturnUnlocked"), C("OwnerConfessed"));
            Route("exit-" + name, name, "S3", SceneConnectionTrigger.DialogFinished, Existing("ExitYes"), C("MemoryReturnUnlocked"), C("OwnerConfessed", 0));
        }
        foreach (var route in flow.connections) if (route.from.Name == "S2" || route.from.Name == "S3") route.fade = true;
        changed.Add(flow);
    }

    static InvestigationPoint.Rule Rule(StoryData dialogue, DialogCondition[] conditions = null, string[] rewards = null, string[] actions = null, string[] required = null)
    {
        return new InvestigationPoint.Rule { memo = dialogue != null ? dialogue.name : "Travel", dialogue = dialogue,
            conditions = conditions?.ToList() ?? new List<DialogCondition>(), rewards = rewards?.Select(n => items[n]).ToArray() ?? Array.Empty<InventoryItem>(),
            actions = actions?.Select(n => A(n)).ToList() ?? new List<DialogAction>(), requiredItems = required?.Select(n => items[n]).ToArray() ?? Array.Empty<InventoryItem>() };
    }
    static void ConfigurePoint(GameObject target, bool memoryOnly, ConditionID collected, params InvestigationPoint.Rule[] rules)
    {
        foreach (var npc in target.GetComponents<NPC>()) Object.DestroyImmediate(npc);
        var point = target.GetComponent<InvestigationPoint>() ?? target.AddComponent<InvestigationPoint>();
        Set(point, "memoryOnly", memoryOnly); Set(point, "flow", flow); Set(point, "rules", rules); Set(point, "collected", collected);
        target.layer = LayerMask.NameToLayer("Col 1F"); target.tag = "Interactable";
    }
    static GameObject Point(Transform parent, string name, Vector2 position, ConditionID collected, params InvestigationPoint.Rule[] rules)
    {
        var target = new GameObject(name); target.transform.SetParent(parent, false); target.transform.position = position;
        var collider = target.AddComponent<BoxCollider2D>(); collider.size = new Vector2(0.65f, 0.65f); collider.offset = new Vector2(0, -0.5f);
        var visual = new GameObject("Investigation Sparkle").AddComponent<SpriteRenderer>(); visual.transform.SetParent(target.transform, false);
        visual.sprite = sparkle; visual.sortingOrder = 20; visual.transform.localScale = Vector3.one * 0.7f;
        ConfigurePoint(target, true, collected, rules); Set(target.GetComponent<InvestigationPoint>(), "sparkle", visual); return target;
    }
    static void Collect(Transform parent, string name, Vector2 position, string flag, string story, params string[] rewards)
    {
        Point(parent, name, position, Flag(flag), Rule(Existing(story), new[] { C(flag, 0) }, rewards, new[] { flag }), Rule(Existing("AlreadyChecked")));
    }
    static void BuildMemory(string name)
    {
        var scene = Open(name); var root = Root(scene, "Tutorial Investigations");
        // Disable only the older generated interactions that these data-driven points replace.
        foreach (var obj in Objects(scene).ToArray())
            if (obj.name == "T4 Leave Memory" || obj.name == "T4 Display Cabinet Investigation") obj.SetActive(false);
        var a = Objects(scene).FirstOrDefault(o => o.name == "A");
        if (a == null) a = CloneActor(actorA, root.transform, "A", new Vector2(1, 1));
        var b = Objects(scene).FirstOrDefault(o => o.name == "B");
        if (b == null) b = CloneActor(actorB, root.transform, "B", new Vector2(2, 1));
        var arules = new List<InvestigationPoint.Rule>();
        if (name == T4 || name == "T2")
        {
            string route = Route("start-PA-" + name, name, "Tutorial_PA_Alibi", SceneConnectionTrigger.Manual, null, C("S3Seen"), C("PACompleted", 0), C("PhoneInspected"));
            var deduction = Rule(null, new[] { C("S3Seen"), C("PACompleted", 0), C("PhoneInspected") }, required: new[] { "PocketWatch", "WatchBox" });
            deduction.actions.Add(A("DeductionReturnPhase", name == "T2" ? 2 : 4));
            deduction.anyItems = new[] { items["ReturnedLetter"], items["CrumpledLetter"] }; deduction.connectionId = route; arules.Add(deduction);
        }
        arules.Add(Rule(Existing("WatchBoxFoundHeavy"), new[] { C("ABoxCollected", 0), C("T3Occurred") }, new[] { "WatchBox" }, new[] { "ABoxCollected" }));
        arules.Add(Rule(Existing("WatchBoxFound"), new[] { C("ABoxCollected", 0), C("T3Occurred", 0) }, new[] { "WatchBox" }, new[] { "ABoxCollected" }));
        arules.Add(Rule(Existing("ATruthRepeat"), new[] { C("PACompleted") })); arules.Add(Rule(Existing("AHint")));
        ConfigurePoint(a, true, null, arules.ToArray());
        var brules = new List<InvestigationPoint.Rule>();
        if (name == T4 || name == "T2")
        {
            string route = Route("start-PB-" + name, name, "Tutorial_PB_Alibi", SceneConnectionTrigger.Manual, null, C("S3Seen"), C("PBCompleted", 0), C("T4CabinetInspected"));
            var deduction = Rule(null, new[] { C("S3Seen"), C("PBCompleted", 0), C("T4CabinetInspected") }, required: new[] { "FlatheadDriver", "ExpiredPawnTicket", "Spring", "Screw" });
            deduction.actions.Add(A("DeductionReturnPhase", name == "T2" ? 2 : 4));
            deduction.connectionId = route; brules.Add(deduction);
        }
        brules.Add(Rule(Existing("DriverFound"), new[] { C("DriverCollected", 0) }, new[] { "FlatheadDriver" }, new[] { "DriverCollected" }));
        brules.Add(Rule(Existing("BTruthRepeat"), new[] { C("PBCompleted") })); brules.Add(Rule(Existing("BHint"))); ConfigurePoint(b, true, null, brules.ToArray());
        Collect(root.transform, "Counter Watch", new Vector2(6, 3), "WatchCollected", "WatchFound", "PocketWatch");
        Collect(root.transform, "Trash Can", Vector2.zero, "LetterCollected", "TrashFound", "ReturnedLetter", "ExpiredPawnTicket");
        var phone = Objects(scene).FirstOrDefault(o => o.name == "phone");
        if (phone != null) ConfigurePoint(phone, true, null, Rule(Existing("PhoneClue"), actions: new[] { "PhoneInspected" }));
        else Point(root.transform, "Telephone", new Vector2(0, 2), null, Rule(Existing("PhoneClue"), actions: new[] { "PhoneInspected" }));
        Point(root.transform, "Cabinet Number 2", new Vector2(3, 2), Flag("T4CabinetInspected"), Rule(Existing("CabinetClue"), new[] { C("T3Occurred") }, actions: new[] { "T4CabinetInspected" }), Rule(Existing("AlreadyChecked")));
        if (name == "T2")
        {
            var vase = Objects(scene).FirstOrDefault(o => o.name == "VaseNormal");
            if (vase != null) ConfigurePoint(vase, true, Flag("ToyCollected"), Rule(Existing("ToyFound"), new[] { C("ToyCollected", 0) }, new[] { "ClockworkToy" }, new[] { "ToyCollected" }), Rule(Existing("AlreadyChecked")));
            else Collect(root.transform, "Vase Toy", new Vector2(4, 0), "ToyCollected", "ToyFound", "ClockworkToy");
        }
        var unknown = Rule(Existing("UnknownSpace")); unknown.showUnknownSpace = true;
        Point(root.transform, "Unremembered Back Door", new Vector2(0, 3), null, unknown);
        Point(root.transform, "Memory Exit", new Vector2(5, -3), null, Rule(Existing("ExitPrompt"), new[] { C("MemoryReturnUnlocked") }), Rule(Existing("ExitPromptBlocked")));
        Presentation(scene, root.transform, false, false, null);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static void BuildDeduction(string type, GameObject actor)
    {
        string name = "Tutorial_" + type + "_Alibi", path = PathFor(name);
        Scene scene;
        if (File.Exists(path)) scene = Open(name);
        else { scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); ownedScenes.Add(scene); opened[name] = scene; }
        var root = Root(scene, "Tutorial Deduction");
        var grid = Open(T4).GetRootGameObjects().First(o => o.GetComponentInChildren<GridLayout>(true) != null);
        Object.Instantiate(grid, root.transform).name = "Existing Shop Tilemap";
        var target = CloneActor(actor, root.transform, type == "PA" ? "A" : "B", new Vector2(2, 1));
        foreach (var collider in target.GetComponentsInChildren<Collider2D>()) Object.DestroyImmediate(collider);
        var entry = Entry(name); entry.memorySpace = true; entry.useSharedCamera = true; entry.hidePlayer = false; entry.lockPlayerInput = true;
        entry.placePlayer = true; entry.playerPosition = new Vector2(2, -1); entry.entryDialog = Existing(type + "_01");
        Presentation(scene, root.transform, true, false, target.transform);
        var track = root.AddComponent<TutorialDeductionTrack>(); Set(track, "actor", target.transform);
        var positions = type == "PA" ? new[] { Vector2.zero, new Vector2(5, 2), new Vector2(0, 2), new Vector2(3, 0) } :
            new[] { Vector2.zero, new Vector2(4, 0), new Vector2(3, 2), new Vector2(3, 0) };
        var lines = type == "PA" ? new[] { "01", "03", "04", "05" } : new[] { "01", "03", "04", "05" };
        Set(track, "beats", positions.Select((p, i) => new TutorialDeductionTrack.Beat { dialogue = Existing(type + "_" + lines[i]), position = p }).ToArray());
        string other = type == "PA" ? "PBCompleted" : "PACompleted";
        Route(type + "-both-truths", name, Reality, SceneConnectionTrigger.DialogFinished, Last(entry.entryDialog), C(other));
        Route(type + "-back-T2", name, "T2", SceneConnectionTrigger.DialogFinished, Last(entry.entryDialog), C(other, 0), C("DeductionReturnPhase", 2));
        Route(type + "-back", name, T4, SceneConnectionTrigger.DialogFinished, Last(entry.entryDialog), C(other, 0));
        EditorSceneManager.SaveScene(scene, path);
    }

    static void BuildReality()
    {
        var scene = Open(Reality); var root = Root(scene, "Tutorial Case Resolution");
        foreach (var old in Objects(scene).SelectMany(o => o.GetComponents<TutorialS6EventController>()).ToArray()) old.enabled = false;
        foreach (var old in Objects(scene).Where(o => o.name == "A(S#END)")) old.SetActive(false);
        var a = Objects(scene).FirstOrDefault(o => o.name == "A" || o.name == "A(S#7)") ?? CloneActor(actorA, root.transform, "A", new Vector2(6, 1));
        a.name = "A";
        ConfigurePoint(a, false, null,
            Rule(Existing("End_01"), new[] { C("FinalBoxEvidence"), C("OwnerConfessed"), C("AQuestioned"), C("CaseSolved", 0) }),
            Rule(Existing("S7_01 A"), new[] { C("OwnerConfessed"), C("AQuestioned", 0) }), Rule(Existing("LateAHint")));
        var assistant = Objects(scene).FirstOrDefault(o => o.name.Equals("assist", StringComparison.OrdinalIgnoreCase)) ?? CloneActor(actorAssistant, root.transform, "assist", new Vector2(-1, -2));
        ConfigurePoint(assistant, false, null, Rule(Existing("LateMemory"), new[] { C("AQuestioned"), C("CaseSolved", 0) }), Rule(Existing("LateAHint")));
        var owner = Objects(scene).FirstOrDefault(o => o.name == "C" || o.name == "Owner");
        if (owner != null) ConfigurePoint(owner, false, null, Rule(Existing("S6_01 B"), new[] { C("OwnerConfessed") }), Rule(Existing("LateAHint")));
        foreach (var floor in Objects(scene).Where(o => o.name == "Floor"))
            foreach (var npc in floor.GetComponents<NPC>()) Object.DestroyImmediate(npc);
        foreach (var obsolete in Objects(Open("S3")).SelectMany(o => o.GetComponents<SceneTransitionPoint>()))
            if (obsolete.connectionId == "b9abfe1d87bf4ebbad8299b016b6f659") { obsolete.gameObject.SetActive(false); EditorSceneManager.MarkSceneDirty(Open("S3")); }
        EditorSceneManager.MarkSceneDirty(scene);
    }
    static void BuildDarkMemory()
    {
        var scene = Open(T35); var controller = Objects(scene).Select(o => o.GetComponent<TutorialT35EventController>()).First(c => c != null);
        Set(controller, "foundEvidence", items["OpenJewelryBox"]); Set(controller, "boxFound", Flag("OpenBoxFound"));
        Set(controller, "darkOverlayImage", null); Set(controller, "fadeOutAfterGetBox", false);
        foreach (var image in Objects(scene).SelectMany(o => o.GetComponents<Image>()).ToArray())
            if (image.name.Contains("Blackout") || image.name.Contains("Dark")) image.gameObject.SetActive(false);
        var root = Root(scene, "Tutorial Search Presentation"); Presentation(scene, root.transform, false, true, null);
        EditorSceneManager.MarkSceneDirty(scene);
    }
    static void Presentation(Scene scene, Transform parent, bool deduction, bool darkMemory, Transform focus)
    {
        var obj = new GameObject("Memory Presentation"); obj.transform.SetParent(parent, false);
        var display = obj.AddComponent<TutorialScenePresentation>(); Set(display, "deduction", deduction); Set(display, "darkMemory", darkMemory); Set(display, "focusActor", focus);
        var canvasObject = new GameObject("Memory Vision", typeof(RectTransform), typeof(Canvas)); canvasObject.transform.SetParent(obj.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -5;
        var imageObject = new GameObject("Unknown Space", typeof(RectTransform), typeof(Image)); imageObject.transform.SetParent(canvasObject.transform, false);
        var image = imageObject.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
        var rect = image.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Set(display, "unknownOverlay", image); imageObject.SetActive(false);
        if (deduction || darkMemory)
        {
            var visionObject = new GameObject("Local Sight", typeof(RectTransform), typeof(Image)); visionObject.transform.SetParent(canvasObject.transform, false);
            var visionImage = visionObject.GetComponent<Image>(); visionImage.raycastTarget = false;
            rect = visionImage.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var vision = visionObject.AddComponent<FlashlightFreeSightOverlay>(); Set(vision, "overlayImage", visionImage);
            Set(vision, "overlayShader", Shader.Find("Naknak/Flashlight Free Sight Overlay")); Set(vision, "visionRadiusWorld", darkMemory ? 1.85f : 4f);
            Set(display, "vision", vision);
        }
        if (deduction)
        {
            var effect = obj.AddComponent<FlashlightWorldDesaturateEffect>(); Set(effect, "exclusions", new[] { focus }); Set(display, "grayscale", effect);
        }
    }
    static void BuildObjectives()
    {
        string path = "Assets/Resources/TutorialCase.asset";
        var config = AssetDatabase.LoadAssetAtPath<TutorialCaseDefinition>(path);
        if (config == null) { config = ScriptableObject.CreateInstance<TutorialCaseDefinition>(); AssetDatabase.CreateAsset(config, path); }
        config.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pretendard-Regular SDF.asset"); config.solved = Flag("CaseSolved");
        config.objectives = new List<TutorialCaseDefinition.Objective>();
        Objective(config, T35, "정전 당시 주인이 떨어뜨린 보석함을 찾자. 카운터 뒤를 조사하자.");
        Objective(config, "T0", "보석함을 조사하고 브로치를 넣은 뒤 전당포로 가자.");
        foreach (string name in new[] { "T2", T4 })
        {
            Objective(config, name, "정전 전후의 시계 상자를 아이템 메뉴에서 확인하고 현실로 돌아가자.", C("AQuestioned"));
            Objective(config, name, "A와 B의 진술을 검증할 증거를 모으고 두 사람을 조사하자.", C("MemoryReturnUnlocked"));
            Objective(config, name, "주인에게 감정을 의뢰하자. 주변 물건도 조사할 수 있다.");
        }
        Objective(config, "S3", "조수에게 말을 걸어 기억을 재탐색하자. 재탐색 중 Ctrl로 대사를 넘길 수 있다.");
        Objective(config, Reality, "현실의 A에게 시계 상자의 변화를 물어보자.", C("FinalBoxEvidence"));
        Objective(config, Reality, "A를 추궁한 뒤 조수와 함께 정전 전후를 다시 확인하자.", C("OwnerConfessed"));
        changed.Add(config);
    }
    static void Objective(TutorialCaseDefinition config, string scene, string text, params DialogCondition[] conditions) =>
        config.objectives.Add(new TutorialCaseDefinition.Objective { scene = Ref(scene), text = text, conditions = conditions.ToList() });
}
