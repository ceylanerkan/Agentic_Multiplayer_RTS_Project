using System.IO;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Gives every script the same GameData, loaded from ai/rag/knowledge/*.json (the single source of game numbers).
/// In the editor it reads the repository folder directly; in a build it reads the copy in StreamingAssets/knowledge
/// that KnowledgeBuildCopy puts there before the build.
/// </summary>
public static class GameDataProvider
{
    static GameData cached;

    public static GameData Get()
    {
        if (cached == null) cached = Load(KnowledgeDir());
        return cached;
    }

    /// <summary>Folder that holds units.json, buildings.json and technologies.json.</summary>
    public static string KnowledgeDir()
    {
#if UNITY_EDITOR
        // Application.dataPath = <repo>/UnityRTS/Assets, so the repo root is two levels up.
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../../ai/rag/knowledge"));
#else
        return Path.Combine(Application.streamingAssetsPath, "knowledge");
#endif
    }

    public static GameData Load(string dir)
    {
        return GameData.FromJson(
            File.ReadAllText(Path.Combine(dir, "units.json")),
            File.ReadAllText(Path.Combine(dir, "buildings.json")),
            File.ReadAllText(Path.Combine(dir, "technologies.json")));
    }
}
