using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WitheredZone : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float tickRate = 0.5f;

    [Header("Area")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Ralentización del jugador (1 = sin efecto)")]
    [SerializeField, Range(0.1f, 1f)] private float playerSpeedMultiplier = 0.8f;
    [SerializeField] private float slowRefreshTime = 0.1f; // se refresca cada frame; expira casi al salir de la zona

    private Coroutine damageRoutine;
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
    private readonly Collider[] slowBuffer = new Collider[8];

    // Lo llama el boss justo después de instanciar la zona (así cada ataque define su propio daño y duración).
    public float Radius => radius;

    public void Configure(float damagePerTick, float lifetime, float tick)
    {
        damage = damagePerTick;
        duration = lifetime;
        tickRate = Mathf.Max(0.05f, tick);
    }

    private void OnEnable()
    {
        // Evita rutinas duplicadas si el objeto se reactiva
        if (damageRoutine != null) StopCoroutine(damageRoutine);
        damageRoutine = StartCoroutine(DamageRoutine());
    }

    private void Update()
    {
        if (playerSpeedMultiplier >= 1f) return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, slowBuffer, targetLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            PlayerControl player = slowBuffer[i].GetComponentInParent<PlayerControl>();
            if (player != null) player.ApplySlow(playerSpeedMultiplier, slowRefreshTime);
        }
    }

    private IEnumerator DamageRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickRate);

            elapsed += tickRate;
            DealDamage();
        }

        Destroy(gameObject);
    }

    private void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers, QueryTriggerInteraction.Ignore);

        damagedTargets.Clear();

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            // Un objetivo con varios colliders recibe solo un tick de daño
            if (damageable == null || !damagedTargets.Add(damageable)) continue;

            damageable.TakeDamage(new DamageInfo { damage = damage });
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
