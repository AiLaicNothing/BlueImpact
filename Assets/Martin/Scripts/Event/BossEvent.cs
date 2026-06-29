using System.Collections;
using TMPro;
using UnityEngine;

public class BossEvent : MonoBehaviour
{
    [Header("Boss Related")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform spawnPos;
    [SerializeField] private Transform desiredGroundPos;
    [SerializeField] private float fallSpeed;
    [SerializeField] private GameObject smokeVfx;

    [SerializeField] private Transform center;
    [SerializeField] private Transform[] corners;

    [Header("Door Event")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform openPos;
    [SerializeField] private Transform closePos;

    private bool eventActive = false;

    private PlayerControl player;
    private void Awake()
    {
        
    }

    private void StartBossEvent()
    {
        StartCoroutine(bossEvent());
    }

    private IEnumerator bossEvent()
    {
        Debug.Log("Block inputs");
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        Debug.Log("Close door");
        yield return MoveDoor(closePos.position);

        Debug.Log("Try spawn boss");
        yield return SpawnBoss();

        GameModeManager.Instance.SetMode(GameMode.Gameplay);
    }

    private IEnumerator SpawnBoss()
    {
        if (bossPrefab == null) yield break;

        var prefab = Instantiate(bossPrefab, spawnPos.position, Quaternion.identity);

        var boss = bossPrefab.GetComponent<Boss_ChimeraGolem>();

        if (boss != null)
        {
            boss.SetCinematic(true);
            Debug.Log($"{boss.inCinematic}");
            boss.GetPositions(center, corners);
            boss.OnSpawn(null, center, false);
        }

        //Call camara event to show boss

        while (Vector3.Distance(prefab.transform.position, desiredGroundPos.position) > 0.01f)
        {
            prefab.transform.position = Vector3.MoveTowards(prefab.transform.position, desiredGroundPos.position, fallSpeed * Time.deltaTime);
            yield return null;
        }

        Debug.Log("Desires pos arrived");

        if (smokeVfx != null)
        {
            var vfx = Instantiate(smokeVfx, desiredGroundPos.position, Quaternion.identity);
            Debug.Log("Get vfx");
            Destroy(vfx, 1.5f);
        }

        yield return new WaitForSeconds(2.5f);

        boss.SetCinematic(false);
        Debug.Log($"{boss.inCinematic}");

        Debug.Log("Start combat");
        // End camera event
    }

    private void OpenDoor()
    {
        StartCoroutine(MoveDoor(openPos.position));
    }
    private void CloseDoor()
    {
        StartCoroutine(MoveDoor(closePos.position));
    }

    private IEnumerator MoveDoor(Vector3 desiredPos)
    {
        if (doorTransform == null) yield break;

        while (Vector3.Distance(doorTransform.position, desiredPos) > 0.01f)
        {
            doorTransform.position = Vector3.MoveTowards(doorTransform.position, desiredPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        doorTransform.position = desiredPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (eventActive) return;

        if (other.gameObject.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerControl>();

            if (player != null)
            {
                eventActive = true;

                StartBossEvent();
            }
            else
            {
                Debug.LogError("Boss event didnt get [PlayerControl] component");
            }
        }
    }
}
