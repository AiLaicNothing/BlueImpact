using UnityEngine;

public class ChallengeFallZone : MonoBehaviour
{
    [SerializeField]
    private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        PlayerControl player =
            other.GetComponentInParent<PlayerControl>();

        if (player == null)
            return;

        player.transform.position =
            respawnPoint.position;
    }
}