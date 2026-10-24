using System.Collections;
using UnityEngine;

public class Elevator_Event : MonoBehaviour
{
    [Header("Persistence")]
    [SerializeField] private WorldEvent_Data eventData;

    [Header("Elevator")]
    [SerializeField] private Transform plattform;
    [SerializeField] private float posY = 10f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float reachThreshold = 0.01f;

    [Header("Initial Position")]
    [Tooltip("In base of its starting point")]
    [SerializeField] private bool isUp = true;

    private float upperY;
    private float lowerY;

    private Coroutine moveRoutine;

    public bool IsUp => isUp;
    public bool IsMoving => moveRoutine != null;

    private void Awake()
    {
        if (plattform == null)
        {
            Debug.LogError(
                "[Elevator_Event] No platform assigned.",
                this
            );
            return;
        }

        float initialY = plattform.localPosition.y;
        float distance = Mathf.Abs(posY);

        if (isUp)
        {
            upperY = initialY;
            lowerY = initialY - distance;
        }
        else
        {
            lowerY = initialY;
            upperY = initialY + distance;
        }

        RestoreElevatorState();
    }

    private void RestoreElevatorState()
    {
        if (plattform == null) return;

        if (Event_Manager.Instance != null &&eventData != null && 
            Event_Manager.Instance.TryGetState(eventData, out WorldEvent_State saved))
        {
            // 0 = Top
            // 1 = Bottom
            if (saved.state == 0 || saved.state == 1)
            {
                isUp = saved.state == 0;
            }
        }

        // Restore the elevator immediately,
        SetPlatformY(isUp ? upperY : lowerY);
    }

    private void SaveElevatorState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning(
                "[Elevator_Event] Missing Event_Manager or eventData.", this);
            return;
        }

        Event_Manager.Instance.SaveState(eventData, false, isUp ? 0 : 1);
    }

    public void ToggleElevator()
    {
        if (isUp)
        {
            GoDown();
        }
        else
        {
            GoUp();
        }
    }

    public void GoUp()
    {
        MoveTo(true);
    }

    public void GoDown()
    {
        MoveTo(false);
    }

    private void MoveTo(bool goingUp)
    {
        if (plattform == null) return;

        float targetY = goingUp ? upperY : lowerY;

        if (moveRoutine != null && isUp == goingUp) return;

        if (moveRoutine == null && Mathf.Abs(plattform.localPosition.y - targetY)<= reachThreshold)
        {
            isUp = goingUp;
            return;
        }

        // Change the current destination.
        isUp = goingUp;

        // Cancel existing movement so the elevator
        // can reverse direction when requested.
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        SaveElevatorState();

        moveRoutine = StartCoroutine(MovePlatformTo(targetY));
    }


    private IEnumerator MovePlatformTo(float targetY)
    {
        while (Mathf.Abs(plattform.localPosition.y - targetY) > reachThreshold)
        {
            Vector3 currentPosition = plattform.localPosition;

            float newY = Mathf.MoveTowards(currentPosition.y, targetY, Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);

            plattform.localPosition = new Vector3(currentPosition.x, newY, currentPosition.z);

            yield return null;
        }

        SetPlatformY(targetY);

        moveRoutine = null;
    }

    private void SetPlatformY(float targetY)
    {
        if (plattform == null) return;

        Vector3 position = plattform.localPosition;
        position.y = targetY;

        plattform.localPosition = position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ToggleElevator();
        }
    }
}