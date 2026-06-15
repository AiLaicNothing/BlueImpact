using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Freeze")]
public class Freeze_Skill : Skill
{
    [Header("Spin Size")]
    [SerializeField] private Vector3 hitBoxSize;

    [Header("Offset")]
    [SerializeField] private Vector3 startOffset;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private bool debug;
    private GameObject debugBox;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        DealDamage(player);
    }

    private void DealDamage(PlayerControl player)
    {
        player.blockVelocity = true;

        Vector3 startPos = player.transform.position + player.Model.right * startOffset.x + player.Model.up * startOffset.y + player.Model.forward * startOffset.z;

        if (debug) debugBox = player.ShowHitboxPersistent(startPos, hitBoxSize * 0.5f * 2, player.transform.rotation, debugBox);

        Collider[] hits = Physics.OverlapBox(startPos, hitBoxSize * 0.5f, player.transform.rotation, enemyLayer);

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                Vector3 dir = (target.transform.position - player.transform.position).normalized;

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

                damageable.TakeDamage(info);
            }
        }
    }
}
