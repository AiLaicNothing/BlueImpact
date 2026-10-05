using UnityEngine;
using System.Collections;
[CreateAssetMenu(menuName = "Player/Skills/Solis/Solar Descent")]
public class SolarDescent_Skill : Skill
{
    [Header("Damage")]
    public HitData hitData;
    [Header("Jump")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private Vector3 downForce;
    [SerializeField] private float chargeTime = 1f;
    [SerializeField] private bool debug;
    [Header("Impact")]
    [SerializeField] private float impactRadius = 6f;
    [Header("Vfx")]
    [SerializeField] ParticleSystem vfxFire;
    [SerializeField] GameObject groundShockwavePrefab;
    public string castSecondAnimation;
    private bool isExecuting;
    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;
    private GameObject debugBox;
    private GameObject debugBox2;
    [Header("Spawn")]
    [SerializeField] private Vector3 spawnOffSet;
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";
    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.StartCoroutine(Descent(player,targetPoint,lockTargetPos));
    }

    IEnumerator Descent(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.Rb.linearVelocity = new Vector3(player.Rb.linearVelocity.x, Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics.gravity.y)), player.Rb.linearVelocity.z);
        player.PlayAudio(actionSound, 0.8f);
        yield return new WaitForSeconds(0.3f);
        player.Rb.isKinematic = true;
        /*vfxFire = GameObject.Find("FireInSwordVfx").GetComponent<ParticleSystem>();
        vfxFire.Play();*/
        if (player.Anim != null)
        {
            int skill = Animator.StringToHash($"{castSecondAnimation}");

            if (player.Anim.HasState(0, skill))
            {
                player.Anim.CrossFade(skill, 0.8f, 0);
            }
            else
            {
                Debug.Log("[PlayerAnimator] is missing castAnimation State");
            }
        }
        yield return new WaitForSeconds(chargeTime);
        player.Rb.isKinematic = false;

        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        Vector3 spawnPos = player.Model.position + player.Model.forward * spawnOffSet.z + Vector3.up * spawnOffSet.y;

        Vector3 dir = (finalTarget - spawnPos).normalized;

        if (dir.sqrMagnitude < 0.0001f) dir = player.Model.forward;

        Quaternion rot = Quaternion.LookRotation(dir);

        Vector3 center = spawnPos + dir * (player.Model.localScale.z / 2f);

        Vector3 halfExtents = new Vector3(player.Model.localScale.x / 2f, player.Model.localScale.y / 2f, player.Model.localScale.z / 2f);
         

        player.Rb.linearVelocity = downForce;

        if (debug) debugBox2 = player.ShowHitboxPersistent(center, halfExtents * 2, player.Model.rotation, debugBox2);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, player.Model.rotation, enemyLayer);

        DamageInfo info = new DamageInfo
        {
            damage = ((player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * hitData.physicalScale) + (player.PlayerStatsManager.GetActualValue(StatType.DañoMágico) * hitData.magicalScale))/2,
            hitDirection = dir,
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

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(info);
            }
        }
        bool hitGround = false;
        while (!hitGround)
        {
            if (Physics.Raycast(player.Model.position, Vector3.down, 1.1f, groundLayer))
            {
                hitGround = true;
            }
            yield return null;
        }
        //vfxFire.Stop();
        OnGroundImpact(player, targetPoint, lockTargetPos);
        if (debugBox != null)
        {
            GameObject.Destroy(debugBox);
            debugBox = null;
        }
        if (debugBox2 != null)
        {
            GameObject.Destroy(debugBox2);
            debugBox2 = null;
        }
    }

    void OnGroundImpact(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        GameObject vfx2 = Instantiate(groundShockwavePrefab, new Vector3(player.Model.position.x, player.Model.position.y -0.5f, player.Model.position.z), Quaternion.identity);

        Vector3 finalTarget;

        // PRIORITIZE LOCK TARGET
        if (lockTargetPos != Vector3.zero)
        {
            finalTarget = lockTargetPos;
        }
        else
        {
            finalTarget = targetPoint;
        }

        // Direction toward target
        Vector3 dir = (finalTarget - player.Model.position).normalized;
        if (debug) debugBox = player.ShowHitboxPersistent(player.Model.position, new Vector3(impactRadius * 2, impactRadius * 2, impactRadius * 2), player.Model.rotation, debugBox);

        Collider[] hits = Physics.OverlapSphere(player.Model.position, impactRadius, enemyLayer);

        DamageInfo info = new DamageInfo
        {
            damage = ((player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * hitData.physicalScale) + (player.PlayerStatsManager.GetActualValue(StatType.DañoMágico) * hitData.magicalScale)),
            hitDirection = dir,
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

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(info);
            }
        }
        Destroy(vfx2, 5f);
    }
}
