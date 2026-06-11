using UnityEngine;

public class ChallengePlatform : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float sinkDepth = 10f;
    [SerializeField] private float moveSpeed = 3f;

    private Vector3 _visiblePosition;
    private Vector3 _hiddenPosition;
    private Vector3 _targetPosition;

    private bool _isMoving;

    private bool _isVisible;
    private void Start()
    {
        _visiblePosition = transform.position;

        _hiddenPosition =
            _visiblePosition - Vector3.up * sinkDepth;

        transform.position = _hiddenPosition;

        _targetPosition = _hiddenPosition;
    }


    private void Update()
    {
        if (!_isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(
            transform.position,
            _targetPosition) < 0.01f)
        {
            transform.position = _targetPosition;
            _isMoving = false;
        }
    }

    private void OnVisibilityChanged(bool previous, bool current)
    {
        ApplyState(current);
    }

    private void ApplyState(bool visible)
    {
        _targetPosition =
            visible
            ? _visiblePosition
            : _hiddenPosition;

        _isMoving = true;
    }

    public void Show()
    {
        if (_isVisible)
            return;

        _isVisible = true;

        ApplyState(true);
    }

    public void Hide()
    {
        if (!_isVisible)
            return;

        _isVisible = false;

        ApplyState(false);
    }
}