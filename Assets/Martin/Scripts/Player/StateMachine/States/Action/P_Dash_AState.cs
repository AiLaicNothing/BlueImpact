using UnityEngine;

public class P_Dash_AState : PlayerState
{
    public P_Dash_AState(PlayerControl player) : base(player) { }

    private float timer;
    private Vector3 dashDir;
    private float dashSpeed;

    public override void OnEnter()
    {
        if (!player.PlayerStatsManager.CanConsume(StatType.Stamina, (int)player.DashCost))
        {
            Debug.Log("No hay stamina para dash");
            player.ChangeActionState(player.iddle_AState);
            return;
        }

        player.PlayerStatsManager.Consume(StatType.Stamina, (int)player.DashCost);

        timer = player.DashDuration;
        dashSpeed = player.DashDistance / player.DashDuration;
        player.isPerformingAct = true;

        Vector2 input = player.Input.moveInput;
        if (input.magnitude > 0.1f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0;
            camRight.Normalize();

            dashDir = (camForward * input.y + camRight * input.x).normalized;
        }
        else
        {
            dashDir = player.Model.transform.forward;
        }

        player.Rb.useGravity = false;
    }

    public override void OnUpdate()
    {
        timer -= Time.deltaTime;

        Vector3 velocity = dashDir * dashSpeed;
        velocity.y = 0;
        player.Rb.linearVelocity = velocity;

        if (timer <= 0)
        {
            player.ChangeActionState(player.iddle_AState);
        }
    }

    public override void OnExit()
    {
        player.Rb.useGravity = true;
        player.isPerformingAct = false;
    }
}