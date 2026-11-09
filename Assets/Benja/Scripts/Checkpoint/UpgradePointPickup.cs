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
    [SerializeField] private PlayerControl currentPlayer;
    [Min(0.01f)] [SerializeField] private float durationDissolve = 1.5f;
    [SerializeField] private Material dieMaterial;
    [SerializeField] private Material dieMaterial2;
    [ColorUsage(true, true)]
    [SerializeField]
    private Color EdgeColor = new Color(0.1875f * 2.33059f, 0.45937f * 2.33059f, 0.75f * 2.33059f, 1f);
    [ColorUsage(true, true)]
    [SerializeField]
    private Color EdgeColor2 = new Color(0.1875f * 2.33059f, 0.45937f * 2.33059f, 0.75f * 2.33059f, 1f);

    private readonly List<Material> clonMaterials = new List<Material>();

    private static readonly int DissolveProgressHash = Shader.PropertyToID("_DisolveProgress");
    private static readonly int EdgeColorHash = Shader.PropertyToID("_EdgeColor");
    private static readonly int MainTexHash = Shader.PropertyToID("_MainTex");
    private Renderer[] renderers;

    private bool consumed;

    private void Start()
    {
        // If already collected in a previous visit,
        // remove this pickup from the scene.
        renderers = model.transform.GetComponentsInChildren<Renderer>(true);
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

        TryFindPlayer();
        ChangeMaterials();
        StartCoroutine(DieEffect());
        StartCoroutine(FadeOutCoroutine());
        ShowPopupFeedback();

        Destroy(model, 1f);
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
        ChangeMaterials();
        StartCoroutine(DieEffect());
        StartCoroutine(FadeOutCoroutine());
    }
    private void ChangeMaterials()
    {
        if (dieMaterial == null) return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (renderer.gameObject.GetComponent<TrailRenderer>()) continue;
            if (renderer.gameObject.GetComponent<ParticleSystem>()) continue;
            Material[] original = renderer.sharedMaterials;
            Material[] dissolve = new Material[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                Material clone = new Material(dieMaterial);
                if(renderer.gameObject.name == "cristal")
                {
                    clone = new Material(dieMaterial2);
                }
                if (clone.HasProperty(DissolveProgressHash)) clone.SetFloat(DissolveProgressHash, 0f);

                if (clone.HasProperty(EdgeColorHash))
                {
                    clone.SetColor(EdgeColorHash, EdgeColor);

                    if (renderer.gameObject.name == "cristal")
                    {
                        clone.SetColor(EdgeColorHash, EdgeColor2);

                    }
                }

                if (original[i] != null && original[i].HasProperty(MainTexHash) &&
                    clone.HasProperty(MainTexHash))
                {
                    Texture texture = original[i].GetTexture(MainTexHash);
                    if (texture != null) clone.SetTexture(MainTexHash, texture);
                }

                dissolve[i] = clone;
                clonMaterials.Add(clone);
            }

            renderer.sharedMaterials = dissolve;
        }
    }

    private IEnumerator DieEffect()
    {
        float elapsed = 0f;

        while (elapsed < durationDissolve)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / durationDissolve);

            foreach (Material material in clonMaterials)
            {
                if (material != null && material.HasProperty(DissolveProgressHash))
                    material.SetFloat(DissolveProgressHash, progress);
            }

            yield return null;
        }

    }
    private IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;


        float duration = Mathf.Max(0.01f, fadeOutDuration);
        pickupVfx.enabled = true;
        pickupVfx.transform.position = new Vector3(pickupVfx.transform.position.x + 0.1f, pickupVfx.transform.position.y + 0.1f, pickupVfx.transform.position.z + 0.1f);


        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            pickupVfx.gameObject.transform.position = currentPlayer.Model.position;


            yield return null;
        }

    }
    private void ShowPopupFeedback()
    {
        if (!showPopup || PopupUI.Instance == null) return;

        PopupUI.Instance.Show($"+{upgradePointsReward} Puntos de Mejora");
    }
    private void OnDestroy()
    {
        foreach (Material material in clonMaterials)
        {
            if (material != null) Destroy(material);
        }
        clonMaterials.Clear();

    }
}