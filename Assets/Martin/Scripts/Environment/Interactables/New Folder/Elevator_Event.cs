using System.Collections;
using UnityEngine;

public class Elevator_Event : MonoBehaviour
{
    [Header("Elevator")]
    [SerializeField] private Transform plattform;
    [SerializeField] private Transform upperPos;
    [SerializeField] private Transform lowerPos;
    [SerializeField] private float moveSpeed = 5f;

    private Coroutine moveRoutine;
    private bool targetIsUpper;

    private void Awake()
    {
        if (plattform != null && upperPos != null)
        {
            targetIsUpper = Vector3.Distance(plattform.position, upperPos.position) < Vector3.Distance(plattform.position, lowerPos.position);
        }
    }

    // For pressure plates or other systems that should toggle the elevator.
    public void ToggleElevator()
    {
        if (targetIsUpper)
        {
            GoDown();
        }
        else
        {
            GoUp();
        }
    }

    // These can be called directly by a lever through UnityEvents.
    public void GoUp()
    {
        MoveTo(upperPos, true);
    }

    public void GoDown()
    {
        MoveTo(lowerPos, false);
    }

    private void MoveTo(Transform destination, bool goingUp)
    {
        if (plattform == null || destination == null) return;

        // Ignore repeated requests for the current destination.
        if (targetIsUpper == goingUp && moveRoutine != null) return;

        targetIsUpper = goingUp;

        // Cancel the previous movement if a new destination is requested.
        if (moveRoutine != null) StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MovePlattform(destination));
    }

    private IEnumerator MovePlattform(Transform finalPos)
    {
        while (Vector3.Distance(plattform.position, finalPos.position) > 0.01f)
        {
            plattform.position = Vector3.MoveTowards(plattform.position, finalPos.position, moveSpeed * Time.deltaTime);

            yield return null;
        }

        plattform.position = finalPos.position;
        moveRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ToggleElevator();
        }
    }
}
