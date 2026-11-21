using System;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public class CameraRequest
{
    [Header("Camera")]
    [Tooltip("If assigned, ID is ignored")]
    public CinemachineCamera camera;
    [Tooltip("Use it the camera lives in anothes scene from the event")]
    public int cameraID;

    [Header("Timing")]
    [Tooltip("Duration active of the camera + the blend duration")]
    public float duration = 3f;
    [Tooltip("If checked, Duration is ignored and the camera stays active " +
         "until the event calls Release on this request.")]
    public bool holdUntilReleased;

    [Header("Priority")]
    public int activePriority = 100;
    public int inactivePriority = -100;

    [Header("Targets")]
    public Transform followTarget;
    public Transform lookAtTarget;

    [Header("Transition")]
    public bool overrideBlendIn = true;
    public CinemachineBlendDefinition blendIn = new(CinemachineBlendDefinition.Styles.EaseInOut, 1f);

    public bool overrideBlendOut = true;
    public CinemachineBlendDefinition blendOut = new(CinemachineBlendDefinition.Styles.EaseInOut, 1f);

    [Header("Behaviour")]
    public bool restoreGameplayCamera = true;
    public bool interruptCurrent = true;

    [Header("Player Control")]
    [Tooltip("Si está marcado, desactiva el movimiento e input del jugador durante la cinemática")]
    public bool pausePlayer = true;

    [Tooltip("Si está marcado, silencia todos los sonidos del jugador durante la cinemática")]
    public bool mutePlayerAudio = true;
}