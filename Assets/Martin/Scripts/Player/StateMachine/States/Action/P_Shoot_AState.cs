using UnityEngine;

public class P_Shoot_AState : PlayerState
{
    public P_Shoot_AState(PlayerControl player) : base(player) { }

    float timer;
    float fireCooldown;
    bool hasShoot;

    private Vector3 saveTargetPoint;

    public override void OnEnter()
    {
        timer = 0f;
        fireCooldown = 0f;
        hasShoot = false;

        if (player.LockOnTarget != null && player.LockOnTarget.isTargeting && player.LockOnTarget.CurrentTarget != null)
            saveTargetPoint = player.LockOnTarget.CurrentTarget.position;
        else
            saveTargetPoint = player.GetViewPoint();

        // Only CHECK here, don't consume yet
        if (!player._Stats.CanConsume(player.ShootData.statsType, player.ShootData.cost))
        {
            Debug.Log($"No hay {player.ShootData.resourceType} para {player.ShootData.name} [Shoot]");
            player.ChangeActionState(player.iddle_AState);
            return;
        }

        Debug.Log("Shoot State");
        player.Anim.SetTrigger("Shoot");
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;
        fireCooldown -= Time.deltaTime;

        if (!hasShoot && timer >= player.ShootData.shootTime)
        {
            hasShoot = true;
            if (!Shoot())
            {
                player.ChangeActionState(player.iddle_AState);
                return;
            }
        }

        // Only chain into the next shot AFTER this one has fired and the cooldown ended
        if (hasShoot && fireCooldown <= 0f && player.Input.AttackBuffered)
        {
            player.ChangeActionState(player.shoot_AState);
            return;
        }

        if (timer >= player.ShootData.shootTime + 0.1f && player.IsGrounded())
        {
            player.ChangeActionState(player.iddle_AState);
            return;
        }

        base.OnUpdate();
    }

    private bool Shoot()
    {
        // Re-check in case the resource was spent during the wind-up
        if (!player._Stats.CanConsume(player.ShootData.statsType, player.ShootData.cost))
            return false;

        player._Stats.ConsumeStat(player.ShootData.statsType, player.ShootData.cost);

        fireCooldown = player.ShootData.timeBtwShot;
        player.Shoot(saveTargetPoint);
        return true;
    }
}

