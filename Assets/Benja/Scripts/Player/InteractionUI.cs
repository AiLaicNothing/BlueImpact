using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private DynamicInputText interactionText;

    [Header("Player")]
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root != null) root.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned += HandlePlayerSpawned;

        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerControl player = PlayerSpawn_Manager.Instance.CurrentPlayerControl;

            if (player != null) SetPlayer(player);
        }
    }

    private void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandlePlayerSpawned(PlayerControl player)
    {
        SetPlayer(player);
    }

    private void SetPlayer(PlayerControl player)
    {
        playerInputHandler = null;

        if (player == null) return;

        playerInputHandler =
            player.GetComponentInChildren<PlayerInputHandler>(true);

        if (playerInputHandler == null)
        {
            Debug.LogWarning("[InteractionUI] PlayerInputHandler not found.", player);
        }
    }

    public void SetInteractable(IInteractable interactable)
    {
        if (interactable == null)
        {
            Clear();
            return;
        }

        string text = interactable.GetInteractionText();

        if (string.IsNullOrEmpty(text))
        {
            Clear();
            return;
        }

        if (root != null) root.SetActive(true);

        if (interactionText != null)interactionText.SetTemplate("{Interact}: " + text);
    }

    public void Clear()
    {
        if (interactionText != null) interactionText.SetTemplate("");

        if (root != null) root.SetActive(false);
    }
}