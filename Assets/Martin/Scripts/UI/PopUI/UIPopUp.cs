using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIPopUp : MonoBehaviour, IGamepadPanel
{
    public static UIPopUp Instance;

    [Header("Panel")]
    [SerializeField] private GameObject popUpPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Media")]
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI tittleText;
    [SerializeField] private DynamicInputText popupDescription;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextPage;
    [SerializeField] private Button previousPage;

    private List<PopUpPage> currentPages = new();
    private int currentPageIndex;
    private bool isOpen;

    private void Awake()
    {
        Instance = this;

        if (panelCanvasGroup == null && popUpPanel != null) panelCanvasGroup = popUpPanel.GetComponent<CanvasGroup>();

        if (popUpPanel != null) popUpPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(ClosePopUp);

        if (nextPage != null) nextPage.onClick.AddListener(NextPage);

        if (previousPage != null) previousPage.onClick.AddListener(PreviousPage);

        if (videoPlayer == null) videoPlayer = FindAnyObjectByType<VideoPlayer>();
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            isOpen = false;
            GamepadCursor.Instance?.Release(this);
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePopUp);

        if (nextPage != null) nextPage.onClick.RemoveListener(NextPage);

        if (previousPage != null) previousPage.onClick.RemoveListener(PreviousPage);

        if (Instance == this) Instance = null;
    }

    public void ShowPopUp(List<PopUpPage> pages)
    {
        if (pages == null || pages.Count == 0) return;

        currentPages = new List<PopUpPage>(pages);
        currentPageIndex = 0;

        popUpPanel.SetActive(true);
        isOpen = true;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        LoadPage();

        MutePlayerLoopAudio();
        Time.timeScale = 0f;

        GamepadCursor.Instance?.Acquire(this, ClosePopUp);
    }

    public void ClosePopUp()
    {
        if (!isOpen) return;

        isOpen = false;

        GamepadCursor.Instance?.Release(this);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        if (videoScreen != null) videoScreen.texture = null;

        if (tittleText != null)
            tittleText.text = string.Empty;

        if (popupDescription != null) popupDescription.SetTemplate("");

        popUpPanel.SetActive(false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        currentPages.Clear();
        currentPageIndex = 0;

        bool pausedNow = PauseManager.Instance != null && PauseManager.Instance.IsPaused;

        if (!pausedNow) Time.timeScale = 1f;
    }

    private void MutePlayerLoopAudio()
    {
        PlayerControl player = FindFirstObjectByType<PlayerControl>();

        if (player == null || player.audioLoopSource == null) return;

        player.audioLoopSource.Stop();
    }

    private void LoadPage()
    {
        if (currentPageIndex < 0 ||
            currentPageIndex >= currentPages.Count) return;

        PopUpPage page = currentPages[currentPageIndex];

        if (page == null) return;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = page.video;
        }

        if (videoScreen != null) videoScreen.texture = page.texture;

        if (tittleText != null) tittleText.text = page.title;

        if (popupDescription != null) popupDescription.SetTemplate(page.description);

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

    public void SetInputEnabled(bool enabled) {}

    public void SetInteractable(bool interactable)
    {
        if (closeButton != null) closeButton.interactable = interactable;

        if (nextPage != null)
            nextPage.interactable = interactable &&
                currentPageIndex < currentPages.Count - 1;

        if (previousPage != null)
            previousPage.interactable = interactable &&
            currentPageIndex > 0;
    }

    public string GetPanelName() => "UIPopUp";
}