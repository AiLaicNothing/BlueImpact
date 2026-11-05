using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Arma en la escena el evento final del Blue Impact con formas basicas:
//
// Boss > BossZone > BLUE IMPACT (BlueImpactEvent + AudioSource)
//    Crystal           <- BlueImpact + collider. Cuelga del techo y luego baja al centro
//       Diamond, Core, IdleVfx, ReadyVfx, GlowLight
//    Chains            <- cadenas enteras (se apagan al romperse)
//    ChainsBroken      <- restos colgando del techo (se encienden al romperse)
//    Anchors           <- placas del techo donde se enganchan las cadenas
//    ChainBreakPoint   <- donde sale el VFX de ruptura
//    DescendTarget     <- donde termina el cristal, al alcance del jugador
//    Cameras           <- Cam_Chains y Cam_Descend (CinemachineCamera + CameraID)
//
// Tambien asigna BlueImpactEvent en el BossEvent del prefab Boss (override de escena).
public static class BlueImpactSetupBuilder
{
    private const string ScenePath = "Assets/Scenes/Zone_00_Tutorial.unity";
    private const string MatFolder = "Assets/Martin/Scripts/EndEvent/BlueImpact_Materials";
    private const string AudioFolder = "Assets/Martin/Scripts/Enemy/Boss/Morgana/BossAudio_Morgana/";

    private const float CrystalHeightAboveGround = 1.4f;  // al alcance de PlayerInteraction (radio 2 m)
    private const float AnchorSpread = 3.5f;              // separacion de las cadenas en el techo
    private const int ChainCount = 4;
    private const float LinkLength = 0.45f;
    private const float DescendSeconds = 5f;

