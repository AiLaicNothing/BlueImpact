using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogEntry : MonoBehaviour
{
    [SerializeField] private Button logButton;
    [SerializeField] private TextMeshProUGUI logName;
    private Log_Data data;

    public event Action<Log_Data> OnSelected;

    public void Initialize(Log_Data log_Data)
    {
        data = log_Data;
        logName.text = data.logName;

        logButton.onClick.AddListener(HandleClick);
    }
    private void HandleClick()
    {
        OnSelected?.Invoke(data);
    }

    private void OnDestroy()
    {
        logButton.onClick.RemoveListener(HandleClick);
    }
}
