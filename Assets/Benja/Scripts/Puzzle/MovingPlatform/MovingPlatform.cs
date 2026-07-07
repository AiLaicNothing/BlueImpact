using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modo de recorrido de la plataforma una vez que llega al último punto de la ruta.
/// </summary>
public enum PlatformLoopMode
{
    PingPong,   // A -> B -> C -> D -> C -> B -> A -> ...
    Loop,       // A -> B -> C -> D -> A -> B -> C -> D -> ...
    Once        // A -> B -> C -> D  y se detiene ahí
}

/// <summary>
/// Plataforma móvil genérica y modular.
/// Arrastra Transforms vacíos (waypoints) en el Inspector, en el orden en que
/// quieres que la plataforma los visite. La lista es 100% escalable: podés
/// tener 2 puntos (A->B) o 10 (A->B->C->D->...), sin tocar código.
///
/// Siempre está en movimiento (no requiere trigger externo para empezar).
/// Transporta al player parenteandolo temporalmente mientras está encima,
/// para que el Rigidbody del player (que sobreescribe su propia velocity
/// cada FixedUpdate) se mueva junto con la plataforma sin pelear con la física.
/// </summary>
[DisallowMultipleComponent]
public class MovingPlatform : MonoBehaviour
{
    [Header("Ruta (escalable: agregá los puntos que quieras)")]
    [Tooltip("Orden de visita. Podés tener 2, 3, 4... los que necesites.")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Movimiento")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private PlatformLoopMode loopMode = PlatformLoopMode.PingPong;
    [Tooltip("Tiempo que espera parada en cada waypoint antes de seguir.")]
    [SerializeField] private float waitTimeAtPoint = 0f;
    [Tooltip("Si está activo, arranca moviéndose apenas empieza la escena.")]
    [SerializeField] private bool startMovingOnAwake = true;
    [Tooltip("Índice del waypoint inicial (posición de partida de la plataforma).")]
    [SerializeField] private int startWaypointIndex = 0;

    [Header("Transporte de Player")]
    [Tooltip("Tag que debe tener el player para ser arrastrado por la plataforma.")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Si es true, el jugador se parentea a la plataforma (recomendado para Rigidbody con velocity manual).")]
    [SerializeField] private bool parentPlayerWhileOnTop = true;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.8f);

    private int currentIndex;
    private int direction = 1; // 1 = avanzando en la lista, -1 = retrocediendo (para PingPong)
    private bool isMoving;
    private float waitTimer;
    private Rigidbody platformRb;

    private readonly HashSet<Transform> ridingPlayers = new HashSet<Transform>();
    private readonly Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>();

    public bool IsMoving => isMoving;

    private void Awake()
    {
        platformRb = GetComponent<Rigidbody>();

        if (platformRb != null)
        {
            // Una plataforma cinemática se mueve por MovePosition sin que la física externa la empuje.
            platformRb.isKinematic = true;
        }

        if (waypoints.Count > 0)
        {
            startWaypointIndex = Mathf.Clamp(startWaypointIndex, 0, waypoints.Count - 1);
            currentIndex = startWaypointIndex;
            transform.position = waypoints[currentIndex].position;
        }

        isMoving = startMovingOnAwake && waypoints.Count >= 2;
    }

    private void FixedUpdate()
    {
        if (waypoints.Count < 2) return;
        if (!isMoving)
        {
            HandleWait();
            return;
        }

        MoveTowardsCurrentTarget();
    }

    private void HandleWait()
    {
        if (waitTimer <= 0f) return;

        waitTimer -= Time.fixedDeltaTime;
        if (waitTimer <= 0f)
        {
            isMoving = true;
        }
    }

    private void MoveTowardsCurrentTarget()
    {
        int targetIndex = GetNextIndex();
        Vector3 targetPos = waypoints[targetIndex].position;

        Vector3 currentPos = platformRb != null ? platformRb.position : transform.position;
        Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, speed * Time.fixedDeltaTime);

        if (platformRb != null)
            platformRb.MovePosition(newPos);
        else
            transform.position = newPos;

        if (Vector3.Distance(newPos, targetPos) <= 0.001f)
        {
            currentIndex = targetIndex;
            AdvanceRouteLogic();
        }
    }

