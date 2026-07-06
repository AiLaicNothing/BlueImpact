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

    [Header("Player Control")]
    [Tooltip("Si está marcado, desactiva el movimiento e input del jugador durante la cinemática")]
    public bool pausePlayer = true;

    [Tooltip("Si está marcado, silencia todos los sonidos del jugador durante la cinemática")]
    public bool mutePlayerAudio = true;
}