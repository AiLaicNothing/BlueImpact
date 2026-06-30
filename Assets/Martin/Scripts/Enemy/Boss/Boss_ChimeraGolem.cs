using System.Collections;
using UnityEngine;

public class Boss_ChimeraGolem : EnemyBase
{
    [Header("Heavy Blow")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float attackDelay = 0.5f;
    [SerializeField] private Vector3 hitBoxHit;
    [SerializeField] private GameObject hitVfx;

    [Header("Whitered Field")]
    [SerializeField] private float fieldDuration;
    [SerializeField] private GameObject fieldVfx;

    [Header("Uel Flare")]
    [SerializeField] private float startUpDuration;
    [SerializeField] private float channelDuration;
    [SerializeField] private float rainRadius;
    [SerializeField] private int fireballsPerWave = 4;
    [SerializeField] private float waveInterval = 0.5f;
    [SerializeField] private float spawnHeight = 18f;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private GameObject magicCircleVfx;

    [Header("Car Flare")]
    [SerializeField] private int bulletPerShoot;
    [SerializeField] private float timeBtwShoot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject shootVfx;

    [Header("Ground Slam")]
    [SerializeField] private float hitBoxRadius;
    [SerializeField] private GameObject smokeVfx;
    [SerializeField] private LayerMask targerLayer;

    private bool isAttacking;
    private int normalAttackCount;
    private int nextSpecialThreshold;

    [Header("Locations")]
    [SerializeField] private Transform centerArena;
    [SerializeField] private Transform[] corners;

    public GameObject target;
    public bool inCinematic;

    protected override void Start()
    {
        base.Start();
        nextSpecialThreshold = Random.Range(3, 6);
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        HandleAi();
    }

    private void HandleAi()
    {
        if (inCinematic) return;

        if (isAttacking) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        anim.Play("Idle");

        Debug.Log("Deciding Attack");
        isAttacking = true;

        // Decide attack
        if (normalAttackCount >= nextSpecialThreshold)
        {
            normalAttackCount = 0;
            nextSpecialThreshold = Random.Range(3, 6);

            yield return UelFlare();
        }
        else
        {
            int attack = Random.Range(0, 4);

            switch (attack)
            {
                case 0:
                    Debug.Log("Deciding HeavyBlow");
                    yield return HeavyBlow();
                    break;

                case 1:
                    Debug.Log("Deciding carflare");
                    yield return CarFlare();
                    break;

                case 2:
                    Debug.Log("GroundSlam");
                    yield return GroundSlam();
                    break;

                case 3:
                    yield return WhitheredField();
                    break;
            }

            normalAttackCount++;
        }

        // Small delay before choosing another attack.
        yield return new WaitForSeconds(0.75f);

        isAttacking = false;
    }

    private IEnumerator HeavyBlow()
    {
        // Do a melee hit

        if (target == null) yield break;

        // Lock the player's position at the start.
        Vector3 playerPos = target.transform.position;
        playerPos.y = transform.position.y;

        Vector3 dir = (playerPos - transform.position).normalized;

        // Final destination.
        Vector3 stopPos = playerPos - dir * stopDistance;

        anim.Play("Dash");

        while (Vector3.Distance(transform.position, stopPos) > 0.05f)
        {
            transform.forward = dir;
            transform.position = Vector3.MoveTowards(transform.position, stopPos, dashSpeed * Time.deltaTime);

            yield return null;
        }

        anim.Play("Golpe_Sis");

        // Wind-up before striking.
        float timer = 0f;

        while (timer < attackDelay)
        {
            timer += Time.deltaTime;

            FaceTarget(6);

            yield return null;
        }

        // Spawn hit VFX.
        if (hitVfx != null)
        {
            Instantiate(hitVfx, transform.position + transform.forward * hitBoxHit.z, Quaternion.identity);
        }

        // Check hit.
        Vector3 center = transform.position + transform.forward * hitBoxHit.z;

        Collider[] hits = Physics.OverlapBox(center, hitBoxHit * 0.5f, transform.rotation, targerLayer);

        foreach (Collider hit in hits)
        {
            DamageInfo info = new DamageInfo
            {
                damage = stats.damage,
            };

            hit.GetComponent<PlayerControl>().TakeDamage(info);
        }

        yield return new WaitForSeconds(1.5f);

        Debug.Log("End Heavy Blow attack");
    }

    private IEnumerator WhitheredField()
    {
        // Create around the boss a Field that inflict damage to player

        if (fieldVfx != null)
        {
            var field = Instantiate(fieldVfx, transform.position, Quaternion.identity);

            field.transform.SetParent(transform, false);
        }

        yield return new WaitForSeconds(fieldDuration);

        Debug.Log("End Withered Field attack");
    }

    private IEnumerator UelFlare()
    {
        //While channeling for x secs, make a area where fire fall to the ground like rain

        yield return MoveToPoint(centerArena);

        anim.Play("Llamarada_Uel");

        yield return new WaitForSeconds(startUpDuration);

        if (magicCircleVfx  != null)
        {
            Vector3 circlePos = transform.position + Vector3.up * spawnHeight;

            var vfx = Instantiate(magicCircleVfx, circlePos, Quaternion.identity);

            Destroy(vfx, channelDuration);
        }

        float timer = 0f;

        while (timer < channelDuration)
        {
            for (int i = 0; i < fireballsPerWave; i++)
            {
                Vector2 randomPoint = Random.insideUnitCircle * rainRadius;

                Vector3 spawnPos = transform.position + new Vector3(randomPoint.x, spawnHeight, randomPoint.y);

                if (fireballPrefab != null)
                {
                    var prefab = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);

                    var proj = prefab.GetComponent<E_Projectile>();

                    proj.InitProj(10f, Vector3.down);
                }
            }

            timer += waveInterval;

            yield return new WaitForSeconds(waveInterval);
        }

        yield return new WaitForSeconds(1.5f);

        Debug.Log("Attack Uel Flare end");
    }

