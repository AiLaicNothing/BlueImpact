using UnityEngine;

public class MusicZone : MonoBehaviour
{
    [SerializeField] private Soundtrack track;
    [SerializeField] private bool activateOnStart;

    [SerializeField] Color gizmoColor = new Color(0.3f, 0.6f, 1f, 0.12f);

    public Soundtrack Track => track;

    // Runs when you add the component in the editor: makes the collider a trigger automatically.
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (activateOnStart && Soundtrack_Manager.IsAvailable) Soundtrack_Manager.Instance.EnterZone(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other) && Soundtrack_Manager.IsAvailable) Soundtrack_Manager.Instance.EnterZone(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other) && Soundtrack_Manager.IsAvailable) Soundtrack_Manager.Instance.ExitZone(this);
    }

    // Runs when this zone's scene is unloaded while the player is still inside.
    void OnDisable()
    {
        if (Soundtrack_Manager.IsAvailable) Soundtrack_Manager.Instance.ExitZone(this);
    }

    // True if the collider is the player, or a child collider of the player's Rigidbody.
    bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player")) return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) return true;
        return false;
    }

    // Draws the zone in the Scene view so you can see where each music area is.
    void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
            return;
        }

        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
