using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharSelector_UI : MonoBehaviour
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
    private int currentIndex;
    private bool hasSelectedAlready;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        // ✅ CREAR AudioSource LOCAL para la música
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;  // ✅ LOOP ACTIVO
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (hasSelectedAlready) return;

        StartSelector();
    }

    private void StartSelector()
    {
        if (!panel.activeSelf)
        {
            panel.SetActive(true);
        }

        // ✅ REPRODUCIR MÚSICA EN LOOP
        if (music != null && !audioSource.isPlaying)
        {
            audioSource.clip = music;
            audioSource.volume = 0.5f;
            audioSource.Play();
            Debug.Log("🎵 Música del selector iniciada en loop");
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
        confirmButton.onClick.AddListener(() => StartGame());
    }

    private void SelectCharacter(int index)
    {
        var data = CharSelector_Manager.Instance.GetCharacterInfo(index);

        if (data == null) return;

        currentIndex = index;

        charName.text = data.name;
        description.text = data.description;
        charImage.sprite = data.portrait;

        PlayerSpawn_Manager.Instance.SetCharacter(data);
    }

    private void StartGame()
    {
        panel.SetActive(false);

        // ✅ DETENER MÚSICA DEL SELECTOR
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("⏹️ Música del selector detenida");
        }

        var spawnPoint = CharSelector_Manager.Instance.GetInitialSpawnPoint();

        // ✅ CONFIGURAR RESPAWN INICIAL
        CharSelector_Manager.Instance.SetupRespawn();

        PlayerSpawn_Manager.Instance.SpawnCharacter(spawnPoint);
    }

    private void OnDestroy()
    {
        // ✅ LIMPIAR AudioSource
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}