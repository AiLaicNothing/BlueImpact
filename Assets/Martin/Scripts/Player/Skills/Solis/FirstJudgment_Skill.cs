using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/First Judgment")]
public class FirstJudgment_Skill : Skill
{
    [Header("Prefab")]
    public GameObject swordPrefab;

    public HitData hitData;

    [Header("Targeting")]
    public float maxRange = 20f;
    public float spawnHeight = 15f;

    [Header("SFX")]
    public GameObject spawnSFX;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        SummonSword(player, targetPoint, lockTargetPos);
    }

    private void SummonSword(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        // clamp range
        Vector3 dir = (finalTarget - player.transform.position);
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            finalTarget = player.transform.position + dir.normalized * maxRange;
        }

        // spawn above target
        Vector3 spawnPos = finalTarget + Vector3.up * spawnHeight;

        GameObject sword = Instantiate(swordPrefab, spawnPos, Quaternion.identity);

        FirstJudgmentSword proj = sword.GetComponent<FirstJudgmentSword>();

        proj.Initialize(hitData, player);

        if (spawnSFX != null)
        {
        }
    }
}
