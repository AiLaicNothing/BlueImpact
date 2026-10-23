using UnityEngine;

public class BasePlattform : MonoBehaviour
{
    [SerializeField] private Transform plattform;
    private Transform player;
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (player == null) player = other.transform;

            player.parent = plattform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (player == null) player = other.transform;

            player.parent = null;
        }
    }
}
