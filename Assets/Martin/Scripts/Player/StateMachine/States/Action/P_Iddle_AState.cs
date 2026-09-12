using UnityEngine;

public class P_Iddle_AState : PlayerState
{
    public P_Iddle_AState(PlayerControl player) : base(player) { }
    public override void OnEnter()
    {
        Debug.Log("Enter Iddle Action State");

        if (player.Anim != null)
        {
            int empty = Animator.StringToHash("Empty");

            if (player.Anim.HasState(1, empty))
            {
                player.Anim.Play(empty);
            }
            else
            {
                Debug.Log("[PlayerAnimator] is missing idle State");
            }
        }
    }
    bool isProjecting;
    int trueIndex = -1;
    public override void OnUpdate()
    {
        //if (player.Input.hasDashed && player.HasStamina(player.DashCost))

        if (player.Input.hasDashed)
        {
            player.ChangeActionState(player.dash_AState);
            return;
        }

        if (player.Input.AttackBuffered)
        {
            var type = player.Input.bufferedAttackType;

            player.Input.UseAttackBufer();

            //--> Input [F] melee atatack for both of them [Melee/Range]
            if (type == AttackInputType.Melee)
            {
                //--> If is not touching the ground and has not attacked in the air
                if (!player.IsGrounded && !player.hasUsedAirAttack)
                {
                    //-->Do airAttack
                    player.ChangeActionState(player.airAttackAState);
                }
                else if (player.IsGrounded)
                {
                    //-->Do grounded attack
                    player.ChangeActionState(player.attack_AState);
                }
            }

            //--> Input is mouse left button
            if (type == AttackInputType.Primary)
            {
                //--> If player is range do shoot else do melee attack
                if (player.IsRange)
                {
                    player.ChangeActionState(player.shoot_AState);
                }
                else
                {
                    if (!player.IsGrounded && !player.hasUsedAirAttack)
                    {
                        player.ChangeActionState(player.airAttackAState);
                    }
                    else if (player.IsGrounded)
                    {
                        player.ChangeActionState(player.attack_AState);
                    }
                }
                return;

            }

        }
        int index = player.Input.skillPressedIndex;
        if (isProjecting)
        {
            var skill = player.GetSkill(trueIndex);
            if (player.Input.skillCanceled)
            {
                player.skill_AState.SetSkill(skill, trueIndex);
                player.blockVelocity = true;
                player.ChangeActionState(player.skill_AState);
                trueIndex = -1;
                isProjecting = false;
            }
            else
            {
                skill.UpdateProjectionPosition(player);
            }
            return;
        }

        if (index != -1)
        {
            trueIndex = index;
            Debug.Log($"Player: Try change to skill state {index}");
            var skill = player.GetSkill(index);

            if (skill != null && player.IsSkillReady(index))
            {
                if (skill.haveProjection)
                {
                    skill.CreateProjection(player);
                    skill.UpdateProjectionPosition(player);
                    isProjecting = true;
                    return;
                }
                player.skill_AState.SetSkill(skill, index);
                player.ChangeActionState(player.skill_AState);
                return;
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}
