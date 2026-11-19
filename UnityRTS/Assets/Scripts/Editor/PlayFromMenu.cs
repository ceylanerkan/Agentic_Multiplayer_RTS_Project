using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Play mode always starts in the Menu scene, whatever scene is open in the editor (UNITY_FEEDBACK 2026-11-18:
/// pressing Play with Game open skipped the launcher, so no match ever started). The Game scene only works when
/// the NetworkRunner loads it. Turn it off with Tools > RTS > Play from Menu scene to test a scene on its own.
/// </summary>
[InitializeOnLoad]
public static class PlayFromMenu
{
    const string MenuScene = "Assets/Scenes/Menu.unity";
    const string PrefKey = "RTS.PlayFromMenu";
    const string MenuPath = "Tools/RTS/Play from Menu scene";

    static PlayFromMenu() { EditorApplication.delayCall += Apply; }   // assets are not ready inside the static constructor

    static bool Enabled { get { return EditorPrefs.GetBool(PrefKey, true); } }

    /// <summary>Also called by RtsSetupTools after it creates the Menu scene.</summary>
    public static void Apply()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene);
        EditorSceneManager.playModeStartScene = Enabled ? scene : null;   // null = play the open scene (Unity default)
    }

    [MenuItem(MenuPath)]
    static void Toggle() { EditorPrefs.SetBool(PrefKey, !Enabled); Apply(); }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate() { Menu.SetChecked(MenuPath, Enabled); return true; }
}
