using System.Collections.Generic;
using System.Text;
using RTS.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom panel. Shows what is selected (type, HP, queue progress) and creates the action buttons from the game data:
///  - a Villager is selected: one button per buildable building (starts the placement ghost),
///  - a finished own building is selected: one button per unit it trains and technology it researches.
/// A button is disabled (and shows why) when GameActionValidator says the action is not allowed right now.
/// The host checks again when the command arrives, so this is only for convenience.
/// </summary>
public class SelectionPanel : MonoBehaviour
{
    [SerializeField] TMP_Text info;               // selection info, several lines
    [SerializeField] RectTransform buttonRoot;    // empty RectTransform with a Layout Group; buttons are created inside
    [SerializeField] TMP_FontAsset font;          // optional; the TMP default is used when empty

    readonly List<GameObject> buttons = new List<GameObject>();
    readonly List<System.Action> refreshers = new List<System.Action>();
    string builtFor = "";
    float nextRefresh;
    GameActionValidator validator;

    void Update()
    {
        var sel = SelectionManager.Instance; var relay = CommandRelay.Local;
        if (sel == null || relay == null) return;
        if (validator == null) validator = new GameActionValidator(GameDataProvider.Get());

        string key = SelectionKey(sel);
        if (key != builtFor) { Rebuild(sel, relay); builtFor = key; }

        if (Time.unscaledTime < nextRefresh) return;
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
        foreach (var u in sel.Units) if (u.GetComponent<VillagerGatherer>() != null) villager = true;
        sb.Append("U").Append(sel.Units.Count).Append(villager);
        return sb.ToString();
    }

    void Rebuild(SelectionManager sel, CommandRelay relay)
    {
        foreach (var b in buttons) Destroy(b);
        buttons.Clear(); refreshers.Clear();
        var data = GameDataProvider.Get();
        var state = relay.GetComponent<NetworkPlayerState>();
        string ownBase = state.Slot == 1 ? "P1Base" : "P2Base";

        if (sel.Units.Count > 0)
        {
            bool villager = false;
            foreach (var u in sel.Units) if (u.GetComponent<VillagerGatherer>() != null) villager = true;
            if (!villager) return;
            foreach (var def in data.Buildings.Values)
            {
                if (!def.Buildable) continue;
                var d = def;
                var action = new PlanAction { ActionType = "BUILD", BuildingType = d.Key, TargetZone = ownBase };
                AddButton("Build " + d.Key, d, action, state, () => BuildPlacement.Instance.BeginPlacement(d.Key));
            }
        }
        else if (sel.Building != null && sel.Building.Completed)
        {
            var b = sel.Building; var bdef = data.Buildings[b.TypeKey.ToString()];
            foreach (var uk in bdef.Trains)
            {
                var k = uk; var udef = data.Units[k];
                var action = new PlanAction { ActionType = "TRAIN_UNIT", UnitType = k };
                AddButton("Train " + k, udef, action, state, () => relay.SendTrain(k, b.Object.Id));
            }
            foreach (var tk in bdef.Researches)
            {
                var k = tk; var tdef = data.Techs[k];
                var action = new PlanAction { ActionType = "RESEARCH", TechType = k };
                AddButton("Research " + k, tdef, action, state, () => relay.SendResearch(k, b.Object.Id), () => state.HasTech(k));
            }
        }
    }

    void AddButton(string label, DefRecord def, PlanAction action, NetworkPlayerState state, System.Action onClick, System.Func<bool> alreadyDone = null)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(buttonRoot, false);
        go.GetComponent<LayoutElement>().preferredHeight = 34; go.GetComponent<LayoutElement>().minHeight = 34;
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var rt = (RectTransform)textGo.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.fontSize = 14; text.alignment = TextAlignmentOptions.Center; text.color = Color.black;
        if (font != null) text.font = font;
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(() => onClick());
        buttons.Add(go);

        refreshers.Add(() =>
        {
            if (button == null) return;
            string cost = CostText(def.Cost);
            if (alreadyDone != null && alreadyDone()) { button.interactable = false; text.text = label + " (done)"; return; }
            var result = validator.Validate(action, state.ToCore());
            button.interactable = result.Ok;
            // Show the reason from the validator, so the player knows why the button is grey.
            text.text = result.Ok ? label + " " + cost : label + " " + cost + "\n<size=10>" + result.Reason + "</size>";
        });
    }

    static string CostText(Dictionary<string, int> cost)
    {
        var sb = new StringBuilder("(");
        bool first = true;
        foreach (var kv in cost) { if (!first) sb.Append(' '); sb.Append(kv.Value).Append(kv.Key[0]); first = false; }
        return sb.Append(')').ToString();
    }

    void UpdateInfo(SelectionManager sel)
    {
        if (info == null) return;
        var data = GameDataProvider.Get();
        var sb = new StringBuilder();
        if (sel.Units.Count == 1)
        {
            var u = sel.Units[0]; var def = data.Units[u.TypeKey.ToString()];
            sb.Append(def.Key).Append("  HP ").Append(Mathf.CeilToInt(u.Hp)).Append('/').Append(def.Hp)
              .Append("\nattack ").Append(def.Attack).Append("  armor ").Append(def.Armor).Append("  range ").Append(def.Range);
        }
        else if (sel.Units.Count > 1)
        {
            var counts = new SortedDictionary<string, int>();
            foreach (var u in sel.Units) { string k = u.TypeKey.ToString(); counts[k] = counts.ContainsKey(k) ? counts[k] + 1 : 1; }
            sb.Append(sel.Units.Count).Append(" units: ");
            foreach (var kv in counts) sb.Append(kv.Value).Append(' ').Append(kv.Key).Append("  ");
        }
        else if (sel.Building != null)
        {
            var b = sel.Building; var def = data.Buildings[b.TypeKey.ToString()];
            sb.Append(def.Key).Append("  HP ").Append(Mathf.CeilToInt(b.Hp)).Append('/').Append(def.Hp);
            if (!b.Completed) sb.Append("\nUnder construction ").Append(Mathf.RoundToInt(b.BuildProgress * 100)).Append('%');
            else if (b.QueueCount > 0)
                sb.Append("\n").Append(b.CurrentIsResearch ? "Researching " : "Training ").Append(b.CurrentKey.ToString())
                  .Append(' ').Append(Mathf.RoundToInt(b.CurrentProgress * 100)).Append("%  (queue ").Append(b.QueueCount).Append(')');
        }
        info.text = sb.ToString();
    }
}
