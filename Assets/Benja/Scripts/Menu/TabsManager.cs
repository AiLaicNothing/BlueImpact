using UnityEngine;
using UnityEngine.UI;

public class TabsManager : MonoBehaviour
{
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private CanvasGroup[] tabContents;

    private int currentTabIndex = 0;

    private void Awake()
    {
        // Conectar cada botón a su índice
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            if (tabButtons[i] != null)
            {
                tabButtons[i].onClick.AddListener(() => ShowTab(index));
            }
        }

        // Mostrar primer tab
        ShowTab(0);
    }

    /// <summary>
    /// Muestra un tab específico y oculta los demás
    /// </summary>
    public void ShowTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabContents.Length)
            return;

        // Ocultar tab anterior
        if (currentTabIndex < tabContents.Length && tabContents[currentTabIndex] != null)
        {
            tabContents[currentTabIndex].alpha = 0;
            tabContents[currentTabIndex].interactable = false;
            tabContents[currentTabIndex].blocksRaycasts = false;
        }

        // Mostrar nuevo tab
        currentTabIndex = tabIndex;
        if (tabContents[currentTabIndex] != null)
        {
            tabContents[currentTabIndex].alpha = 1;
            tabContents[currentTabIndex].interactable = true;
            tabContents[currentTabIndex].blocksRaycasts = true;

            Debug.Log($"Tab {tabIndex} abierto");
        }
    }

    /// <summary>
    /// Ir al siguiente tab (circular)
    /// </summary>
    public void NextTab()
    {
        int nextIndex = (currentTabIndex + 1) % tabContents.Length;
        ShowTab(nextIndex);
    }

    /// <summary>
    /// Ir al tab anterior (circular)
    /// </summary>
    public void PreviousTab()
    {
        int prevIndex = currentTabIndex - 1;
        if (prevIndex < 0) prevIndex = tabContents.Length - 1;
        ShowTab(prevIndex);
    }

    /// <summary>
    /// Obtiene el índice actual
    /// </summary>
    public int GetCurrentTabIndex()
    {
        return currentTabIndex;
    }
}