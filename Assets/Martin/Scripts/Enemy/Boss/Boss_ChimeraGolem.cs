using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_ChimeraGolem : EnemyBase
{
    private enum NormalAttack { None, GolpeCisDesatado, LlamaradaCar, GolpeContraSuelo, CampoMarchito }

    [Header("Detección (misma regla que el Lava Golem)")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float maxChaseDistance = 45f;
    [SerializeField] private float detectionDelay = 0.25f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private float playerSearchInterval = 0.5f;

    [Header("Ritmo")]
    [SerializeField] private float pauseBetweenAttacks = 0.75f;
    [SerializeField] private int specialMinNormals = 3;
    [SerializeField] private int specialMaxNormals = 5;
    [SerializeField] private float meleeRange = 4f;   // por debajo: ataques cuerpo a cuerpo
    [SerializeField] private float midRange = 10f;    // entre meleeRange y midRange: zona o cañón

    [Header("Golpe Cis Desatado (melee)")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float maxDashDistance = 2f;   // el dash nunca pasa de aquí
    [SerializeField] private float attackDelay = 0.6f;     // aviso: el brazo se enciende antes del golpe
    [SerializeField] private float secondSwingDelay = 0.8f;
    [SerializeField] private float cisRecovery = 1.5f;
    [SerializeField] private Vector3 hitBoxHit;
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject armTellVfx;

    [Header("Campo Marchito (control de zona)")]
    [SerializeField] private Vector3 fieldOffset;
    [SerializeField] private GameObject withereZone;
    [SerializeField] private GameObject fieldWarningVfx;
    [SerializeField] private GameObject fieldVfx; // visual del campo (se escala al radio de la zona)
    [SerializeField] private float fieldWarnTime = 1.2f;
    [SerializeField] private float fieldDamagePerTick = 8f;
    [SerializeField] private float fieldDuration = 4f;
    [SerializeField] private float fieldTickRate = 0.5f;
    [SerializeField] private float fieldRecovery = 1.5f;
    private GameObject activeZone; // solo una zona activa a la vez

    [Header("Llamarada Uel (especial)")]
    [SerializeField] private float uelChannelTime = 2f;
    [SerializeField] private int uelWaves = 3;
    [SerializeField] private int fireballsPerWave = 4;
    [SerializeField] private float waveInterval = 0.5f;    // entre bolas de una oleada
    [SerializeField] private float waveGap = 1.5f;         // entre oleadas
    [SerializeField] private float uelRecovery = 3f;
    [SerializeField] private float rainRadius = 8f;
    [SerializeField] private float spawnHeight = 18f;
    [SerializeField] private float fireballDamage = 22f;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private GameObject magicCircleVfx;
    [SerializeField] private GameObject warningVfx;
    [SerializeField] private Vector3 offsetWarning;

    [Header("Llamarada Car (cañón)")]
    [SerializeField] private int bulletPerShoot = 3;
    [SerializeField] private float timeBtwShoot = 0.35f;
    [SerializeField] private float carTell = 0.6f;
    [SerializeField] private float carDamage = 15f;
    [SerializeField] private float carRecovery = 1.5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject shootVfx;

    [Header("Golpe contra el suelo (onda de choque)")]
    [SerializeField] private float slamTell = 0.8f;
    [SerializeField] private float hitBoxRadius = 4f;       // radio exterior de la onda
    [SerializeField] private float shockInnerRadius = 3f;   // dentro: daño completo; entre inner y outer: mitad
    [SerializeField] private float slamRecovery = 2.5f;
    [SerializeField] private GameObject smokeVfx;
    [SerializeField] private LayerMask targerLayer;         // se mantiene el nombre para no perder la asignación del Inspector

    [Header("Locations")]
    [SerializeField] private Transform centerArena;
    [SerializeField] private Transform[] corners;

    public GameObject target;
    public bool inCinematic;

    [SerializeField] private bool verboseLogs;

    private Coroutine attackRoutine;
    private bool isAttacking;
    private bool hasDetectedPlayer;
    private float detectionTimer;
    private float nextPlayerSearchTime;
    private int normalAttackCount;
    private int nextSpecialThreshold;
    private NormalAttack lastNormal = NormalAttack.None;

    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
    private readonly Dictionary<IDamageable, float> shockDamage = new Dictionary<IDamageable, float>();
    private readonly Collider[] overlapBuffer = new Collider[16];

    private void Log(string message)
    {
        if (verboseLogs) Debug.Log("[Quimera] " + message);
    }

    // Aplica daño una sola vez a cada objetivo, aunque tenga varios colliders
    private void DamageTargets(Collider[] hits, float damage)
    {
        damagedTargets.Clear();

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null || !damagedTargets.Add(damageable)) continue;

            damageable.TakeDamage(new DamageInfo { damage = damage });
        }
    }

    protected override void Start()
    {
        base.Start();
        nextSpecialThreshold = Random.Range(specialMinNormals, specialMaxNormals + 1);
    }

    protected override void Update()
    {
        if (isDead)
        {
            CancelAttack();
            return;
        }

        base.Update();

        UpdateTarget();
        HandleDetection();

        // Aturdido o tambaleado: se corta el ataque en curso
        if (isStunned || IsStaggered)
        {
            CancelAttack();
            return;
        }

        HandleAi();
    }

    // ---------- Detección ----------

    private void UpdateTarget()
    {
        if (HasValidPlayer()) return;
        if (Time.time < nextPlayerSearchTime) return;

        nextPlayerSearchTime = Time.time + playerSearchInterval;
        player = FindFirstObjectByType<PlayerControl>();
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
                return false;
        }

        return true;
    }

    private void HandleDetection()
    {
        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            detectionTimer = 0f;
            return;
        }

        if (!hasDetectedPlayer)
        {
            if (DistanceToPlayer() <= detectionRange && HasLineOfSight())
            {
                detectionTimer += Time.deltaTime;

                if (detectionTimer >= detectionDelay)
                {
                    hasDetectedPlayer = true;
                    Log("Jugador detectado");
                }
            }
            else
            {
                detectionTimer = 0f;
            }
        }
        else
        {
            Transform home = HomePoint;

            if (home != null && Vector3.Distance(player.transform.position, home.position) > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                detectionTimer = 0f;
                Log("Jugador perdido");
            }
        }
    }

    // ---------- Selección de ataque ----------

    private void HandleAi()
    {
        if (inCinematic || isAttacking || !hasDetectedPlayer || !HasValidPlayer()) return;

        target = player.gameObject;
        isAttacking = true;
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        PlayAnim("Idle");

        if (normalAttackCount >= nextSpecialThreshold)
        {
            normalAttackCount = 0;
            nextSpecialThreshold = Random.Range(specialMinNormals, specialMaxNormals + 1);

            Log("Especial: Llamarada Uel");
            yield return UelFlare();
        }
        else
        {
            NormalAttack attack = ChooseNormalAttack();
            lastNormal = attack;
            normalAttackCount++;

            Log("Ataque: " + attack);

            switch (attack)
            {
                case NormalAttack.GolpeCisDesatado: yield return GolpeCisDesatado(); break;
                case NormalAttack.LlamaradaCar:     yield return CarFlare();        break;
                case NormalAttack.GolpeContraSuelo: yield return GroundSlam();      break;
                case NormalAttack.CampoMarchito:    yield return WhitheredField();  break;
            }
        }

        yield return new WaitForSeconds(pauseBetweenAttacks);

        EndAttack();
    }

    // Elige según la distancia, sin zona duplicada y sin repetir el último ataque
    private NormalAttack ChooseNormalAttack()
    {
        float dist = DistanceToPlayer();
        List<NormalAttack> pool = new List<NormalAttack>(4);

        if (dist <= meleeRange)
        {
            pool.Add(NormalAttack.GolpeCisDesatado);
            pool.Add(NormalAttack.GolpeContraSuelo);
        }
        else if (dist <= midRange)
        {
            pool.Add(NormalAttack.CampoMarchito);
            pool.Add(NormalAttack.LlamaradaCar);
        }
        else
        {
            pool.Add(NormalAttack.LlamaradaCar);
        }

        if (activeZone != null) pool.Remove(NormalAttack.CampoMarchito);
        if (pool.Count > 1) pool.Remove(lastNormal);
        if (pool.Count == 0) return NormalAttack.LlamaradaCar;

        return pool[Random.Range(0, pool.Count)];
    }

    private void EndAttack()
    {
        attackRoutine = null;
        isAttacking = false;
    }

    private void CancelAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isAttacking = false;
    }

    // ---------- Golpe Cis Desatado ----------

    private IEnumerator GolpeCisDesatado()
    {
        if (!HasValidPlayer()) yield break;

        // Dash corto: como mucho maxDashDistance, y se detiene antes del jugador
        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        float dashLength = Mathf.Min(maxDashDistance, dist - stopDistance);

        if (dashLength > 0.05f)
        {
            Vector3 dir = toPlayer / dist;
            Vector3 dashTarget = transform.position + dir * dashLength;

            PlayAnim("Dash");

            while (Vector3.Distance(transform.position, dashTarget) > 0.05f)
            {
                transform.forward = dir;
                transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // Aviso: el brazo se enciende antes del golpe
        PlayAnim("Golpe_Sis");

        if (armTellVfx != null)
        {
            GameObject tell = Instantiate(armTellVfx, transform.position, transform.rotation, transform);
            Destroy(tell, attackDelay);
        }

        yield return WindUpFacing(attackDelay);

        PerformCisSwing();

        // "Desatado": si el jugador sigue frente al boss, hay un segundo golpe
        if (IsTargetInFront())
        {
            yield return new WaitForSeconds(secondSwingDelay);
            PerformCisSwing();
        }

        yield return new WaitForSeconds(cisRecovery);
    }

    private void PerformCisSwing()
    {
        Vector3 center = transform.position + transform.forward * hitBoxHit.z;

        if (hitVfx != null) Instantiate(hitVfx, center, Quaternion.identity);

        Collider[] hits = Physics.OverlapBox(center, hitBoxHit * 0.5f, transform.rotation, targerLayer);
        DamageTargets(hits, stats.damage);
    }

    private bool IsTargetInFront()
    {
        if (!HasValidPlayer()) return false;

        Vector3 center = transform.position + transform.forward * hitBoxHit.z;
        return Vector3.Distance(center, player.transform.position) <= hitBoxHit.z * 0.5f + 0.5f;
    }

    // ---------- Campo Marchito ----------

    private IEnumerator WhitheredField()
    {
        if (!HasValidPlayer() || withereZone == null) yield break;

        Vector3 rayOrigin = player.transform.position + Vector3.up * 2f;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f, whatIsGround)) yield break;

        Vector3 point = hit.point + fieldOffset;

        // Aviso bajo el jugador: el círculo se ve antes de que aparezca la zona
        if (fieldWarningVfx != null)
        {
            GameObject warn = Instantiate(fieldWarningVfx, point, Quaternion.identity);
            ScaleToZoneRadius(warn);
            Destroy(warn, fieldWarnTime);
        }

        yield return new WaitForSeconds(fieldWarnTime);

        activeZone = Instantiate(withereZone, point, Quaternion.identity);

        if (activeZone.TryGetComponent(out WitheredZone zone))
        {
            zone.Configure(fieldDamagePerTick, fieldDuration, fieldTickRate);
        }

        if (fieldVfx != null)
        {
            GameObject vfx = Instantiate(fieldVfx, point, Quaternion.identity);
            ScaleToZoneRadius(vfx);
            Destroy(vfx, fieldDuration);
        }

        yield return new WaitForSeconds(fieldRecovery);
    }

    // Escala un quad de VFX (1 unidad = 1 m) para que cubra el diámetro de la zona
    private void ScaleToZoneRadius(GameObject vfx)
    {
        if (vfx == null || withereZone == null) return;
        if (!withereZone.TryGetComponent(out WitheredZone zone)) return;

        float d = zone.Radius * 2f;
        vfx.transform.localScale = new Vector3(d, d, 1f);
    }

    // ---------- Llamarada Uel (especial) ----------

    private IEnumerator UelFlare()
    {
        yield return MoveToPoint(centerArena);

        PlayAnim("Llamarada_Uel");

        GameObject circle = null;
        if (magicCircleVfx != null)
        {
            circle = Instantiate(magicCircleVfx, transform.position + Vector3.up * spawnHeight, Quaternion.identity);
        }

        yield return new WaitForSeconds(uelChannelTime);

        for (int w = 0; w < uelWaves; w++)
        {
            for (int i = 0; i < fireballsPerWave; i++)
            {
                SpawnRainFireball();
                yield return new WaitForSeconds(waveInterval);
            }

            yield return new WaitForSeconds(waveGap);
        }

        if (circle != null) Destroy(circle);

        yield return new WaitForSeconds(uelRecovery);
    }

    private void SpawnRainFireball()
    {
        Vector2 randomPoint = Random.insideUnitCircle * rainRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomPoint.x, spawnHeight, randomPoint.y);

        if (!Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, Mathf.Infinity, whatIsGround)) return;

        // El aviso dura lo que tarda la bola en llegar, con la velocidad del prefab
        float speed = 1f;
        if (fireballPrefab != null && fireballPrefab.TryGetComponent(out E_Projectile proto))
        {
            speed = Mathf.Max(0.01f, proto.Speed);
        }

        float travelTime = Vector3.Distance(spawnPos, hit.point) / speed;

        if (warningVfx != null)
        {
            Destroy(Instantiate(warningVfx, hit.point + offsetWarning, Quaternion.identity), travelTime);
        }

        if (fireballPrefab != null)
        {
            GameObject bolt = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);

            if (bolt.TryGetComponent(out E_Projectile proj))
            {
                proj.InitProj(fireballDamage, Vector3.down);
            }
        }
    }

    // ---------- Llamarada Car (cañón) ----------

    private IEnumerator CarFlare()
    {
        if (corners != null && corners.Length > 0)
        {
            Transform spot = corners[Random.Range(0, corners.Length)];
            yield return MoveToPoint(spot);
        }

        PlayAnim("Llamarada_Car");

        yield return new WaitForSeconds(carTell);

        for (int i = 0; i < bulletPerShoot; i++)
        {
            if (!HasValidPlayer() || firePoint == null) break;

            Vector3 dir = (player.transform.position - firePoint.position).normalized;
            transform.forward = new Vector3(dir.x, 0f, dir.z);

            if (shootVfx != null)
            {
                Destroy(Instantiate(shootVfx, firePoint.position, Quaternion.identity), 2f);
            }

            if (bulletPrefab != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));

                if (bullet.TryGetComponent(out E_Projectile proj))
                {
                    proj.InitProj(carDamage, dir);
                }
            }

            yield return WindUpFacing(timeBtwShoot);
        }

        yield return new WaitForSeconds(carRecovery);
    }

    // ---------- Golpe contra el suelo ----------

    private IEnumerator GroundSlam()
    {
        // Sin dash: el golpe cae donde está el boss
        PlayAnim("Golpe_Suelo");

        yield return new WaitForSeconds(slamTell);

        if (smokeVfx != null)
        {
            Destroy(Instantiate(smokeVfx, transform.position, Quaternion.identity), 1.5f);
        }

        ShockwaveHit();

        yield return new WaitForSeconds(slamRecovery);
    }

    private void ShockwaveHit()
    {
        float fullDamage = stats.damage * 1.5f;
        int count = Physics.OverlapSphereNonAlloc(transform.position, hitBoxRadius, overlapBuffer, targerLayer);

        shockDamage.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.ClosestPointOnBounds(transform.position));
            float dmg = dist <= shockInnerRadius ? fullDamage : fullDamage * 0.5f;

            // Un objetivo con varios colliders recibe solo el daño mayor de la onda
            if (!shockDamage.TryGetValue(damageable, out float previous) || dmg > previous)
            {
                shockDamage[damageable] = dmg;
            }
        }

        foreach (KeyValuePair<IDamageable, float> entry in shockDamage)
        {
            entry.Key.TakeDamage(new DamageInfo { damage = entry.Value });
        }
    }

    // ---------- Utilidades ----------

    private IEnumerator MoveToPoint(Transform point)
    {
        if (point == null) yield break;

        PlayAnim("Walk_2");

        while (true)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = new Vector3(point.position.x, currentPos.y, point.position.z);
            Vector3 dir = (targetPos - currentPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 25f * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(currentPos, targetPos, 10f * Time.deltaTime);

            Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);

            if (Vector2.Distance(currentXZ, targetXZ) <= 0.01f)
            {
                transform.position = targetPos;
                yield break;
            }

            yield return null;
        }
    }

    // Espera mirando al jugador
    private IEnumerator WindUpFacing(float seconds)
    {
        float t = 0f;

        while (t < seconds)
        {
            FaceTarget(6);
            t += Time.deltaTime;
            yield return null;
        }
    }

    private void FaceTarget(float speed)
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, speed * Time.deltaTime);
    }

    public void SetCinematic(bool value)
    {
        inCinematic = value;
    }

    public void GetPositions(Transform center, Transform[] Corners)
    {
        centerArena = center;
        corners = Corners;
    }
}
