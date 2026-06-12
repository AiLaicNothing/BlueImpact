using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Spin Attack")]
public class SpinAttack_Skill : Skill
{
    [Header("HitBox")]
    [SerializeField] private Vector3 hitBoxSize;
    [SerializeField] private Vector3 spawnOffSet;
    [SerializeField] private LayerMask targets;

    [Header("HitTime")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private float tickRate = 0.4f;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private GameObject debugBox;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.StartCoroutine(SpinRoutine(player));
    }

    private IEnumerator SpinRoutine(PlayerControl player)
    {
        player.blockVelocity = true;

        float timer = 0;

        while (timer < duration)
        {
            DealDamage(player);

            yield return new WaitForSeconds(tickRate);

            timer += tickRate;
        }

        if (debugBox != null)
        {
            GameObject.Destroy(debugBox);
            debugBox = null;
        }
    }

    private void DealDamage(PlayerControl player)
    {
        Vector3 startPos = player.transform.position + player.Model.right * spawnOffSet.x + player.Model.up * spawnOffSet.y + player.Model.forward * spawnOffSet.z;

        debugBox = player.ShowHitboxPersistent(startPos, hitBoxSize * 0.5f * 2, player.transform.rotation, debugBox);

        Collider[] hits = Physics.OverlapBox(startPos, hitBoxSize * 0.5f, player.transform.rotation, targets);

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                Vector3 dir = (target.transform.position - player.transform.position).normalized;


                DamageInfo info = new DamageInfo
                {
                    damage = 1,
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
