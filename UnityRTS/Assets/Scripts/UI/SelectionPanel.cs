using System.Collections.Generic;
using System.Text;
using RTS.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The bottom of the screen beside the mini map: what is selected (picture, HP, values, queue; several units as tiles) and the action
/// buttons made from the game data:
///  - a Villager is selected: one button per buildable building (starts the placement ghost),
///  - a finished own building is selected: one button per unit it trains and technology it researches.
/// A button is dark (its tooltip says why) when GameActionValidator says the action is not allowed right now.
/// The host checks again when the command arrives, so this is only for convenience.
/// </summary>
public class SelectionPanel : MonoBehaviour
{
    const int Columns = 4, MaxTiles = 16;

    readonly List<GameObject> buttons = new List<GameObject>();
    readonly List<System.Action> refreshers = new List<System.Action>();
    string builtFor = "";
    float nextRefresh;
    GameActionValidator validator;

    RectTransform infoBox, commandBox, single, several;
    RawImage picture;
    TMP_Text title, values, activity, hpText, severalTitle, nothing;
    UiKit.Bar hp, work;
    readonly RawImage[] queue = new RawImage[ProductionQueue.MaxLength];
    readonly GameObject[] queueSlots = new GameObject[ProductionQueue.MaxLength];

    class Tile { public Button Button; public RawImage Icon; public UiKit.Bar Hp; public NetworkUnit Unit; }
    readonly List<Tile> tiles = new List<Tile>();

