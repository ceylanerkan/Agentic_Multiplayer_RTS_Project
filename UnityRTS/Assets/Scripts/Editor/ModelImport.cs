using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings of the characters (Resources/Models/Units/*.fbx): animations for the Animation component (ModelLook plays them by
/// name), no own material files. Pictures of the models stay at most 1024 pixels.
/// </summary>
public class ModelImport : AssetPostprocessor
{
    const string Models = "Assets/Resources/Models/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Models)) return;
        var m = (ModelImporter)assetImporter;
        m.animationType = ModelImporterAnimationType.Legacy;
        m.importAnimation = true;
        m.importCameras = false; m.importLights = false;
        m.isReadable = true; // ModelLook measures the skin
        m.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; // only for the names, ModelLook sets the materials
        m.materialLocation = ModelImporterMaterialLocation.InPrefab;
        m.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Models)) return;
        var t = (TextureImporter)assetImporter;
        t.maxTextureSize = 1024; t.mipmapEnabled = true;
        if (assetPath.Contains("Leaves")) { t.alphaIsTransparency = true; t.mipMapsPreserveCoverage = true; t.alphaTestReferenceValue = 0.4f; }
    }
}
