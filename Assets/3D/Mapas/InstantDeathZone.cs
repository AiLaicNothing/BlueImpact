using UnityEngine;

/// <summary>
/// Collider (NO debe ser Trigger) que aplica daño masivo al player
/// apenas colisiona con él, matándolo a través del flujo normal de TakeDamage/IDamageable.
/// Pensado para zonas de agua, lava, abismos, etc.
/// </summary>
public class InstantDeathZone : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float damageAmount = 99999f;

    private void Reset()
    {
        // Asegura que el collider NO sea trigger apenas se agrega el componente
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        DamageInfo info = new DamageInfo
        {
            damage = damageAmount
        };

        damageable.TakeDamage(info);
    }
}