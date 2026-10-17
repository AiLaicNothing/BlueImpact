using UnityEngine;

public class LavaGolemAnimEvents : MonoBehaviour
{
    private Miniboss_LavaGolem golem;

    private void Awake()
    {
        golem = GetComponentInParent<Miniboss_LavaGolem>();
    }

    // Llamado desde el Animation Event del clip Punch
    public void OnFlujoImpact()
    {
        golem.OnFlujoImpact();
    }
}