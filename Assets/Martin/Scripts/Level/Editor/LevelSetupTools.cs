#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú: BlueImpact > Niveles.
// Configura el bloqueo en TODAS las escenas de nivel: LevelConfig (asset) y LevelZone (trigger).
// LevelObjectiveManager y el panel de Quest van SOLO en la escena de inicio (Testing_Scene),
// porque las escenas de nivel se descargan al cruzar un SceneLoader y se llevarían la quest y el progreso.
// La LevelGate de salida NO se crea aquí: cada nivel tiene su propia salida, hay que colocarla a mano.
public static class LevelSetupTools
{
    private const float DefaultThreshold = 0.7f;
    private const float TutorialThreshold = 0.5f;
    private const float ZoneMargin = 12f;
    private const string PersistentScene = "Assets/Martin/Testing_Scene.unity";

    private static readonly string[] LevelScenes =
    {
        "Assets/Scenes/Zone_00_Tutorial.unity",
        "Assets/Scenes/Zone_02.unity",
        "Assets/Scenes/Zone_03.unity",
        "Assets/Scenes/Zone_04.unity",
        "Assets/Scenes/Zone_05.unity",
    };

    // =====================================================================
    // MENÚ
    // =====================================================================

    [MenuItem("BlueImpact/Niveles/Configurar todos los niveles")]
    public static void ConfigureAllLevels()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        StringBuilder report = new StringBuilder();
        report.AppendLine("[LevelSetup] Resultado:");

        foreach (string path in LevelScenes)
        {
            if (!File.Exists(path))
            {
                report.AppendLine($"  {path}: NO EXISTE, se omite.");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            report.AppendLine("  " + ConfigureScene(scene));
            EditorSceneManager.SaveScene(scene);
        }

        report.AppendLine("  " + EnsurePersistentUi());
        Debug.Log(report.ToString());
    }

    [MenuItem("BlueImpact/Niveles/Revisar niveles (sin cambiar nada)")]
    public static void ReviewAllLevels()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        StringBuilder report = new StringBuilder();
        report.AppendLine("[LevelSetup] Revisión (no se modificó ninguna escena):");

        foreach (string path in LevelScenes)
        {
            if (!File.Exists(path))
            {
                report.AppendLine($"  {path}: NO EXISTE.");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int total = CountSources(scene);
            float threshold = ThresholdFor(scene.name);
            int required = Mathf.CeilToInt(total * threshold - 0.0001f);

            report.AppendLine($"  {scene.name}: {total} enemigos contables, " +
                              $"{required} necesarios ({threshold:P0}), " +
                              $"zona: {(HasZone(scene) ? "sí" : "NO")}");

            // Desglose por componente, para comparar con lo que ves en la escena.
            foreach (ILevelSpawnSource source in SceneComponents<MonoBehaviour>(scene).OfType<ILevelSpawnSource>())
            {
                MonoBehaviour mb = (MonoBehaviour)source;
                report.AppendLine($"      - {mb.name} ({mb.GetType().Name}): {source.CountLevelEnemies()}");
            }
        }

        Debug.Log(report.ToString());
    }

