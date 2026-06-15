using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Magic Bullet")]
public class MagicBullet_Skill : Skill
{
    [Header("Prefabs")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private GameObject proyectilePrefab;

    [Header("Offsets")]
    [SerializeField] private float backOffset = 1.5f;
    [SerializeField] private float heightOffset = 2f;
    [SerializeField] private float sideOffset = 2f;

    [Header("Shooting")]
    [SerializeField] private float speed;
    [SerializeField] private int proyectilePerNode = 2;
    [SerializeField] private float timeBtwShoot;

    [SerializeField] private HitData hitData;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        SummonBullets(player, targetPoint, lockTargetPos);
    }

    private void SummonBullets(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 basePos = player.transform.position - player.Model.forward * backOffset + player.Model.up * heightOffset;

        Vector3 left = basePos - player.Model.right * sideOffset;
        Vector3 middle = basePos;
        Vector3 right = basePos + player.Model.right * sideOffset;

        player.StartCoroutine(CastSkill(player, left, targetPoint, lockTargetPos));
        player.StartCoroutine(CastSkill(player, middle, targetPoint, lockTargetPos));
        player.StartCoroutine(CastSkill(player, right, targetPoint, lockTargetPos));
    }

    private IEnumerator CastSkill(PlayerControl player, Vector3 pos, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, pos, player.Model.rotation);
        }

        yield return new WaitForSeconds(0.2f);

        Vector3 finalTarget;

        if (lockTargetPos != Vector3.zero)
        {
            finalTarget = lockTargetPos;
        }
        else
        {
            finalTarget = targetPoint;
        }

        Vector3 baseDir = (finalTarget - pos).normalized;

        for (int i = 0; i < proyectilePerNode; i++)
        {
            GameObject proj = Instantiate(proyectilePrefab, pos, Quaternion.identity);

            var proyectile = proj.GetComponent<P_Projectile>();

            if (proyectile != null)
            {

                proyectile.Initialize(hitData, player, baseDir, speed, finalTarget);
            }

            yield return new WaitForSeconds(timeBtwShoot);
        }
    }

}
