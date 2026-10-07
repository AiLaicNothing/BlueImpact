using UnityEngine;
using System.Collections;

public class FinalSwordPrefab : MonoBehaviour
{
    [Header("Vfx")]
    public GameObject swordVfx;
    public GameObject explotionPrefab;
    public Animator anim;
    public ParticleSystem auraVfx;
    [Header("Values")]
    public float chargeTime = 1.5f;
    public float speed = 90f;
    public float radius = 4f;
    public bool canFinish;
    public bool trueFinish;
    float timer;
    [Header("Layer")]
    public LayerMask enemyLayer;
    
    [Header("Dmg")]
    [HideInInspector]public PlayerControl player;
    [HideInInspector] public HitData hitData;
    [HideInInspector] public DamageInfo info;
    [HideInInspector] public Vector3 direction;
    float extraDmg;
    private bool doingDmg = false;
    public void Initialize(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos, HitData hitData)
    {
        StartCoroutine(ExecuteSkill(player, targetPoint, hitData));
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if(timer < 2.2f && player != null)
        {
            player.blockVelocity = true;
            player.ChangeActionState(player.skill_AState);
        }
    }
    IEnumerator ExecuteSkill(PlayerControl player, Vector3 targetPoint, HitData hitData)
    {
        player.blockVelocity = true;
        swordVfx.SetActive(true);
        anim.enabled = true;
        anim.Play("FinalSwordAnimation");
        auraVfx.Play();
        ChangeDmgInfo(player, targetPoint, hitData);
        DoDmg(radius);
        yield return new WaitForSeconds(chargeTime);
        this.player.blockVelocity = true;
        this.player.ChangeActionState(this.player.skill_AState);
        auraVfx.Stop();
        while (!trueFinish) 
        {
            yield return null;
        }
        //swordVfx.SetActive(false);
        player.blockVelocity = true;
        GameObject explotionVfx = Instantiate(explotionPrefab, swordVfx.transform.position,
            new Quaternion(explotionPrefab.transform.rotation.x, transform.rotation.y, explotionPrefab.transform.rotation.z, transform.rotation.w));
        Destroy(explotionVfx, 3);

        ChangeDmgInfo(player, targetPoint, hitData);
        DoDmg(radius - 2.2f);
        Destroy(gameObject, 3);
        this.player.blockVelocity = false;
        this.player.ChangeActionState(this.player.iddle_AState);
    }
    void DoDmg(float radius)
    {
        Collider[] hits = Physics.OverlapBox(transform.position, new Vector3(radius / 2, radius * 2, radius /2), Quaternion.identity, enemyLayer);
        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(info);
            }
        }
    }
    public void ChangeDmgInfo(PlayerControl player, Vector3 dir, HitData hitdata)
    {
        hitData = hitdata;
        this.player = player;
        direction = dir.normalized;
        info = new DamageInfo
        {
            damage = ((player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * (hitData.physicalScale + extraDmg)) + (player.PlayerStatsManager.GetActualValue(StatType.DañoMágico) * hitData.magicalScale)),
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
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position,new Vector3(radius / 2, radius * 2, radius / 2));
    }
    public void CanFinish()
    {
        canFinish = true;
    }
}
