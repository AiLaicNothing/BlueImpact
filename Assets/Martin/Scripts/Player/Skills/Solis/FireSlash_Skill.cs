using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Fire Slash")]
public class FireSlash_Skill : Skill
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float proyectileSpeed;
    [SerializeField] private float damage;
    [SerializeField] private HitData hitData;

    [Header("Spawn")]
    [SerializeField] private Vector3 spawnOffSet = new Vector3(0, 0, 1.5f);
    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        Vector3 spawnPos = player.Model.position + player.Model.forward * spawnOffSet.z + Vector3.up * spawnOffSet.y;

        Vector3 dir = (finalTarget - spawnPos).normalized;

        if (dir.sqrMagnitude < 0.0001f) dir = player.Model.forward;

        Quaternion rot = Quaternion.LookRotation(dir);

        GameObject prefab = Object.Instantiate(projectilePrefab, spawnPos, rot);

        P_Projectile proyectile = prefab.GetComponent<P_Projectile>();

        if (proyectile != null)
        {
            proyectile.Initialize(10, hitData, dir, proyectileSpeed, Vector3.zero);
        }
    }

}