    void Awake()
    {
        UiKit.Clear(transform);
        var own = GetComponent<Image>(); if (own != null) { own.enabled = false; own.raycastTarget = false; }
        var rt = UiKit.At((RectTransform)transform, 0, 0, 0, 0, 10, 10); // only an anchor for the two boxes
        float x = 12 + 4 + 212 * MiniMap.Aspect + 10;

        infoBox = UiKit.Frame(rt, "Info"); UiKit.At((RectTransform)infoBox.parent, 0, 0, x, 12, 440, 216);
        nothing = UiKit.Text(infoBox, "Select units or a building", 21, UiKit.Dim, TextAlignmentOptions.Center, FontStyles.Italic); UiKit.Stretch(nothing.rectTransform, 0, 0, 1, 1, 10, 10, -10, -10);

        single = UiKit.Stretch(UiKit.Empty(infoBox, "Single"), 0, 0, 1, 1, 0, 0, 0, 0);
        var frame = UiKit.Box(single, "Picture", new Color(0.12f, 0.22f, 0.40f), true); UiKit.At(frame, 0, 1, 16, -16, 104, 104);
        picture = UiKit.Icon(frame, "villager", UiKit.Paper); UiKit.Stretch(picture.rectTransform, 0, 0, 1, 1, 10, 10, -10, -10);
        title = UiKit.Fit(UiKit.Text(single, "", 28, UiKit.Gold, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), 16); UiKit.At(title.rectTransform, 0, 1, 136, -12, 290, 38);
        hp = new UiKit.Bar(single, UiKit.Green); UiKit.At(hp.Root, 0, 1, 136, -54, 286, 22);
        hpText = UiKit.Text(hp.Root, "", 16, Color.white, TextAlignmentOptions.Center, FontStyles.Bold); UiKit.Stretch(hpText.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
        values = UiKit.Fit(UiKit.Text(single, "", 20, UiKit.Paper), 13); UiKit.At(values.rectTransform, 0, 1, 136, -82, 290, 38);
        activity = UiKit.Fit(UiKit.Text(single, "", 20, UiKit.Dim), 13); UiKit.At(activity.rectTransform, 0, 1, 16, -124, 406, 28);
        work = new UiKit.Bar(single, UiKit.Gold); UiKit.At(work.Root, 0, 0, 16, 14, 406, 10);
        for (int i = 0; i < queue.Length; i++)
        {
            var slot = UiKit.Box(single, "Queue", UiKit.Field, true); UiKit.At(slot, 0, 0, 16 + i * 46, 28, 40, 32); queueSlots[i] = slot.gameObject;
            queue[i] = UiKit.Icon(slot, "time", UiKit.Paper); UiKit.At(queue[i].rectTransform, 0.5f, 0.5f, 0, 0, 26, 26);
        }

        several = UiKit.Stretch(UiKit.Empty(infoBox, "Several"), 0, 0, 1, 1, 0, 0, 0, 0);
        severalTitle = UiKit.Fit(UiKit.Text(several, "", 20, UiKit.Gold, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), 13); UiKit.At(severalTitle.rectTransform, 0, 1, 16, -8, 406, 30);
        for (int i = 0; i < MaxTiles; i++)
        {
            var t = new Tile();
            t.Button = UiKit.Button(several, "Unit", new Color(0.12f, 0.22f, 0.40f), () => Keep(t.Unit)); UiKit.At((RectTransform)t.Button.transform, 0, 1, 14 + i % 8 * 52, -44 - i / 8 * 80, 48, 70);
            t.Icon = UiKit.Icon(t.Button.transform, "villager", UiKit.Paper); UiKit.At(t.Icon.rectTransform, 0.5f, 1, 0, -6, 38, 38);
            t.Hp = new UiKit.Bar(t.Button.transform, UiKit.Green); UiKit.Stretch(t.Hp.Root, 0, 0, 1, 0, 4, 6, -4, 14);
            tiles.Add(t);
        }

        commandBox = UiKit.Frame(rt, "Commands"); UiKit.At((RectTransform)commandBox.parent, 0, 0, x + 450, 12, 16 + Columns * 96, 216);
        commandBox.parent.gameObject.SetActive(false);
    }

    /// <summary>A click on a tile keeps only this unit selected.</summary>
    static void Keep(NetworkUnit u)
    {
        var sel = SelectionManager.Instance;
        if (sel == null || u == null) return;
        sel.Clear(); sel.Units.Add(u);
    }

    void Update()
    {
        var sel = SelectionManager.Instance; var relay = CommandRelay.Local;
        if (sel == null || relay == null) return;
        if (validator == null) validator = new GameActionValidator(GameDataProvider.Get());

        string key = SelectionKey(sel);
        bool changed = key != builtFor;
        if (changed) { Rebuild(sel, relay); builtFor = key; }

        if (!changed && Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.2f;
        UpdateInfo(sel);
        foreach (var r in refreshers) r();
    }

    // Identifies the selection plus the building's completion, so the buttons are rebuilt when construction ends.
    static string SelectionKey(SelectionManager sel)
    {
        var sb = new StringBuilder();
        if (sel.Building != null) sb.Append("B").Append(sel.Building.Object.Id.Raw).Append(sel.Building.Completed);
        bool villager = false;
        foreach (var u in sel.Units) if (u.CanBuild) villager = true;
        sb.Append("U").Append(sel.Units.Count).Append(villager);
        return sb.ToString();
    }

    // ---- action buttons ----
    void Rebuild(SelectionManager sel, CommandRelay relay)
    {
        foreach (var b in buttons) { b.SetActive(false); Destroy(b); }
        buttons.Clear(); refreshers.Clear();
        UiKit.HideTip();
        var data = GameDataProvider.Get();
        var state = relay.GetComponent<NetworkPlayerState>();
        string ownBase = state.Slot == 1 ? "P1Base" : "P2Base";

        if (sel.Units.Count > 0)
        {
            bool villager = false;
            foreach (var u in sel.Units) if (u.CanBuild) villager = true;
            if (villager)
                foreach (var def in data.Buildings.Values)
                {
                    if (!def.Buildable) continue;
                    var d = def;
                    var action = new PlanAction { ActionType = "BUILD", BuildingType = d.Key, TargetZone = ownBase };
                    AddButton("Build " + EntityVisuals.Pretty(d.Key), d, action, state, () => BuildPlacement.Instance.BeginPlacement(d.Key));
                }
        }
        else if (sel.Building != null && sel.Building.Completed)
        {
            var b = sel.Building; var bdef = data.Buildings[b.TypeKey.ToString()];
            var me = relay.Object.InputAuthority;   // not b.Owner: b may be despawned when the button refreshes
            foreach (var uk in bdef.Trains)
            {
                var k = uk; var udef = data.Units[k];
                var action = new PlanAction { ActionType = "TRAIN_UNIT", UnitType = k };
                AddButton("Train " + EntityVisuals.Pretty(k), udef, action, state, () => relay.SendTrain(k, b.Object.Id), null, () => QueueBlock(b, k, false));
            }
            foreach (var tk in bdef.Researches)
            {
                var k = tk; var tdef = data.Techs[k];
                var action = new PlanAction { ActionType = "RESEARCH", TechType = k };
                AddButton("Research " + EntityVisuals.Pretty(k), tdef, action, state, () => relay.SendResearch(k, b.Object.Id), () => state.HasTech(k), () => NetworkBuilding.BeingResearched(k, me) ? "already being researched" : QueueBlock(b, k, true));
            }
        }
        commandBox.parent.gameObject.SetActive(buttons.Count > 0);
    }

    // the building may have been destroyed since the buttons were made
    static string QueueBlock(NetworkBuilding b, string key, bool research)
    {
        return b == null || b.Object == null || !b.Object.IsValid ? null : b.WhyNotQueue(key, research);
    }

    void AddButton(string label, DefRecord def, PlanAction action, NetworkPlayerState state, System.Action onClick, System.Func<bool> alreadyDone = null, System.Func<string> blocked = null)
    {
        int n = buttons.Count;
        var button = UiKit.Button(commandBox, label, UiKit.Field, onClick);
        UiKit.At((RectTransform)button.transform, 0, 1, 10 + n % Columns * 96, -10 - n / Columns * 98, 90, 92);
        var icon = UiKit.Icon(button.transform, def.Key, UiKit.Paper); UiKit.At(icon.rectTransform, 0.5f, 1, 0, -6, 50, 50);
        var cost = UiKit.Fit(UiKit.Text(button.transform, UiKit.CostText(def.Cost, false), 18, UiKit.Paper, TextAlignmentOptions.Center, FontStyles.Bold), 11); UiKit.Stretch(cost.rectTransform, 0, 0, 1, 0, 2, 4, -2, 32);
        buttons.Add(button.gameObject);

        string reason = null; bool done = false;
        UiHover.Add(button, () => "<b><color=#EDC25C>" + label + "</color></b>\n" + UiKit.CostText(def.Cost, true)
            + (def.TimeSeconds > 0 ? "   <color=#A8B0BC>" + def.TimeSeconds + " s</color>" : "") + Values(def) + "\n<size=90%><color=#C9CCD2>" + def.Description + "</color></size>"
            + (done ? "\n<color=#5CC760>Done</color>" : reason != null ? "\n<color=#F2594A>" + reason + "</color>" : ""));

        refreshers.Add(() =>
        {
            if (button == null) return;
            if (alreadyDone != null && alreadyDone())
            {
                done = true; button.interactable = false; icon.color = UiKit.Green; cost.text = "<color=#5CC760>done</color>";
                return;
            }
            var result = validator.Validate(action, state.ToCore());
            string queueReason = blocked != null ? blocked() : null;
            if (result.Ok && queueReason != null) result = ValidationResult.Fail(queueReason);
            button.interactable = result.Ok;
            reason = result.Ok ? null : result.Reason; // from the validator, so the player knows why the button is dark
            icon.color = result.Ok ? UiKit.Paper : new Color(0.55f, 0.56f, 0.58f, 0.75f);
            cost.alpha = result.Ok ? 1f : 0.55f;
        });
    }

    static string Values(DefRecord d)
    {
        if (d.Kind == "unit") return "\n<color=#A8B0BC>HP " + d.Hp + "   attack " + d.Attack + "   armor " + d.Armor + "   range " + d.Range + "</color>";
        return d.Kind == "building" ? "\n<color=#A8B0BC>HP " + d.Hp + "</color>" : "";
    }

    // ---- what is selected ----
    void UpdateInfo(SelectionManager sel)
    {
        var data = GameDataProvider.Get();
        bool one = sel.Units.Count == 1 || sel.Units.Count == 0 && sel.Building != null;
        single.gameObject.SetActive(one); several.gameObject.SetActive(sel.Units.Count > 1); nothing.gameObject.SetActive(!one && sel.Units.Count == 0);

        if (sel.Units.Count == 1)
        {
            var u = sel.Units[0]; var def = data.Units[u.TypeKey.ToString()];
            Head(def, u.Hp);
            values.text = "Attack <b>" + def.Attack + "</b>    Armor <b>" + def.Armor + "</b>    Range <b>" + def.Range + "</b>";
            var g = u.GetComponent<VillagerGatherer>();
            activity.text = u.AttackTarget.IsValid ? "Attacking" : g != null && g.Busy ? "Working" : u.HasMoveTarget ? "Moving" : "Idle";
            ShowQueue(0, null); work.Root.gameObject.SetActive(false);
        }
        else if (sel.Units.Count > 1)
        {
            var counts = new SortedDictionary<string, int>();
            foreach (var u in sel.Units) { string k = u.TypeKey.ToString(); counts[k] = counts.ContainsKey(k) ? counts[k] + 1 : 1; }
            var sb = new StringBuilder(sel.Units.Count + " units:");
            foreach (var kv in counts) sb.Append("  ").Append(kv.Value).Append(' ').Append(EntityVisuals.Pretty(kv.Key));
            severalTitle.text = sb.ToString();
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i]; bool on = i < sel.Units.Count;
                if (t.Button.gameObject.activeSelf != on) t.Button.gameObject.SetActive(on);
                t.Unit = on ? sel.Units[i] : null;
                if (!on) continue;
                string k = t.Unit.TypeKey.ToString(); float part = t.Unit.Hp / data.Units[k].Hp;
                UiKit.SetIcon(t.Icon, k); t.Hp.Set(part, UiKit.HpColour(part));
            }
        }
        else if (sel.Building != null)
        {
            var b = sel.Building; var def = data.Buildings[b.TypeKey.ToString()];
            Head(def, b.Hp);
            values.text = def.Key == "house" ? "Room for 5 more" : def.Trains.Count > 0 ? "Trains units" : "";
            bool working = b.Completed && b.QueueCount > 0;
            if (!b.Completed) activity.text = "Under construction  " + Mathf.RoundToInt(b.BuildProgress * 100) + "%";
            else if (working) activity.text = (b.CurrentIsResearch ? "Researching " : "Training ") + EntityVisuals.Pretty(b.CurrentKey.ToString()) + "  " + Mathf.RoundToInt(b.CurrentProgress * 100) + "%";
            else activity.text = "";
            work.Root.gameObject.SetActive(!b.Completed || working);
            work.Set(b.Completed ? b.CurrentProgress : b.BuildProgress);
            ShowQueue(working ? b.QueueCount : 0, b.CurrentKey.ToString());
        }
    }

    void Head(DefRecord def, float now)
    {
        UiKit.SetIcon(picture, def.Key);
        title.text = EntityVisuals.Pretty(def.Key);
        float part = def.Hp > 0 ? now / def.Hp : 0f;
        hp.Set(part, UiKit.HpColour(part));
        hpText.text = Mathf.CeilToInt(now) + " / " + def.Hp;
    }

    // a client knows what the queue is working on and how many wait, not what they are
    void ShowQueue(int count, string current)
    {
        for (int i = 0; i < queue.Length; i++)
        {
            queueSlots[i].SetActive(i < count);
            if (i < count) UiKit.SetIcon(queue[i], i == 0 ? current : "time");
        }
    }
}
