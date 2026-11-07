using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Objeto interactuable de un solo uso que otorga puntos de mejora al player
/// y luego desaparece. NO cuenta como checkpoint (no se registra en
/// CheckpointManager, no setea respawn, no abre el menú de checkpoint).
///
/// Uso típico: recompensas de exploración, secretos, mini-jefes, cofres, etc.
/// </summary>
public class UpgradePointPickup : MonoBehaviour, IInteractable
{
    [Header("Persistence")]
    [SerializeField] private Pickup_Data pickupData;

    [Header("Reward")]
    [SerializeField] private int upgradePointsReward = 1;

    [Header("Interaction")]
    [SerializeField] private string interactionText = "Recoger Punto de Mejora";

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float sfxVolume = 1f;

    [Header("Feedback")]
    [SerializeField] private GameObject model;
    [SerializeField] private float fadeOutDuration = 2f;
    [SerializeField] private TrailRenderer pickupVfx;
    [SerializeField] private bool showPopup = true;
    private PlayerControl currentPlayer;

    private bool consumed;

    private void Start()
    {
        // If already collected in a previous visit,
        // remove this pickup from the scene.
        if (WasPreviouslyCollected())
        {
            consumed = true;
            Destroy(model);
        }
        TryFindPlayer();
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
    }
        public void Interact()
    {
        if (consumed) return;

        // Don't allow collection without persistence.
        if (Pickup_Manager.Instance == null || pickupData == null ||
            string.IsNullOrWhiteSpace(pickupData.PickupID))
        {
            Debug.LogWarning("[UpgradePointPickup] Missing Pickup_Manager or valid Pickup_Data.", this);
            return;
        }

        // Check again in case another system already
        // marked the pickup as collected.
        if (WasPreviouslyCollected())
        {
            consumed = true;
            Destroy(gameObject);
            return;
        }

        // Lock this pickup against repeated interactions.
        consumed = true;

        // Give reward first.
        if (!GivePointsToPlayer())
        {
            consumed = false;
            return;
        }

        // Save the pickup as collected.
        Pickup_Manager.Instance.MarkCollected(pickupData);

        // Hide interaction prompt.
        InteractionUI.Instance?.SetInteractable(null);

        // Feedback.
        PlayPickupSound();
        SpawnVfx();
        ShowPopupFeedback();

        Destroy(model);
    }

    public string GetInteractionText()
    {
        return consumed ? null : interactionText;
    }

    private bool GivePointsToPlayer()
    {
        PlayerStatsManager playerStats = FindFirstObjectByType<PlayerStatsManager>();

        CharacterStatsManager characterStats = CharacterStatsManager.Instance;

        if (playerStats == null || characterStats == null)
        {
            Debug.LogError("[UpgradePointPickup] PlayerStatsManager or CharacterStatsManager not found.", this);

            return false;
        }

        playerStats.AddUpgradePoints(upgradePointsReward);
        characterStats.AddStatPoints(upgradePointsReward);

        Debug.Log($"+{upgradePointsReward} Upgrade Points obtained.");

        return true;
    }

    private bool WasPreviouslyCollected()
    {
        return Pickup_Manager.Instance != null && Pickup_Manager.Instance.IsCollected(pickupData);
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null || Audio_Manager.Instance == null) return;

        Audio_Manager.Instance.PlaySFX(pickupSound, sfxVolume);
    }

    private void SpawnVfx()
    {
        if (pickupVfx == null) return;

        //GameObject vfx = Instantiate(pickupVfx, transform.position,Quaternion.identity);
        //Destroy(vfx, 3f);
        StartCoroutine(FadeOutCoroutine());
    }
    private IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;


        float duration = Mathf.Max(0.01f, fadeOutDuration);
        pickupVfx.enabled = true;


        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            pickupVfx.gameObject.transform.position = currentPlayer.Model.position;


            yield return null;
        }

        pickupVfx.enabled = false;
    }
    private void ShowPopupFeedback()
    {
        if (!showPopup || PopupUI.Instance == null) return;

        PopupUI.Instance.Show($"+{upgradePointsReward} Puntos de Mejora");
    }
}