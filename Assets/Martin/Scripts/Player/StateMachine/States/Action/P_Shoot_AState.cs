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
        {
            saveTargetPoint = player.LockOnTarget.CurrentTarget.position;
        }
        else
        {
            saveTargetPoint = player.GetViewPoint();
        }

        //Shoot if shooting if has resourses
        if (!player.PlayerStatsManager.CanConsume(player.ShootData.resourceType, player.ShootData.cost))
        {
            Debug.Log($"No hay {player.ShootData.resourceType} para {player.ShootData.name} [Shoot]");
            player.ChangeActionState(player.iddle_AState);
            return;
        }

        player.PlayerStatsManager.Consume(player.ShootData.resourceType, player.ShootData.cost);

        Debug.Log("Shoot State");
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;
        fireCooldown -= Time.deltaTime;

        if (!hasShoot && timer >= player.ShootData.shootTime)
        {
            Shoot();
            hasShoot = true;
        }

        if (player.Input.AttackBuffered && fireCooldown <= 0f)
        {
            player.ChangeActionState(player.shoot_AState);
        }

        if (timer >= player.ShootData.shootTime + 0.1f)
        {
            player.ChangeActionState(player.iddle_AState);
        }
    }

    public override void OnExit()
    {

    }

    private void Shoot()
    {
        fireCooldown = player.ShootData.timeBtwShot;

        player.Shoot(saveTargetPoint);
    }
}
