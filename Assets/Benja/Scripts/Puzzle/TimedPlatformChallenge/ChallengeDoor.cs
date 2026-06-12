using UnityEngine;

public class ChallengeDoor : MonoBehaviour
{
    [Header("Points")]
    [SerializeField] private Transform openPoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    private Vector3 closedPosition;
    private Vector3 targetPosition;

    private bool isMoving;

    private void Awake()
    {
        closedPosition = transform.position;
        targetPosition = closedPosition;
    }

    private void Update()
    {
        if (!isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    public void Open()
    {
        if (openPoint == null)
        {
            Debug.LogWarning(
                $"[ChallengeDoor] OpenPoint no asignado en {name}");

            return;
        }

        targetPosition = openPoint.position;

        isMoving = true;

        audioSource?.PlayOneShot(openClip);
    }

    public void Close()
    {
        targetPosition = closedPosition;

        isMoving = true;

        audioSource?.PlayOneShot(closeClip);
    }

    public void SetOpenedState()
    {
        if (openPoint == null)
            return;

        transform.position = openPoint.position;

        targetPosition = openPoint.position;

        isMoving = false;
    }

    public void SetClosedState()
    {
        transform.position = closedPosition;

        targetPosition = closedPosition;

        isMoving = false;
    }
}