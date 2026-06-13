using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Blood Moon")]
public class BloodMoon_Skill : Skill
{
    [SerializeField] private GameObject moonPrefab;

    [Header("Spawn")]
    [SerializeField] private float maxRange = 20f;
    [SerializeField] private float height;
    [SerializeField] private Vector3 offset;
    [SerializeField] private HitData hitData;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        SummonMoon(player, targetPoint, lockTargetPos);
    }

    private void SummonMoon(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;

        //Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        //// clamp range
        //Vector3 dir = (finalTarget - player.transform.position);
        //float dist = dir.magnitude;

        //if (dist > maxRange)
        //{
        //    finalTarget = player.transform.position + dir.normalized * maxRange;
        //}

        Vector3 spawnPos = player.transform.position + player.Model.forward * offset.z + player.Model.right * offset.x + Vector3.up * height;

        GameObject moon = Instantiate(moonPrefab, spawnPos, Quaternion.identity);

        BloodMoon moonProj = moon.GetComponent<BloodMoon>();

        if (moonProj != null)
        {
            moonProj.Initialize(player, hitData, targetPoint);
        }
    }
}
