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

    [Header("Sfx")]
    [SerializeField] private AudioClip music;

    [Header("Buttons")]
    [SerializeField] private Button[] charButtons;
    [SerializeField] private Button confirmButton;
    private int currentIndex;
    private bool hasSelectedAlready;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
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

        Audio_Manager.Instance.PlayMusic(music, 0.15f);
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

        var spawnPoint = CharSelector_Manager.Instance.GetInitialSpawnPoint();

        // ✅ CONFIGURAR RESPAWN INICIAL
        CharSelector_Manager.Instance.SetupRespawn();

        PlayerSpawn_Manager.Instance.SpawnCharacter(spawnPoint);
    }
}
