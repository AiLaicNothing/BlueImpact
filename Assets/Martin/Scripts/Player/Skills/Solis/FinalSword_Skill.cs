using UnityEngine;
using System.Collections;
[CreateAssetMenu(menuName = "Player/Skills/Solis/Final Sword")]
public class FinalSword_Skill : Skill
{
    [Header("Damage")]
    public HitData hitData;
    [Header("Prefab")]
    public GameObject swordPrefab;
    [Header("Spawn")]
    [SerializeField]
    private Vector3 spawnOffSet;
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";
    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;
        Vector3 spawnPos = player.Model.position + player.Model.forward * spawnOffSet.z + Vector3.up * spawnOffSet.y;
        Vector3 dir = (finalTarget - spawnPos).normalized;

        if (dir.sqrMagnitude < 0.0001f) dir = player.Model.forward;
        dir.x = player.transform.position.x;
        Quaternion rot = Quaternion.LookRotation(dir);
        GameObject finalSwordPrefab = Instantiate(swordPrefab, spawnPos, rot);
        FinalSwordPrefab fSword = finalSwordPrefab.GetComponent<FinalSwordPrefab>();
        if(fSword != null)
        {
            fSword.Initialize(player, dir, Vector3.zero, hitData);
        }
    }
}
