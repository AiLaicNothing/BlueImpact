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
    private GameObject bossObject;

    [SerializeField] private Transform center;
    [SerializeField] private Transform[] corners;

    [Header("Door Event")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform openPos;
    [SerializeField] private Transform closePos;

    [Header("End Event")]
    [SerializeField] private GameObject secret;

    [Header("Cameras")]
    [SerializeField] private CameraRequest bossCamera;
    [SerializeField] private CameraRequest RewardCamera;

    private bool eventActive = false;

    private GameObject player;
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

        //var boss = bossObject.GetComponent<Boss_ChimeraGolem>();

        //if (boss.IsDead)
        //{
        //    Debug.Log("Try dead boss event");
        //    yield return OnDeathEvent();
        //}
        //Debug.Log("Try end event");

    }

    private IEnumerator SpawnBoss()
    {

        if (bossCamera != null)
        {
            CameraEventRelay.Instance.Play(bossCamera);
        }

        if (bossPrefab == null) yield break;

        bossObject = Instantiate(bossPrefab, spawnPos.position, spawnPos.rotation);

        var boss = bossObject.GetComponent<Boss_ChimeraGolem>();

        if (boss != null)
        {
            boss.SetCinematic(true);
            Debug.Log($"{boss.inCinematic}");
            boss.GetPositions(center, corners);
            boss.OnSpawn(null, center, false, spawnPos);
            Debug.Log($"{player.name}");
            boss.target = player;
        }

        //Call camara event to show boss

        var anim = bossObject.GetComponentInChildren<Animator>();

        anim.Play("Caida");

        while (Vector3.Distance(bossObject.transform.position, desiredGroundPos.position) > 0.01f)
        {
            bossObject.transform.position = Vector3.MoveTowards(bossObject.transform.position, desiredGroundPos.position, fallSpeed * Time.deltaTime);
            yield return null;
        }

        Debug.Log("Desires pos arrived");
        anim.Play("Aterrizaje");

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

    private IEnumerator OnDeathEvent()
    {
        yield return new WaitForSeconds(1.5f);

        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        secret.SetActive(true);

        GameModeManager.Instance.SetMode(GameMode.Gameplay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (eventActive) return;

        if (other.gameObject.CompareTag("Player"))
        {
            player = other.gameObject;

            var playerControl = player.GetComponent<PlayerControl>();

            if (playerControl != null)
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
