using UnityEngine;

[System.Serializable]
public class AttackStep 
{
    [Header("VFX")]
    public GameObject attackVfx;
    public float vfxSpawnTime;
    public float vfxDuration;
    public Vector3 vfxOffset = Vector3.zero;
    public Vector3 vfxRotOffset = Vector3.zero;

    [Header("Animation")]
    public string name;
    public float duration;

    [Header("Combo")]
    public float comboWindowStart;
    public float comboWindowEnd;

    [Header("HitBox")]
    public float hitTime;
    public Vector3 hitBoxSize = new Vector3(1, 1, 2);
    public Vector3 hitBoxOffSet = new Vector3(0, 0, 1);

    [Header("Movement")]
    public float moveDis = 0f;
    public float moveStartTime = 0f;
    public float moveDuration = 0.12f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float turnSpeed = 80f;

    [Header("Movement Lock-On")]
    public float lockOnStopDistance = 1.5f;

    public HitData hitData;
}

