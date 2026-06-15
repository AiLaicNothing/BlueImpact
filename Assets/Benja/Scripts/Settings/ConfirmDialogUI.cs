using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmDialogUI : MonoBehaviour
{
    public static ConfirmDialogUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action onConfirmCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        gameObject.SetActive(false);
    }

    public void Show(string message, System.Action onConfirm = null)
    {
        messageText.text = message;
        onConfirmCallback = onConfirm;

        gameObject.SetActive(true);
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
    }

    public void Close()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);
    }

    private void OnConfirm()
    {
        onConfirmCallback?.Invoke();
        Close();
    }

    private void OnCancel()
    {
        Close();
    }
}