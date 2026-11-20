using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundTrack", menuName = "Audio/Soundtrack")]
public class Soundtrack : ScriptableObject
{
    public AudioClip clip;

    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;

    [Tooltip("Seconds to fade in. Negative number = default")]
    public float fadeInTime = -1f;
    public float fadeOutTime = -1f;

    [Tooltip("If music continue from where it was last time")]
    public bool resumeFromLastPosition = true;

    [Tooltip("Time before the track reset if it wasnt hear for")]
    public float forgetPositionAfter = 90f;
}
