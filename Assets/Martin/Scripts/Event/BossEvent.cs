using System.Collections;
using UnityEngine;

public class BossEvent : MonoBehaviour
{
    [Header("Boss Related")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform spawnPos;
    [SerializeField] private Transform desiredGroundPos;
    [SerializeField] private float fallSpeed = 6f;
    [SerializeField] private GameObject smokeVfx;
    private GameObject bossObject;
    private Boss_MedusaMorgana boss;

    [SerializeField] private Transform center;
    [SerializeField] private Transform[] corners;
    [SerializeField] private EnemySpawner bossSummonSpawner;

    [Header("Boss HUD")]
    [SerializeField] private BossHealthHUD bossHud;

    [Header("Ritmo de la intro (segundos)")]
    [SerializeField] private float pauseAfterDoor = 0.4f;
    [SerializeField] private float barrierWait = 1f;
    [SerializeField] private float pauseAfterLanding = 0.5f;
    [SerializeField] private float pauseBeforeCombat = 0.8f;

    [Header("Cinematica de fase 2")]
    [SerializeField] private CameraRequest phaseTwoCamera;
    [SerializeField] private float pauseBeforePhaseTwo = 0.5f;
    [SerializeField] private float pauseAfterPhaseTwo = 0.6f;

    [Header("Door Event")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform openPos;
    [SerializeField] private Transform closePos;

    [SerializeField] private GameObject barrier;

    [Header("End Event")]
    [SerializeField] private GameObject secretUI;
    [SerializeField] private GameObject continueUI;

    [Header("Cameras")]
    [SerializeField] private CameraRequest doorCamera;
    [SerializeField] private CameraRequest barrierCamera;
    [SerializeField] private CameraRequest bossCamera;
    [SerializeField] private CameraRequest RewardCamera;

    private bool eventActive;
    private bool hasStarted;
    private bool eventComplete;

    private GameObject player;
    private PlayerControl playerControl;

    private void OnEnable()
    {
        PlayerControl.OnPlayerDied += ResetEvent;
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerControl.OnPlayerDied -= ResetEvent;
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    // Se ejecuta cuando el jugador es instanciado (despues de elegir personaje)
    private void OnPlayerSpawned(PlayerControl control)
    {
        playerControl = control;
    }

    private void StartBossEvent()
    {
        StartCoroutine(BossSequence());
    }

    // Secuencia completa, en el orden en que la ve el jugador
    private IEnumerator BossSequence()
    {
        hasStarted = true;

        // 1. Se bloquea al jugador. SetMode(Cutscene) ya llama a LockPlayerControl().
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
        }

        // 2. La puerta se cierra
        yield return MoveDoor(closePos.position);
        yield return new WaitForSeconds(pauseAfterDoor);

        // 3. Aparece la barrera
        yield return DoBarrier(true);

        // 4. Entrada del boss: aparece, grita y se muestran sus barras
        yield return SpawnBoss();

        if (boss == null)
        {
            Debug.LogError("[BossEvent] El prefab del boss no tiene el componente Boss_MedusaMorgana");
            ResetEvent();
            yield break;
        }

        // 5. El jugador recupera el control y el boss empieza a atacar tras su delay inicial
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
        }

        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        boss.SetCinematic(false);

        Debug.Log("[BossEvent] Inicia el combate");

        // 6. Combate: se espera a que el boss sea destruido
        while (bossObject != null)
        {
            yield return null;
        }

        yield return OnDeathEvent();
    }

    private IEnumerator SpawnBoss()
    {
        if (bossPrefab == null) yield break;

        if (bossCamera != null)
        {
            CameraEventRelay.Instance.Play(bossCamera);
        }

        bossObject = Instantiate(bossPrefab, spawnPos.position, spawnPos.rotation);
        boss = bossObject.GetComponentInChildren<Boss_MedusaMorgana>();

        if (boss == null) yield break;

        boss.SetCinematic(true);
        boss.SetArena(center, corners, bossSummonSpawner);
        boss.OnSpawn(null, center, false, spawnPos);
        boss.OnPhaseTwoStarted += OnBossPhaseTwo;

        // Entrada: baja desde el punto de spawn hasta el suelo
        if (desiredGroundPos != null && fallSpeed > 0f)
        {
            while (Vector3.Distance(bossObject.transform.position, desiredGroundPos.position) > 0.01f)
            {
                bossObject.transform.position = Vector3.MoveTowards(
                    bossObject.transform.position,
                    desiredGroundPos.position,
                    fallSpeed * Time.deltaTime);

                yield return null;
            }

            bossObject.transform.position = desiredGroundPos.position;
        }

        if (smokeVfx != null)
        {
            Vector3 vfxPos = desiredGroundPos != null ? desiredGroundPos.position : bossObject.transform.position;
            GameObject vfx = Instantiate(smokeVfx, vfxPos, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        yield return new WaitForSeconds(pauseAfterLanding);

        // Mira al jugador, grita y mientras tanto aparecen sus barras (se llenan solas)
        boss.FacePlayerInstant();

        if (bossHud != null)
        {
            bossHud.Bind(boss);
            bossHud.Show();
        }

        yield return boss.PlayIntro();

        yield return new WaitForSeconds(pauseBeforeCombat);
    }

    // El boss avisa cuando su vida baja del umbral de la fase 2
    private void OnBossPhaseTwo()
    {
        StartCoroutine(PhaseTwoSequence());
    }

    // Interrumpe el juego, mueve la camara al boss y lo deja hacer su cambio de fase
    private IEnumerator PhaseTwoSequence()
    {
        if (boss == null) yield break;

        Debug.Log("[BossEvent] Cinematica de fase 2");

        // 1. Se bloquea al jugador. SetMode(Cutscene) ya llama a LockPlayerControl().
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
        }

        // 2. La camara pasa al boss
        if (phaseTwoCamera != null)
        {
            CameraEventRelay.Instance.Play(phaseTwoCamera);
        }

        yield return new WaitForSeconds(pauseBeforePhaseTwo);

        // 3. Grito, VFX, cambio de modelo y entrada de la fase 2
        if (boss == null) yield break;

        yield return boss.PlayPhaseTwoTransition();

        // Si el boss murio durante la cinematica, el evento final toma el control
        if (boss == null) yield break;

        yield return new WaitForSeconds(pauseAfterPhaseTwo);

        // 4. El jugador recupera el control y el boss vuelve a atacar tras su delay inicial
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
        }

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (boss != null)
        {
            boss.SetCinematic(false);
        }
    }

    private IEnumerator MoveDoor(Vector3 desiredPos)
    {
        if (doorCamera != null)
        {
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
            CameraEventRelay.Instance.Play(barrierCamera);
        }

        if (barrier != null)
        {
            barrier.SetActive(isActive);
        }

        yield return new WaitForSeconds(barrierWait);
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

        if (bossHud != null)
        {
            bossHud.HideImmediate();
        }

        if (bossObject != null)
        {
            Destroy(bossObject);
        }

        boss = null;

        if (doorTransform != null && openPos != null)
        {
            doorTransform.position = openPos.position;
        }

        if (barrier != null)
        {
            barrier.SetActive(false);
        }

        hasStarted = false;
        eventActive = false;
    }

    private IEnumerator OnDeathEvent()
    {
        // A partir de aqui ya no se debe poder resetear el evento
        eventComplete = true;

        var ending = Ending_Cinematic.Instance;

        if (bossHud != null)
        {
            bossHud.Hide();
        }

        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[BossEvent] Jugador pausado para el evento de muerte");
        }

        Debug.Log("[BossEvent] Mostrando cinematica final");
        ending.ShowCutscene();

        yield return new WaitForSeconds(8.5f);

        ending.activeVideo(false);
        ending.ContinueVideo(true);

        yield return new WaitForSeconds(6f);

        Debug.Log("[BossEvent] Volviendo al menu");
        SceneLoaderManager.Instance.LoadSceneNormal("menu");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (eventActive) return;

        if (!other.gameObject.CompareTag("Player")) return;

        player = other.gameObject;

        PlayerControl control = player.GetComponent<PlayerControl>();

        if (control == null)
        {
            Debug.LogError("Boss event didnt get [PlayerControl] component");
            return;
        }

        playerControl = control;
        eventActive = true;

        StartBossEvent();
    }
}