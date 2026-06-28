using System.Collections;
using UnityEngine;

public class Boss_ChimeraGolem : EnemyBase
{
    [Header("Heavy Blow")]
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


    private void HandelAi()
    {
        
    }

    private IEnumerator HeavyBlow()
    {
        // Do a melee hit

        //Logic -> Do a hitbox, a boxcast that check player
        //Logic -> while it last, move/dash toward the player
        //Logic -> When it end wait for 1.5f secs
        yield break;
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
            var vfx = Instantiate(magicCircleVfx, transform.position, Quaternion.identity);
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
                    var proj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
                }
            }

            timer += waveInterval;

            yield return new WaitForSeconds(waveInterval);
        }

        yield return new WaitForSeconds(1.5f);
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
            if (player != null)
            {
                Vector3 dir = (player.transform.position - firePoint.position).normalized;

                transform.forward = new Vector3(dir.x, 0f, dir.z);

                if (shootVfx != null)
                {
                    var vfx = Instantiate(shootVfx, firePoint.position, Quaternion.identity);
                }

                if (bulletPrefab != null)
                {
                    var proj = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
                }

                yield return new WaitForSeconds(timeBtwShoot);
            }
        }

        yield return new WaitForSeconds(1.5f);
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
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, hitBoxRadius);

        foreach (Collider hit in hits)
        {
            DamageInfo info = new DamageInfo
            {
                damage = stats.damage,
            };

            player.TakeDamage(info);
        }

        yield return new WaitForSeconds(1.5f);
    }
}
