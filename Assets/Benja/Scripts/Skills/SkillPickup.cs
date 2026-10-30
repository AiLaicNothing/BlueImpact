using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillEntry
{
    public string characterName;
    public Skill skill;
}

public class SkillPickup : MonoBehaviour, IInteractable
{
    [Header("Persistence")]
    [SerializeField] private Pickup_Data pickupData;

    [Header("Skills")]
    [SerializeField] private SkillEntry[] skillsByCharacter;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Fade Out")]
    [SerializeField] private float fadeOutDuration = 2f;

    private bool pickedUp;
    private bool characterConfigured;

    private Skill currentSkill;
    private PlayerControl currentPlayer;

    private Renderer[] renderers;
    private Collider[] colliders;

    private readonly List<Material> fadeMaterials = new();
    private readonly List<Color> originalColors = new();

    // =========================
    // INITIALIZATION
    // =========================

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        CacheMaterials();

        // Hide until the correct character skill is identified.
        // Do not disable the GameObject itself.
        SetPickupVisible(false);
    }

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
        if (!HasPersistence())
        {
            Debug.LogWarning("[SkillPickup] Missing Pickup_Manager or valid Pickup_Data.", this);
            return;
        }

        if (WasPreviouslyCollected())
        {
            RemoveCollectedPickup();
            return;
        }

        TryFindPlayer();
    }

    private void Update()
    {
        if (pickedUp || characterConfigured) return;

        if (!HasPersistence()) return;

        if (WasPreviouslyCollected())
        {
            RemoveCollectedPickup(); return;
        }

        TryFindPlayer();
    }


    private void OnPlayerSpawned(PlayerControl player)
    {
        if (pickedUp) return;

        characterConfigured = false;
        ConfigureForPlayer(player);
    }

    private void TryFindPlayer()
    {
        if (currentPlayer == null)
        {
            if (Player_Manager.Instance == null) return;

            GameObject playerObject = Player_Manager.Instance.GetPlayer();

            if (playerObject == null) return;

            currentPlayer = playerObject.GetComponent<PlayerControl>();
        }

        if (currentPlayer != null) ConfigureForPlayer(currentPlayer);
    }

    private void ConfigureForPlayer(PlayerControl player)
    {
        if (pickedUp || player == null) return;

        currentPlayer = player;
        currentSkill = null;

        SetPickupVisible(false);

        if (!HasPersistence()) return;

        // Never reactivate a previously collected pickup.
        if (WasPreviouslyCollected())
        {
            RemoveCollectedPickup(); 
            return;
        }

        if (player.CurrentCharacterInfo == null ||
            player.CurrentCharacterInfo.data == null)
        {

            return;
        }

        string characterName = player.CurrentCharacterInfo.data.characterName;

        if (skillsByCharacter != null)
        {
            foreach (SkillEntry entry in skillsByCharacter)
            {
                if (entry == null || entry.skill == null) continue;

                if (entry.characterName == characterName)
                {
                    currentSkill = entry.skill;
                    break;
                }
            }
        }

        characterConfigured = true;

        if (currentSkill != null)
        {
            SetPickupVisible(true);

            Debug.Log(
                $"[SkillPickup] Available skill: {currentSkill.skillName}", this);
        }
        else
        {
            Debug.Log(
                $"[SkillPickup] No skill configured for {characterName}.", this);
        }
    }

    public void Interact()
    {
        if (pickedUp || currentSkill == null || currentPlayer == null) return;

        if (!HasPersistence()) return;

        if (WasPreviouslyCollected())
        {
            RemoveCollectedPickup(); return;
        }

        pickedUp = true;

        // Unlock the skill first.
        currentPlayer.UnlockSkill(currentSkill);

        // Record this world pickup as collected.
        bool saved = Pickup_Manager.Instance.MarkCollected(pickupData);

        if (!saved)
        {
            Debug.LogError(
                "[SkillPickup] Could not mark pickup as collected.", this);

            pickedUp = false;
            return;
        }

        InteractionUI.Instance?.SetInteractable(null);

        Debug.Log(
            $"[SkillPickup] Skill unlocked: {currentSkill.skillName}", this);

        PlayPickupSound();

        if (destroyOnPickup)
        {
            StartCoroutine(FadeOutCoroutine());
        }
        else
        {

            SetPickupVisible(false);
        }
    }

    public string GetInteractionText()
    {
        if (pickedUp || currentSkill == null) return null;

        return $"Desbloquear: {currentSkill.skillName}";
    }

    private bool HasPersistence()
    {
        return Pickup_Manager.Instance != null && pickupData != null &&
               !string.IsNullOrWhiteSpace(pickupData.PickupID);
    }

    private bool WasPreviouslyCollected()
    {
        return Pickup_Manager.Instance != null &&
               Pickup_Manager.Instance.IsCollected(pickupData);
    }

    private void RemoveCollectedPickup()
    {
        pickedUp = true;

        SetPickupVisible(false);

        Destroy(gameObject);
    }

    private void SetPickupVisible(bool visible)
    {
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null) renderer.enabled = visible;
            }
        }

        if (colliders != null)
        {
            foreach (Collider col in colliders)
            {
                if (col != null) col.enabled = visible;
            }
        }
    }

    private void CacheMaterials()
    {
        fadeMaterials.Clear();
        originalColors.Clear();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            foreach (Material material in renderer.materials)
            {
                if (material == null) continue;

                if (!material.HasProperty("_Color") &&
                    !material.HasProperty("_BaseColor")) continue;

                fadeMaterials.Add(material);
                originalColors.Add(material.color);
            }
        }
    }

    private void PlayPickupSound()
    {
        if (audioSource == null || pickupSound == null) return;

        audioSource.PlayOneShot(pickupSound, soundVolume);
    }

    private IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;

        float initialVolume = audioSource != null ? audioSource.volume : 1f;

        float duration = Mathf.Max(0.01f, fadeOutDuration);

        // Prevent another interaction during the fade.
        if (colliders != null)
        {
            foreach (Collider col in colliders)
            {
                if (col != null) col.enabled = false;
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Fade audio.
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Lerp(initialVolume, 0f, t);
            }

            // Fade materials.
            for (int i = 0; i < fadeMaterials.Count; i++)
            {
                Material material = fadeMaterials[i];

                if (material == null) continue;

                Color color = originalColors[i];

                color.a = Mathf.Lerp(originalColors[i].a, 0f, t);

                material.color = color;
            }

            yield return null;
        }

        SetPickupVisible(false);

        gameObject.SetActive(false);
    }
}