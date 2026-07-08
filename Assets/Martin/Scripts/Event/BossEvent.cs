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

    [SerializeField] private GameObject barrier;

    [Header("End Event")]
    [SerializeField] private GameObject secret;

    [Header("Cameras")]
    [SerializeField] private CameraRequest doorCamera;
    [SerializeField] private CameraRequest barrierCamera;
    [SerializeField] private CameraRequest bossCamera;
    [SerializeField] private CameraRequest RewardCamera;

    private bool eventActive = false;
    private bool hasStarted;
    private bool eventComplete;

    private GameObject player;
    private PlayerControl playerControl;

    private void OnEnable()
    {
        PlayerControl.OnPlayerDied += ResetEvent;
    }

    private void OnDisable()
    {
        PlayerControl.OnPlayerDied -= ResetEvent;
    }

    private void Awake()
    {
        // Suscribirse a OnPlayerSpawned para obtener referencia al jugador
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
        }
    }

    /// <summary>
    /// Se ejecuta cuando el jugador es instanciado (después de seleccionar personaje)
    /// </summary>
    private void OnPlayerSpawned(PlayerControl control)
    {
        playerControl = control;
    }

    private void StartBossEvent()
    {
       StartCoroutine(bossEvent());
    }

    private IEnumerator bossEvent()
    {
        hasStarted = true;

        Debug.Log("Block inputs");
        GameModeManager.Instance.SetMode(GameMode.Cutscene);
        // 📌 NOTA: SetMode(GameMode.Cutscene) llama automáticamente a LockPlayerControl()
        // NO hacemos doble llamada para evitar sobrescribir previousConstraints

        // 🔇 Silenciar audio (NO está vinculado a GameMode)
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[BossEvent] 🔒 Jugador pausado y silenciado (GameMode + Audio)");
        }

        Debug.Log("Close door");
        yield return MoveDoor(closePos.position);

        //Debug.Log("Create Barrier");
        yield return DoBarrier(true);

        Debug.Log("Try spawn boss");
        yield return SpawnBoss();

        // 🔓 Reactivar jugador
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[BossEvent] 🔓 Audio del jugador reactivado");
        }

        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        // 📌 NOTA: SetMode(GameMode.Gameplay) llama automáticamente a UnlockPlayerControl()

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

    private IEnumerator MoveDoor(Vector3 desiredPos)
    {
        if (doorCamera != null)
        {
            // 📌 Asegurar que doorCamera tenga pausePlayer=true y mutePlayerAudio=true en inspector
            CameraEventRelay.Instance.Play(doorCamera);
        }

        if (doorTransform == null) yield break;

        while (Vector3.Distance(doorTransform.position, desiredPos) > 0.01f)
        {
            doorTransform.position = Vector3.MoveTowards(doorTransform.position, desiredPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        doorTransform.position = desiredPos;
    }

    private IEnumerator DoBarrier(bool isActive)
    {
        if (barrierCamera != null)
        {
            // 📌 Asegurar que barrierCamera tenga pausePlayer=true y mutePlayerAudio=true en inspector
            CameraEventRelay.Instance.Play(barrierCamera);
        }

        if (barrier != null)
        {
            barrier.SetActive(isActive);
        }

        yield return new WaitForSeconds(1f);
    }

    public void ResetEvent()
    {
        if (!hasStarted || eventComplete) return;

        StopAllCoroutines();

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
        }

        if (bossObject != null)
        {
            Destroy(bossObject);
        }

        doorTransform.position = openPos.position;

        barrier.SetActive(false);

        hasStarted = false;
        eventActive = false;
    }

    private IEnumerator OnDeathEvent()
    {
        // 🔒 Pausar jugador para evento de muerte
        GameModeManager.Instance.SetMode(GameMode.Cutscene);
        // 📌 SetMode(Cutscene) llama automáticamente a LockPlayerControl()

        // 🔇 Silenciar audio
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[BossEvent] 🔒 Jugador pausado para evento de muerte");
        }

        yield return new WaitForSeconds(1.5f);

        secret.SetActive(true);

        // 🔓 Reactivar jugador después del evento
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[BossEvent] 🔓 Audio reactivado después de evento de muerte");
        }

        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        // 📌 SetMode(Gameplay) llama automáticamente a UnlockPlayerControl()
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