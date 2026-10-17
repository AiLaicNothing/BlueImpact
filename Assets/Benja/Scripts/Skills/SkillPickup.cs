using UnityEngine;
using System.Collections;

[System.Serializable]
public class SkillEntry
{
    public string characterName;
    public Skill skill;
}

public class SkillPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private SkillEntry[] skillsByCharacter;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Fade Out Settings")]
    [SerializeField] private float fadeOutDuration = 2f;  // Tiempo de desvanecimiento

    private bool pickedUp = false;
    private Skill currentSkill;
    private PlayerControl currentPlayer;
    private Material[] materials;

    private void OnEnable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void Start()
    {
        // ✅ GUARDAR REFERENCIAS A LOS MATERIALES
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            materials = renderer.materials;
        }
    }

    private void OnPlayerSpawned(PlayerControl player)
    {
        currentPlayer = player;

        if (player.CurrentCharacterInfo == null)
        {
            Debug.LogWarning("⚠️ CurrentCharacterInfo es null");
            return;
        }

        foreach (var entry in skillsByCharacter)
        {
            if (entry.characterName == player.CurrentCharacterInfo.data.characterName)
            {
                currentSkill = entry.skill;
                gameObject.SetActive(true);
                pickedUp = false;
                Debug.Log($"✅ SkillPickup para: {player.CurrentCharacterInfo.data.characterName}");
                return;
            }
        }

        gameObject.SetActive(false);
    }


    private void Update()
    {
        // Si ya tenemos al jugador, no necesitamos buscarlo otra vez.
        if (currentPlayer != null)
            return;

        // Comprobar si existe el administrador de jugadores.
        if (Player_Manager.Instance == null)
            return;

        // Obtener el objeto del jugador.
        GameObject playerObject = Player_Manager.Instance.GetPlayer();

        // Comprobar si el jugador existe.
        if (playerObject == null)
            return;

        // Obtener el componente PlayerControl.
        currentPlayer = playerObject.GetComponent<PlayerControl>();

        // Comprobar si el componente existe.
        if (currentPlayer == null)
        {
            Debug.LogWarning(
                "SkillPickup: el jugador no tiene el componente PlayerControl."
            );
            return;
        }

        // Esperar a que se cargue la información del personaje.
        if (currentPlayer.CurrentCharacterInfo == null)
            return;

        // Buscar la habilidad correspondiente al personaje.
        if (skillsByCharacter == null)
            return;

        foreach (SkillEntry entry in skillsByCharacter)
        {
            if (entry == null || entry.skill == null)
                continue;

            if (entry.characterName ==
                currentPlayer.CurrentCharacterInfo.data.characterName)
            {
                currentSkill = entry.skill;
                pickedUp = false;
                gameObject.SetActive(true);

                Debug.Log(
                    $"SkillPickup configurado para: " +
                    $"{currentPlayer.CurrentCharacterInfo.data.characterName}"
                );

                return;
            }
        }

        // Si no existe una habilidad para este personaje,
        // ocultar el objeto.
        gameObject.SetActive(false);
    }


    public void Interact()
    {
        // ✅ SOLO PERMITE INTERACTUAR UNA VEZ
        if (pickedUp || currentSkill == null || currentPlayer == null)
            return;

        pickedUp = true;  // ← MARCA COMO USADO INMEDIATAMENTE

        // ✅ REPRODUCIR SONIDO Y FADE OUT
        PlayPickupSound();
        currentPlayer.UnlockSkill(currentSkill);

        Debug.Log($"✅ Skill desbloqueada: {currentSkill.skillName}");

        // ✅ INICIA EL FADE OUT
        if (destroyOnPickup)
        {
            StartCoroutine(FadeOutCoroutine());
        }
    }

    private void PlayPickupSound()
    {
        if (audioSource == null || pickupSound == null)
        {
            Debug.LogWarning("⚠️ AudioSource o pickupSound no asignados en SkillPickup");
            return;
        }

        audioSource.PlayOneShot(pickupSound, soundVolume);
    }

    // ✅ COROUTINE PARA FADE OUT VISUAL + AUDIO
    private IEnumerator FadeOutCoroutine()
    {
        float elapsedTime = 0f;
        float initialVolume = soundVolume;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;  // 0 a 1

            // Fade out del audio
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Lerp(initialVolume, 0f, t);
            }

            // Fade out visual
            if (materials != null)
            {
                foreach (Material material in materials)
                {
                    Color color = material.color;
                    color.a = Mathf.Lerp(1f, 0f, t);
                    material.color = color;
                }
            }

            yield return null;
        }

        // ✅ ASEGURAR QUE QUEDE COMPLETAMENTE INVISIBLE Y SILENCIOSO
        if (audioSource != null)
            audioSource.volume = 0f;

        if (materials != null)
        {
            foreach (Material material in materials)
            {
                Color color = material.color;
                color.a = 0f;
                material.color = color;
            }
        }

        // ✅ DESACTIVAR OBJETO
        gameObject.SetActive(false);
    }

    public string GetInteractionText()
    {
        if (currentSkill == null)
            return "Desbloquear";
        return $"Desbloquear: {currentSkill.skillName}";
    }
}