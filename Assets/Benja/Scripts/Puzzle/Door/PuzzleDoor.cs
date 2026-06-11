using UnityEngine;
using UnityEngine.UIElements;

public class PuzzleDoor : MonoBehaviour, IActivatable
{
    public enum MoveAxis { X, Y, Z }

    [Header("Movimiento")]
    public MoveAxis axis = MoveAxis.Y;
    public float moveDistance = 3f;
    public float moveSpeed = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;

    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private Vector3 _targetPosition;

    private bool _isMoving = false;

    private bool _isOpen;
    private void Start()
    {
        Debug.Log($"[PuzzleDoor] Start -> {name}");

        _closedPosition = transform.position;

        Vector3 direction = axis switch
        {
            MoveAxis.X => Vector3.right,
            MoveAxis.Y => Vector3.up,
            MoveAxis.Z => Vector3.forward,
            _ => Vector3.up
        };

        _openPosition = _closedPosition + direction * moveDistance;

        _targetPosition = _closedPosition;

        Debug.Log($"[PuzzleDoor] ClosedPos: {_closedPosition}");
        Debug.Log($"[PuzzleDoor] OpenPos: {_openPosition}");
    }

    

    private void Update()
    {
        if (!_isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPosition,
            moveSpeed * Time.deltaTime);

        Debug.Log($"[PuzzleDoor] Moviendo puerta -> {transform.position}");

        if (Vector3.Distance(transform.position, _targetPosition) < 0.001f)
        {
            transform.position = _targetPosition;

            _isMoving = false;

            Debug.Log("[PuzzleDoor] Movimiento completado");
        }
    }

    private void ApplyState(bool open, bool playAudio)
    {
        Debug.Log($"[PuzzleDoor] ApplyState -> Open = {open}");

        _targetPosition = open ? _openPosition : _closedPosition;

        _isMoving = true;

        if (!playAudio) return;

        if (open)
        {
            Debug.Log("[PuzzleDoor] Reproduciendo sonido OPEN");
            audioSource?.PlayOneShot(openClip);
        }
        else
        {
            Debug.Log("[PuzzleDoor] Reproduciendo sonido CLOSE");
            audioSource?.PlayOneShot(closeClip);
        }
    }

    public void Activate()
    {
        if (_isOpen)
            return;

        _isOpen = true;

        ApplyState(true, true);
    }

    public void Deactivate()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        ApplyState(false, true);
    }
}