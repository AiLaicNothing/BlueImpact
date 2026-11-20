using UnityEngine;

public class BossMusic : MonoBehaviour
{
    [SerializeField] Soundtrack bossTrack;

    [Tooltip("Start the boss music when the player enters this object's trigger collider.")]
    [SerializeField] bool startOnTriggerEnter = false;

    bool isActive = false;
    public bool IsActive { get { return isActive; } }

    public void StartFight()
    {
        if (isActive || !Soundtrack_Manager.IsAvailable) return;

        isActive = true;

        Soundtrack_Manager.Instance.StartBossMusic(bossTrack);
    }

    // Call when the boss dies (or the player escapes / dies).
    public void EndFight()
    {
        if (!isActive) return;

        isActive = false;

        if (Soundtrack_Manager.IsAvailable) Soundtrack_Manager.Instance.EndBossMusic();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!startOnTriggerEnter) return;

        bool isPlayer = other.CompareTag("Player") ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));

        if (isPlayer) StartFight();
    }

    void OnDisable()
    {
        if (isActive) EndFight();
    }
}
