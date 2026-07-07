using System.Collections;
using UnityEngine;

public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance
    {
        get;
        private set;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void Teleport(Checkpoint destination)
    {
        StartCoroutine(TeleportRoutine(destination));
    }

    private IEnumerator TeleportRoutine(Checkpoint destination)
    {
        // ✅ BUSCAR EL PLAYER DINÁMICAMENTE (por tag)
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("❌ Player no encontrado");
            yield break;
        }

        // ✅ TELETRANSPORTAR DIRECTAMENTE (sin fade)
        player.SetPositionAndRotation(
            destination.SpawnPoint.position,
            destination.SpawnPoint.rotation);

        yield return new WaitForSecondsRealtime(0.1f);

        // ✅ CERRAR MENÚ
        CheckpointMenuUI.Instance?.CloseMenu();
    }
}