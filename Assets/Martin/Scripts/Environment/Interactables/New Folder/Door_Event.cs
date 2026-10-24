using UnityEngine;

//This door event can close and open it move in base of pos variables
public class Door_Event : MonoBehaviour
{
    [Header("Persistence")]
    [SerializeField] private WorldEvent_Data eventData;

    [Header("Settings")]
    [SerializeField] private Type animType = Type.Transform;

    [Header("Transform")]
    [SerializeField] private float posX;
    [SerializeField] private float posY;
    [SerializeField] private float posZ;
    [SerializeField] private Transform _transform;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Initial State")]
    [SerializeField] private bool isUp = false;

    [Header("Animation")]
    [SerializeField] private string anim;
    private Animator _animator;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isMoving;
    private bool isComplete;

    public bool IsUp => isUp;
    public bool IsMoving => isMoving;
    public bool IsComplete => isComplete;

    public enum Type
    {
        Transform,
        Animation
    }

    private void Awake()
    {
        if (_transform == null) _transform = transform;

        Vector3 initialPosition = _transform.localPosition;
        Vector3 offset = new Vector3(posX, posY, posZ);

        if (isUp)
        {
            openPosition = initialPosition;
            closedPosition = initialPosition - offset;
        }
        else
        {
            closedPosition = initialPosition;
            openPosition = initialPosition + offset;
        }

        if (animType == Type.Animation)
        {
            _animator = GetComponent<Animator>();

            if (_animator == null)
            {
                Debug.LogError("Door_Event requires an Animator.", this);
            }
        }
    }

    private void Start()
    {
        RestoreEventState();
    }

    private void Update()
    {
        if (!isMoving || animType != Type.Transform) return;

        MoveDoor();
    }

    private void RestoreEventState()
    {
        if (Event_Manager.Instance != null && eventData != null &&
            Event_Manager.Instance.TryGetState(eventData, out WorldEvent_State saved))
        {
            isComplete = saved.completed;

            // 0 = Up / Open
            // 1 = Down / Closed
            if (saved.state == 0 || saved.state == 1) isUp = saved.state == 0;
        }

        SetDoorInstant();

        Debug.Log($"[Door_Event] Restored. IsUp: {isUp}, Completed: {isComplete}", this);
    }

    private void SaveEventState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning("[Door_Event] Missing Event_Manager or WorldEvent_Data.", this);
            return;
        }

        Event_Manager.Instance.SaveState(eventData, isComplete, isUp ? 0 : 1);
    }

    public void StartEvent()
    {
        if (isComplete) return;

        SetDoorState(!isUp);
    }

    public void GoUp()
    {
        if (isComplete) return;

        SetDoorState(true);
    }

    public void GoDown()
    {
        if (isComplete) return;

        SetDoorState(false);
    }

    public void CompleteEvent()
    {
        if (isComplete) return;

        isComplete = true;

        SaveEventState();
    }

    private void SetDoorState(bool up)
    {
        if (isUp == up) return;

        isUp = up;

        if (animType == Type.Transform)
        {
            isMoving = true;
        }
        else
        {
            PlayAnim();
        }

        SaveEventState();
    }

    private void MoveDoor()
    {
        Vector3 targetPosition = isUp? openPosition : closedPosition;

        _transform.localPosition = Vector3.MoveTowards(_transform.localPosition, targetPosition, Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);

        if (_transform.localPosition == targetPosition)
        {
            isMoving = false;
        }
    }

    private void PlayAnim()
    {
        if (_animator == null || string.IsNullOrEmpty(anim)) return;

        // Animator Bool parameter:
        // true  = Up / Open
        // false = Down / Closed

        _animator.SetBool(anim, isUp);
    }

    private void SetDoorInstant()
    {
        isMoving = false;

        if (animType == Type.Transform)
        {
            _transform.localPosition = isUp? openPosition : closedPosition;
        }
        else
        {
            PlayAnim();
        }
    }
}