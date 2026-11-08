using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleport_Manager : MonoBehaviour
{
    public static Teleport_Manager Instance { get; private set; }

    private bool isTeleporting;
    public bool IsTeleporting => isTeleporting;

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

    // Existing gameplay usage is unchanged:
    // Teleport_Manager.Instance.Teleport(discoveredCheckpoint);
    public void Teleport(DiscoveredCheckpoint destination, bool isCheat = false)
    {
        if (destination == null)
        {
            Debug.LogError("[Teleport] Destination is null.");
            return;
        }

        BeginTeleport(destination._sceneName, destination._checkpointID, isCheat);
    }

    // Debug usage: pass the CheckpointData ScriptableObject directly.
    // No additional constructor is needed in DiscoveredCheckpoint.
    public void Teleport(CheckpointData destination, bool isCheat = true)
    {
        if (destination == null)
        {
            Debug.LogError("[Teleport] CheckpointData is null.");
            return;
        }

        BeginTeleport(destination.locationSceneName, destination.checkpointID, isCheat);
    }

    private void BeginTeleport(string sceneName, string checkpointID, bool isCheat)
    {
        if (isTeleporting)
        {
            Debug.LogWarning("[Teleport] A teleport is already in progress.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName) ||
            string.IsNullOrWhiteSpace(checkpointID))
        {
            Debug.LogError("[Teleport] Destination scene name or checkpoint ID is empty.");
            return;
        }

        StartCoroutine(TeleportRoutine(sceneName, checkpointID, isCheat));
    }

    private IEnumerator TeleportRoutine(string sceneName, string checkpointID, bool isCheat)
    {
        isTeleporting = true;

        try
        {
            if (SceneLoaderManager.Instance == null)
            {
                Debug.LogError("[Teleport] SceneLoaderManager.Instance is missing.");
                yield break;
            }

            Debug.Log($"[Teleport] Loading scene '{sceneName}'.");
            yield return SceneLoaderManager.Instance.LoadSceneAndWait(sceneName);

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"[Teleport] Scene '{sceneName}' was not loaded.");
                yield break;
            }

            if (Checkpoints_Manager.Instance == null)
            {
                Debug.LogError("[Teleport] Checkpoints_Manager.Instance is missing.");
                yield break;
            }

            Checkpoint checkpoint = Checkpoints_Manager.Instance.FindCheckpointInScene(
                sceneName, checkpointID);

            if (checkpoint == null)
            {
                Debug.LogError($"[Teleport] Checkpoint '{checkpointID}' was not found in '{sceneName}'.");
                yield break;
            }

            if (checkpoint.SpawnPoint == null)
            {
                Debug.LogError($"[Teleport] Checkpoint '{checkpointID}' has no SpawnPoint assigned.");
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

            player.transform.SetPositionAndRotation(targetPosition, targetRotation);

            // Only dynamic rigidbodies allow direct velocity changes.
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Physics.SyncTransforms();

            if (playerControl != null)
            {
                playerControl.blockVelocity = false;
                playerControl.enabled = true;
                playerControl.UnlockPlayerControl();
            }

            // Normal checkpoint travel updates the saved checkpoint/respawn.
            // Debug travel leaves it unchanged.
            if (!isCheat)
                Checkpoints_Manager.Instance.SetActiveCheckpoint(checkpoint);

            Debug.Log($"[Teleport] Teleported to '{checkpointID}' in '{sceneName}' (cheat: {isCheat}).");

            yield return null;

            if (!isCheat)
            {
                if (Checkpoint_Menu.Instance != null)
                    Checkpoint_Menu.Instance.Close();
                else
                    Debug.LogWarning("[Teleport] Checkpoint_Menu.Instance is missing.");
            }
        }
        finally
        {
            isTeleporting = false;
        }
    }
}
