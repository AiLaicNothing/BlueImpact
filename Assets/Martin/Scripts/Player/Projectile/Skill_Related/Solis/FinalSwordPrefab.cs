using UnityEngine;
using System.Collections;

public class FinalSwordPrefab : MonoBehaviour
{
    [Header("Vfx")]
    public GameObject swordVfx;
    public ParticleSystem auraVfx;
    public ParticleSystem finalExplotionVfx;

    [Header("Values")]
    public float chargeTime = 1.5f;
    public float speed = 90f;
    public float radius = 4f;

    [Header("Layer")]
    public LayerMask enemyLayer;
    
    [Header("Dmg")]
    private PlayerControl player;
    private HitData hitData;
    private DamageInfo info;
    private Vector3 direction;
    float extraDmg;
    private bool doingDmg = false;
    public void Initialize(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos, HitData hitData)
    {
        StartCoroutine(ExecuteSkill(player, targetPoint, hitData));
    }

    IEnumerator ExecuteSkill(PlayerControl player, Vector3 targetPoint, HitData hitData)
    {
        swordVfx.SetActive(true);
        if (auraVfx) auraVfx.Play();
        ChangeDmgInfo(player, targetPoint, hitData);
        DoDmg(radius);
        yield return new WaitForSeconds(chargeTime); 
        if (auraVfx) auraVfx.Stop();
        float anguloRotado = 0f;
        while (anguloRotado < 90f) 
        {
            float paso = speed * Time.deltaTime;
            transform.Rotate(Vector3.right, paso);
            anguloRotado += paso;
            yield return null;
        }
        swordVfx.SetActive(false);
        if (finalExplotionVfx) finalExplotionVfx.Play();

        ChangeDmgInfo(player, targetPoint, hitData);
        DoDmg(radius * 1.5f);
    }
    void DoDmg(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(player.Model.position, radius, enemyLayer);
        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(info);
            }
        }
    }
    void ChangeDmgInfo(PlayerControl player, Vector3 dir, HitData hitdata)
    {
        hitData = hitdata;
        this.player = player;
        direction = dir.normalized;
        info = new DamageInfo
        {
            damage = (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Physical_Damage) * hitData.physicalScale) + (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Magical_Damage) * hitData.magicalScale),
            hitDirection = transform.forward,
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
        extraDmg += 1;
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == enemyLayer)
        {
            ChangeDmgInfo(player, direction, hitData);
            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(info);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
