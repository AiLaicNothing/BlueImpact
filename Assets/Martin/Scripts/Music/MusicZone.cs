using UnityEngine;

public class MusicZone : MonoBehaviour
{
    [Tooltip("Music when the player goes through in the direction of the arrow.")]
    [SerializeField] Soundtrack trackForward;

    [Tooltip("Music when the player goes through against the arrow (going back). Empty = don't change.")]
    [SerializeField] Soundtrack trackBackward;

    [Tooltip("Play Track Forward as soon as this scene starts. Only use it in the scene where the player " +
             "spawns.")]
    [SerializeField] bool playOnStart = false;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (playOnStart && trackForward != null && Soundtrack_Manager.IsAvailable)
            Soundtrack_Manager.Instance.SetZoneMusic(trackForward);
    }

    // We use Exit so we know which side the player came out of.
    // If the player walks in and turns back, the music doesn't change.
    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (!Soundtrack_Manager.IsAvailable) return;

        // Positive = player is on the arrow side of the trigger.

        Vector3 toPlayer = other.bounds.center - transform.position;

        bool wentForward = Vector3.Dot(transform.forward, toPlayer) > 0f;

        Soundtrack track = wentForward ? trackForward : trackBackward;

        if (track != null) Soundtrack_Manager.Instance.SetZoneMusic(track);
    }

    // True if the collider is the player, or a child collider of the player's Rigidbody.
    bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player")) return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) return true;

        return false;
    }

    // Draws the trigger and an arrow showing the forward direction in the Scene view.
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();

        if (col == null) return;

        BoxCollider box = col as BoxCollider;

        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.15f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        // Arrow
        Vector3 start = col.bounds.center;
        Vector3 end = start + transform.forward * 2f;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawLine(end, end - transform.forward * 0.5f + transform.right * 0.3f);
        Gizmos.DrawLine(end, end - transform.forward * 0.5f - transform.right * 0.3f);
    }
}