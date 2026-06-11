using System;
using UnityEngine;

[Serializable]
public class CameraRequest
{
    [Header("Camera")]
    public int cameraID;

    [Header("Timing")]
    public float duration = 3f;

    [Header("Priority")]
    public int activePriority = 100;
    public int inactivePriority = 0;

    [Header("Targets")]
    public Transform followTarget;
    public Transform lookAtTarget;

    [Header("Behaviour")]
    public bool restoreGameplayCamera = true;
    public bool interruptCurrent = true;


    public bool pausePlayer = true;
}
