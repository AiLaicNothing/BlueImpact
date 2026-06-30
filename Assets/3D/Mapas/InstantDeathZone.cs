using UnityEngine;

/// <summary>
/// Collider (debe estar marcado como Trigger) que aplica daño masivo al player
/// apenas lo toca, matándolo a través del flujo normal de TakeDamage/IDamageable.
/// Pensado para zonas de agua, lava, abismos, etc.
/// </summary>
public class InstantDeathZone : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float damageAmount = 99999f;

    private void Reset()
    {
        // Asegura que el collider sea trigger apenas se agrega el componente
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        DamageInfo info = new DamageInfo
        {
            damage = damageAmount
        };

        damageable.TakeDamage(info);
    }
}