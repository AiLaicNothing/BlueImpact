using UnityEngine;

public class P_Dash_AState : PlayerState
{
    public P_Dash_AState(PlayerControl player) : base(player) { }

    private float timer;
    private Vector3 dashDir;
    private float dashSpeed;
    private bool dashStarted;

    public override void OnEnter()
    {
        player.canDash = false;

        dashStarted = false;

        // Check stamina before starting the dash
        //if (!player.PlayerStatsManager.CanConsume(StatType.Estamina, (int)player.DashCost))
        //{
        //    Debug.Log("No hay stamina para dash");

        //    // Do NOT start the dash
        //    player.ChangeActionState(player.iddle_AState);
        //    return;
        //}

        if (!player._Stats.CanConsume(StatsType.Stamina, (int)player.DashCost))
        {
            Debug.Log($"No hay stamina para dash {player._Stats.CurrentStamina} / {(int)player.DashCost}");

            // Do NOT start the dash
            player.ChangeActionState(player.iddle_AState);
            return;
        }

        // Consume stamina only when the dash actually starts
        //player.PlayerStatsManager.Consume(StatType.Estamina, (int)player.DashCost);
        player._Stats.ConsumeStat(StatsType.Stamina, (int)player.DashCost);

        dashStarted = true;

        timer = player.DashDuration;
        dashSpeed = player.DashDistance / player.DashDuration;

        player.isPerformingAct = true;

        player.PlayAudio(player.dash, 1f);

        // Get dash direction
        Vector2 input = player.Input.moveInput;

        if (input.magnitude > 0.1f)
        {
            Vector3 camForward = player.MainCam.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = player.MainCam.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            dashDir = (camForward * input.y + camRight * input.x).normalized;
        }
        else
        {
            dashDir = player.Model.transform.forward;
            dashDir.y = 0f;
            dashDir.Normalize();
        }

        // Play animation
        if (player.Anim != null)
        {
            int dash = Animator.StringToHash("Dash");

            if (player.Anim.HasState(0, dash))
            {
                player.Anim.SetTrigger("Dashing");
            }
            else
            {
                Debug.Log("[PlayerAnimator] is missing Dash State");
            }
        }
    }

    public override void OnUpdate()
    {
        // If the dash never started, immediately leave
        if (!dashStarted)
            return;

        timer -= Time.deltaTime;

        // Preserve vertical velocity so gravity continues working
        float currentYVelocity = player.Rb.linearVelocity.y;

        Vector3 velocity = dashDir * dashSpeed;
        velocity.y = currentYVelocity;

        player.Rb.linearVelocity = velocity;

        if (timer <= 0f)
        {
            player.ChangeActionState(player.iddle_AState);
        }
        base.OnUpdate();
    }

    public override void OnExit()
    {
        if (!dashStarted)
            return;

        // Stop horizontal dash movement,
        // but KEEP vertical velocity/gravity.
        Vector3 velocity = player.Rb.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;

        player.Rb.linearVelocity = velocity;

        player.isPerformingAct = false;
        base.OnExit();

    }
}