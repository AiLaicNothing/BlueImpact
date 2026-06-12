using UnityEngine;

public interface IDamageable 
{
    void TakeDamage(in DamageInfo info);
}

public struct DamageInfo
{
    public float damage;
    public Vector3 hitDirection;

    public ThrowType throwType;
    public float stunDuration;

    public bool keepInAir;
    public float airLiftForce;
    public float pushForce;
    public float knockDownForce;
    public float knockDownForwardScale;

    public float staggerBuild;
}
