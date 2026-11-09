using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MoonBeamCollision : MonoBehaviour
{
    [Header("Damage")]
    public float radius;
    public LayerMask enemyLayer;
    private DamageInfo info;
    public GameObject damageSFX;
    public bool canRotate;
    public bool canMove;
    public bool canDmg;
    [SerializeField] GameObject sir;
    [SerializeField] List<Transform> tBeams;
    [SerializeField] float speedRotation;
    [SerializeField] float beamSpeed;
    private void Awake()
    {
    }
    public void Initialize(HitData hitData, PlayerControl player, GameObject sir, List<Transform> tBeams, float speedRotation, float beamSpeed)
    {
        this.sir = sir;
        this.tBeams = tBeams;
        this.speedRotation = speedRotation;
        this.beamSpeed = beamSpeed;
        info = new DamageInfo
        {
            damage = ((player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * hitData.physicalScale) + (player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * hitData.magicalScale)),
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
    private void Update()
    {
        if (canRotate)
        {
            sir.gameObject.transform.Rotate(Vector3.up, speedRotation * Time.deltaTime);
        }
        if (canMove)
        {
            foreach (Transform beam in tBeams)
            {
                beam.Translate(Vector3.forward * beamSpeed * Time.deltaTime, Space.Self);
            }

        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        //if (!canDmg) return;
        IDamageable dmg = other.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            Debug.Log("MoonBeamDmg");
            Vector3 dir = (other.transform.position - transform.position).normalized;

            dmg.TakeDamage(info);
            if (damageSFX != null)
            {
            }
        }
        

    }

    
    private void DealDamage()
    {

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        if (damageSFX != null)
        {
        }

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponentInParent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage(info);
            }
        }
    }
}
