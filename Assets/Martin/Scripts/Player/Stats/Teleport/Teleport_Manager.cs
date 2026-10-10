using System.Collections;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleport_Manager : MonoBehaviour
{
    public static Teleport_Manager Instance { get; private set; }

    private bool isTeleporting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Teleport(DiscoveredCheckpoint destination)
    {
        if (isTeleporting)
        {
            Debug.LogWarning("[Teleport] A teleport is already in progress.");
            return;
        }

        if (destination == null)
        {
            Debug.LogError("[Teleport] Destination is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(destination._sceneName) ||
            string.IsNullOrWhiteSpace(destination._checkpointID))
        {
            Debug.LogError("[Teleport] Destination scene name or checkpoint ID is empty.");
            return;
        }

        StartCoroutine(TeleportRoutine(destination));
    }

    private IEnumerator TeleportRoutine(DiscoveredCheckpoint destination)
    {
        isTeleporting = true;

        try
        {
            if (SceneLoaderManager.Instance == null)
            {
                Debug.LogError("[Teleport] SceneLoaderManager.Instance is missing.");
                yield break;
            }

            Debug.Log($"[Teleport] Loading scene '{destination._sceneName}'.");

            yield return SceneLoaderManager.Instance.LoadSceneAndWait(destination._sceneName);

            var scene = SceneManager.GetSceneByName(destination._sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError(
                    $"[Teleport] Scene '{destination._sceneName}' was not loaded.");
                yield break;
            }

            if (Checkpoints_Manager.Instance == null)
            {
                Debug.LogError("[Teleport] Checkpoints_Manager.Instance is missing.");
                yield break;
            }

            Checkpoint checkpoint =
                Checkpoints_Manager.Instance.FindCheckpointInScene(
                    destination._sceneName,
                    destination._checkpointID);

            if (checkpoint == null)
            {
                Debug.LogError(
                    $"[Teleport] Checkpoint '{destination._checkpointID}' " +
                    $"was not found in '{destination._sceneName}'.");
                yield break;
            }

            if (checkpoint.SpawnPoint == null)
            {
                Debug.LogError(
                    $"[Teleport] Checkpoint '{destination._checkpointID}' " +
                    "has no SpawnPoint assigned.");
                yield break;
            }

            if (Player_Manager.Instance == null)
            {
                Debug.LogError("[Teleport] Player_Manager.Instance is missing.");
                yield break;
            }

            GameObject player = Player_Manager.Instance.GetPlayer();

            if (player == null)
            {
                Debug.LogError("[Teleport] Player_Manager.GetPlayer() returned null.");
                yield break;
            }

            Vector3 targetPosition = checkpoint.SpawnPoint.position;
            Quaternion targetRotation = checkpoint.SpawnPoint.rotation;

            PlayerControl playerControl = player.GetComponent<PlayerControl>();
            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (playerControl != null)
                playerControl.enabled = false;

            // Teleport the player.
            player.transform.SetPositionAndRotation(targetPosition, targetRotation);

            // Reset Rigidbody physics, if present.
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Clear the movement lock shown in your PlayerControl usage.
            if (playerControl != null)
            {
                playerControl.blockVelocity = false;
                playerControl.enabled = true;
                playerControl.UnlockPlayerControl();
            }

            Physics.SyncTransforms();

            Debug.Log(
                $"[Teleport] PlayerControl enabled: " +
                $"{(playerControl != null && playerControl.enabled)}, " +
                $"Time scale: {Time.timeScale}");

            Checkpoints_Manager.Instance.SetActiveCheckpoint(checkpoint);

            Debug.Log(
                $"[Teleport] Successfully teleported to checkpoint " +
                $"'{destination._checkpointID}' in '{destination._sceneName}'.");

            // Close the menu only after teleportation succeeds.
            yield return null;

            if (Checkpoint_Menu.Instance != null)
                Checkpoint_Menu.Instance.Close();
            else
                Debug.LogWarning("[Teleport] Checkpoint_Menu.Instance is missing.");
        }
        finally
        {
            isTeleporting = false;
        }
    }
}
