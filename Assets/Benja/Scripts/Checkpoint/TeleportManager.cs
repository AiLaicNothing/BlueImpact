using System.Collections;
using UnityEngine;

public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance
    {
        get;
        private set;
    }

    [SerializeField]
    private Transform player;

    private void Awake()
    {
        Instance = this;
    }

    public void Teleport(
        Checkpoint destination)
    {
        StartCoroutine(
            TeleportRoutine(destination));
    }

    private IEnumerator TeleportRoutine(
        Checkpoint destination)
    {
        yield return FadeUI.Instance.FadeOut();

        player.SetPositionAndRotation(
            destination.SpawnPoint.position,
            destination.SpawnPoint.rotation);

        yield return new WaitForSecondsRealtime(
            0.2f);

        CheckpointMenuUI.Instance.CloseMenu();

        yield return FadeUI.Instance.FadeIn();
    }
}