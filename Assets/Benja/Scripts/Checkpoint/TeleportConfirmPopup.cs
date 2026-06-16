using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TeleportConfirmPopup : MonoBehaviour
{
    public static TeleportConfirmPopup Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private EventSystem eventSystem;

    private Action confirmAction;

    private void Awake()
    {
        Instance = this;

        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        gameObject.SetActive(false);

        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Close);
    }

    public void Open(string checkpointName, Action onConfirm)
    {
        confirmAction = onConfirm;

        descriptionText.text = $"¿Viajar a {checkpointName}?";

        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // ✅ SELECCIONAR BOTÓN CONFIRMAR PARA GAMEPAD
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(confirmButton.gameObject);
        }
    }

    private void Confirm()
    {
        confirmAction?.Invoke();
        Close();
    }

    private void Close()
    {
        confirmAction = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);

        // ✅ VOLVER A SELECCIONAR PANEL ANTERIOR
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}