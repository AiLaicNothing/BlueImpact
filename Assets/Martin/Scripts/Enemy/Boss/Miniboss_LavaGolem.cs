using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Miniboss_LavaGolem : EnemyBase
{
    [Header("Targeting")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float maxChaseDistance = 20f;
    [SerializeField] private float detectionDelay = 0.25f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Flujo Cinético - Melee")]
    [SerializeField] private float flujoCineticoRange = 4f;
    [SerializeField] private Vector3 flujoHitBoxSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 flujoHitBoxPos = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private float flujoWindUpTime = 1.5f;
    [SerializeField] private float flujoArmsAirTime = 3f;
    [SerializeField] private float flujoHitTime = 0.8f;
    [SerializeField] private int flujoMeleeDamage = 18;
    [SerializeField] private GameObject flujoVfx;

    [Header("Llamarada Densa - Projectile")]
    [SerializeField] private float llamaradaRange = 10f;
    [SerializeField] private float llamaradaChargeTime = 5f;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int llamaradaDamage = 50;
    [SerializeField] private GameObject chargeVfx;
    [SerializeField] private GameObject castVfx;

    [Header("Patrol")]
    [SerializeField] private float patrolStopDistance = 0.5f;

    [Header("Debug")]
    [SerializeField] private GameObject hitBoxPrefab;

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool returningHome;
    private bool isPerformingAction;
    private float detectionTimer;

    private int patrolIndex;
    private int patrolDir = 1;
    private float actionCooldown;
    private int attackCount;
    private int nextSpecialThreshold;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0.25f;
        }

        nextSpecialThreshold = Random.Range(2, 4);
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;
        if (isStunned || IsStaggered) return;
        if (agent == null || !agent.enabled) return;

        UpdateTarget();
        HandleDetection();

        if (isPerformingAction) return;

        HandleActions();
        HandleMovement();

        // Animation
        if (agent.velocity.sqrMagnitude >= 0.01f)
        {
            //anim.Play("Walk");
        }
        else
        {
            //anim.Play("Idle");
        }
    }

    private void UpdateTarget()
    {
        if (!HasValidPlayer())
        {
            player = FindFirstObjectByType<PlayerControl>();
        }
    }

    private bool HasLineOfSight()
    {
        if (!HasValidPlayer()) return false;

        Vector3 origin = transform.position + Vector3.up * targetRayHeight;
        Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;

        Vector3 dir = (targetPos - origin).normalized;
        float rayDistance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            if (hit.transform != player.transform && hit.transform.root != player.transform.root)
            {
                return false;
            }
        }

        return true;
    }

    private void HandleDetection()
    {
        if (safeZone == null) return;

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
            detectionTimer = 0f;
            return;
        }

        float playerDistHome = Vector3.Distance(player.transform.position, safeZone.position);
        float distPlayer = DistanceToPlayer();

        if (!hasDetectedPlayer)
        {
            if (distPlayer <= detectionRange && HasLineOfSight())
            {
                detectionTimer += Time.deltaTime;

                if (detectionTimer >= detectionDelay)
                {
                    hasDetectedPlayer = true;
                    isFollowingPlayer = true;
                    Debug.Log($"[LavaGolem] Jugador detectado a {distPlayer:F2}m");
                }
            }
            else
            {
                detectionTimer = 0f;
            }
        }
        else
        {
            if (playerDistHome > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                isFollowingPlayer = false;
                returningHome = true;
                detectionTimer = 0f;
                Debug.Log("[LavaGolem] Jugador perdido - retornando a spawn");
            }
        }
    }

    private void HandleMovement()
    {
        if (isFollowingPlayer && HasValidPlayer())
        {
            float distance = DistanceToPlayer();

            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
            RotateToVelocity();
        }
        else if (returningHome)
        {
            agent.isStopped = false;
            agent.SetDestination(spawnPos.position);

            RotateToVelocity();

            if (!agent.pathPending && agent.remainingDistance <= patrolStopDistance)
            {
                returningHome = false;
                agent.ResetPath();
            }
        }
        else if (hasPatrol)
        {
            agent.isStopped = false;
            HandlePatrol();
            RotateToVelocity();
        }
        else
        {
            agent.ResetPath();
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0) return;

        Transform posDesired = patrolZones[patrolIndex];
        agent.SetDestination(posDesired.position);

        float dist = Vector3.Distance(transform.position, posDesired.position);

        if (dist <= patrolStopDistance)
        {
            patrolIndex += patrolDir;

            if (patrolIndex >= patrolZones.Length)
            {
                patrolIndex = patrolZones.Length - 2;
                patrolDir = -1;
            }
            else if (patrolIndex < 0)
            {
                patrolIndex = 1;
                patrolDir = 1;
            }
        }
    }

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer || !HasValidPlayer()) return;

        actionCooldown -= Time.deltaTime;

        if (actionCooldown <= 0f)
        {
            Debug.Log($"[LavaGolem] Decidiendo ataque - Contador: {attackCount}/{nextSpecialThreshold}");

            // Decidir ataque
            if (attackCount >= nextSpecialThreshold)
            {
                attackCount = 0;
                nextSpecialThreshold = Random.Range(2, 4);
                Debug.Log("[LavaGolem] ATAQUE ESPECIAL: Llamarada Densa");
                StartCoroutine(PerformLlamarada());
            }
            else
            {
                if (Random.value > 0.4f)
                {
                    Debug.Log("[LavaGolem] ATAQUE NORMAL: Flujo Cinético");
                    StartCoroutine(PerformFlujo());
                }
                else
                {
                    Debug.Log("[LavaGolem] ATAQUE NORMAL: Llamarada Densa");
                    StartCoroutine(PerformLlamarada());
                }

                attackCount++;
            }
        }
    }

    private IEnumerator PerformFlujo()
    {
        Debug.Log("[Flujo] Iniciando Flujo Cinético");
        isPerformingAction = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        float distance = DistanceToPlayer();
        Debug.Log($"[Flujo] Distancia al jugador: {distance:F2}m (rango necesario: {flujoCineticoRange}m)");

        // Si está muy lejos, acercarse primero
        if (distance > flujoCineticoRange)
        {
            Debug.Log("[Flujo] Demasiado lejos, acercándose...");
            if (agent != null)
            {
                agent.isStopped = false;
            }

            float timeout = 2f;
            float elapsed = 0f;

            while (distance > flujoCineticoRange && elapsed < timeout)
            {
                if (HasValidPlayer())
                {
                    agent.SetDestination(player.transform.position);
                    RotateToVelocity();
                    distance = DistanceToPlayer();
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            Debug.Log($"[Flujo] En rango - Distancia final: {distance:F2}m");
        }

        // Wind-up
        Debug.Log("[Flujo] Wind-up: levantando brazos...");
        //anim.Play("Flujo_WindUp");
        FaceTargetInstant();

        yield return new WaitForSeconds(flujoWindUpTime);

        // Brazos en el aire
        Debug.Log("[Flujo] Brazos en el aire, esperando...");
        //  anim.Play("Flujo_Raise");

        float raiseTimer = 0f;
        while (raiseTimer < flujoArmsAirTime)
        {
            FaceTargetInstant();
            raiseTimer += Time.deltaTime;
            yield return null;
        }

        // Golpe
        Debug.Log("[Flujo] ¡GOLPE!");
        //  anim.Play("Flujo_Slam");

        yield return new WaitForSeconds(flujoHitTime);

        // Instanciar VFX de impacto
        if (flujoVfx != null)
        {
            Debug.Log("[Flujo] Instanciando VFX de impacto");
            Instantiate(flujoVfx, transform.position + transform.forward * flujoHitBoxPos.z, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[Flujo] flujoVfx NO asignado");
        }

        // Raycast de daño
        Vector3 center = transform.position + transform.forward * flujoHitBoxPos.z;
        Collider[] hits = Physics.OverlapBox(center, flujoHitBoxSize * 0.5f, transform.rotation);

        Debug.Log($"[Flujo] Hitbox check: {hits.Length} colisiones detectadas");

        ShowHitbox(center, flujoHitBoxSize, transform.rotation);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            Debug.Log($"[Flujo] ¡IMPACTO! En: {hit.name}");

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                Debug.Log($"[Flujo] Aplicando {flujoMeleeDamage} daño");
                DamageInfo info = new DamageInfo
                {
                    damage = flujoMeleeDamage,
                    hitDirection = transform.forward,
                };

                damageable.TakeDamage(in info);
            }
        }

        yield return new WaitForSeconds(1f);

        isPerformingAction = false;
        actionCooldown = 2f;
        Debug.Log("[Flujo] Flujo Cinético completado");
    }

    private IEnumerator PerformLlamarada()
    {
        Debug.Log("[Llamarada] Iniciando Llamarada Densa");
        isPerformingAction = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTargetInstant();

        // Animación de carga
        Debug.Log("[Llamarada] Reproduciendo animación 'Llamarada_Charge'");
        //anim.Play("Llamarada_Charge");

        // VFX de carga
        GameObject chargeEffect = null;
        if (chargeVfx != null && firePoint != null)
        {
            Debug.Log("[Llamarada] Instanciando VFX de carga");
            chargeEffect = Instantiate(chargeVfx, firePoint.position, Quaternion.identity);
            chargeEffect.transform.SetParent(firePoint);
        }
        else
        {
            Debug.LogWarning("[Llamarada] chargeVfx o firePoint NO asignados");
        }

        // Tiempo de carga
        Debug.Log($"[Llamarada] Cargando durante {llamaradaChargeTime}s...");
        float chargeTimer = 0f;
        while (chargeTimer < llamaradaChargeTime)
        {
            if (HasValidPlayer())
            {
                FaceTargetInstant();
            }

            chargeTimer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[Llamarada] Carga completada!");

        // Disparar
        Debug.Log("[Llamarada] Reproduciendo animación 'Llamarada_Cast'");
        //anim.Play("Llamarada_Cast");

        if (chargeEffect != null)
        {
            Destroy(chargeEffect);
        }

        if (castVfx != null && firePoint != null)
        {
            Debug.Log("[Llamarada] Instanciando VFX de disparo");
            Instantiate(castVfx, firePoint.position, Quaternion.identity);
        }

        if (!HasValidPlayer())
        {
            Debug.LogError("[Llamarada] Jugador NO válido!");
            isPerformingAction = false;
            actionCooldown = 3f;
            yield break;
        }

        if (fireballPrefab == null)
        {
            Debug.LogError("[Llamarada] fireballPrefab NO asignado en Inspector!");
            isPerformingAction = false;
            actionCooldown = 3f;
            yield break;
        }

        if (firePoint == null)
        {
            Debug.LogError("[Llamarada] firePoint NO asignado en Inspector!");
            isPerformingAction = false;
            actionCooldown = 3f;
            yield break;
        }

        Debug.Log("[Llamarada] Creando proyectil...");
        Vector3 targetPos = player.transform.position + Vector3.up * 0.5f;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        Debug.Log($"[Llamarada] Desde: {firePoint.position}, Hacia: {targetPos}, Dirección: {dir}");

        GameObject proj = Instantiate(fireballPrefab, firePoint.position, Quaternion.LookRotation(dir));
        Debug.Log($"[Llamarada] Proyectil instanciado: {proj.name}");

        var projectile = proj.GetComponent<E_Projectile>();

        if (projectile != null)
        {
            Debug.Log($"[Llamarada] Inicializando proyectil con daño {llamaradaDamage}");
            projectile.InitProj(llamaradaDamage, dir);
        }
        else
        {
            Debug.LogError("[Llamarada] El prefab NO tiene componente E_Projectile!");
        }

        yield return new WaitForSeconds(1.5f);

        isPerformingAction = false;
        actionCooldown = 3f;
        Debug.Log("[Llamarada] Llamarada Densa completada");
    }

    private void RotateToVelocity()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0f;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void FaceTargetInstant()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private float DistanceToPlayer()
    {
        if (!HasValidPlayer()) return Mathf.Infinity;
        return Vector3.Distance(transform.position, player.transform.position);
    }

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        if (hitBoxPrefab == null || !debug) return;

        GameObject box = Instantiate(hitBoxPrefab, center, rot);
        box.transform.localScale = size;
        Destroy(box, 0.2f);
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        // Detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Flujo range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, flujoCineticoRange);

        // Llamarada range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, llamaradaRange);

        // Safe zone
        if (safeZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(safeZone.position, 1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(safeZone.position, maxChaseDistance);
        }
    }
}