    [MenuItem("BlueImpact/Niveles/Configurar escena activa")]
    public static void ConfigureActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        string line = ConfigureScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[LevelSetup] " + line + " Guárdala con Ctrl+S.");
    }

    // =====================================================================
    // CONFIGURACIÓN POR ESCENA
    // =====================================================================

    // Devuelve una línea de reporte para la escena.
    private static string ConfigureScene(Scene scene)
    {
        int total = CountSources(scene);
        if (total == 0)
            return $"{scene.name}: sin enemigos contables (ILevelSpawnSource). Se omite.";

        float threshold = ThresholdFor(scene.name);
        LevelConfig config = GetOrCreateConfig(scene.name, threshold);

        string zoneState = EnsureZone(scene, config) ? "zona creada" : "zona ya existía";
        int gates = EnsureForwardGates(scene, config);
        RemoveLocalUi(scene);

        int required = config.RequiredKills(total);
        return $"{scene.name}: {total} enemigos, {required} necesarios ({threshold:P0}), {zoneState}, " +
               $"{gates} compuerta(s) nueva(s). Revisa el tamaño de LevelZone y las compuertas.";
    }

    // =====================================================================
    // COMPUERTAS EN SALIDAS QUE AVANZAN DE ZONA
    // =====================================================================

    // Bloquea los SceneLoader que llevan a una zona de número mayor (Zone_02 -> Zone_03, etc.).
    // Los tutoriales y los regresos al hub no se bloquean.
    // Cada compuerta: trigger más grande para el aviso + Blocker sólido del tamaño del SceneLoader.
    // Devuelve cuántas compuertas creó (las que ya existan con el mismo nombre se omiten).
    private static int EnsureForwardGates(Scene scene, LevelConfig config)
    {
        int created = 0;

        foreach (SceneLoader loader in SceneComponents<SceneLoader>(scene))
        {
            string target = new SerializedObject(loader).FindProperty("sceneNameLoad").stringValue;
            if (!IsForwardGate(scene.name, target)) continue;

            // Nombre único por salida (no por destino): varias salidas pueden llevar a la misma zona.
            string gateName = "LevelGate_" + loader.name + "_" + target;
            if (SceneComponents<LevelGate>(scene).Any(g => g.name == gateName)) continue;

            Collider loaderCol = loader.GetComponent<Collider>();
            if (loaderCol == null)
            {
                Debug.LogWarning($"[LevelSetup] {scene.name}: '{loader.name}' no tiene collider. No se puso compuerta.", loader);
                continue;
            }

            Bounds b = loaderCol.bounds;

            GameObject gate = new GameObject(gateName);
            SceneManager.MoveGameObjectToScene(gate, scene);
            gate.transform.position = b.center;

            BoxCollider trigger = gate.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = b.size + new Vector3(4f, 2f, 4f);

            // Blocker visible: un cubo 3D del tamaño de la salida, para que se vea como puerta.
            // Al abrir la compuerta, LevelGate lo desactiva y desaparece junto con su collider.
            GameObject blockerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockerGo.name = "Blocker";
            SceneManager.MoveGameObjectToScene(blockerGo, scene);
            blockerGo.transform.SetParent(gate.transform, false);
            blockerGo.transform.localPosition = Vector3.zero;
            blockerGo.transform.localScale = b.size;
            BoxCollider blocker = blockerGo.GetComponent<BoxCollider>();
            blocker.isTrigger = false;

            LevelGate lg = gate.AddComponent<LevelGate>();
            SerializedObject so = new SerializedObject(lg);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("blocker").objectReferenceValue = blocker;
            so.FindProperty("lockedVisual").objectReferenceValue = blockerGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            created++;
        }

        return created;
    }

    private static bool IsForwardGate(string currentScene, string targetScene)
    {
        int from = ZoneNumber(currentScene);
        int to = ZoneNumber(targetScene);

        // Solo zonas de hub en adelante (Zone_02 y superiores); tutoriales (0 y 1) no se bloquean.
        // El destino tiene que ser una escena de la lista del juego (no p. ej. Zone_04_ForgottenLibrary).
        bool isGameScene = System.Array.Exists(LevelScenes, p => System.IO.Path.GetFileNameWithoutExtension(p) == targetScene);
        return from >= 2 && to > from && to <= 5 && isGameScene;
    }

    // "Zone_03" -> 3. "Zone_00_Tutorial" -> 0. Otros nombres -> -1.
    private static int ZoneNumber(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || !sceneName.StartsWith("Zone_")) return -1;

        string digits = new string(sceneName.Substring(5).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out int n) ? n : -1;
    }

    private static float ThresholdFor(string sceneName)
    {
        return sceneName.Contains("Tutorial") ? TutorialThreshold : DefaultThreshold;
    }

    private static int CountSources(Scene scene)
    {
        return SceneComponents<MonoBehaviour>(scene)
            .OfType<ILevelSpawnSource>()
            .Sum(s => s.CountLevelEnemies());
    }

    private static bool HasZone(Scene scene)
    {
        return SceneComponents<LevelZone>(scene).Length > 0;
    }

    // =====================================================================
    // CONFIG
    // =====================================================================

    private static LevelConfig GetOrCreateConfig(string sceneName, float threshold)
    {
        EnsureFolder("Assets/Level/Configs");

        string path = $"Assets/Level/Configs/LevelConfig_{sceneName}.asset";
        LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        SerializedObject so = new SerializedObject(config);
        so.FindProperty("levelId").stringValue = sceneName;
        so.FindProperty("displayName").stringValue = sceneName;
        so.FindProperty("killThreshold").floatValue = threshold;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return config;
    }

    // =====================================================================
    // ZONA (trigger que cubre todos los spawns del nivel)
    // =====================================================================

    // Devuelve true si creó la zona; false si ya existía (solo actualiza su config).
    private static bool EnsureZone(Scene scene, LevelConfig config)
    {
        LevelZone zone = SceneComponents<LevelZone>(scene).FirstOrDefault();
        bool created = false;

        if (zone == null)
        {
            Bounds area = ComputeArea(scene);

            GameObject go = new GameObject("LevelZone_" + scene.name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = area.center;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(area.size.x, Mathf.Max(area.size.y, 6f), area.size.z);

            zone = go.AddComponent<LevelZone>();
            created = true;
        }

        SerializedObject so = new SerializedObject(zone);
        so.FindProperty("config").objectReferenceValue = config;
        so.ApplyModifiedPropertiesWithoutUndo();

        return created;
    }

    // Caja que engloba cada componente contable y todas las referencias a Transform que tenga
    // (spawn points, safe points, puertas, arrays). Margen de ZoneMargin metros por lado.
    private static Bounds ComputeArea(Scene scene)
    {
        List<Vector3> points = new List<Vector3>();

        foreach (ILevelSpawnSource source in SceneComponents<MonoBehaviour>(scene).OfType<ILevelSpawnSource>())
        {
            MonoBehaviour mb = (MonoBehaviour)source;
            points.Add(mb.transform.position);

            SerializedObject so = new SerializedObject(mb);
            SerializedProperty it = so.GetIterator();

            while (it.NextVisible(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference &&
                    it.objectReferenceValue is Transform t &&
                    t.gameObject.scene == scene)
                {
                    points.Add(t.position);
                }
            }
        }

        if (points.Count == 0) return new Bounds(Vector3.zero, Vector3.one * 10f);

        Bounds b = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 p in points) b.Encapsulate(p);

        b.Expand(new Vector3(ZoneMargin * 2f, 6f, ZoneMargin * 2f));
        return b;
    }

    // =====================================================================
    // MANAGER
    // =====================================================================

    // El manager y el panel de Quest viven en la escena persistente. Si una escena de nivel los tiene,
    // se quitan: al descargar la escena se perdería la quest y el progreso de kills.
    private static void RemoveLocalUi(Scene scene)
    {
        foreach (LevelObjectiveManager m in SceneComponents<LevelObjectiveManager>(scene))
            Object.DestroyImmediate(m.gameObject);
        foreach (QuestPanelUI q in SceneComponents<QuestPanelUI>(scene))
            Object.DestroyImmediate(q.gameObject);
    }

    // Pone el manager y el panel de Quest una sola vez en la escena persistente.
    private static string EnsurePersistentUi()
    {
        if (!File.Exists(PersistentScene))
            return $"{PersistentScene}: NO EXISTE. Ajusta PersistentScene en LevelSetupTools o crea el manager y el QuestCanvas a mano.";

        Scene scene = EditorSceneManager.OpenScene(PersistentScene, OpenSceneMode.Single);
        EnsureManager(scene);
        EnsureQuestPanel(scene);
        EditorSceneManager.SaveScene(scene);
        return $"{PersistentScene}: manager y panel de Quest listos.";
    }

    private static void EnsureManager(Scene scene)
    {
        if (ExistsInLoadedScenes<LevelObjectiveManager>()) return;

        GameObject go = new GameObject("LevelObjectiveManager");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<LevelObjectiveManager>();
    }

    // =====================================================================
    // PANEL DE QUEST
    // =====================================================================

    private static void EnsureQuestPanel(Scene scene)
    {
        if (ExistsInLoadedScenes<QuestPanelUI>()) return;

        GameObject canvasGo = new GameObject("QuestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject root = NewRect("QuestPanel", canvasGo.transform);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        SetTopLeft(root.GetComponent<RectTransform>(), new Vector2(40f, -40f), new Vector2(640f, 120f));

        TMP_Text killsText = NewText("KillsText", root.transform, "Enemigos necesarios para pasar el nivel: 0 / 0", 26f);
        SetTopLeft(killsText.rectTransform, new Vector2(20f, -20f), new Vector2(600f, 40f));

        TMP_Text bossText = NewText("BossEntry", root.transform, "Encuentro misterioso", 26f);
        SetTopLeft(bossText.rectTransform, new Vector2(20f, -70f), new Vector2(600f, 40f));
        bossText.gameObject.SetActive(false);

        TMP_Text toastText = NewText("Toast", canvasGo.transform, "", 30f);
        toastText.alignment = TextAlignmentOptions.Center;
        RectTransform toastRt = toastText.rectTransform;
        toastRt.anchorMin = new Vector2(0.5f, 0f);
        toastRt.anchorMax = new Vector2(0.5f, 0f);
        toastRt.pivot = new Vector2(0.5f, 0f);
        toastRt.anchoredPosition = new Vector2(0f, 120f);
        toastRt.sizeDelta = new Vector2(900f, 60f);
        toastText.gameObject.SetActive(false);

        QuestPanelUI ui = canvasGo.AddComponent<QuestPanelUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("root").objectReferenceValue = root;
        so.FindProperty("killsText").objectReferenceValue = killsText;
        so.FindProperty("bossEntry").objectReferenceValue = bossText.gameObject;
        so.FindProperty("bossText").objectReferenceValue = bossText;
        so.FindProperty("toastRoot").objectReferenceValue = toastText.gameObject;
        so.FindProperty("toastText").objectReferenceValue = toastText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TMP_Text NewText(string name, Transform parent, string text, float size)
    {
        GameObject go = NewRect(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        return tmp;
    }

    private static void SetTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // =====================================================================
    // UTILIDADES
    // =====================================================================

    private static T[] SceneComponents<T>(Scene scene) where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.gameObject.scene == scene)
            .ToArray();
    }

    private static bool ExistsInLoadedScenes<T>() where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
