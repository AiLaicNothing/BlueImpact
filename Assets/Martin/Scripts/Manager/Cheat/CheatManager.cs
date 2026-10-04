using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        if (Input.GetKey(KeyCode.P))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TeleportTo(LevelTuto);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TeleportTo(Level1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TeleportTo(Level2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                StartCoroutine(TeleportToLevel("Zone_03", "", Level3));
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                StartCoroutine(TeleportToLevel("Testing4", "Zone_03", Level4));

                TeleportTo(Level4);
            }
        }
    }

    private IEnumerator TeleportToLevel(string SceneToLoad,string SceneToUnload, Transform teleportPOint)
    {
        if (player == null)
        {
            Debug.LogWarning("CheatManager: Player not assigned.");
            yield break;
        }

        isTeleporting = true;

        Debug.Log("Teleporting to: " + SceneToLoad);

        // --------------------------------------------------
        // 1. Load destination scene and WAIT
        // --------------------------------------------------

        yield return StartCoroutine( SceneLoaderManager.Instance.LoadSceneAndWait(SceneToLoad));

        // --------------------------------------------------
        // 3. Force teleport
        // --------------------------------------------------

        TeleportTo(teleportPOint);

        // --------------------------------------------------
        // 4. Unload previous scenes
        // --------------------------------------------------

        yield return StartCoroutine(UnloadOtherLevelScenes(SceneToUnload));

        isTeleporting = false;

        Debug.Log("Teleport complete.");
    }

    private IEnumerator UnloadOtherLevelScenes(string destinationScene)
    {
        yield return StartCoroutine(SceneLoaderManager.Instance.UnloadSceneAndWait(destinationScene));
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

        if (controller != null)controller.enabled = true;
    }


    private void UnlockSkills()
    {

    }

    private void GetPoints()
    {

    }

    public void SetPlayerTarget(GameObject player)
    {
        this.player = player;
    }

}
