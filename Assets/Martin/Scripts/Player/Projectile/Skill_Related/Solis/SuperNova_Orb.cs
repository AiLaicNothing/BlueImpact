using System;
using UnityEngine;

public class SuperNova_Orb : MonoBehaviour
{
    private Transform player;

    private float radius;
    private float angularSpeed;
    private float duration;

    private float angle;
    private float timer;

    private Vector3 orbitForward;
    private Vector3 orbitRight;

    private DamageInfo info;
    private DamageInfo explosionInfo;

    private float explosionRadius;

    private LayerMask enemyLayer;

    public void Initialize(PlayerControl player, HitData hitData, float r, float speed, float time, float startAngle, float explodeRadius, HitData explosionData, LayerMask enemies)
    {
        info = new DamageInfo
        {
            damage = ((player.Stats.GetCurrentValue(StatType.PhysicalDamage) * hitData.physicalScale) + (player.Stats.GetCurrentValue(StatType.MagicalDamage) * hitData.magicalScale)),
            throwType = hitData.throwType,
            stunDuration = hitData.stunDuration,
            keepInAir = hitData.keepInAir,
            airLiftForce = hitData.airLiftForce,
            pushForce = hitData.pushForce,
            knockDownForce = hitData.knockDownForce,
            knockDownForwardScale = hitData.knockDownForwardScale,
            staggerBuild = hitData.staggerCharge
        };

        explosionInfo = new DamageInfo
        {
            damage = ((player.Stats.GetCurrentValue(StatType.PhysicalDamage) * explosionData.physicalScale) + (player.Stats.GetCurrentValue(StatType.MagicalDamage) * explosionData.magicalScale)),
            throwType = hitData.throwType,
            stunDuration = hitData.stunDuration,
            keepInAir = hitData.keepInAir,
            airLiftForce = hitData.airLiftForce,
            pushForce = hitData.pushForce,
            knockDownForce = hitData.knockDownForce,
            knockDownForwardScale = hitData.knockDownForwardScale,
            staggerBuild = hitData.staggerCharge
        };

        this.player = player.transform;

        radius = r;
        angularSpeed = speed;
        duration = time;

        angle = startAngle;
        explosionRadius = explodeRadius;
        enemyLayer = enemies;

        orbitForward = player.Model.forward;
        orbitRight = player.Model.right;
    }

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;

        if (timer >= duration)
        {
            Explode();

            Destroy(gameObject);
            return;
        }

        Orbit();
    }

    void Orbit()
    {
        angle += angularSpeed * Time.deltaTime;

        float rad = angle * Mathf.Deg2Rad;

        Vector3 offset = orbitForward * Mathf.Cos(rad) * radius + orbitRight * Mathf.Sin(rad) * radius;

        Vector3 targetPos = player.position + offset;

        transform.position = targetPos;

        Vector3 tangent = -orbitForward * Mathf.Sin(rad) + orbitRight * Mathf.Cos(rad);

        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }
    }

    void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage(explosionInfo);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            IDamageable dmg = other.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;

                dmg.TakeDamage(info);
            }
        }
    }
}
