using UnityEngine;

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
    [SerializeField] private GameObject pickupVfx;
    [SerializeField] private bool showPopup = true;

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
    }

    public void Interact()
    {
        if (consumed) return;

        // Don't allow collection without persistence.
        if (Pickup_Manager.Instance == null || pickupData == null ||
            string.IsNullOrWhiteSpace(pickupData.PickupID))
        {
            Debug.LogWarning(
                "[UpgradePointPickup] Missing Pickup_Manager or valid Pickup_Data.", this);
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

    // =========================
    // REWARD
    // =========================

    private bool GivePointsToPlayer()
    {
        PlayerStatsManager playerStats = FindFirstObjectByType<PlayerStatsManager>();

        CharacterStatsManager characterStats = CharacterStatsManager.Instance;

        if (playerStats == null || characterStats == null)
        {
            Debug.LogError(
                "[UpgradePointPickup] PlayerStatsManager or CharacterStatsManager not found.",
                this
            );

            return false;
        }

        playerStats.AddUpgradePoints(upgradePointsReward);
        characterStats.AddStatPoints(upgradePointsReward);

        Debug.Log($"+{upgradePointsReward} Upgrade Points obtained.");

        return true;
    }

    // =========================
    // PERSISTENCE
    // =========================

    private bool WasPreviouslyCollected()
    {
        return Pickup_Manager.Instance != null && Pickup_Manager.Instance.IsCollected(pickupData);
    }

    // =========================
    // FEEDBACK
    // =========================

    private void PlayPickupSound()
    {
        if (pickupSound == null || Audio_Manager.Instance == null) return;

        Audio_Manager.Instance.PlaySFX(pickupSound, sfxVolume);
    }

    private void SpawnVfx()
    {
        if (pickupVfx == null) return;

        GameObject vfx = Instantiate(pickupVfx, transform.position,Quaternion.identity);

        Destroy(vfx, 3f);
    }

    private void ShowPopupFeedback()
    {
        if (!showPopup || PopupUI.Instance == null) return;

        PopupUI.Instance.Show($"+{upgradePointsReward} Puntos de Mejora");
    }
}