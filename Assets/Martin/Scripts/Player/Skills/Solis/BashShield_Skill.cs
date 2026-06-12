using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Bash Shield")]
public class BashShield_Skill : Skill
{
    public float dashSpeed;
    public float duration;

    public HitData hitData;
    public Vector3 hitBoxSize;
    public Vector3 hitBoxOffset;

    public override void ExecuteSkill(PlayerControl player, Vector3  targetPoint, Vector3 lockTargetPos)
    {
        if (lockTargetPos != Vector3.zero)
        {
            Vector3 dir = (lockTargetPos - player.transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                player.Model.rotation = Quaternion.LookRotation(dir);
            }
        }

        //if (actionSound != null)
        //{
        //    AudioManager.Instance.PlaySFX(actionSound);
        //}

        player.StartCoroutine(BashRoutine(player));
    }

    private IEnumerator BashRoutine(PlayerControl player)
    {
        player.blockVelocity = false;

        float timer = duration;

        while (timer > 0f)
        {
            timer -= Time.fixedDeltaTime;

            Vector3 velocity = player.Model.forward * dashSpeed;
            velocity.y = player.Rb.linearVelocity.y;
            player.Rb.linearVelocity = velocity;

            CheckHits(player);

            yield return new WaitForFixedUpdate();
        }

        player.blockVelocity = false;
    }

    private void CheckHits(PlayerControl player)
    {
        Vector3 center = player.Model.position + player.Model.forward * hitBoxOffset.z + Vector3.up * hitBoxOffset.y;

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, player.Model.rotation);

        player.ShowHitbox(center, hitBoxSize, player.Model.transform.rotation);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) continue;

            Vector3 hitDir = player.Model.transform.forward;

            DamageInfo info = new DamageInfo
            {
                damage = 1,
                hitDirection = hitDir,
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

            break;
        }
    }
}
