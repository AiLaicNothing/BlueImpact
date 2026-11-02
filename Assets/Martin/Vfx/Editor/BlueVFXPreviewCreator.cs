#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BlueVFXPreviewCreator
{
    private const string Mats = "Assets/Martin/Vfx/Materials/";

    [MenuItem("BlueImpact/VFX/Crear objeto de prueba")]
    public static void Create()
    {
        var old = GameObject.Find("VFX_Preview");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("VFX_Preview");
        Undo.RegisterCreatedObjectUndo(root, "Crear VFX_Preview");
        var preview = root.AddComponent<BlueVFXPreview>();

        preview.withered = MakeQuad(root, "Campo_Marchito", "M_WitheredGround", new Vector3(-12f, 0.05f, 0f), 8f);
        preview.shockwave = MakeQuad(root, "Onda_Choque", "M_Shockwave", new Vector3(-3f, 0.06f, 0f), 8f);
        preview.magma = MakeQuad(root, "Magma", "M_MagmaCrack", new Vector3(4f, 0.07f, 0f), 5f);
        preview.warn = MakeQuad(root, "Aviso", "M_WarnRing", new Vector3(10f, 0.08f, 0f), 5f);

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Bola_Fuego";
        Object.DestroyImmediate(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(root.transform, false);
        sphere.transform.localPosition = new Vector3(16f, 1.5f, 0f);
        var sr = sphere.GetComponent<Renderer>();
        sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Mats + "M_FireballCore.mat");
        preview.fireball = sr;

        // Delante de la cámara de la vista de escena, si existe
        var sv = SceneView.lastActiveSceneView;
        if (sv != null) root.transform.position = new Vector3(sv.pivot.x, 0f, sv.pivot.z);

        Selection.activeGameObject = root;
        EditorSceneManager_MarkDirty();
        Debug.Log("[BlueVFX] VFX_Preview creado. Es un objeto de prueba: bórralo antes de guardar la escena si no lo quieres.");
    }

    private static Renderer MakeQuad(GameObject parent, string name, string mat, Vector3 pos, float size)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent.transform, false);
        q.transform.localPosition = pos;
        q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(size, size, 1f);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Mats + mat + ".mat");
        return r;
    }

    private static void EditorSceneManager_MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
#endif
