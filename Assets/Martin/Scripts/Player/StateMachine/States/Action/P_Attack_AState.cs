using UnityEngine;

public class P_Attack_AState : PlayerState
{
    public P_Attack_AState(PlayerController player) : base(player) { }

    int comboIndex;
    float timer;
    bool canCombo;
    bool hasHit;
    bool hasSpawnedVfx;
    public override void OnEnter()
    {
        comboIndex = 0;

        Debug.Log("Enter Basic Attack State");

        StartAttack();
    }

    public override void OnUpdate()
    {
        //if (player.Input.hasDashed && player.HasStamina(player.DashCost))
        if (player.Input.hasDashed)

        {
            player.ChangeActionState(player.dash_AState);
            return;
        }

        var attackSteps = player.NormalCombo.attackSteps[comboIndex];

        timer -= Time.deltaTime;

        float elapsed = attackSteps.duration - timer;

        if (elapsed < attackSteps.hitTime)
        {
            Vector2 inputDir = player.Input.moveInput.normalized;
            Vector3 moveDir = player.GetCameraRelativeDir(inputDir);

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                player.RotatePlayerModelToward(moveDir, attackSteps.turnSpeed);
            }
        }

        if (!hasSpawnedVfx && elapsed >= attackSteps.vfxSpawnTime)
        {
            //player.PlayAttackVfxLocal(comboIndex, true);
            //player.RequestAttackVfx(comboIndex, true);
            hasSpawnedVfx = true;
        }

        if (elapsed >= attackSteps.hitTime && !hasHit)
        {
            player.DoHit(comboIndex, true);
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

            if (comboIndex < player.NormalCombo.attackSteps.Length - 1)
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

        player.isPerformingAct = false;
        player.blockVelocity = false;
    }

    private void StartAttack()
    {
        var attackSteps = player.NormalCombo.attackSteps[comboIndex];

        timer = attackSteps.duration;
        canCombo = false;
        hasHit = false;
        hasSpawnedVfx = false;

        player.isPerformingAct = true;
        player.blockVelocity = true;

        Vector3? lockTargetPos = null;

        if (player.LockOnTarget != null &&
            player.LockOnTarget.isTargeting &&
            player.LockOnTarget.CurrentTarget != null)
        {
            lockTargetPos = player.LockOnTarget.CurrentTarget.position;
        }

        player.StartAttackMove(attackSteps, lockTargetPos);

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