    [MenuItem("Tools/Morgana/5. Armar Blue Impact y cadenas")]
    public static void Build()
    {
        if (!OpenTargetScene()) return;

        Scene scene = SceneManager.GetActiveScene();

        BossEvent bossEvent = Object.FindFirstObjectByType<BossEvent>(FindObjectsInactive.Include);

        if (bossEvent == null)
        {
            Debug.LogError("[BlueImpact Setup] No hay BossEvent en " + scene.name + " (deberia estar en Boss > BossZone)");
            return;
        }

        Transform zone = bossEvent.transform;
        SerializedObject bossSo = new SerializedObject(bossEvent);
        Transform center = bossSo.FindProperty("center").objectReferenceValue as Transform;
        Transform spawnPos = bossSo.FindProperty("spawnPos").objectReferenceValue as Transform;

        Vector3 centerPos = center != null ? center.position : zone.position;

        // ---------- Raiz: BLUE IMPACT con el BlueImpactEvent ----------
        BlueImpactEvent evt = Object.FindFirstObjectByType<BlueImpactEvent>(FindObjectsInactive.Include);
        GameObject root;

        if (evt != null)
        {
            root = evt.gameObject;

            if (root.transform.Find("Crystal") != null)
            {
                Selection.activeGameObject = root;
                Debug.LogWarning("[BlueImpact Setup] '" + root.name + "' ya tiene el montaje (hijo 'Crystal'). Borra sus hijos si quieres rehacerlo.");
                return;
            }

            CleanPlaceholder(root);
        }
        else
        {
            root = new GameObject("BLUE IMPACT");
            Undo.RegisterCreatedObjectUndo(root, "Crear BLUE IMPACT");
            root.transform.SetParent(zone, false);
            evt = Undo.AddComponent<BlueImpactEvent>(root);
        }

        Undo.RecordObject(root.transform, "Mover BLUE IMPACT");
        root.transform.position = centerPos;
        root.transform.rotation = Quaternion.identity;

        // ---------- Alturas ----------
        // Los raycasts en modo edicion necesitan las posiciones al dia (y el cubo de prueba ya borrado)
        Physics.SyncTransforms();
        float groundY = FindGroundY(centerPos);
        Vector3 descendPos = new Vector3(centerPos.x, groundY + CrystalHeightAboveGround, centerPos.z);

        // El boss cae desde spawnPos: el cristal cuelga mas arriba para que no lo atraviese
        float desiredHang = Mathf.Max(groundY + 12f, spawnPos != null ? spawnPos.position.y + 3f : groundY + 12f);
        float ceilingY;
        bool hasCeiling = FindCeilingY(new Vector3(centerPos.x, groundY + 2f, centerPos.z), out ceilingY);

        float hangY = desiredHang;

        if (hasCeiling)
        {
            hangY = Mathf.Min(desiredHang, ceilingY - 4f);

            if (hangY < groundY + 6f) hangY = groundY + 6f;

            if (spawnPos != null && hangY < spawnPos.position.y + 1f)
            {
                Debug.LogWarning("[BlueImpact Setup] El techo esta bajo: el cristal queda en la trayectoria de caida de Morgana. Mueve SpawnPos o el cristal si se ve mal.");
            }
        }
        else
        {
            ceilingY = hangY + 8f;
        }

        Vector3 hangPos = new Vector3(centerPos.x, hangY, centerPos.z);
        float anchorY = ceilingY - 0.2f;

        // ---------- Materiales ----------
        Material crystalMat = GetLitMat("M_BlueImpact_Crystal", new Color(0.18f, 0.55f, 1f), new Color(0.25f, 0.9f, 2.8f), 0.2f, 0.9f);
        Material coreMat = GetLitMat("M_BlueImpact_Core", new Color(0.7f, 0.95f, 1f), new Color(1.2f, 2.6f, 5f), 0f, 1f);
        Material chainMat = GetLitMat("M_BlueImpact_Chain", new Color(0.16f, 0.17f, 0.2f), Color.black, 0.85f, 0.45f);
        Material anchorMat = GetLitMat("M_BlueImpact_Anchor", new Color(0.24f, 0.24f, 0.28f), Color.black, 0.1f, 0.2f);
        Material auraMat = GetAuraMat("M_BlueImpact_Aura", new Color(0.35f, 0.8f, 1f, 0.18f));

        // ---------- Cristal ----------
        GameObject crystal = Child(root, "Crystal", hangPos);
        crystal.layer = FindInteractableLayer();

        SphereCollider col = crystal.AddComponent<SphereCollider>();
        col.radius = 1f;

        GameObject diamondRoot = Child(crystal, "Diamond", hangPos);
        diamondRoot.transform.localScale = new Vector3(1f, 1.6f, 1f);
        GameObject diamond = Prim(PrimitiveType.Cube, diamondRoot, "Shape", hangPos, crystalMat);
        diamond.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        diamond.transform.localScale = Vector3.one * 0.85f;

        GameObject core = Prim(PrimitiveType.Sphere, crystal, "Core", hangPos, coreMat);
        core.transform.localScale = Vector3.one * 0.45f;

        // Idle: anillo de fragmentos flotando mientras cuelga
        GameObject idleVfx = Child(crystal, "IdleVfx", hangPos);

        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            Vector3 p = hangPos + new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a * 2f) * 0.25f, Mathf.Sin(a) * 1.4f);
            GameObject shard = Prim(PrimitiveType.Cube, idleVfx, "Shard_" + (i + 1), p, crystalMat);
            shard.transform.localScale = new Vector3(0.12f, 0.3f, 0.12f);
            shard.transform.rotation = Quaternion.Euler(30f, i * 60f, 20f);
        }

        // Ready: aura y circulo en el suelo cuando ya se puede usar
        GameObject readyVfx = Child(crystal, "ReadyVfx", hangPos);
        GameObject aura = Prim(PrimitiveType.Sphere, readyVfx, "Aura", hangPos, auraMat);
        aura.transform.localScale = Vector3.one * 2.6f;

        GameObject ring = Prim(PrimitiveType.Cylinder, readyVfx, "GroundRing", hangPos + Vector3.down * (CrystalHeightAboveGround - 0.03f), auraMat);
        ring.transform.localScale = new Vector3(3f, 0.01f, 3f);

        GameObject lightGo = Child(crystal, "GlowLight", hangPos);
        Light glow = lightGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.35f, 0.75f, 1f);
        glow.range = 14f;
        glow.intensity = 1.5f;

        BlueImpact crystalScript = crystal.AddComponent<BlueImpact>();
        SerializedObject cso = new SerializedObject(crystalScript);
        cso.FindProperty("interactable").boolValue = false;
        cso.FindProperty("eventOwner").objectReferenceValue = evt;
        cso.FindProperty("idleVfx").objectReferenceValue = idleVfx;
        cso.FindProperty("readyVfx").objectReferenceValue = readyVfx;
        cso.FindProperty("glowLight").objectReferenceValue = glow;
        cso.ApplyModifiedPropertiesWithoutUndo();

        idleVfx.SetActive(true);
        readyVfx.SetActive(false);

        // ---------- Cadenas ----------
        Vector3 crystalTop = hangPos + Vector3.up * 1.15f;

        GameObject anchors = Child(root, "Anchors", new Vector3(centerPos.x, anchorY, centerPos.z));
        GameObject chainsRoot = Child(root, "Chains", crystalTop);
        GameObject brokenRoot = Child(root, "ChainsBroken", crystalTop);

        List<GameObject> chains = new List<GameObject>();
        List<GameObject> broken = new List<GameObject>();

        for (int i = 0; i < ChainCount; i++)
        {
            float a = (45f + i * 360f / ChainCount) * Mathf.Deg2Rad;
            Vector3 anchorPos = new Vector3(centerPos.x + Mathf.Cos(a) * AnchorSpread, anchorY, centerPos.z + Mathf.Sin(a) * AnchorSpread);

            GameObject plate = Prim(PrimitiveType.Cube, anchors, "Anchor_" + (i + 1), anchorPos + Vector3.up * 0.1f, anchorMat);
            plate.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);

            GameObject hook = Prim(PrimitiveType.Cylinder, anchors, "Hook_" + (i + 1), anchorPos - Vector3.up * 0.15f, chainMat);
            hook.transform.localScale = new Vector3(0.25f, 0.15f, 0.25f);

            chains.Add(BuildChain(chainsRoot, "Chain_" + (i + 1), anchorPos, crystalTop, 1f, chainMat, false));
            broken.Add(BuildChain(brokenRoot, "Chain_" + (i + 1) + "_Broken", anchorPos, crystalTop, 0.4f, chainMat, true));
        }

        // Arranca con las cadenas enteras (BlueImpactEvent tambien lo fuerza en Start)
        chainsRoot.SetActive(true);
        brokenRoot.SetActive(false);

        GameObject breakPoint = Child(root, "ChainBreakPoint", crystalTop);
        GameObject descendTarget = Child(root, "DescendTarget", descendPos);

        // ---------- Camaras ----------
        int camChainsId = FreeCameraId(405);
        int camDescendId = FreeCameraId(camChainsId + 1);

        float pathLen = Vector3.Distance(hangPos, descendPos);
        GameObject cameras = Child(root, "Cameras", centerPos);

        // Desde el suelo mirando hacia arriba a las cadenas
        Vector3 chainsCamPos = new Vector3(centerPos.x, groundY + 2.5f, centerPos.z) + new Vector3(-0.6f, 0f, -1f).normalized * 11f;
        CreateCamera(cameras, "Cam_Chains", chainsCamPos, crystalTop, camChainsId, 55f);

        // Plano abierto que cubre todo el descenso
        Vector3 mid = (hangPos + descendPos) * 0.5f;
        float camDist = Mathf.Max(10f, pathLen * 1.05f);
        Vector3 descendCamPos = new Vector3(centerPos.x, groundY + 3f, centerPos.z) + new Vector3(0.7f, 0f, -1f).normalized * camDist;
        CreateCamera(cameras, "Cam_Descend", descendCamPos, mid, camDescendId, 60f);

        // ---------- Audio ----------
        AudioSource src = root.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(root);
        src.playOnAwake = false;
        src.spatialBlend = 0.4f;
        src.minDistance = 8f;
        src.maxDistance = 60f;

        // ---------- BlueImpactEvent ----------
        float descendSpeed = Mathf.Max(0.5f, pathLen / DescendSeconds);

        SerializedObject eso = new SerializedObject(evt);
        eso.FindProperty("blueImpact").objectReferenceValue = crystalScript;
        eso.FindProperty("descendTarget").objectReferenceValue = descendTarget.transform;
        eso.FindProperty("descendSpeed").floatValue = descendSpeed;
        eso.FindProperty("descendTimeout").floatValue = DescendSeconds + 10f;
        SetArray(eso.FindProperty("chains"), chains);
        SetArray(eso.FindProperty("brokenChains"), broken);
        eso.FindProperty("chainVfxPoint").objectReferenceValue = breakPoint.transform;
        eso.FindProperty("audioSource").objectReferenceValue = src;

        float chainShot = eso.FindProperty("pauseBeforeChains").floatValue + eso.FindProperty("pauseAfterChains").floatValue + 0.3f;
        float descendShot = DescendSeconds + eso.FindProperty("pauseAfterDescend").floatValue + 0.3f;
        SetCameraRequest(eso.FindProperty("chainCamera"), camChainsId, chainShot);
        SetCameraRequest(eso.FindProperty("descendCamera"), camDescendId, descendShot);

        // Audio provisional con los sonidos de Morgana que ya estan en el proyecto
        SetClipIfEmpty(eso, "chainBreakSfx", "Morgana_PhaseTransform");
        SetClipIfEmpty(eso, "descendLoopSfx", "Climax_BorderLoop");
        SetClipIfEmpty(eso, "destroySfx", "Meteor_Impact_01");
        SetClipIfEmpty(eso, "absorbSfx", "Climax_CrystalGlow");

        eso.ApplyModifiedProperties();

        // ---------- BossEvent: pasa al evento final al morir el boss ----------
        bossSo.Update();
        bossSo.FindProperty("blueImpactEvent").objectReferenceValue = evt;
        bossSo.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(bossEvent);

        // No se guarda sola: la escena puede tener otros cambios. Guarda con Ctrl+S.
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;

        Debug.Log("[BlueImpact Setup] Listo en " + scene.name +
                  ". Cristal colgando a " + hangY.ToString("0.0") + " m (suelo " + groundY.ToString("0.0") +
                  (hasCeiling ? ", techo " + ceilingY.ToString("0.0") : ", sin techo detectado") +
                  "). Baja " + pathLen.ToString("0.0") + " m en " + DescendSeconds + " s. Camaras " + camChainsId + " y " + camDescendId +
                  ". BossEvent.blueImpactEvent asignado. Guarda la escena (Ctrl+S).");
    }

    // ---------- ESCENA ----------

    private static bool OpenTargetScene()
    {
        if (SceneManager.GetActiveScene().path == ScenePath) return true;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            Debug.LogError("[BlueImpact Setup] No existe la escena " + ScenePath);
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    // El cubo de prueba tenia BlueImpact en el mismo objeto que BlueImpactEvent.
    // BlueImpact pasa al hijo Crystal y la raiz queda solo como controlador del evento.
    private static void CleanPlaceholder(GameObject root)
    {
        BlueImpact old = root.GetComponent<BlueImpact>();
        if (old != null) Undo.DestroyObjectImmediate(old);

        Collider c = root.GetComponent<Collider>();
        if (c != null) Undo.DestroyObjectImmediate(c);

        MeshRenderer mr = root.GetComponent<MeshRenderer>();
        if (mr != null) Undo.DestroyObjectImmediate(mr);

        MeshFilter mf = root.GetComponent<MeshFilter>();
        if (mf != null) Undo.DestroyObjectImmediate(mf);
    }

    private static float FindGroundY(Vector3 center)
    {
        Vector3 from = center + Vector3.up * 1.5f;

        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return center.y;
    }

    private static bool FindCeilingY(Vector3 from, out float y)
    {
        if (Physics.Raycast(from, Vector3.up, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore))
        {
            y = hit.point.y;
            return true;
        }

        y = 0f;
        return false;
    }

    // Usa la misma capa que los demas interactuables de la escena (palancas, checkpoints...)
    private static int FindInteractableLayer()
    {
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb is IInteractable && !(mb is BlueImpact) && mb.GetComponent<Collider>() != null)
            {
                Debug.Log("[BlueImpact Setup] Capa del cristal copiada de '" + mb.name + "': " + LayerMask.LayerToName(mb.gameObject.layer));
                return mb.gameObject.layer;
            }
        }

        int named = LayerMask.NameToLayer("Interactable");

        if (named >= 0) return named;

        Debug.LogWarning("[BlueImpact Setup] No encontre la capa de interactuables: revisa la capa de 'Crystal' contra el interactableLayer del jugador.");
        return 0;
    }

    private static int FreeCameraId(int start)
    {
        HashSet<int> used = new HashSet<int>();

        foreach (CameraID id in Object.FindObjectsByType<CameraID>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            used.Add(id.ID);
        }

        int candidate = start;
        while (used.Contains(candidate)) candidate++;
        return candidate;
    }

    // ---------- CONSTRUCCION ----------

    private static GameObject Child(GameObject parent, string name, Vector3 worldPos)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = worldPos;
        return go;
    }

    private static GameObject Prim(PrimitiveType type, GameObject parent, string name, Vector3 worldPos, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
        go.name = name;

        // Solo visual: el unico collider del montaje es el del cristal
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        go.transform.SetParent(parent.transform, false);
        go.transform.position = worldPos;

        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.On;

        return go;
    }

    // Cadena de eslabones (cubos alargados alternando 90 grados) del ancla hasta el cristal.
    // 'fraction' < 1 deja solo el tramo de arriba, para la version rota.
    private static GameObject BuildChain(GameObject parent, string name, Vector3 anchor, Vector3 end, float fraction, Material mat, bool brokenLook)
    {
        GameObject chain = Child(parent, name, anchor);

        Vector3 dir = end - anchor;
        float length = dir.magnitude;
        dir.Normalize();

        float step = LinkLength * 0.8f;
        int total = Mathf.Max(2, Mathf.CeilToInt(length / step));
        int count = Mathf.Max(2, Mathf.RoundToInt(total * fraction));

        Quaternion along = Quaternion.FromToRotation(Vector3.up, dir);

        for (int i = 0; i < count; i++)
        {
            Vector3 p = anchor + dir * (step * (i + 0.5f));
            GameObject link = Prim(PrimitiveType.Cube, chain, "Link_" + i.ToString("00"), p, mat);

            Quaternion roll = Quaternion.AngleAxis(i % 2 == 0 ? 0f : 90f, Vector3.up);
            link.transform.rotation = along * roll;
            link.transform.localScale = new Vector3(0.28f, LinkLength, 0.1f);
        }

        // El ultimo eslabon de una cadena rota queda torcido, como arrancado
        if (brokenLook && chain.transform.childCount > 0)
        {
            Transform last = chain.transform.GetChild(chain.transform.childCount - 1);
            last.rotation = last.rotation * Quaternion.Euler(35f, 0f, 20f);
        }

        return chain;
    }

    private static void CreateCamera(GameObject parent, string name, Vector3 pos, Vector3 lookAt, int id, float fov)
    {
        GameObject go = Child(parent, name, pos);
        go.transform.rotation = Quaternion.LookRotation(lookAt - pos);

        CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
        cam.Priority = 0;   // CameraDirector la sube cuando BlueImpactEvent la pide

        LensSettings lens = cam.Lens;
        lens.FieldOfView = fov;
        cam.Lens = lens;

        CameraID camId = go.AddComponent<CameraID>();
        SerializedObject so = new SerializedObject(camId);
        so.FindProperty("id").intValue = id;
        so.FindProperty("cameraReference").objectReferenceValue = cam;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // El evento ya bloquea al jugador con el modo Cutscene. Si la camara tambien lo bloquea,
    // al terminar su duracion lo desbloquea aunque la cinematica siga.
    private static void SetCameraRequest(SerializedProperty req, int id, float duration)
    {
        req.FindPropertyRelative("cameraID").intValue = id;
        req.FindPropertyRelative("duration").floatValue = duration;
        req.FindPropertyRelative("restoreGameplayCamera").boolValue = true;
        req.FindPropertyRelative("interruptCurrent").boolValue = true;
        req.FindPropertyRelative("pausePlayer").boolValue = false;
        req.FindPropertyRelative("mutePlayerAudio").boolValue = false;
    }

    private static void SetArray(SerializedProperty prop, List<GameObject> items)
    {
        prop.arraySize = items.Count;

        for (int i = 0; i < items.Count; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }

    private static void SetClipIfEmpty(SerializedObject so, string field, string file)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null || p.objectReferenceValue != null) return;

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + file + ".wav");
        if (clip != null) p.objectReferenceValue = clip;
    }

    // ---------- MATERIALES ----------

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(MatFolder)) return;

        string parent = System.IO.Path.GetDirectoryName(MatFolder).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(MatFolder));
    }

    private static Material GetLitMat(string name, Color baseColor, Color emission, float metallic, float smoothness)
    {
        string path = MatFolder + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        EnsureFolder();

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader);

        m.SetColor("_BaseColor", baseColor);
        m.SetColor("_Color", baseColor);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);

        if (emission.maxColorComponent > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static Material GetAuraMat(string name, Color color)
    {
        string path = MatFolder + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        EnsureFolder();

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
        m = new Material(shader);

        // Transparente por alpha en URP
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetColor("_BaseColor", color);

        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
