using UnityEngine;

/// <summary>
/// Put this on the child GameObject that has the Animator (only needed when the Animator
/// is NOT on the same GameObject as the Enemy). Animation Events on the clips call these
/// methods and they are forwarded to the Enemy.
///
/// Generic:   AnimEvent(string)  -> "Hit", "Shoot", "End"
/// Shortcuts: Hit(), DoHit(), Shoot(), EndAttack()
/// </summary>
public class EnemyAnimationEvents : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
            Debug.LogWarning($"[EnemyAnimationEvents] {name}: no Enemy found in parents.", this);
    }

    public void AnimEvent(string id) => Forward(id);

    public void Hit() => Forward(Enemy.EvtHit);
    public void DoHit() => Forward(Enemy.EvtHit);
    public void Shoot() => Forward(Enemy.EvtShoot);
    public void EndAttack() => Forward(Enemy.EvtEnd);

    // Old event names already placed on existing clips (Automata / FlyingGolem).
    public void ShootBurst() => Forward(Enemy.EvtShoot);
    public void ShootSpread() => Forward(Enemy.EvtShoot);
    public void FireMagicVolley() => Forward(Enemy.EvtShoot);
    public void FireMagicBullet() => Forward(Enemy.EvtShoot);
    public void ThrowBomb() => Forward(Enemy.EvtShoot);
    // Now automatic in the scripts; kept so Unity doesn't log "no receiver".
    public void SpawnMagicCircles() { }
    public void SpawnChargeVfx() { }

    private void Forward(string id)
    {
        // If the Animator lives on the Enemy itself, Unity already calls the Enemy directly.
        if (enemy == null || enemy.gameObject == gameObject) return;
        enemy.AnimEvent(id);
    }
}
