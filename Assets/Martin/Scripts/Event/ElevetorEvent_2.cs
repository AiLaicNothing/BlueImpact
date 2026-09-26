using System.Collections;
using UnityEngine;

public class ElevetorEvent_2 : MonoBehaviour
{
    [SerializeField] private Transform plattform;
    [SerializeField] private Transform upperPos;
    [SerializeField] private Transform lowerPos;

    private bool isMoving;

    private IEnumerator MovePlattform(Transform finalPos)
    {
        if (finalPos == null) yield break;

        isMoving = true;

        while (Vector3.Distance(plattform.position, finalPos.position) > 0.01f)
        {
            plattform.position = Vector3.MoveTowards(plattform.position, finalPos.position, 5 * Time.deltaTime);
            yield return null;
        }

        plattform.transform.position = finalPos.position;
        isMoving = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isMoving) return;

            if (plattform.position == upperPos.position)
            {
                StartCoroutine(MovePlattform(lowerPos));
            }
            else
            {
                StartCoroutine(MovePlattform(upperPos));
            }
        }
    }
}
