using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public class BloodMoon : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float forwardSpeed = 15f;

    [Header("Targeting")]
    [SerializeField] private float maxRange = 25f;
    [SerializeField] private float spawnHeight = 20f;
    [SerializeField] private float fallSpeed = 35f;

    private Rigidbody rb;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("After Impact")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private float tickRate = 0.5f;
    [SerializeField] private float radius = 5f;

    [SerializeField] private LayerMask enemyLayer;

    private HitData hitData;
    private PlayerControl player;
    private float damage;
    private bool hasLanded;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(PlayerControl player, HitData hitData, Vector3 targetPos )
    {
        this.hitData = hitData;

        Vector3 targetPoint = targetPos;

        Vector3 dir = targetPoint - player.transform.position;
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            dir = dir.normalized * maxRange;
            targetPoint = player.transform.position + dir;
        }

        if (!Physics.Raycast(targetPoint + Vector3.up * 5f, Vector3.down, out RaycastHit groundHit, 20f, groundLayer))
        {
            targetPoint = player.transform.position + player.Model.forward * maxRange;
        }
        else
        {
            targetPoint = groundHit.point;
        }

        transform.position = targetPoint + Vector3.up * spawnHeight;

        rb.linearVelocity = Vector3.down * fallSpeed;
    }

    private void FixedUpdate()
    {
        if (hasLanded) return;

        RotateToVelocity();
        CheckGround();
    }

    private void RotateToVelocity()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void CheckGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            transform.position = hit.point;
            Land();
        }
    }

    void Land()
    {
        hasLanded = true;

        // stop movement
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;

        StartCoroutine(DamageWaves());
    }

    private IEnumerator DamageWaves()
    {
        timer = 0f;

        while (timer < duration)
        {
            DealDamage();

            yield return new WaitForSeconds(tickRate);

            timer += tickRate;
        }

        Destroy(gameObject);
    }

    private void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null && hitData != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                DamageInfo info = new DamageInfo
                {
                    damage = ((player.Stats.GetCurrentValue(StatType.PhysicalDamage) * hitData.physicalScale) + (player.Stats.GetCurrentValue(StatType.MagicalDamage) * hitData.magicalScale)),
                    hitDirection = dir,
                    throwType = hitData.throwType,
                    stunDuration = hitData.stunDuration,
                    keepInAir = hitData.keepInAir,
                    airLiftForce = hitData.airLiftForce,
                    pushForce = hitData.pushForce,
                    knockDownForce = hitData.knockDownForce,
                    knockDownForwardScale = hitData.knockDownForwardScale,
                    staggerBuild = hitData.staggerCharge
                };

                dmg.TakeDamage(info);
            }
        }
    }
}
