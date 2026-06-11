using System.Collections.Generic;
using UnityEngine;

public class P_Projectile : MonoBehaviour
{
    //Decide if follow a enemy
    public bool isHoming;
    //Decide if shoot toward where is aiming or the target lock on --> This get from the target lock on script
    public bool hasTarget;
    public bool randomTarget;

    [Header("Homing Settings")]
    [SerializeField] float maxTargetRange = 20f;
    [SerializeField] private float homingRange = 15f;
    [SerializeField] private float retargetInterval = 0.2f;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    private float retargetTimer;

    private Transform target;

    private float damage;
    private HitData hitData;
    private Vector3 direction;
    private float speed;

    private Rigidbody rb;

    private Vector3 lockTargetPos;

    public void Initialize(float dmg, HitData data, Vector3 dir, float spd, Vector3 lockTargetPos)
    {
        damage = dmg;
        hitData = data;
        direction = dir.normalized;
        speed = spd;

        rb = GetComponent<Rigidbody>();

        this.lockTargetPos = lockTargetPos;

        if (isHoming)
        {
            if (target == null)
            {
                if (hasTarget && this.lockTargetPos != Vector3.zero)
                {
                    target = GetClosestToPoint(lockTargetPos);
                }
                else if (randomTarget)
                {
                    target = GetRandomTarget(transform.position);
                }
                else
                {
                    target = GetClosestTarget(transform.position);
                }
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }

        // Auto destroy after some time
        Destroy(gameObject, 3f);
    }

    private void FixedUpdate()
    {
        if (rb == null || !isHoming) return;

        retargetTimer -= Time.fixedDeltaTime;

        if (retargetTimer <= 0)
        {
            retargetTimer = retargetInterval;

            if (target == null || Vector3.Distance(transform.position, target.position) > homingRange)
            {
                target = GetClosestTarget(transform.position);
            }
        }

        if (target == null) return;

        Vector3 desiredDir = (target.position - transform.position).normalized;
        Vector3 newDir = Vector3.Lerp(rb.linearVelocity.normalized, desiredDir, Time.fixedDeltaTime * 5f);
        rb.linearVelocity = newDir * speed;

        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(rb.linearVelocity.normalized);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 10f);
        }
    }


    Transform GetClosestTarget(Vector3 origin)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float minDist = maxTargetRange;
        Transform closest = null;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(origin, enemy.transform.position);

            if (dist > maxTargetRange) continue;

            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    Transform GetClosestToPoint(Vector3 point)
    {
        return GetClosestTarget(point);
    }

    Transform GetRandomTarget(Vector3 origin)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        List<Transform> validTargets = new List<Transform>();

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(origin, enemy.transform.position);

            if (dist <= maxTargetRange)
            {
                validTargets.Add(enemy.transform);
            }
        }

        if (validTargets.Count == 0) return null;

        return validTargets[Random.Range(0, validTargets.Count)];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            IDamageable target = other.GetComponent<IDamageable>();

            if (target != null)
            {
                target.TakeDamage(10);
            }

            Destroy(gameObject);
        }
    }
}
