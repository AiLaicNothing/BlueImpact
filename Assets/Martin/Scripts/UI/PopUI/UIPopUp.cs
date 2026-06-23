using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIPopUp : MonoBehaviour
{
    public static UIPopUp Instance;

    [SerializeField] private GameObject popUpPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TextMeshProUGUI tittleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextPage;
    [SerializeField] private Button previousPage;

    private List<PopUpPage> currentPages = new List<PopUpPage>();
    private int currentPageIndex;

    private void Awake()
    {
        Instance = this;

        if (panelCanvasGroup == null)
            panelCanvasGroup = popUpPanel.GetComponent<CanvasGroup>();

        if (popUpPanel != null) popUpPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(ClosePopUp);
        if (nextPage != null) nextPage.onClick.AddListener(NextPage);
        if (previousPage != null) previousPage.onClick.AddListener(PreviousPage);
    }

    public void ShowPopUp(List<PopUpPage> pages)
    {
        if (pages == null || pages.Count == 0) return;

        currentPages = pages;
        currentPageIndex = 0;

        popUpPanel.SetActive(true);
        LoadPage();

        // ✅ CAMBIAR GAMEMODE A UI
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(GameMode.UI);
        }

        // ✅ ASEGURAR QUE EL PANEL RECIBE INPUT
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;
    }

    private void LoadPage()
    {
        if (currentPageIndex < 0 || currentPageIndex >= currentPages.Count) return;

        PopUpPage page = currentPages[currentPageIndex];

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = page.video;
        }

        if (videoScreen != null) videoScreen.texture = page.texture;

        if (tittleText != null) tittleText.text = page.title;

        if (descriptionText != null) descriptionText.text = page.description;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasPrevious = currentPageIndex > 0;
        bool hasNext = currentPageIndex < currentPages.Count - 1;
        bool isLastPage = currentPageIndex == currentPages.Count - 1;

        if (previousPage != null) previousPage.gameObject.SetActive(hasPrevious);
        if (nextPage != null) nextPage.gameObject.SetActive(hasNext);
        if (closeButton != null) closeButton.gameObject.SetActive(isLastPage);
    }

    public void NextPage()
    {
        if (currentPageIndex >= currentPages.Count - 1) return;

        currentPageIndex++;
        LoadPage();
    }

    public void PreviousPage()
    {
        if (currentPageIndex <= 0) return;

        currentPageIndex--;
        LoadPage();
    }

    public void ClosePopUp()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        if (videoScreen != null) videoScreen.texture = null;
        if (tittleText != null) tittleText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;

        popUpPanel.SetActive(false);

        // ✅ DESACTIVAR INPUT DEL PANEL
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        // ✅ RESTAURAR GAMEMODE
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(GameMode.Gameplay);
        }

        currentPages.Clear();
        currentPageIndex = 0;

        Time.timeScale = 1f;
    }
}