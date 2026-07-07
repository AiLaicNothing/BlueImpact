using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 🎵 UIAudioFeedback - Sonidos globales para botones (Persiste entre escenas)
/// 
/// ✅ Suena cuando seleccionas botones en Menu Y Game
/// ✅ Se adapta al EventSystem de cada escena automáticamente
/// </summary>
public class UIAudioFeedback : MonoBehaviour
{
    public static UIAudioFeedback Instance { get; private set; }

    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private float soundVolume = 0.5f;

    private GameObject lastSelectedButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("✅ UIAudioFeedback inicializado (persiste entre escenas)");
    }

    private void Update()
    {
        // ✅ OBTENER EventSystem ACTUAL (cambia por escena)
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null || Audio_Manager.Instance == null)
            return;

        // ✅ DETECTAR CAMBIO DE SELECCIÓN
        GameObject currentSelected = eventSystem.currentSelectedGameObject;

        if (currentSelected != lastSelectedButton && currentSelected != null)
        {
            if (currentSelected.GetComponent<Button>() != null)
            {
                if (selectSound != null)
                {
                    Audio_Manager.Instance.PlaySFX(selectSound, soundVolume);
                    Debug.Log($"🔘 Botón seleccionado: {currentSelected.name}");
                }

                lastSelectedButton = currentSelected;
            }
        }

        if (currentSelected == null && lastSelectedButton != null)
        {
            lastSelectedButton = null;
        }
    }
}