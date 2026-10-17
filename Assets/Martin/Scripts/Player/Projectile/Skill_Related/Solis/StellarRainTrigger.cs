using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class StellarRainTrigger : MonoBehaviour
{
    public GameObject crystalsImpact;
    public GameObject parent;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    [Header("Magic Circle Configurations")]
    [SerializeField] Image startCircle;
    private ParticleSystem floeParticles;
    [SerializeField] float duration;
    private float currentOffset = 0f;
    private float timer = 0f;
    private float timer2 = 0f;
    [Header("Damage")]
    public float radius;
    public LayerMask enemyLayer;
    private DamageInfo info;
    public GameObject damageSFX;
    private void Awake()
    {
        floeParticles = GetComponent<ParticleSystem>();
        floeParticles.Stop();
        startCircle.fillAmount = 0;
        timer = duration;
    }
    public void Initialize(HitData hitData, PlayerControl player)
    {
        floeParticles.Stop();
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

    void Update()
    {
        UpdateMagicCircle();
    }

    private void AciveFloe()
    {
        floeParticles.Play();
    }
    private void UpdateMagicCircle()
    {
        if (!floeParticles.isEmitting)
        {
            timer2 += Time.deltaTime;
            startCircle.fillClockwise = false;
            startCircle.fillAmount = timer2 / 0.5f;

            Invoke(nameof(AciveFloe), 0.5f);
            return;
        }
        timer -= Time.deltaTime;
        startCircle.fillClockwise = true;
        startCircle.fillAmount = timer / duration;
        Destroy(parent, duration);
    }


    public void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = floeParticles.GetCollisionEvents(other, collisionEvents);
        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 collisionPoint = collisionEvents[i].intersection;

            Vector3 collisionNormal = collisionEvents[i].normal;
            Quaternion rotation = Quaternion.LookRotation(collisionNormal);

            if (crystalsImpact != null)
            {
                GameObject crystals = Instantiate(crystalsImpact, collisionPoint, rotation);

                Destroy(crystals, 1f);
            }
        }
            
        IDamageable dmg = other.GetComponent<IDamageable>();
        if (dmg != null)
        {
            Debug.Log("crystalDmg");
            Vector3 dir = (other.transform.position - transform.position).normalized;

            dmg.TakeDamage(info);
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
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage(info);
            }
        }
    }
}
