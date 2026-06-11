using Unity.Cinemachine;
using UnityEngine;

public class CinemachineControllerDialogue : MonoBehaviour
{
    public CinemachineVirtualCamera currentCam;

    void Start()
    {
        if (currentCam != null)
        {
            currentCam.Priority = 10;
        }
    }

    public void SwitchCamera(CinemachineVirtualCamera newCam)
    {
        if (currentCam == newCam) return;

        if (currentCam != null)
            currentCam.Priority = 0;

        newCam.Priority = 10;
        currentCam = newCam;

        Debug.Log("Cambiando a: " + newCam.name);
    }
}
