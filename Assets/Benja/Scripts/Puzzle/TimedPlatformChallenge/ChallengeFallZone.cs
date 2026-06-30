using UnityEngine;

public class ChallengeFallZone : MonoBehaviour
{
    [Header("Challenge")]
    [Tooltip("Challenge que se reseteará cuando el jugador caiga en esta zona.")]
    [SerializeField]
    private TimedPlatformChallenge challenge;

    [Header("Damage")]
    [Tooltip("Daño aplicado al caer. Debe ser mayor o igual a la vida máxima del jugador para garantizar la muerte.")]
    [SerializeField]
    private float fallDamage = 99999f;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ChallengeFallZone] Trigger activado por: {other.name}");

        PlayerControl player =
            other.GetComponentInParent<PlayerControl>();

        if (player == null)
        {
            Debug.LogWarning($"[ChallengeFallZone] '{other.name}' no tiene PlayerControl en sí mismo ni en sus padres.");
            return;
        }

        Debug.Log($"[ChallengeFallZone] PlayerControl encontrado en: {player.name}");

        IDamageable damageable = player as IDamageable;

        if (damageable == null)
        {
            Debug.LogWarning("[ChallengeFallZone] PlayerControl no implementa IDamageable (revisar).");
            return;
        }

        DamageInfo info = new DamageInfo
        {
            damage = fallDamage
        };

        Debug.Log($"[ChallengeFallZone] Aplicando {fallDamage} de daño.");

        damageable.TakeDamage(in info);

        // El respawn en el último checkpoint lo maneja PlayerControl.OnDead() -> RespawnManager.

        if (challenge != null)
        {
            challenge.ResetChallenge();
        }
    }
}