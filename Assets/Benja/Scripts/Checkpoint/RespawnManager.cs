using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance
    {
        get;
        private set;
    }

    private Transform currentRespawn;

    private void Awake()
    {
        Instance = this;
    }

    public void SetRespawn(
        Transform point)
    {
        currentRespawn = point;

        Debug.Log(
            $"Respawn cambiado a: {point.name}");
    }

    public void RespawnPlayer(
        Transform player)
    {
        if (currentRespawn == null)
            return;

        player.SetPositionAndRotation(
            currentRespawn.position,
            currentRespawn.rotation);
    }
}