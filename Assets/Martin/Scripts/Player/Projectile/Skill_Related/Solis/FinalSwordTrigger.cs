using UnityEngine;

public class FinalSwordTrigger : MonoBehaviour
{
    [SerializeField] FinalSwordPrefab fSPrefab;
    [Header("Layer")]
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] LayerMask groundLayer;

    [Header("Dmg")]
    private PlayerControl player;
    private HitData hitData;
    private DamageInfo info;
    private Vector3 direction;
    private void Update()
    {
        if (fSPrefab.canFinish)
        {
            if (Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer))
            {
                Debug.Log("TriggerGround");
                fSPrefab.trueFinish = true;
            }
            
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == enemyLayer && !fSPrefab.trueFinish)
        {
            fSPrefab.ChangeDmgInfo(fSPrefab.player, fSPrefab.direction, fSPrefab.hitData);
            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(fSPrefab.info);
            }
        }
    }
}
