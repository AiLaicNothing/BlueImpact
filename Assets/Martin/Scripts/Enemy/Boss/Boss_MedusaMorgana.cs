using System.Collections;
using UnityEngine;

public class Boss_MedusaMorgana : EnemyBase
{
    [Header("Fases")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoThreshold = 0.6f;
    [SerializeField] private float pauseBetweenAttacks = 0.75f;

    [Header("Resonancia (Fase 1)")]
    [SerializeField] private float resonanciaWindUp = 1.5f;      // Z: segundos moviendo la espada
    [SerializeField] private int resonanciaProjectiles = 6;      // X: proyectiles del area
    [SerializeField] private float resonanciaRangeOffset = 2f;   // 2U de rango melee
    [SerializeField] private float resonanciaRectWidth = 6f;
    [SerializeField] private float resonanciaRectDepth = 8f;
    [SerializeField] private float resonanciaShotHeight = 1f;
    [SerializeField] private float resonanciaDamage = 10f;       // DMG x proyectil
    [SerializeField] private GameObject projectilePrefab;

    [Header("Part-Titura (Fase 1)")]
    [SerializeField] private float partTituraDuration = 12f;
    [SerializeField] private float partTituraMoveSpeed = 3f;
    [SerializeField] private float partTituraSpinSpeed = 720f;
    [SerializeField] private float partTituraStopDistance = 1.5f;
    [SerializeField] private float partTituraHitInterval = 1.5f; // Y: segundos entre golpes
    [SerializeField] private float partTituraHitRange = 2f;
    [SerializeField] private float partTituraHitRadius = 1.5f;
    [SerializeField] private float partTituraDamage = 12f;       // DMG x hit

    [Header("Climax Serpentinus (Fase 2)")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 15f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private float climaxChannelTime = 2f;
    [SerializeField] private float borderWarningTime = 3f;       // X: segundos iluminado antes del dano
    [SerializeField] private float climaxDuration = 30f;         // Duracion total de la habilidad
    [SerializeField] private float borderTickInterval = 0.5f;
    [SerializeField] private float borderDamagePerSecond = 5f;   // DMG x seg
    [SerializeField] private GameObject arenaBorderVfx;
    [SerializeField] private GameObject golemPrefab;
    [SerializeField] private int golemsToSummon = 5;             // Y: enemigos a invocar
    [SerializeField] private Transform[] summonPoints;

    [Header("Disonancia (Fase 2)")]
    [SerializeField] private float disonanciaAimTime = 1.5f;
    [SerializeField] private float markDuration = 4f;
    [SerializeField] private float beamDelay = 1f;
    [SerializeField] private float beamRadius = 1.5f;            // Distancia/radio del haz
    [SerializeField] private float disonanciaDamage = 60f;       // DMG
    [SerializeField] private GameObject markVfx;
    [SerializeField] private GameObject beamWarningVfx;
    [SerializeField] private GameObject beamVfx;

    [Header("Rotacion")]
    [SerializeField] private float turnSpeed = 8f;

    private bool isAttacking;
    private bool inPhaseTwo;

    public bool inCinematic;

    protected override void Start()
    {
        base.Start();

        if (arenaCenter == null) arenaCenter = transform;
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        if (!inPhaseTwo && GetHealthPercent() < phaseTwoThreshold)
        {
            EnterPhaseTwo();
        }

        HandleAi();
    }

    private void EnterPhaseTwo()
    {
        inPhaseTwo = true;
        Debug.Log("[Morgana] Entrando en fase 2");
    }

    // Reemplaza el cuerpo con la propiedad de vida que expone EnemyBase
    // Ejemplo: return currentHealth / stats.maxHealth;
    private float GetHealthPercent()
    {
        return 1f;
    }

    private void HandleAi()
    {
        if (inCinematic) return;
        if (isAttacking) return;
        if (!HasValidPlayer()) return;

        isAttacking = true;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        anim.Play("Idle");

        if (!inPhaseTwo)
        {
            if (Random.value < 0.5f)
                yield return Resonancia();
            else
                yield return PartTitura();
        }
        else
        {
            int pick = Random.Range(0, 4);

            switch (pick)
            {
                case 0:
                    yield return Resonancia();
                    break;
                case 1:
                    yield return PartTitura();
                    break;
                case 2:
                    yield return ClimaxSerpentinus();
                    break;
                case 3:
                    yield return Disonancia();
                    break;
            }
        }

        yield return new WaitForSeconds(pauseBetweenAttacks);

        isAttacking = false;
    }

    // ---------- FASE 1 ----------

    private IEnumerator Resonancia()
    {
        Debug.Log("[Morgana] Resonancia");
        anim.Play("Resonancia_Gesto");

        float timer = 0f;
        while (timer < resonanciaWindUp)
        {
            FaceTarget(turnSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        if (!HasValidPlayer()) yield break;

        anim.Play("Resonancia_Disparo");

        ResonanciaBurst(FlatDirToPlayer());

        yield return new WaitForSeconds(0.5f);
    }

    private void ResonanciaBurst(Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 origin = transform.position + forward * resonanciaRangeOffset + Vector3.up * resonanciaShotHeight;

        int count = Mathf.Max(1, resonanciaProjectiles);
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);
        int spawned = 0;

        for (int r = 0; r < rows && spawned < count; r++)
        {
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float u = cols == 1 ? 0.5f : (float)c / (cols - 1);
                float v = rows == 1 ? 0.5f : (float)r / (rows - 1);

                Vector3 pos = origin
                    + right * ((u - 0.5f) * resonanciaRectWidth)
                    + forward * (v * resonanciaRectDepth);

                FireProjectile(pos, forward, resonanciaDamage);
                spawned++;
            }
        }
    }

    private IEnumerator PartTitura()
    {
        Debug.Log("[Morgana] Part-Titura");
        anim.Play("Part_Titura");

        float timer = 0f;
        float hitTimer = 0f;

        while (timer < partTituraDuration)
        {
            if (!HasValidPlayer()) break;

            Vector3 dir = FlatDirToPlayer();
            float dist = DistanceToPlayerFlat();

            transform.Rotate(0f, partTituraSpinSpeed * Time.deltaTime, 0f);

            if (dist > partTituraStopDistance)
            {
                transform.position += dir * partTituraMoveSpeed * Time.deltaTime;
            }

            hitTimer += Time.deltaTime;

            if (hitTimer >= partTituraHitInterval)
            {
                hitTimer = 0f;

                Vector3 hitCenter = transform.position + transform.forward * partTituraHitRange;
                DamagePlayerWithin(hitCenter, partTituraHitRadius, partTituraDamage, transform.forward);
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    // ---------- FASE 2 ----------

    private IEnumerator ClimaxSerpentinus()
    {
        Debug.Log("[Morgana] Climax Serpentinus");
        anim.Play("Climax_Energia");

        yield return new WaitForSeconds(climaxChannelTime);

        if (arenaBorderVfx != null) arenaBorderVfx.SetActive(true);

        // El borde se ilumina como aviso antes de que haga dano
        yield return new WaitForSeconds(borderWarningTime);

        SummonGolems();

        float remaining = Mathf.Max(0f, climaxDuration - climaxChannelTime - borderWarningTime);
        yield return BorderDamageOverTime(remaining);

        if (arenaBorderVfx != null) arenaBorderVfx.SetActive(false);
    }

    private IEnumerator BorderDamageOverTime(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (HasValidPlayer() && IsInsideBorder(player.transform.position))
            {
                DamagePlayer(borderDamagePerSecond * borderTickInterval, Vector3.zero);
            }

            yield return new WaitForSeconds(borderTickInterval);
            timer += borderTickInterval;
        }
    }

    private bool IsInsideBorder(Vector3 pos)
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Vector2 p = new Vector2(pos.x, pos.z);
        Vector2 c = new Vector2(center.x, center.z);

        return Vector2.Distance(p, c) >= arenaRadius - borderWidth;
    }

    private void SummonGolems()
    {
        if (golemPrefab == null)
        {
            Debug.LogWarning("[Morgana] golemPrefab no asignado");
            return;
        }

        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;

        for (int i = 0; i < golemsToSummon; i++)
        {
            Vector3 pos;

            if (summonPoints != null && summonPoints.Length > 0)
            {
                pos = summonPoints[i % summonPoints.Length].position;
            }
            else
            {
                Vector2 r = Random.insideUnitCircle * arenaRadius * 0.6f;
                pos = center + new Vector3(r.x, 0f, r.y);
            }

            Instantiate(golemPrefab, pos, Quaternion.identity);
        }
    }

    private IEnumerator Disonancia()
    {
        Debug.Log("[Morgana] Disonancia");
        anim.Play("Disonancia_Apuntar");

        float aimTimer = 0f;
        while (aimTimer < disonanciaAimTime)
        {
            FaceTarget(turnSpeed * 2f);
            aimTimer += Time.deltaTime;
            yield return null;
        }

        if (!HasValidPlayer()) yield break;

        GameObject marker = null;
        if (markVfx != null)
        {
            marker = Instantiate(markVfx, player.transform.position, Quaternion.identity);
        }

        Vector3 lastPos = player.transform.position;
        float markTimer = 0f;

        // El jugador queda marcado y el marcador lo sigue durante markDuration
        while (markTimer < markDuration)
        {
            if (HasValidPlayer())
            {
                lastPos = player.transform.position;
            }

            if (marker != null)
            {
                marker.transform.position = lastPos + Vector3.up * 0.1f;
            }

            markTimer += Time.deltaTime;
            yield return null;
        }

        if (marker != null) Destroy(marker);

        // La posicion del haz es donde estaba el jugador al terminar la marca
        Vector3 beamPos = lastPos;

        if (beamWarningVfx != null)
        {
            GameObject warning = Instantiate(beamWarningVfx, beamPos, Quaternion.identity);
            Destroy(warning, beamDelay);
        }

        yield return new WaitForSeconds(beamDelay);

        if (beamVfx != null)
        {
            GameObject beam = Instantiate(beamVfx, beamPos, Quaternion.identity);
            Destroy(beam, 0.5f);
        }

        DamagePlayerWithin(beamPos, beamRadius, disonanciaDamage, Vector3.up);
    }

    // ---------- HELPERS ----------

    private void FireProjectile(Vector3 pos, Vector3 dir, float damage)
    {
        if (projectilePrefab == null) return;

        GameObject proj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(dir));
        E_Projectile projectile = proj.GetComponent<E_Projectile>();

        if (projectile != null)
        {
            projectile.InitProj(damage, dir);
        }
    }

    private void DamagePlayer(float amount, Vector3 hitDir)
    {
        if (!HasValidPlayer()) return;

        IDamageable damageable = player.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        DamageInfo info = new DamageInfo
        {
            damage = amount,
            hitDirection = hitDir,
        };

        damageable.TakeDamage(in info);
    }

    // Aplica dano una sola vez si el jugador esta dentro del radio dado
    private void DamagePlayerWithin(Vector3 center, float radius, float amount, Vector3 hitDir)
    {
        if (!HasValidPlayer()) return;

        Vector2 p = new Vector2(player.transform.position.x, player.transform.position.z);
        Vector2 c = new Vector2(center.x, center.z);

        if (Vector2.Distance(p, c) <= radius)
        {
            DamagePlayer(amount, hitDir);
        }
    }

    private Vector3 FlatDirToPlayer()
    {
        if (!HasValidPlayer()) return transform.forward;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
    }

    private float DistanceToPlayerFlat()
    {
        if (!HasValidPlayer()) return Mathf.Infinity;

        Vector3 a = transform.position;
        Vector3 b = player.transform.position;
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void FaceTarget(float speed)
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = FlatDirToPlayer();
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, speed * Time.deltaTime);
    }

    public void SetCinematic(bool value)
    {
        inCinematic = value;
    }

    public void GetPositions(Transform center, Transform[] corners)
    {
        arenaCenter = center;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debug) return;

        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, arenaRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, arenaRadius - borderWidth);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, resonanciaRangeOffset);
    }
}