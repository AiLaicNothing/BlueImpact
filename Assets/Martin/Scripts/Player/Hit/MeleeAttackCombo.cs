using UnityEngine;

[CreateAssetMenu(menuName = ("Player/Basic Combo Data"))]
public class MeleeAttackCombo : ScriptableObject
{
    public AttackStep[] attackSteps;
}
