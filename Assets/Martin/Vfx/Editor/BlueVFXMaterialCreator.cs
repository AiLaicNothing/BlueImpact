#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BlueVFXMaterialCreator
{
    private const string Folder = "Assets/Martin/Vfx/Materials";

    [MenuItem("BlueImpact/VFX/Crear materiales")]
    public static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Martin/Vfx", "Materials");

        Create("M_WitheredGround", "BlueImpact/VFX/WitheredGround");
        Create("M_FireballCore", "BlueImpact/VFX/FireballCore");
        Create("M_Shockwave", "BlueImpact/VFX/Shockwave");
        Create("M_MagmaCrack", "BlueImpact/VFX/MagmaCrack");
        Create("M_WarnRing", "BlueImpact/VFX/WarnRing");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Create(string name, string shaderName)
    {
        string path = $"{Folder}/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        Shader sh = Shader.Find(shaderName);
        if (sh == null) { Debug.LogError($"[BlueVFX] Shader no encontrado: {shaderName}"); return; }
        AssetDatabase.CreateAsset(new Material(sh), path);
    }
}
#endif
