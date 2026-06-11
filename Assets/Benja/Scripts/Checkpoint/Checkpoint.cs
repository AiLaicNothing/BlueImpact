using UnityEngine;

public class Checkpoint : MonoBehaviour, IInteractable
{
    [SerializeField]
    private CheckpointData checkpointData;

    [SerializeField]
    private Transform spawnPoint;

    public CheckpointData Data => checkpointData;

    public Transform SpawnPoint => spawnPoint;

    public void Interact()
    {
        InteractionUI.Instance.SetInteractable(null); // 👈 ocultas UI

        CheckpointManager.Instance.Interact(this);
    }

    public string GetInteractionText()
    {
        return checkpointData.checkpointName;
    }
}