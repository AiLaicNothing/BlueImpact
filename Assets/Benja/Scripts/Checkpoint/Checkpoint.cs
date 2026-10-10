using UnityEngine;

public class Checkpoint : MonoBehaviour, IInteractable
{
    [SerializeField]
    private CheckpointData checkpointData;

    [SerializeField]
    private Transform spawnPoint;

    public CheckpointData Data => checkpointData;

    public Transform SpawnPoint => spawnPoint;

    #region Interface Interactable functions

    public void Interact()
    {
        InteractionUI.Instance.SetInteractable(null); // 👈 ocultas UI

        //CheckpointManager.Instance.Interact(this);

        Interaction();
    }

    public string GetInteractionText()
    {
        if (checkpointData.checkpointName != null)
        {
            return checkpointData.checkpointName;
        }
        else
        {
            return "Missing name in data";
        }
    }

    #endregion

    private void Interaction()
    {
        Checkpoints_Manager.Instance.SetActiveCheckpoint(this);

        bool firstDiscovery = Checkpoints_Manager.Instance.DiscoverCheckpoint(this);

        if (firstDiscovery)
        {
            CharacterStatsManager.Instance.AddStatPoints(checkpointData.upgradePointsReward);

            if (PopupUI.Instance != null)
            {
                PopupUI.Instance.Show($"¡Checkpoint Descubierto!\n+{checkpointData.upgradePointsReward} Puntos de Mejora");
            }
            else
            {
                Debug.LogWarning("PopupUI.Instance es null - popup no mostrado");
            }
        }

        if (Checkpoint_Menu.Instance != null)
        {
            Checkpoint_Menu.Instance.Open(this);
        }
        else
        {
            Debug.LogError("[Checkpoint] Checkpoint_Menu Instance is missing");
        }
    }
}