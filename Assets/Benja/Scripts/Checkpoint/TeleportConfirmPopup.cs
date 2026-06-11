using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeleportConfirmPopup : MonoBehaviour
{
    public static TeleportConfirmPopup Instance
    {
        get;
        private set;
    }

    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Button confirmButton;

    [SerializeField] private Button cancelButton;

    private Action confirmAction;

    private void Awake()
    {
        Instance = this;

        gameObject.SetActive(false);

        confirmButton.onClick.AddListener(
            Confirm);

        cancelButton.onClick.AddListener(
            Close);
    }

    public void Open(
        string checkpointName,
        Action onConfirm)
    {
        confirmAction = onConfirm;

        descriptionText.text =
            $"¿Viajar a {checkpointName}?";

        gameObject.SetActive(true);

        confirmButton.Select();
    }

    private void Confirm()
    {
        confirmAction?.Invoke();

        Close();
    }

    private void Close()
    {
        confirmAction = null;

        gameObject.SetActive(false);
    }
}