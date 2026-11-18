using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Menu Tools > RTS > Export AI snapshot (Play Mode, in a match): writes ai/exports/snapshot.json for the pytest contract test.</summary>
public static class SnapshotExportMenu
{
    [MenuItem("Tools/RTS/Export AI snapshot")]
    static void Export()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ai/exports/snapshot.json"));
        string written = AiSnapshotBuilder.Export(path);
        Debug.Log(written != null ? "Snapshot written to " + written : "No snapshot: start a match first (Play Mode).");
    }
}
