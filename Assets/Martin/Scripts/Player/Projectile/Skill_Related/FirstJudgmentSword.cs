using System;
using System.Collections;
using UnityEngine;

public class FirstJudgmentSword : MonoBehaviour
{
    [Header("Fall")]
    public float initialSpeed = 20f;
    public float gravity = -80f;

    [Header("Ground Check")]
    [SerializeField] private float groundOffset = 2f;
    public Transform groundCheck;
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayer;

    [Header("Damage")]
    public float radius = 5f;
    public LayerMask enemyLayer;
    private DamageInfo info;

    [Header("VFX")]
    public GameObject impactSFX;
    public GameObject damageSFX;

    private Rigidbody rb;
    private bool hasLanded;

    private Vector3 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(HitData hitData)
    {
        velocity = Vector3.down * initialSpeed;

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
    }

    private void Update()
    {
        CheckGround();
    }
    void FixedUpdate()
    {
        if (hasLanded) return;

        ApplyMovement();
    }

    void ApplyMovement()
    {
        velocity.y += gravity * Time.fixedDeltaTime;

        rb.linearVelocity = velocity;
    }

    void CheckGround()
    {
        if (Physics.Raycast(groundCheck.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            // snap to ground
            rb.linearVelocity = Vector3.zero;
            transform.position = hit.point + Vector3.up * groundOffset;

            Land();
        }
    }

    void Land()
    {
        hasLanded = true;

        if (impactSFX != null)
        {
        }

        StartCoroutine(ImpactRoutine());
    }

    private IEnumerator ImpactRoutine()
    {
        yield return new WaitForSeconds(0.05f);

        DealDamage();

        Destroy(gameObject, 0.1f);
    }

    private void DealDamage()
    {

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        if (damageSFX != null)
        {
        }

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage(info);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}