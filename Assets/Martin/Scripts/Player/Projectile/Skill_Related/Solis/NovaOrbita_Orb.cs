using System;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public class NovaOrbita_Orb : MonoBehaviour
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

    public void Initialize(PlayerControl player, HitData hitData, float r, float speed, float time, float startAngle)
    {
        info = new DamageInfo
        {
            damage = 1,
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable != null)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;

                damageable.TakeDamage(info);
            }
        }
    }
}
