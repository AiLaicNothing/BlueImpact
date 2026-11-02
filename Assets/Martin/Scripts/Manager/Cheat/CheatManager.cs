using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheatManager : MonoBehaviour
{
    [SerializeField] private Transform LevelTuto;
    [SerializeField] private Transform Level1;
    [SerializeField] private Transform Level2;
    [SerializeField] private Transform Level3;
    [SerializeField] private Transform Level4;

    private GameObject player;
    private bool isTeleporting;

    public static CheatManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isTeleporting) return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.pKey.isPressed) return;

        if (kb.digit1Key.wasPressedThisFrame)
        {
            TeleportTo(LevelTuto);
        }
        else if (kb.digit2Key.wasPressedThisFrame)
        {
            TeleportTo(Level1);
        }
        else if (kb.digit3Key.wasPressedThisFrame)
        {
            TeleportTo(Level2);
        }
        else if (kb.digit4Key.wasPressedThisFrame)
        {
            StartCoroutine(TeleportToLevel("Zone_03", "", Level3));
        }
        else if (kb.digit5Key.wasPressedThisFrame)
        {
            // Antes había un TeleportTo(Level4) aquí que movía al jugador antes de cargar la escena.
            StartCoroutine(TeleportToLevel("Testing4", "Zone_03", Level4));
        }
    }

    private IEnumerator TeleportToLevel(string sceneToLoad, string sceneToUnload, Transform teleportPoint)
    {
        if (player == null || SceneLoaderManager.Instance == null)
        {
            Debug.LogWarning("CheatManager: Player o SceneLoaderManager no disponibles.");
            yield break;
        }

        isTeleporting = true;
        try
        {
            Debug.Log("Teleporting to: " + sceneToLoad);

            // 1. Cargar la escena destino y esperar
            yield return StartCoroutine(SceneLoaderManager.Instance.LoadSceneAndWait(sceneToLoad));

            // 2. Teletransportar
            TeleportTo(teleportPoint);

            // 3. Descargar la escena anterior (si se indicó)
            if (!string.IsNullOrEmpty(sceneToUnload))
            {
                yield return StartCoroutine(SceneLoaderManager.Instance.UnloadSceneAndWait(sceneToUnload));
            }

            Debug.Log("Teleport complete.");
        }
        finally
        {
            // Se libera aunque la carga falle o se detenga la corrutina
            isTeleporting = false;
        }
    }

    private void TeleportTo(Transform desiredPos)
    {
        if (player == null || desiredPos == null) return;

        PlayerControl controller = player.GetComponent<PlayerControl>();
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (controller != null) controller.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = desiredPos.position;
        }
        else
        {
            player.transform.position = desiredPos.position;
        }

        if (controller != null) controller.enabled = true;
    }

    public void SetPlayerTarget(GameObject player)
    {
        this.player = player;
    }
}
