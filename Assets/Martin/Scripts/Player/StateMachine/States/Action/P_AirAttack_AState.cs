using UnityEngine;

public class P_AirAttack_AState : PlayerState
{
    public P_AirAttack_AState(PlayerControl player) : base(player) { }

    int comboIndex;
    float timer;
    bool canCombo;
    bool hasHit;

    public override void OnEnter()
    {
        player.isPerformingAct = true;

        player.blockVelocity = true;

        comboIndex = 0;

        player.hasUsedAirAttack = true;

        Vector3 vel = player.Rb.linearVelocity;
        vel.y = 0;
        player.Rb.linearVelocity = vel;

        //This can be change
        player.Rb.useGravity = false;

        StartAttack();

        //-->Play animation here
        Debug.Log("Enter Air Attack State");
    }

    public override void OnUpdate()
    {
        //if (player.Input.hasDashed && player.HasStamina(player.DashCost))
        if (player.Input.hasDashed)
        {
            player.ChangeActionState(player.dash_AState);
            return;
        }
        var attackSteps = player.AirCombo.attackSteps[comboIndex];

        timer -= Time.deltaTime;

        float elapsed = attackSteps.duration - timer;

        if (elapsed >= attackSteps.hitTime && !hasHit)
        {
            player.DoHit(comboIndex, false);
            hasHit = true;
        }

        //--> Check if it can continue combo / recive input window to continue
        if (elapsed >= attackSteps.comboWindowStart && elapsed <= attackSteps.comboWindowEnd)
        {
            canCombo = true;
        }

        //--> Check if recive input to continue combo
        if (canCombo && player.Input.AttackBuffered)
        {
            var type = player.Input.bufferedAttackType;

            if (!CheckMeleeInput(type))
            {
                return;
            }

            player.Input.UseAttackBufer();

            if (comboIndex < player.AirCombo.attackSteps.Length - 1)
            {
                comboIndex++;
                StartAttack();
                return;
            }
        }

        //--> The attack end when the window to continue combo end
        if (elapsed > attackSteps.comboWindowEnd)
        {
            player.ChangeActionState(player.iddle_AState);
        }
    }

    public override void OnExit()
    {
        player.Rb.useGravity = true;

        player.isPerformingAct = false;

        player.blockVelocity = false;
    }

    private void StartAttack()
    {
        var attackSteps = player.AirCombo.attackSteps[comboIndex];

        timer = attackSteps.duration;
        canCombo = false;
        hasHit = false;

        player.isPerformingAct = true;
        player.blockVelocity = true;

        //player.PlayAttackVfxLocal(comboIndex, true);
        //player.RequestAttackVfx(comboIndex, true);

        //--> Play animation
        Debug.Log($"Player attacked with {attackSteps.name}");

    }

    private bool CheckMeleeInput(AttackInputType type)
    {
        if (type == AttackInputType.Melee)
        {
            return true;
        }

        if (!player.IsRange && type == AttackInputType.Primary)
        {
            return true;
        }

        return false;
    }
}
