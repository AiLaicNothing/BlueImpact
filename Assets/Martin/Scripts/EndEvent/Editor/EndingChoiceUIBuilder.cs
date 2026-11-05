using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Crea el Canvas con la eleccion del final (Destruirlo / Usarlo) en la escena del boss,
// le conecta las referencias de EndingChoiceUI y lo asigna al BlueImpactEvent de la escena.
public static class EndingChoiceUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Zone_00_Tutorial.unity";
    private const string TitleFontPath = "Assets/Roseling/Blueimpact_letras-Regular SDF.asset";

    private static readonly Color Dim = new Color(0.01f, 0.02f, 0.08f, 0.82f);
    private static readonly Color BoxColor = new Color(0.04f, 0.06f, 0.18f, 0.96f);
    private static readonly Color Glow = new Color(0.35f, 0.85f, 1f, 1f);
    private static readonly Color TextColor = new Color(0.86f, 0.93f, 1f, 1f);
    private static readonly Color DestroyColor = new Color(0.55f, 0.12f, 0.16f, 1f);
    private static readonly Color UseColor = new Color(0.10f, 0.38f, 0.62f, 1f);

    [MenuItem("Tools/Morgana/4. Crear Canvas de eleccion de final")]
    public static void Build()
    {
        if (!OpenTargetScene()) return;

        Scene scene = SceneManager.GetActiveScene();

        EndingChoiceUI existing = UnityEngine.Object.FindFirstObjectByType<EndingChoiceUI>(FindObjectsInactive.Include);

        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.LogWarning("[EndingChoiceUI] Ya existe '" + existing.name + "' en " + scene.name + ". No se crea otro; borralo si quieres rehacerlo.");
            WireToEvent(existing);
            return;
        }

        EnsureEventSystem();

        // ---------- Canvas raiz ----------
        GameObject root = new GameObject("EndingChoiceCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(root, "Crear Canvas de eleccion de final");

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // encima del HUD

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // ---------- Panel (fondo oscuro a pantalla completa) ----------
        GameObject panel = CreateUI("Panel", root.transform);
        Stretch(panel);
        panel.AddComponent<Image>().color = Dim;

        // ---------- Caja central ----------
        GameObject box = CreateUI("Box", panel.transform);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(980, 460);
        box.AddComponent<Image>().color = BoxColor;

        Outline outline = box.AddComponent<Outline>();
        outline.effectColor = new Color(Glow.r, Glow.g, Glow.b, 0.8f);
        outline.effectDistance = new Vector2(3, -3);

        VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(60, 60, 50, 50);
        vlg.spacing = 28;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Titulo decorativo
        TMP_Text title = CreateText("Title", box.transform, "BLUE IMPACT", 64, Glow, LoadFont(TitleFontPath));
        title.characterSpacing = 6;
        SetPreferredHeight(title.gameObject, 80);

        // Texto de la pregunta (lo llena EndingChoiceUI)
        TMP_Text prompt = CreateText("Prompt", box.transform, "El Blue Impact late frente a ti.", 36, TextColor, null);
        prompt.fontStyle = FontStyles.Italic;
        SetPreferredHeight(prompt.gameObject, 110);

        // Fila de botones
        GameObject row = CreateUI("Buttons", box.transform);
        SetPreferredHeight(row, 110);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 60;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        Button destroyButton = CreateButton("DestroyButton", row.transform, "Destruirlo", DestroyColor, out TMP_Text destroyLabel);
        Button useButton = CreateButton("UseButton", row.transform, "Usarlo", UseColor, out TMP_Text useLabel);

        // Navegacion con mando: izquierda/derecha entre los dos botones
        SetNavigation(destroyButton, null, useButton);
        SetNavigation(useButton, destroyButton, null);

        // ---------- EndingChoiceUI ----------
        EndingChoiceUI ui = root.AddComponent<EndingChoiceUI>();

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("promptText").objectReferenceValue = prompt;
        so.FindProperty("destroyButton").objectReferenceValue = destroyButton;
        so.FindProperty("useButton").objectReferenceValue = useButton;
        so.FindProperty("destroyLabel").objectReferenceValue = destroyLabel;
        so.FindProperty("useLabel").objectReferenceValue = useLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Arranca oculto (EndingChoiceUI tambien lo apaga en Awake)
        panel.SetActive(false);

        WireToEvent(ui);
        CheckCinematic();

        // No se guarda sola: la escena puede tener otros cambios sin guardar. Guarda con Ctrl+S.
        EditorSceneManager.MarkSceneDirty(scene);

        Selection.activeGameObject = root;
        Debug.Log("[EndingChoiceUI] Canvas creado en " + scene.path + ". Guarda la escena (Ctrl+S) para conservarlo.");
    }

    // ---------- ESCENA ----------

    private static bool OpenTargetScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path == ScenePath) return true;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            Debug.LogError("[EndingChoiceUI] No existe la escena " + ScenePath);
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static void WireToEvent(EndingChoiceUI ui)
    {
        BlueImpactEvent evt = UnityEngine.Object.FindFirstObjectByType<BlueImpactEvent>(FindObjectsInactive.Include);

        if (evt == null)
        {
            Debug.LogWarning("[EndingChoiceUI] No hay BlueImpactEvent en la escena: asigna el canvas a su campo 'choiceUI' cuando lo agregues.");
            return;
        }

        SerializedObject so = new SerializedObject(evt);
        SerializedProperty prop = so.FindProperty("choiceUI");

        if (prop.objectReferenceValue == ui) return;

        prop.objectReferenceValue = ui;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(evt.gameObject.scene);
        Debug.Log("[EndingChoiceUI] Asignado a choiceUI de '" + evt.name + "'");
    }

    private static void CheckCinematic()
    {
        if (UnityEngine.Object.FindFirstObjectByType<Ending_Cinematic>(FindObjectsInactive.Include) == null)
        {
            Debug.LogWarning("[EndingChoiceUI] No hay Ending_Cinematic en la escena: sin el, al elegir se vuelve directo al menu.");
        }
    }

    // No se crea uno: el resto de la UI de las zonas usa el EventSystem persistente del juego,
    // y uno duplicado daria conflictos. Solo se avisa para poder probar la escena suelta.
    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        Debug.Log("[EndingChoiceUI] La escena no tiene EventSystem propio: los botones usan el persistente del juego. " +
                  "Si abres la escena sola para probar, agrega uno temporal.");
    }

    // ---------- UI ----------

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetPreferredHeight(GameObject go, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, Color color, TMP_FontAsset font)
    {
        GameObject go = CreateUI(name, parent);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();

        if (font != null) t.font = font;

        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;

        return t;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color baseColor, out TMP_Text labelText)
    {
        GameObject go = CreateUI(name, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 96);

        // El color va en la Image (se ve igual en el editor); el Button solo lo aclara al seleccionarlo
        Image img = go.AddComponent<Image>();
        img.color = baseColor;

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(Glow.r, Glow.g, Glow.b, 0.6f);
        ol.effectDistance = new Vector2(2, -2);

        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;

        // Mismos colores con mouse y con mando: el seleccionado brilla
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.6f, 1.8f, 2f, 1f);
        cb.selectedColor = new Color(1.6f, 1.8f, 2f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        cb.fadeDuration = 0.12f;
        b.colors = cb;

        labelText = CreateText("Label", go.transform, label, 38, Color.white, null);
        labelText.fontStyle = FontStyles.Bold;
        Stretch(labelText.gameObject);

        return b;
    }

    private static void SetNavigation(Button b, Selectable left, Selectable right)
    {
        Navigation nav = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = left,
            selectOnRight = right,
        };

        b.navigation = nav;
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
    }
}
