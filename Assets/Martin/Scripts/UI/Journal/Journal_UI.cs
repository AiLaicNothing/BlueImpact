using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Journal_UI : MonoBehaviour
{
    [Header("Journal")]
    [SerializeField] private CanvasGroup journalCanvasGroup;

    [Header("Panels")]
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject logPanel;

    [Header("Buttons")]
    [SerializeField] private Button characterButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button logButton;
    [SerializeField] private Button closeButton;

    private PlayerInputHandler playerInputHandler;

    private bool inJournal;

    public bool IsOpen => inJournal;

    private void Awake()
    {
        if (journalCanvasGroup == null) journalCanvasGroup = GetComponent<CanvasGroup>();

        ShowContentPanel(characterPanel);
        SetJournalVisible(false);
    }

    private void OnEnable()
    {
        if (characterButton != null) characterButton.onClick.AddListener(OnCharacterButton);

        if (inventoryButton != null) inventoryButton.onClick.AddListener(OnInventoryButton);

        if (logButton != null) logButton.onClick.AddListener(OnLogButton);

        if (closeButton != null) closeButton.onClick.AddListener(CloseJournal);

        TrySubscribe();
    }

    private void OnDisable()
    {
        if (characterButton != null) characterButton.onClick.RemoveListener(OnCharacterButton);

        if (inventoryButton != null) inventoryButton.onClick.RemoveListener(OnInventoryButton);

        if (logButton != null) logButton.onClick.RemoveListener(OnLogButton);

        if (closeButton != null) closeButton.onClick.RemoveListener(CloseJournal);

        if (playerInputHandler != null &&
            playerInputHandler.journalAction != null)
        {
            playerInputHandler.journalAction.performed -= OnJournalInput;
        }
    }

    private void Update()
    {
        // Retry if the player input handler wasn't available yet.
        if (playerInputHandler == null) TrySubscribe();
    }

    #region Input

    private void TrySubscribe()
    {
        if (playerInputHandler == null) playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        if (playerInputHandler == null || playerInputHandler.journalAction == null) return;

        playerInputHandler.journalAction.performed -= OnJournalInput;
        playerInputHandler.journalAction.performed += OnJournalInput;
    }

    private void OnJournalInput(InputAction.CallbackContext context)
    {
        var gc = GamepadCursor.Instance;

        // Let a higher-priority popup or panel handle closing first.
        if (gc != null && gc.HasOpenPanels && !gc.IsTop(this) && gc.TopCanClose)
        {
            gc.CloseTop();
            return;
        }

        if (inJournal) CloseJournal();
        else OpenJournal();
    }

    #endregion

    #region Open / Close

    public void OpenJournal()
    {
        if (inJournal) return;

        GamepadCursor.Instance?.CloseAllExcept(this);

        inJournal = true;

        SetJournalVisible(true);

        GamepadCursor.Instance?.Acquire(this, CloseJournal);
    }

    public void CloseJournal()
    {
        if (!inJournal) return;

        inJournal = false;

        SetJournalVisible(false);

        GamepadCursor.Instance?.Release(this);
    }

    private void SetJournalVisible(bool visible)
    {
        if (journalCanvasGroup == null) return;

        journalCanvasGroup.alpha = visible ? 1f : 0f;
        journalCanvasGroup.interactable = visible;
        journalCanvasGroup.blocksRaycasts = visible;
    }

    #endregion

    #region Navigation

    public void OnCharacterButton()
    {
        ShowContentPanel(characterPanel);
    }

    public void OnInventoryButton()
    {
        ShowContentPanel(inventoryPanel);
    }

    public void OnLogButton()
    {
        ShowContentPanel(logPanel);
    }

    private void ShowContentPanel(GameObject selectedPanel)
    {
        if (characterPanel != null)
            characterPanel.SetActive(characterPanel == selectedPanel);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(inventoryPanel == selectedPanel);

        if (logPanel != null)
            logPanel.SetActive(logPanel == selectedPanel);
    }

    #endregion
}
