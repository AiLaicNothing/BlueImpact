using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharSelector_UI : MonoBehaviour, IGamepadPanel
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI charName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Image charImage;

    [Header("Audio")]
    [SerializeField] private AudioClip music;
    private AudioSource audioSource;

    [Header("Buttons")]
    [SerializeField] private Button[] charButtons;
    [SerializeField] private Button confirmButton;

    private bool hasSelectedAlready;
    private bool isRegistered; // Si este panel está en la pila de GamepadCursor
    private int currentIndex;
    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (SaveLoadManager.canLoad)
        {
            LoadData();
            SelectCharacter(currentIndex);
            StartGame();
            hasSelectedAlready = true;
        }
        if (hasSelectedAlready) return;

        StartSelector();
    }
    void LoadData()
    {
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            currentIndex = loadedData.currentIndexCharacter;
        }
    }

    private void OnDestroy()
    {
        if (audioSource != null)
            audioSource.Stop();

        Unregister();
    }

    // ---------- Registro en GamepadCursor ----------

    // Sin onClose: B no hace nada en el selector de personajes
    private void Register()
    {
        if (isRegistered || GamepadCursor.Instance == null) return;

        GamepadCursor.Instance.Acquire(this);
        isRegistered = true;
    }

    private void Unregister()
    {
        if (!isRegistered) return;

        if (GamepadCursor.Instance != null)
            GamepadCursor.Instance.Release(this);

        isRegistered = false;
    }

    // ---------- Flujo del selector ----------

    private void StartSelector()
    {
        if (!panel.activeSelf)
            panel.SetActive(true);

        // Cursor visible para el mouse; el movimiento con stick lo maneja GamepadCursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Register();

        if (music != null && !audioSource.isPlaying)
        {
            audioSource.clip = music;
            audioSource.volume = 0.5f;
            audioSource.Play();
        }

        SelectCharacter(0);
        SetButtons();
    }

    private void SetButtons()
    {
        for (int i = 0; i < charButtons.Length; i++)
        {
            int index = i;
            charButtons[i].onClick.RemoveAllListeners();
            charButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(StartGame);
    }

    private void SelectCharacter(int index)
    {
        if (CharSelector_Manager.Instance == null)
        {
            Debug.LogError("[CharSelector_UI] CharSelector_Manager.Instance is NULL.");
            return;
        }

        CharacterInfo data = CharSelector_Manager.Instance.GetCharacterInfo(index);

        if (data == null || data.data == null)
        {
            Debug.LogError($"[CharSelector_UI] CharacterInfo inválido en índice {index}.");
            return;
        }

        if (charName == null || description == null || charImage == null)
        {
            Debug.LogError(
                "[CharSelector_UI] Faltan referencias de UI. " +
                $"charName: {charName != null}, " +
                $"description: {description != null}, " +
                $"charImage: {charImage != null}"
            );
            return;
        }

        if (PlayerSpawn_Manager.Instance == null)
        {
            Debug.LogError("[CharSelector_UI] PlayerSpawn_Manager.Instance is NULL.");
            return;
        }

        charName.text = data.data.characterName;
        description.text = data.description;
        charImage.sprite = data.portrait;

        Player_Manager.Instance.SetCharacterData(data);
        PlayerSpawn_Manager.Instance.SetCharacter(data);

        if (CharacterStatsManager.Instance != null)
            CharacterStatsManager.Instance.Initialize();
    }

    private void StartGame()
    {
        SaveLoadManager.Instance.SaveGame("currentIndexCharacter", currentIndex);
        panel.SetActive(false);

        Unregister();

        if (audioSource != null && audioSource.isPlaying) { 
            audioSource.Stop();
            Debug.Log("⏹️ Música del selector detenida");
        }
        if (SaveLoadManager.canLoad)
        {
            SaveData loadedData = SaveLoadManager.Instance.LoadGame();
            if (loadedData != null)
            {
                if(loadedData.discoveredCheckpoints.Count > 0)
                {
                    var spawnPoints = CharSelector_Manager.Instance.GetInitialSpawnPoint();

                    PlayerSpawn_Manager.Instance.SpawnCharacter(spawnPoints);
                    Debug.Log("cargando checkpoint guardado");
                    Invoke(nameof(LoadRespawn), 0.5f);
                    return;
                }
            }
        }
        var spawnPoint = CharSelector_Manager.Instance.GetInitialSpawnPoint();
        CharSelector_Manager.Instance.SetupRespawn();
        PlayerSpawn_Manager.Instance.SpawnCharacter(spawnPoint);
    }

    // ---------- IGamepadPanel ----------

    void LoadRespawn()
    {   
        RespawnManager.Instance.Respawn();
    }
    public void SetInputEnabled(bool enabled)
    {
        // Solo activa o desactiva la presencia de este panel en la pila de GamepadCursor
        if (enabled && panel != null && panel.activeSelf)
            Register();
        else
            Unregister();
    }

    public void SetInteractable(bool interactable)
    {
        foreach (var btn in charButtons)
            btn.interactable = interactable;

        confirmButton.interactable = interactable;
    }

    public string GetPanelName() => "CharSelector_UI";
}