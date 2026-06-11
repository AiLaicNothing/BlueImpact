using UnityEngine;

[CreateAssetMenu(menuName = ("Enemy/Stats"))]
public class EnemyStats : ScriptableObject 
{
    [Header("Stats")]
    public float maxHp;
    public float damage;

    [Header("Stagger")]
    public bool hasStagger;
    public float staggerThreshold;
    public float staggerDuration;
    public float timeResetStagger;
}
