using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Fatuo Fire")]
public class FatuoFire_Skill : Skill
{
    [Header("Spin Size")]
    [SerializeField] private Vector3 hitBoxSize;

    [Header("Offset")]
    [SerializeField] private Vector3 startOffset;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;
    private GameObject debugBox;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;

        DealDamage(player);
    }

    private void DealDamage(PlayerControl player)
    {
        Vector3 startPos = player.transform.position + player.Model.right * startOffset.x + player.Model.up * startOffset.y + player.Model.forward * startOffset.z;

        player.ShowHitbox(startPos, hitBoxSize, player.Model.transform.rotation);

        Collider[] hits = Physics.OverlapBox(startPos, hitBoxSize * 0.5f, player.transform.rotation, enemyLayer);

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                DamageInfo info = new DamageInfo
                {
                    damage = 1,
                    hitDirection = player.Model.forward,
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
