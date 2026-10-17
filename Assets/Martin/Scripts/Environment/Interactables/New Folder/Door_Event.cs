using UnityEngine;

//This door event can close and open it move in base of pos variables
public class Door_Event : MonoBehaviour
{
    [SerializeField] private Type animType = Type.Transform;

    [Header("Transform")]
    [SerializeField] private float posX;
    [SerializeField] private float posY;
    [SerializeField] private float posZ;
    [SerializeField] private Transform _transform;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Animation")]
    [SerializeField] private string anim;
    private Animator _animator;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isOpen;
    private bool isMoving;
    private bool isComplete = false;

    public enum Type
    {
        Transform,
        Animation
    }

    private void Awake()
    {
        if (_transform == null) _transform = transform;

        closedPosition = _transform.localPosition;

        // X, Y and Z represent the offset from the closed position.
        openPosition = closedPosition + new Vector3(posX, posY, posZ);

        if (animType == Type.Animation)
        {
            _animator = GetComponent<Animator>();

            if (_animator == null) Debug.LogError("Door_Event requires an Animator.", this);
        }
    }

    private void Update()
    {
        if (!isMoving || animType != Type.Transform) return;

        MoveDoor();
    }

    public void StartEvent()
    {
        if (isComplete) return;

        isOpen = !isOpen;

        if (animType == Type.Transform)
        {
            isMoving = true;
        }
        else
        {
            PlayAnim();
        }
    }

    public void CompleteEvent()
    {
        isComplete = true;
    }

    private void MoveDoor()
    {
        Vector3 targetPosition = isOpen ? openPosition : closedPosition;

        _transform.localPosition = Vector3.MoveTowards( _transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);

        if (_transform.localPosition == targetPosition)
        {
            isMoving = false;
        }
    }

    private void PlayAnim()
    {
        if (_animator == null) return;

        _animator.SetBool(anim, isOpen);
    }
}
