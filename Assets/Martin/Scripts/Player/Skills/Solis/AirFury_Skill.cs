using UnityEngine;
using System.Collections;
[CreateAssetMenu(menuName = "Player/Skills/Solis/Air Fury")]
public class AirFury_Skill : Skill
{
    [Header("Beam Size")]
    [SerializeField] private float maxRange = 15f;
    [SerializeField] private float width = 2f;
    [SerializeField] private float height = 2f;
    [SerializeField] private bool debug;

    [Header("Time")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private float tickRate;

    [Header("Offset")]
    [SerializeField] private Vector3 startOffset;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    [Header("Layer")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask enemyLayer;
    private GameObject debugBox;
    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private Vector3 vfxOffset;
    bool firstPos;
    Vector3 trueStartPos;
    Vector3 trueDir;
    Quaternion trueRot;
    [Header("Sfx")]
    [SerializeField] private GameObject sfx;
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";
    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        firstPos = false;
        player.StartCoroutine(AirRoutine(player, targetPoint, lockTargetPos));
    }

    private IEnumerator AirRoutine(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;

        float timer = 0f;
        bool soundPlayed = false;
        if (vfx != null)
        {
            player.blockVelocity = true;
            var vfxPrefab = InstanceVfx(player, targetPoint, lockTargetPos);
            yield return new WaitForSeconds(0.6f);
            Destroy(vfxPrefab, duration);
        }
        while (timer < duration)
        {
            if (!soundPlayed)
            {
                player.PlayAudio(actionSound, 0.8f);
                soundPlayed = true;
            }
            FireAir(player, targetPoint, lockTargetPos);
            yield return new WaitForSeconds(tickRate);
            timer += tickRate;
        }

        if (debugBox != null)
        {
            GameObject.Destroy(debugBox);
            debugBox = null;
        }
    }
    GameObject InstanceVfx(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 vfxPos = player.transform.position + player.Model.right * vfxOffset.x + player.Model.up * vfxOffset.y + player.Model.forward * vfxOffset.z;

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
        Vector3 dir = (finalTarget - vfxPos).normalized;


        Quaternion rot = Quaternion.LookRotation(dir);


        return Instantiate(vfx, vfxPos, rot);
    }
    void FireAir(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 startPos = player.transform.position + player.Model.right * startOffset.x + player.Model.up * startOffset.y + player.Model.forward * startOffset.z;

        if (!firstPos)
        {
            trueStartPos = startPos;
            trueDir = player.Model.forward * startOffset.z;
            trueRot = player.Model.rotation;
            firstPos = true;
        }
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
        Vector3 dir = (finalTarget - trueStartPos).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = player.Model.forward;
        float finalDistance = maxRange;

        // Obstacle check
        if (Physics.Raycast(trueStartPos, dir, out RaycastHit hit, maxRange, obstacleLayer))
        {
            finalDistance = hit.distance;
        }

        Vector3 center = trueStartPos + dir * (finalDistance / 2f);

        Vector3 halfExtents = new Vector3(width / 2f, height / 2f, finalDistance / 2f);

        Quaternion rot = Quaternion.LookRotation(dir);

        if (debug) debugBox = player.ShowHitboxPersistent(center, halfExtents * 2, rot, debugBox);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, enemyLayer);

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
    }
}
