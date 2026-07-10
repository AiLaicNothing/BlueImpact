using System.Collections;
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

    private Coroutine damageRoutine;

    private void OnEnable()
    {
        damageRoutine = StartCoroutine(DamageRoutine());
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

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent(out IDamageable damageable)) continue;

            Vector3 hitDir = (hit.transform.position - transform.position).normalized;

            DamageInfo info = new DamageInfo
            {
                damage = damage,
            };

            damageable.TakeDamage(info);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}  

