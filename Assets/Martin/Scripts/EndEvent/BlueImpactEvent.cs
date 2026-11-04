using System.Collections;
using UnityEngine;

// Secuencia final del juego, despues de derrotar a Morgana:
//
//   1. Cinematica: se rompen las cadenas que sostienen el cristal
//   2. El Blue Impact desciende al centro de la sala
//   3. Vuelve el gameplay y el jugador puede interactuar con el
//   4. Elige destruirlo o usarlo
//        Usarlo     -> Final 1 (Ambicion)
//        Destruirlo -> tiene el objeto secreto? Final 3 (Curacion) : Final 2 (Destruccion)
//   5. Cinematica del final elegido y vuelta al menu
//
// Lo dispara BossEvent cuando el boss muere.
public class BlueImpactEvent : MonoBehaviour
{
    [Header("Blue Impact")]
    [SerializeField] private BlueImpact blueImpact;
    [SerializeField] private Transform descendTarget;      // centro de la sala, a la altura final
    [SerializeField] private float descendSpeed = 1.2f;
    [SerializeField] private float descendTimeout = 20f;   // red de seguridad por si no llega

    [Header("Cadenas")]
    [SerializeField] private GameObject[] chains;          // se desactivan al romperse
    [SerializeField] private GameObject[] brokenChains;    // version rota, se activa (opcional)
    [SerializeField] private Animator chainAnimator;       // opcional
    [SerializeField] private string chainBreakTrigger = "Break";
    [SerializeField] private GameObject chainBreakVfx;     // PREFAB
    [SerializeField] private Transform chainVfxPoint;

    [Header("Ritmo (segundos)")]
    [SerializeField] private float pauseBeforeChains = 1.5f;
    [SerializeField] private float pauseAfterChains = 1f;
    [SerializeField] private float pauseAfterDescend = 1f;
    [SerializeField] private float pauseBeforeEnding = 0.5f;
    [SerializeField] private float continueScreenDelay = 6f;

    [Header("Camaras")]
    [SerializeField] private CameraRequest chainCamera;
    [SerializeField] private CameraRequest descendCamera;

    [Header("UI de eleccion")]
    [SerializeField] private EndingChoiceUI choiceUI;

    [Header("Final secreto")]
    // Si el jugador tiene este objeto y elige destruir el cristal, sale el final 3
    [SerializeField] private Item_Data secretItem;

    [Header("Escena final")]
    [SerializeField] private string menuSceneName = "menu";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chainBreakSfx;
    [SerializeField] private AudioClip descendLoopSfx;
    [SerializeField] private AudioClip destroySfx;
    [SerializeField] private AudioClip absorbSfx;

    private PlayerControl playerControl;
    private bool sequenceStarted;
    private bool endingStarted;

