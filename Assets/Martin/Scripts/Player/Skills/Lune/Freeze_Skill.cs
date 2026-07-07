using System.Collections;
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
    [SerializeField] private float hitTime;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private Vector3 vfxOffset;
    [SerializeField] private bool debug;
    private GameObject debugBox;

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        //DealDamage(player);
        player.StartCoroutine(CastIceSpyke(player));
    }

    private void DealDamage(PlayerControl player)
    {
        //player.blockVelocity = true;

        Vector3 startPos = player.transform.position + player.Model.right * startOffset.x + player.Model.up * startOffset.y + player.Model.forward * startOffset.z;

        //Vector3 vfxPos = player.transform.position + player.Model.right * vfxOffset.x + player.Model.up * vfxOffset.y + player.Model.forward * vfxOffset.z;

        if (debug) player.ShowHitbox(startPos, hitBoxSize * 0.5f * 2, player.Model.transform.rotation);

        Collider[] hits = Physics.OverlapBox(startPos, hitBoxSize * 0.5f, player.Model.transform.rotation, enemyLayer);

        //if (vfx != null)
        //{
        //    var vfxPrefab = Instantiate(vfx, vfxPos, player.Model.rotation); 
        //    Destroy(vfxPrefab,1.5f);
        //}

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                Vector3 dir = (target.transform.position - player.transform.position).normalized;

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

                damageable.TakeDamage(info);
            }
        }
        player.PlayAudio(actionSound, 0.8f);

    }

    private IEnumerator CastIceSpyke(PlayerControl player)
    {
        player.blockVelocity = true;

        Vector3 vfxPos = player.transform.position + player.Model.right * vfxOffset.x + player.Model.up * vfxOffset.y + player.Model.forward * vfxOffset.z;

        if (vfx != null)
        {
            var vfxPrefab = Instantiate(vfx, vfxPos, player.Model.rotation);
            Destroy(vfxPrefab, 1.5f);
        }

        yield return new WaitForSeconds(hitTime);

        DealDamage(player);
    }
}