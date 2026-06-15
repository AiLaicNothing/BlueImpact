using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected Button modifyButton;
    [SerializeField] protected Button backButton;

    protected List<SettingsTabUI> tabs = new();
    protected int currentTabIndex = 0;
    protected bool isActive = false;

    protected virtual void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        if (modifyButton != null)
            modifyButton.onClick.AddListener(OnModifyPressed);
    }

    protected virtual void OnEnable()
    {
        // Escuchar inputs para navegación de tabs
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
        isActive = true;
        canvasGroup.alpha = 1;
        ShowTab(0);
    }

    public virtual void Close()
    {
        isActive = false;
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void ShowTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Count) return;

        // Ocultar tab anterior
        if (currentTabIndex < tabs.Count)
        {
            tabs[currentTabIndex].SetActive(false);
        }

        // Mostrar nuevo tab
        currentTabIndex = tabIndex;
        tabs[currentTabIndex].SetActive(true);
    }

    public void NextTab()
    {
        int nextIndex = (currentTabIndex + 1) % tabs.Count;
        ShowTab(nextIndex);
    }

    public void PreviousTab()
    {
        int prevIndex = currentTabIndex - 1;
        if (prevIndex < 0) prevIndex = tabs.Count - 1;
        ShowTab(prevIndex);
    }

    protected virtual void OnModifyPressed()
    {
        // Override en clases derivadas
    }

    protected virtual void OnBackPressed()
    {
        Close();
    }

    protected void RegisterTab(SettingsTabUI tab)
    {
        tabs.Add(tab);
    }
}

// ==================== BASE TAB ====================

public class SettingsTabUI : MonoBehaviour
{
    protected CanvasGroup canvasGroup;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public virtual void SetActive(bool active)
    {
        canvasGroup.alpha = active ? 1 : 0;
        canvasGroup.interactable = active;
        canvasGroup.blocksRaycasts = active;
    }

    public virtual void Refresh()
    {
        // Override en clases derivadas para actualizar valores mostrados
    }
}