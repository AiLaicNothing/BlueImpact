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
    public float dmgTickRate = 0.70f;
    public float speed = 90f;
    public float radius = 4f;
    public Vector3 offSet;
    public Vector3 dmgRadius;
    public bool canFinish;
    public bool trueFinish;
    float timer;
    [Header("Debug")]
    [SerializeField] bool debug;
    [SerializeField] Vector3 lastCenter;
    [SerializeField] Vector3 lastRadius;
    [SerializeField] Quaternion lastRot;
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
        DoDmg(new Vector3(radius / 2, radius * 2, radius / 2), transform.position, transform.rotation);
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
        DoDmg(dmgRadius, explotionVfx.transform.position + new Vector3(0,offSet.y,0),
            new Quaternion(explotionPrefab.transform.rotation.x, transform.parent.rotation.y, explotionPrefab.transform.rotation.z, transform.parent.rotation.w));
        FinalDmgInfo(player, targetPoint, hitData);
        StartCoroutine(FinalDmg(explotionVfx));
        Destroy(transform.parent.gameObject, 3);
        this.player.blockVelocity = false;
        this.player.ChangeActionState(this.player.iddle_AState);
    }
    IEnumerator FinalDmg(GameObject explotionVfx)
    {
        yield return null;
        while (true)
        {
            DoDmg(dmgRadius, explotionVfx.transform.position + new Vector3(0, offSet.y, 0),
                new Quaternion(explotionPrefab.transform.rotation.x, transform.parent.rotation.y, explotionPrefab.transform.rotation.z, transform.parent.rotation.w));
            yield return new WaitForSeconds(dmgTickRate);

        }
    }
    void DebuggingCollider(Vector3 radius, Vector3 offSet, Quaternion rot)
    {
        if (debug)
        {
            Vector3 center =  offSet;
            lastCenter = center;
            lastRadius = radius;
            lastRot = rot;
        }
    }
    void DoDmg(Vector3 radius, Vector3 offSet, Quaternion rot)
    {
        Vector3 center = transform.position + offSet;
        DebuggingCollider(radius, offSet, rot);
        Collider[] hits = Physics.OverlapBox(offSet, radius, rot, enemyLayer);
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
            damage = (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Physical_Damage) * hitData.physicalScale + extraDmg) + (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Magical_Damage) * hitData.magicalScale),
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
    public void FinalDmgInfo(PlayerControl player, Vector3 dir, HitData hitdata)
    {
        hitData = hitdata;
        this.player = player;
        direction = dir.normalized;
        info = new DamageInfo
        {
            damage = (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Physical_Damage) * hitData.physicalScale / 2) + (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Magical_Damage) * hitData.magicalScale / 2),
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
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + offSet, dmgRadius);
        if (debug)
        {
            Matrix4x4 originalMatrix = Gizmos.matrix;

            Gizmos.color = Color.green;

            Gizmos.matrix = Matrix4x4.TRS(lastCenter, lastRot, Vector3.one);

            Gizmos.DrawCube(Vector3.zero, lastRadius * 2f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, lastRadius * 2f);

            Gizmos.matrix = originalMatrix;
        }
    }
    public void CanFinish()
    {
        canFinish = true;
    }
    public void CanTrueFinish()
    {
        trueFinish = true;
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