    private void OnEnable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerControl control)
    {
        playerControl = control;
    }

    private void Start()
    {
        // El cristal no se toca hasta que termine de bajar
        if (blueImpact != null)
        {
            blueImpact.Bind(this);
            blueImpact.SetInteractable(false);
        }
        else
        {
            Debug.LogWarning("[BlueImpact] Falta asignar el cristal, el evento final no va a funcionar", this);
        }

        SetChainsBroken(false);

        if (secretItem == null)
        {
            Debug.LogWarning("[BlueImpact] secretItem sin asignar: el final 3 nunca va a salir", this);
        }
    }

    // La llama BossEvent al morir el boss
    public void StartSequence(PlayerControl control)
    {
        if (sequenceStarted) return;

        sequenceStarted = true;

        if (control != null) playerControl = control;

        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        Debug.Log("[BlueImpact] Inicia el evento final");

        // 1. Se bloquea al jugador para la cinematica de las cadenas
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null) playerControl.MutePlayerAudio(true);

        if (chainCamera != null && CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(chainCamera);
        }

        yield return new WaitForSeconds(pauseBeforeChains);

        // 2. Se rompen las cadenas
        BreakChains();

        yield return new WaitForSeconds(pauseAfterChains);

        // 3. El cristal desciende al centro
        if (descendCamera != null && CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(descendCamera);
        }

        yield return DescendCrystal();

        yield return new WaitForSeconds(pauseAfterDescend);

        // 4. Vuelve el control y el cristal queda disponible
        if (playerControl != null) playerControl.MutePlayerAudio(false);

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (blueImpact != null) blueImpact.SetInteractable(true);

        Debug.Log("[BlueImpact] El jugador ya puede interactuar con el cristal");
    }

    // ---------- CADENAS ----------

    private void BreakChains()
    {
        Debug.Log("[BlueImpact] Se rompen las cadenas");

        if (chainAnimator != null && !string.IsNullOrEmpty(chainBreakTrigger))
        {
            chainAnimator.SetTrigger(chainBreakTrigger);
        }

        SetChainsBroken(true);

        if (chainBreakVfx != null)
        {
            Vector3 pos = chainVfxPoint != null ? chainVfxPoint.position : transform.position;
            GameObject vfx = Instantiate(chainBreakVfx, pos, Quaternion.identity);
            Destroy(vfx, 4f);
        }

        PlaySfx(chainBreakSfx);
    }

    private void SetChainsBroken(bool broken)
    {
        if (chains != null)
        {
            foreach (GameObject c in chains)
            {
                if (c != null) c.SetActive(!broken);
            }
        }

        if (brokenChains != null)
        {
            foreach (GameObject c in brokenChains)
            {
                if (c != null) c.SetActive(broken);
            }
        }
    }

    // ---------- DESCENSO ----------

    private IEnumerator DescendCrystal()
    {
        if (blueImpact == null || descendTarget == null)
        {
            Debug.LogWarning("[BlueImpact] Sin cristal o sin destino, se salta el descenso");
            yield break;
        }

        Transform crystal = blueImpact.transform;

        PlayLoop(descendLoopSfx);

        float timer = 0f;

        while (timer < descendTimeout &&
               Vector3.Distance(crystal.position, descendTarget.position) > 0.01f)
        {
            crystal.position = Vector3.MoveTowards(
                crystal.position,
                descendTarget.position,
                descendSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        crystal.position = descendTarget.position;

        StopLoop();
    }

    // ---------- ELECCION ----------

    // La llama BlueImpact cuando el jugador interactua
    public void OpenChoice()
    {
        if (endingStarted) return;

        Debug.Log("[BlueImpact] El jugador interactuo, se abre la eleccion");

        // Cutscene detiene al jugador y muestra el cursor (lo hace GameModeManager)
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (choiceUI == null)
        {
            Debug.LogError("[BlueImpact] Falta choiceUI: no hay como elegir, se va al final de destruccion", this);
            OnChoice(true);
            return;
        }

        choiceUI.Open(OnChoice);
    }

    // destroyed = true si eligio destruirlo, false si eligio usarlo
    private void OnChoice(bool destroyed)
    {
        if (endingStarted) return;

        endingStarted = true;

        EndingType ending = ResolveEnding(destroyed);

        PlaySfx(destroyed ? destroySfx : absorbSfx);

        StartCoroutine(PlayEnding(ending));
    }

    // Aqui se decide el final secreto
    private EndingType ResolveEnding(bool destroyed)
    {
        if (!destroyed)
        {
            Debug.Log("[BlueImpact] Eligio usarlo: Final 1 (Ambicion)");
            return EndingType.Ambicion;
        }

        if (HasSecretItem())
        {
            Debug.Log("[BlueImpact] Eligio destruirlo con el objeto secreto: Final 3 (Curacion)");
            return EndingType.Curacion;
        }

        Debug.Log("[BlueImpact] Eligio destruirlo sin el objeto: Final 2 (Destruccion)");
        return EndingType.Destruccion;
    }

    private bool HasSecretItem()
    {
        if (secretItem == null) return false;

        if (Inventory_Manager.Instance == null)
        {
            Debug.LogWarning("[BlueImpact] No hay Inventory_Manager en la escena");
            return false;
        }

        return Inventory_Manager.Instance.HasItem(secretItem);
    }

    // ---------- CINEMATICA FINAL ----------

    private IEnumerator PlayEnding(EndingType ending)
    {
        yield return new WaitForSecondsRealtime(pauseBeforeEnding);

        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null) playerControl.MutePlayerAudio(true);

        Ending_Cinematic cinematic = Ending_Cinematic.Instance;

        if (cinematic == null)
        {
            Debug.LogError("[BlueImpact] No hay Ending_Cinematic, se vuelve al menu directo");
            SceneLoaderManager.Instance.LoadSceneNormal(menuSceneName);
            yield break;
        }

        cinematic.ShowEnding(ending);

        // Se espera lo que dura el video, no un numero fijo
        float wait = cinematic.CurrentClipLength;
        if (wait <= 0f) wait = 8.5f;

        yield return new WaitForSecondsRealtime(wait);

        cinematic.activeVideo(false);
        cinematic.ContinueVideo(true);

        yield return new WaitForSecondsRealtime(continueScreenDelay);

        Debug.Log("[BlueImpact] Final terminado, volviendo al menu");

        SceneLoaderManager.Instance.LoadSceneNormal(menuSceneName);
    }

    // ---------- AUDIO ----------

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.PlayOneShot(clip);
    }

    private void PlayLoop(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void StopLoop()
    {
        if (audioSource == null || !audioSource.loop) return;

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = null;
    }

    // ---------- GIZMOS ----------

    private void OnDrawGizmosSelected()
    {
        if (descendTarget == null) return;

        Gizmos.color = new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawWireSphere(descendTarget.position, 0.6f);

        if (blueImpact != null)
        {
            Gizmos.DrawLine(blueImpact.transform.position, descendTarget.position);
        }
    }
}
