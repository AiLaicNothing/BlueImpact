using Unity.Cinemachine;
using UnityEngine;

public class CameraID : MonoBehaviour
{
    [SerializeField]
    private int id;

    [SerializeField]
    private CinemachineCamera cameraReference;

    public int ID => id;

    public CinemachineCamera Camera => cameraReference;

    private void Reset()
    {
        cameraReference =
            GetComponent<CinemachineCamera>();
    }
}