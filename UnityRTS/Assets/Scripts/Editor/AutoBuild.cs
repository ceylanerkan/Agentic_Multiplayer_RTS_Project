using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Windows builds without the Build Profiles window (also with the editor closed):
///   Unity.exe -batchmode -quit -projectPath UnityRTS -executeMethod AutoBuild.Game -logFile build.log
/// TestBuild adds the automatic two-player match (Testing/AutoMatch.cs), which the game build does not contain.
/// </summary>
public static class AutoBuild
{
    [MenuItem("Tools/RTS/Build Windows game (Builds folder)")]
    public static void Game() { Build("Builds/UnityRTS.exe"); }

    public static void TestBuild() { Build("Builds/AutoTest/UnityRTS.exe", "RTS_AUTOTEST"); }

    static void Build(string path, params string[] defines)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            extraScriptingDefines = defines,
        });
        Debug.Log("RTS build " + path + ": " + report.summary.result + ", " + report.summary.totalErrors + " errors, " + report.summary.totalTime.TotalSeconds.ToString("0") + " s");
        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
