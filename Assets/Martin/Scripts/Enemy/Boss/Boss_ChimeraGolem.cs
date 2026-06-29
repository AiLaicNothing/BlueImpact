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

    [Header("Locations")]
    [SerializeField] private Transform centerArena;
    [SerializeField] private Transform[] corners;

    public GameObject target;
    public bool inCinematic;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(HeavyBlow());
        }
    }

    private void HandelAi()
    {
        if (inCinematic) return;
    }

    private IEnumerator HeavyBlow()
    {
        // Do a melee hit

        //Logic -> Do a hitbox, a boxcast that check player
        //Logic -> while it last, move/dash toward the player
        //Logic -> When it end wait for 1.5f secs
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

        //Logic -> wait startUpDuration,
        //Logic -> above the enemy, x distance, make a circle/ radius of rain radius, call vfx
        //Logic -> after that spawn randomly inside that circle the fireballs, falling to the ground
        //Logic -> Channel duration end, stop doing attack and wait 1.5f secs

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

        //Logic -> wait for x secs to synch with animation
        //Logic -> Shoot untill reaching the bullePerShot
        //Logic -> There is a time between the instante of bullets -> timeBtwShoot
        //Logic -> Stop doing attack and wait 1.5f secs

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

        //Logic -> Wait for x secs to synch with animation
        //Logic -> Make a sphere in base of hitBoxRadius, then check if player
        //Logic -> Do damage if inside
        //Logic -> Stop doing attack and wait 1.5f secs
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

            player.TakeDamage(info);
        }

        yield return new WaitForSeconds(1.5f);

        Debug.Log("End Ground Slam attack");
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
