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
    [Header("Recompensa")]
    [SerializeField] private int upgradePointsReward = 1;

    [Header("Texto de interacción")]
    [SerializeField] private string interactionText = "Recoger Punto de Mejora";

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float sfxVolume = 1f;

    [Header("Feedback opcional")]
    [Tooltip("VFX que se instancia en la posición del pickup al recogerlo (opcional).")]
    [SerializeField] private GameObject pickupVfx;
    [Tooltip("Popup de texto tipo '+X Puntos' (opcional, requiere PopupUI).")]
    [SerializeField] private bool showPopup = true;

    private bool consumed = false;

    public void Interact()
    {
        if (consumed) return;
        consumed = true;

        // Ocultar el prompt de interacción inmediatamente
        InteractionUI.Instance?.SetInteractable(null);

        GivePointsToPlayer();
        PlayPickupSound();
        SpawnVfx();
        ShowPopupFeedback();

        // Desaparece: de un solo uso
        Destroy(gameObject);
    }

    public string GetInteractionText()
    {
        return consumed ? null : interactionText;
    }

    private void GivePointsToPlayer()
    {
        PlayerStatsManager playerStats = FindFirstObjectByType<PlayerStatsManager>();

        if (playerStats == null)
        {
            Debug.LogError("❌ UpgradePointPickup: PlayerStatsManager no encontrado, no se pudieron otorgar puntos");
            return;
        }

        playerStats.AddUpgradePoints(upgradePointsReward);
        Debug.Log($"✅ +{upgradePointsReward} Puntos de Mejora otorgados");
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null || Audio_Manager.Instance == null) return;
        Audio_Manager.Instance.PlaySFX(pickupSound, sfxVolume);
    }

    private void SpawnVfx()
    {
        if (pickupVfx == null) return;
        GameObject vfx = Instantiate(pickupVfx, transform.position, Quaternion.identity);
        Destroy(vfx, 3f);
    }

    private void ShowPopupFeedback()
    {
        if (!showPopup || PopupUI.Instance == null) return;
        PopupUI.Instance.Show($"+{upgradePointsReward} Puntos de Mejora");
    }
}