using UnityEngine;

public class P_Skill_AState : PlayerState
{
    public P_Skill_AState(PlayerControl player) : base(player) { }
    Skill currentSkill;
    int skillIndex;

    float timer;
    bool isCasting = true;

    Vector3 saveTargetPoint;
    public override void OnEnter()
    {
        //--> Check thatk skill is not null
        if (currentSkill == null)
        {
            player.ChangeActionState(player.iddle_AState);
            return;
        }

        //--> Check that the is not in cooldown
        if (!player.IsSkillReady(skillIndex))
        {
            player.ChangeActionState(player.iddle_AState);
            return;
        }

        //--> Check if the player has enough resources to cast
        if (!player.PlayerStatsManager.CanConsume(currentSkill.resourceType, currentSkill.cost))
        {
            Debug.Log($"No hay {currentSkill.resourceType} para {currentSkill.name} [Skill]");
            player.ChangeActionState(player.iddle_AState);
            return;
        }
        Debug.Log($"Player has {player.PlayerStatsManager.GetActualValue(StatType.Mana)}");

        player.PlayerStatsManager.Consume(currentSkill.resourceType, currentSkill.cost);
        Debug.Log($"Skill consume {currentSkill.resourceType} {currentSkill.cost}");

        Debug.Log($"Player has now {player.PlayerStatsManager.GetActualValue(StatType.Mana)}");
        player.isPerformingAct = true;

        isCasting = true;
        timer = currentSkill.castTime;

        saveTargetPoint = player.GetViewPoint();

        //--> call animator and play the animation name of casting
        //player.Animator.Play(currentSkill.castAnimation);
    }

    public override void OnUpdate()
    {
        //--> If casting
        if (isCasting)
        {
            //--> If player dash interrupt and cancel the skill casting
            if (player.Input.hasDashed)
            {
                player.ChangeActionState(player.dash_AState);
                return;
            }
        }

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            if (isCasting)
            {
                isCasting = false;

                //player.Animator.Play(currentSkill.actionAnimation);

                // Execute skill logic
                player.UseSkill(skillIndex, saveTargetPoint);

                // Set cooldown
                player.TriggerCooldown(skillIndex);

                // Optional: short action duration
                // This is the duration of the casting???
                timer = currentSkill.actionTime;
            }
            else
            {
                player.ChangeActionState(player.iddle_AState);
            }
        }
    }

    public override void OnExit()
    {
        player.isPerformingAct = false;
        player.blockVelocity = false;
    }

    public void SetSkill(Skill desiredSkill, int index)
    {
        currentSkill = desiredSkill;
        skillIndex = index;
    }
}