    private int GetNextIndex()
    {
        if (loopMode == PlatformLoopMode.PingPong)
        {
            int next = currentIndex + direction;
            if (next < 0 || next >= waypoints.Count)
            {
                // No debería pasar acá (se corrige en AdvanceRouteLogic), pero por seguridad:
                return currentIndex;
            }
            return next;
        }
        else // Loop u Once
        {
            int next = currentIndex + 1;
            if (next >= waypoints.Count) next = (loopMode == PlatformLoopMode.Loop) ? 0 : currentIndex;
            return next;
        }
    }

    private void AdvanceRouteLogic()
    {
        switch (loopMode)
        {
            case PlatformLoopMode.PingPong:
                // Si llegó al último o al primero, invierte dirección.
                if (currentIndex == waypoints.Count - 1) direction = -1;
                else if (currentIndex == 0) direction = 1;
                break;

            case PlatformLoopMode.Loop:
                // Nada especial: GetNextIndex ya envuelve a 0 al llegar al final.
                break;

            case PlatformLoopMode.Once:
                if (currentIndex == waypoints.Count - 1)
                {
                    isMoving = false;
                    return; // se detiene permanentemente, sin espera
                }
                break;
        }

        if (waitTimeAtPoint > 0f)
        {
            isMoving = false;
            waitTimer = waitTimeAtPoint;
        }
    }

    //===================================================================================
    //=====================      TRANSPORTE DE PLAYER (parenting)     ===================
    //===================================================================================

    //private void OnCollisionEnter(Collision collision)
    //{
    //    TryAttachPlayer(collision.transform);
    //}

    //private void OnCollisionExit(Collision collision)
    //{
    //    TryDetachPlayer(collision.transform);
    //}

    // Si tu plataforma usa un Collider "isTrigger" en vez de colisión sólida normal
    // (por ejemplo un trigger extra arriba de la plataforma para detectar al player),
    // estos también funcionan:
    private void OnTriggerEnter(Collider other)
    {
        TryAttachPlayer(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        TryDetachPlayer(other.transform);
    }

    private void TryAttachPlayer(Transform t)
    {
        if (!parentPlayerWhileOnTop) return;
        if (t == null || !t.CompareTag(playerTag)) return;
        if (ridingPlayers.Contains(t)) return;

        ridingPlayers.Add(t);
        originalParents[t] = t.parent;
        t.SetParent(transform, true);
    }

    private void TryDetachPlayer(Transform t)
    {
        if (t == null) return;
        if (!ridingPlayers.Contains(t)) return;

        ridingPlayers.Remove(t);

        if (originalParents.TryGetValue(t, out Transform originalParent))
        {
            t.SetParent(originalParent, true);
            originalParents.Remove(t);
        }
        else
        {
            t.SetParent(null, true);
        }
    }

    //===================================================================================
    //=====================              API PÚBLICA                  ===================
    //===================================================================================

    /// <summary>Detiene la plataforma manualmente (ej. desde un evento scripteado).</summary>
    public void StopPlatform() => isMoving = false;

    /// <summary>Reanuda el movimiento manualmente.</summary>
    public void ResumePlatform()
    {
        if (waypoints.Count >= 2) isMoving = true;
    }

    /// <summary>Agrega un nuevo waypoint en runtime (útil para generación procedural).</summary>
    public void AddWaypoint(Transform point) => waypoints.Add(point);

    private void OnDrawGizmos()
    {
        if (!drawGizmos || waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = gizmoColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.25f);

            int nextIdx = (i + 1 < waypoints.Count) ? i + 1 : -1;
            if (nextIdx != -1 && waypoints[nextIdx] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIdx].position);
            }
        }

        if (loopMode == PlatformLoopMode.Loop && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
        }
    }
}