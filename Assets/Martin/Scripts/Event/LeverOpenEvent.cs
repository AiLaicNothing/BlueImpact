using System.Collections;
using UnityEngine;

public class LeverOpenEvent : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform door;
    [SerializeField] private Transform upperPos;
    [SerializeField] private Transform lowerPos;

    private bool isOpen;

    private void Start()
    {
        door.position = lowerPos.position;
    }

    public void Interact()
    {
        if (isOpen) return;

        StartCoroutine(MovePlattform(upperPos));
    }
    public string GetInteractionText()
    {
        return "Abrir puerta";
    }

    private IEnumerator MovePlattform(Transform finalPos)
    {
        if (finalPos == null) yield break;

        while (Vector3.Distance(door.position, finalPos.position) > 0.01f)
        {
            door.position = Vector3.MoveTowards(door.position, finalPos.position, 5 * Time.deltaTime);
            yield return null;
        }

        door.transform.position = finalPos.position;
        isOpen = true;
    }
}
