using UnityEngine;

public class FlyingGolemAnimationEvents : MonoBehaviour
{
    [SerializeField] private FlyingGolem golem;

    private void Awake()
    {
        if (golem == null) golem = GetComponentInParent<FlyingGolem>();
        if (golem == null) Debug.LogError("[FlyingGolemAnimationEvents] Missing FlyingGolem in parent.", this);
    }

    public void SpawnMagicCircles() { if (golem != null) golem.SpawnMagicCircles(); }
    public void FireMagicBullet() { if (golem != null) golem.FireMagicBullet(); }
    public void SpawnChargeVfx() { if (golem != null) golem.SpawnChargeVfx(); }
    public void ThrowBomb() { if (golem != null) golem.ThrowBomb(); }
    public void EndAttack() { if (golem != null) golem.EndAttack(); }
}
