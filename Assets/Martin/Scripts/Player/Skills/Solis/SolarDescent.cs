using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Solar Descent")]
public class SolarDescent : Skill
{
    public HitData hitData;
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";
    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {

    }

    void Jump()
    {
        
    }
}
