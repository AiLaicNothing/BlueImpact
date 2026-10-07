using System;
using System.Collections;
using UnityEngine;

public class FirstJudgmentSword : MonoBehaviour
{
    [Header("Fall")]
    public float initialSpeed = 20f;
    public float gravity = -80f;
    [SerializeField] float maxTimer;
    [SerializeField] float gravityExtra;

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
    Material swordMat;
    [SerializeField] GameObject parent;
    private Rigidbody rb;
    private bool hasLanded;

    private Vector3 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        swordMat = GetComponent<MeshRenderer>().material;
        gravity = 0f;
        initialSpeed = 5;
        Vector3 circlePosition = parent.transform.position + new Vector3(0, -7.553535f,0);
        swordMat.SetVector("_Sword_Pivot", circlePosition);
    }

    public void Initialize(HitData hitData, PlayerControl player)
    {
        velocity = Vector3.down * initialSpeed;

        info = new DamageInfo
        {
            damage = ((player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * hitData.physicalScale) + (player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * hitData.magicalScale)),
            throwType = hitData.throwType,
            stunDuration = hitData.stunDuration,
            keepInAir = hitData.keepInAir,
            airHangDuration = hitData.airHangDuration,
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
    float timer = 0;
    void ApplyMovement()
    {
        timer += Time.fixedDeltaTime;
        
        if (timer >= maxTimer) gravity -= 0.65f;
        else gravity -= gravityExtra;
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
            gravity = 0;
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

        Destroy(parent, 2f);
        Destroy(gameObject, 2f);
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