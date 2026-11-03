using System.Reflection;
using Naknak.Inventory;

static class Program
{
    static int checks;
    static readonly ConditionID Chapter = new() { name = "S3Seen" }, Clue = new() { name = "Clue" },
        Completed = new() { name = "Completed" }, ReturnPhase = new() { name = "ReturnPhase" }, Collected = new() { name = "Collected" };
    static readonly InventoryItem Watch = Item("watch"), Box = Item("box"), Letter = Item("letter"),
        OpenLetter = Item("open-letter"), Driver = Item("driver"), Ticket = Item("ticket"), Spring = Item("spring"), Screw = Item("screw");
    static readonly InventoryItem[] Catalog = { Watch, Box, Letter, OpenLetter, Driver, Ticket, Spring, Screw };
    static readonly StoryData Wrong = new() { name = "Wrong" }, Missing = new() { name = "Missing" };
    static InventoryItem Item(string id) => new() { Id = id, DisplayName = id, MaxStack = 1 };
    static DialogCondition C(ConditionID id, int value) => new() { id = id, value = value };
    static void Check(bool result, string message)
    { if (!result) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static InvestigationPoint Setup(bool b = false)
    {
        ConditionManager.Instance = new(); SceneLoadManager.Instance = new(); DialogManager.Instance = new(); GameMenuController.Instance = new();
        GameStateManager.Instance = new(); GameMenuController.IsBlockingGameplay = false;
        SceneLoadManager.IsLoading = SceneLoadManager.IsRestoringSession = SceneLoadManager.IsPreparingMemory = SceneLoadManager.IsCutscene = false;
        SceneLoadManager.IsMemorySpace = true;
        GameMenuController.Inventory = new(Catalog);
        foreach (var item in Catalog.Where(i => i != OpenLetter)) GameMenuController.Inventory.Add(item.Id);
        ConditionManager.Instance.Set(Chapter, 1); ConditionManager.Instance.Set(Clue, 1);
        var rule = new InvestigationPoint.Rule
        {
            presentEvidence = true, evidencePrompt = "Present evidence", connectionId = b ? "PB" : "PA",
            conditions = new() { C(Chapter, 1), C(Completed, 0) }, evidenceConditions = new() { C(Clue, 1) },
            requiredItems = b ? new[] { Driver, Ticket, Spring, Screw } : new[] { Watch, Box },
            anyItems = b ? Array.Empty<InventoryItem>() : new[] { Letter, OpenLetter },
            wrongEvidenceDialog = Wrong, missingEvidenceDialog = Missing,
            actions = new() { new() { id = ReturnPhase, value = 4 } }
        };
        var flow = new SceneFlowContainer(); flow.connections.Add(new() { id = rule.connectionId, conditions = new() { C(Chapter, 1), C(Completed, 0), C(Clue, 1) } });
        var point = new InvestigationPoint(); Set(point, "flow", flow); Set(point, "rules", new[] { rule }); return point;
    }
    static void CloseDialogue(InvestigationPoint point) { DialogManager.Instance.IsDialogOpen = false; point.AdvanceCoroutines(); }
    static void Open(InvestigationPoint point)
    {
        point.Interact(); Check(GameMenuController.Instance.Selected != null, "Witness opens existing item selector");
        Check(SceneLoadManager.Instance.Travelled == null, "Possession alone does not start the route");
    }
    static void Main()
    {
        var point = Setup(); Open(point); GameMenuController.Instance.Cancel();
        Check(ConditionManager.Instance.Get(ReturnPhase) == 0 && SceneLoadManager.Instance.Travelled == null, "Cancel leaves progress unchanged");
        Open(point); GameMenuController.Instance.Choose(Driver);
        Check(DialogManager.Instance.CurrentStory == Wrong && SceneLoadManager.Instance.Travelled == null, "Unrelated proof cannot unlock A");
        Check(GameMenuController.Inventory.Count(Driver.Id) == 1, "Wrong proof is not consumed"); CloseDialogue(point);
        GameMenuController.Inventory.Remove(Watch.Id); Open(point); GameMenuController.Instance.Choose(Box);
        Check(DialogManager.Instance.CurrentStory == Missing && SceneLoadManager.Instance.Travelled == null, "Missing required evidence blocks the route");
        Check(ConditionManager.Instance.Get(ReturnPhase) == 0, "Failed proof executes no route actions"); CloseDialogue(point);
        GameMenuController.Inventory.Add(Watch.Id); ConditionManager.Instance.Set(Clue, 0); Open(point); GameMenuController.Instance.Choose(Watch);
        Check(DialogManager.Instance.CurrentStory == Missing && SceneLoadManager.Instance.Travelled == null, "Uninvestigated clue blocks the route"); CloseDialogue(point);
        ConditionManager.Instance.Set(Clue, 1); var before = GameMenuController.Inventory.Capture(); Open(point); GameMenuController.Instance.Choose(Letter);
        Check(SceneLoadManager.Instance.Travelled?.id == "PA", "Deliberately presenting valid A proof unlocks A's route");
        Check(before.All(i => GameMenuController.Inventory.Count(i.itemId) == i.quantity), "Successful presentation consumes no evidence");
        Check(ConditionManager.Instance.Get(ReturnPhase) == 4 && ConditionManager.Instance.Get(Completed) == 0, "Remember return timeline without prematurely completing deduction");
        var restored = new InventoryModel(Catalog); restored.Restore(GameMenuController.Inventory.Capture());
        Check(before.All(i => restored.Count(i.itemId) == i.quantity), "Presented proof remains in saved inventory data");

        point = Setup(); GameMenuController.Inventory.TryExchange(Letter.Id, OpenLetter.Id); Open(point); GameMenuController.Instance.Choose(OpenLetter);
        Check(SceneLoadManager.Instance.Travelled?.id == "PA", "Opened letter is accepted instead of its envelope");
        point = Setup(); ConditionManager.Instance.Set(Chapter, 0); point.Interact();
        Check(GameMenuController.Instance.Selected == null, "Cannot present before S3");
        point = Setup(); ConditionManager.Instance.Set(Completed, 1); point.Interact();
        Check(GameMenuController.Instance.Selected == null, "Completed routes do not request proof again");
        point = Setup(); SceneLoadManager.IsCutscene = true; point.Interact();
        Check(GameMenuController.Instance.Selected == null, "Cannot present during cutscenes");
        point = Setup(); SceneLoadManager.IsMemorySpace = false; point.Interact();
        Check(GameMenuController.Instance.Selected == null, "Memory evidence point cannot activate in reality");
        point = Setup(); Open(point); ConditionManager.Instance.Set(Chapter, 0); GameMenuController.Instance.Choose(Watch);
        Check(SceneLoadManager.Instance.Travelled == null, "Submission rechecks chapter eligibility");
        point = Setup(); Open(point); point.isActiveAndEnabled = false; GameMenuController.Instance.Choose(Watch);
        Check(SceneLoadManager.Instance.Travelled == null, "Stale callback on inactive actor cannot start a route");
        point = Setup(); GameMenuController.Instance.RejectSelection = true; point.Interact();
        GameMenuController.Instance.RejectSelection = false; Open(point); GameMenuController.Instance.Cancel();
        Check(!GameMenuController.IsBlockingGameplay, "Failed opening does not leave the interaction stuck busy");

        point = Setup(true); Open(point); GameMenuController.Instance.Choose(Watch);
        Check(DialogManager.Instance.CurrentStory == Wrong && SceneLoadManager.Instance.Travelled == null, "A proof cannot unlock B"); CloseDialogue(point);
        GameMenuController.Inventory.Remove(Spring.Id); Open(point); GameMenuController.Instance.Choose(Driver);
        Check(DialogManager.Instance.CurrentStory == Missing && SceneLoadManager.Instance.Travelled == null, "B requires both fragments and its other evidence"); CloseDialogue(point);
        GameMenuController.Inventory.Add(Spring.Id); Open(point); GameMenuController.Instance.Choose(Screw);
        Check(SceneLoadManager.Instance.Travelled?.id == "PB", "Valid B proof unlocks B's route");
        Check(GameMenuController.Inventory.Has(Driver.Id) && GameMenuController.Inventory.Has(Ticket.Id) && GameMenuController.Inventory.Has(Spring.Id) && GameMenuController.Inventory.Has(Screw.Id), "B keeps all presented evidence");

        point = Setup(); GameMenuController.Inventory.Remove(Screw.Id);
        Set(point, "rules", new[] { new InvestigationPoint.Rule { conditions = new() { C(Collected, 0) }, rewards = new[] { Screw }, actions = new() { new() { id = Collected, value = 1 } } } });
        point.Interact(); point.Interact();
        Check(GameMenuController.Inventory.Count(Screw.Id) == 1 && ConditionManager.Instance.Get(Collected) == 1, "Existing one-time pickup rules still work");
        Console.WriteLine($"PASS: {checks} evidence presentation assertions (engine-independent).");
    }
}
