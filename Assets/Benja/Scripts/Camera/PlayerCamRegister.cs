using Unity.Cinemachine;
using UnityEngine;

public class PlayerCamRegister : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera playerCamera;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera =
                GetComponentInChildren<CinemachineCamera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "[PlayerCamRegister] Gameplay camera missing.");

            return;
        }

        CameraDirector.Instance?.
            RegisterPlayerCamera(playerCamera);
    }
}