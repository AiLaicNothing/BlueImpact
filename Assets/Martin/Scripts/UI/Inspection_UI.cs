using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Inspection_UI : MonoBehaviour
{
    public static Inspection_UI Instance { get; private set; }

    [Header("Inspection")]
    [SerializeField] private CanvasGroup inspectionCanvasGroup;

    [Header("Panels")]
    [SerializeField] private GameObject inspectionPanel;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI description;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (inspectionCanvasGroup == null) inspectionCanvasGroup = GetComponent<CanvasGroup>();

        SetInspectionVisible(false);
    }

    private void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseInspection);
    }

    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseInspection);

        if (IsOpen)
        {
            IsOpen = false;
            GamepadCursor.Instance?.Release(this);
        }
    }

    public void OpenInspection(Log_Data log_Data)
    {
        if (IsOpen) return;

        GamepadCursor.Instance?.CloseAllExcept(this);

        IsOpen = true;

        image.sprite = log_Data.logIllustration;
        description.text = log_Data.logDescription;

        SetInspectionVisible(true);

        GamepadCursor.Instance?.Acquire(this, CloseInspection);
    }

    public void CloseInspection()
    {
        if (!IsOpen) return;

        IsOpen = false;
        SetInspectionVisible(false);

        GamepadCursor.Instance?.Release(this);
    }

    private void SetInspectionVisible(bool visible)
    {
        if (inspectionCanvasGroup == null) return;

        inspectionCanvasGroup.alpha = visible ? 1f : 0f;
        inspectionCanvasGroup.interactable = visible;
        inspectionCanvasGroup.blocksRaycasts = visible;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