    private IEnumerator CarFlare()
    {
        // Shoot toward the player

        Transform randomPos = corners[Random.Range(0, corners.Length)];

        yield return MoveToPoint(randomPos);

        anim.Play("Llamarada_Car");

        yield return new WaitForSeconds(0.6f);

        for (int i = 0; i < bulletPerShoot; i++)
        {
            if (target != null)
            {
                Vector3 dir = (target.transform.position - firePoint.position).normalized;

                transform.forward = new Vector3(dir.x, 0f, dir.z);

                if (shootVfx != null)
                {
                    var vfx = Instantiate(shootVfx, firePoint.position, Quaternion.identity);
                }

                if (bulletPrefab != null)
                {
                    var projPrefab = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
                    var  proj = projPrefab.GetComponent<E_Projectile>();

                    proj.InitProj(10f, dir);
                }

                float timer = 0f;

                while (timer < timeBtwShoot)
                {
                    timer += Time.deltaTime;

                    FaceTarget(6);

                    yield return null;
                }
            }
        }

        yield return new WaitForSeconds(1.5f);

        Debug.Log("End Car Flare attack");
    }

    private IEnumerator GroundSlam()
    {
        // Hit the ground causing a ground explosion(hit)

        if (target == null) yield break;

        // Lock the player's position at the start.
        Vector3 playerPos = target.transform.position;
        playerPos.y = transform.position.y;

        Vector3 dir = (playerPos - transform.position).normalized;

        // Final destination.
        Vector3 stopPos = playerPos - dir * stopDistance;

        while (Vector3.Distance(transform.position, stopPos) > 0.05f)
        {
            transform.forward = dir;
            transform.position = Vector3.MoveTowards(transform.position, stopPos, dashSpeed * Time.deltaTime);

            yield return null;
        }

        anim.Play("Golpe_Suelo");

        yield return new WaitForSeconds(0.7f);

        if (smokeVfx != null)
        {
            var vfx = Instantiate(smokeVfx, transform.position, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, hitBoxRadius, targerLayer);

        foreach (Collider hit in hits)
        {
            DamageInfo info = new DamageInfo
            {
                damage = stats.damage,
            };

            hit.gameObject.GetComponent<PlayerControl>().TakeDamage(info);
        }

        yield return new WaitForSeconds(1.5f);

        Debug.Log("End Ground Slam attack");
    }

    private IEnumerator MoveToPoint(Transform target)
    {
        if (target == null) yield break;

        anim.Play("Walk_2");

        while (true)
        {
            Vector3 currentPos = transform.position;

            Vector3 targetPos = new Vector3(target.position.x, currentPos.y, target.position.z);

            Vector3 dir = (targetPos - currentPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 25f * Time.deltaTime); // Rotation speed
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

    private void FaceTarget(float speed)
    {
        if (target == null) return;

        Vector3 dir = target.transform.position - transform.position;
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
