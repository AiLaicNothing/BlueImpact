using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu(menuName = "Player/Skills/Lune/Blood Moon")]
public class BloodMoon_Skill : Skill
{
    [SerializeField] private GameObject moonPrefab;

    [Header("Spawn")]
    [SerializeField] private float maxRange = 20f;
    [SerializeField] private float height;
    [SerializeField] private Vector3 offset;
    [SerializeField] private HitData hitData;
    [SerializeField] private LayerMask groundLayer;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;
    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private float durationVfx;
    [SerializeField] private float firstPause;
    [SerializeField] private float secondPause;
    [SerializeField] private float thirdPause;
    [SerializeField] private float maxTimer;

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
       player.StartCoroutine( SummonMoon(player, targetPoint, lockTargetPos));
    }

    private IEnumerator SummonMoon(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;

        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        // clamp range
        Vector3 dir = (finalTarget - player.transform.position);
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            finalTarget = player.transform.position + dir.normalized * maxRange;
        }
        /*targetPoint = finalTarget;
        dir = targetPoint - player.transform.position;
        dist = dir.magnitude;

        if (dist > maxRange)
        {
            dir = dir.normalized * maxRange;
            targetPoint = player.transform.position + dir;
        }

        if (!Physics.Raycast(targetPoint + Vector3.up * 5f, Vector3.down, out RaycastHit groundHit, 20f, groundLayer))
        {
            targetPoint = player.transform.position + player.Model.forward * maxRange;
        }
        else
        {
            targetPoint = groundHit.point;
        }

        Vector3 spawnPos = targetPoint + Vector3.up * height;*/
        Vector3 spawnPos = player.transform.position + player.Model.forward * offset.z + player.Model.right * offset.x + Vector3.up * height;

        ParticleSystem firstVfx = Instantiate(vfx, spawnPos, Quaternion.identity).GetComponent<ParticleSystem>();
        firstVfx.Play(false);
        ParticleSystem secondVfx = firstVfx.gameObject.transform.Find("VortexBloodMoon").GetComponent<ParticleSystem>();
        ParticleSystem thirdVfx = secondVfx.gameObject.transform.Find("MagicCircleBloodMoon").GetComponent<ParticleSystem>();
        yield return new WaitForSeconds(firstPause);
        firstVfx.Pause(false);
        secondVfx.Play(false);
        yield return new WaitForSeconds(secondPause);
        secondVfx.Pause(false);
        thirdVfx.Play(false);
        yield return new WaitForSeconds(thirdPause);
        thirdVfx.Pause(false);
        GameObject moon = Instantiate(moonPrefab, spawnPos + offset, Quaternion.identity);
        player.PlayAudio(actionSound, 0.8f);
        Vector3 circlePosition = new Vector3(0, firstVfx.transform.position.y + secondVfx.transform.position.y + thirdVfx.transform.position.y, 0);
        BloodMoon moonProj = moon.GetComponent<BloodMoon>();
        Destroy(firstVfx.gameObject, durationVfx);
        if (moonProj != null)
        {
            moonProj.Initialize(player, hitData, finalTarget, circlePosition, maxTimer);
        }
    }
}