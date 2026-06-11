using UnityEngine;

public class P_Dash_AState : PlayerState
{
    public P_Dash_AState(PlayerControl player) : base(player) { }

    float timer;
    Vector3 dashDir;
    float dashSpeed;
    public override void OnEnter()
    {
        Debug.Log("Enter Dash State");

        //if (!player.HasStamina(player.DashCost))
        //{
        //    player.ChangeActionState(player.iddeAction_State);
        //    return;
        //}

        //player.ConsumeStamina(player.DashCost);

        // AUDIO CENTRALIZADO
        //player.PlayDashAudio();

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

        // Maintain constant dash velocity
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

        Debug.Log("Exit Dash State");
    }
}
