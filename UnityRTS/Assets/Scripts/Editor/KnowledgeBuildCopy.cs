using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Before every build, copies the knowledge JSON into Assets/StreamingAssets/knowledge so the built game can read it.
/// The copy is a build artefact (it is in .gitignore), the originals in ai/rag/knowledge stay the source of truth.
/// </summary>
public class KnowledgeBuildCopy : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string src = GameDataProvider.KnowledgeDir();
        string dst = Path.Combine("Assets", "StreamingAssets", "knowledge");
        Directory.CreateDirectory(dst);
        foreach (var f in new[] { "units.json", "buildings.json", "technologies.json" })
            File.Copy(Path.Combine(src, f), Path.Combine(dst, f), true);
        AssetDatabase.Refresh();
    }
}
