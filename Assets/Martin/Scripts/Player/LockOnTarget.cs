using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CMF;

public class LockOnTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private PlayerInputHandler input;

    [Header("Player Model")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private TurnTowardControllerVelocity modelRotation;

    [Header("UI")]
    [SerializeField] private Sprite aimIcon;

    [Header("Target Settings")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private Vector2 targetLockOffset;
    [SerializeField] private float targetAimHeight = 1.2f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Lock Maintenance")]
    [SerializeField] private float lostTargetGraceTime = 0.5f;
    [SerializeField] private float releaseDistanceBuffer = 2f;

    [Header("Rotation")]
    [SerializeField] private float cameraLockSpeed = 360f;
    [SerializeField] private float modelLockSpeed = 500f;

    [Header("Debug")]
    [SerializeField] private bool debugLockOn = false;

    private List<Transform> validTargets = new();

    private Transform currentTarget;

    private CameraInput[] cameraInputs;
    private bool[] previousCameraInputStates;

    private bool previousModelRotationState;
    private bool previousLockInput;

    private float invalidTargetTimer;
    private string invalidReason;

    public bool isTargeting { get; private set; }
    public Transform CurrentTarget => currentTarget;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraController == null && mainCamera != null)
        {
            cameraController =
                mainCamera.GetComponentInParent<CameraController>();
        }

        if (cameraController != null)
        {
            cameraInputs =
                cameraController.GetComponents<CameraInput>();
        }

        if (modelRotation == null && modelRoot != null)
        {
            modelRotation =
                modelRoot.GetComponent<TurnTowardControllerVelocity>();
        }

        if (modelRoot == null && modelRotation != null)
            modelRoot = modelRotation.transform;
    }

    private void Start()
    {
        P_TargetSelector_UI.Instance.SetIcon(aimIcon);

        if (aimIcon != null) P_TargetSelector_UI.Instance.HideIcon();
    }

    private void Update()
    {
        HandleLockInput();

        if (!isTargeting)
            return;

        if (currentTarget == null)
        {
            ClearTarget();
            return;
        }

        // Check whether the current target can still be maintained.
        if (!CanMaintainTarget(currentTarget, out string reason))
        {
            invalidTargetTimer += Time.deltaTime;
            invalidReason = reason;

            if (invalidTargetTimer >= lostTargetGraceTime)
            {
                if (debugLockOn)
                {
                    Debug.Log(
                        $"Lock-on lost: {invalidReason}",
                        this
                    );
                }

                ClearTarget();
                return;
            }
        }
        else
        {
            invalidTargetTimer = 0f;
            invalidReason = null;
        }

        // Switch targets with the scroll wheel.
        HandleTargetSwitch();
    }

    private void LateUpdate()
    {
        if (!isTargeting || currentTarget == null)
            return;

        UpdateLockCamera();
        RotateModelTowardTarget();
        UpdateAimIcon();
    }

    // =========================
    // LOCK INPUT
    // =========================

    private void HandleLockInput()
    {
        if (input == null)
            return;

        bool lockPressed = input.onLockTarget;

        if (lockPressed && !previousLockInput)
        {
            if (isTargeting)
                ClearTarget();
            else
                AssignTarget();
        }

        previousLockInput = lockPressed;
    }

    private void AssignTarget()
    {
        if (mainCamera == null ||
            cameraController == null ||
            modelRoot == null)
        {
            Debug.LogWarning(
                "LockOnTarget: Missing camera or model references.",
                this
            );
            return;
        }

        validTargets = GetValidTargets();

        if (validTargets.Count == 0)
        {
            if (debugLockOn)
                Debug.Log("No valid targets found.", this);

            return;
        }

        currentTarget = validTargets[0];
        isTargeting = true;

        invalidTargetTimer = 0f;
        invalidReason = null;

        DisableNormalRotation();

        if (aimIcon != null) P_TargetSelector_UI.Instance.ShowIcon();

        if (debugLockOn)
        {
            Debug.Log(
                $"Locked onto: {currentTarget.name}",
                this
            );
        }
    }

    private void ClearTarget()
    {
        if (!isTargeting)
            return;

        isTargeting = false;
        currentTarget = null;

        invalidTargetTimer = 0f;
        invalidReason = null;

        RestoreNormalRotation();

        if (aimIcon != null) P_TargetSelector_UI.Instance.HideIcon();
    }

    // =========================
    // CAMERA AND MODEL CONTROL
    // =========================

    private void DisableNormalRotation()
    {
        // Disable camera mouse and joystick inputs.
        if (cameraInputs != null)
        {
            previousCameraInputStates =
                new bool[cameraInputs.Length];

            for (int i = 0; i < cameraInputs.Length; i++)
            {
                if (cameraInputs[i] == null)
                    continue;

                previousCameraInputStates[i] =
                    cameraInputs[i].enabled;

                cameraInputs[i].enabled = false;
            }
        }

        // Disable movement-based model rotation.
        if (modelRotation != null)
        {
            previousModelRotationState = modelRotation.enabled;
            modelRotation.enabled = false;
        }
    }

    private void RestoreNormalRotation()
    {
        if (cameraInputs != null &&
            previousCameraInputStates != null)
        {
            for (int i = 0; i < cameraInputs.Length; i++)
            {
                if (cameraInputs[i] == null)
                    continue;

                cameraInputs[i].enabled =
                    previousCameraInputStates[i];
            }
        }

        if (modelRotation != null)
        {
            modelRotation.enabled =
                previousModelRotationState;
        }
    }

    private Vector3 GetTargetPoint(Transform target)
    {
        return target.position +
               Vector3.up * targetAimHeight +
               new Vector3(
                   targetLockOffset.x,
                   targetLockOffset.y,
                   0f
               );
    }

    private void UpdateLockCamera()
    {
        if (cameraController == null || currentTarget == null)
            return;

        cameraController.RotateTowardPosition(
            GetTargetPoint(currentTarget),
            cameraLockSpeed
        );
    }

    private void RotateModelTowardTarget()
    {
        if (modelRoot == null || currentTarget == null)
            return;

        Vector3 direction =
            currentTarget.position - modelRoot.position;

        Vector3 up = modelRoot.parent != null
            ? modelRoot.parent.up
            : Vector3.up;

        direction = Vector3.ProjectOnPlane(direction, up);

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction, up);

        modelRoot.rotation = Quaternion.RotateTowards(
            modelRoot.rotation,
            targetRotation,
            modelLockSpeed * Time.deltaTime
        );
    }

    private void UpdateAimIcon()
    {
        if (aimIcon == null ||
            mainCamera == null ||
            currentTarget == null)
            return;

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                GetTargetPoint(currentTarget)
            );

        P_TargetSelector_UI.Instance.GetIcon().transform.position = screenPosition;
    }

    // =========================
    // TARGET SWITCHING
    // =========================

    private void HandleTargetSwitch()
    {
        if (!isTargeting || input == null || currentTarget == null)
            return;

        Vector2 scroll = input.scrollInput;

        // Use vertical scroll input.
        if (Mathf.Abs(scroll.y) < 0.01f)
            return;

        List<Transform> targets = GetValidTargets();

        // Keep the current target even if it is temporarily
        // outside the screen.
        if (!targets.Contains(currentTarget))
            targets.Add(currentTarget);

        if (targets.Count <= 1)
            return;

        // Sort targets from left to right on screen.
        targets = targets.OrderBy(target =>
        {
            Vector3 screenPos = mainCamera.WorldToViewportPoint(
                GetTargetPoint(target)
            );

            return screenPos.x;
        }).ToList();

        int currentIndex = targets.IndexOf(currentTarget);

        // Scroll up = right.
        // Scroll down = left.
        int direction = scroll.y > 0f ? 1 : -1;

        int nextIndex = currentIndex + direction;

        // Loop around the list.
        if (nextIndex >= targets.Count)
            nextIndex = 0;

        if (nextIndex < 0)
            nextIndex = targets.Count - 1;

        currentTarget = targets[nextIndex];

        invalidTargetTimer = 0f;
        invalidReason = null;

        if (debugLockOn)
            Debug.Log("Switched target to: " + currentTarget.name);
    }

    // =========================
    // TARGET DETECTION
    // =========================

    private Transform FindEnemyRoot(Transform hitTransform)
    {
        Transform current = hitTransform;

        while (current != null)
        {
            if (current.CompareTag(enemyTag))
                return current;

            current = current.parent;
        }

        return null;
    }

    private List<Transform> GetValidTargets()
    {
        List<Transform> targets = new();

        if (mainCamera == null)
            return targets;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                maxDistance
            );

        foreach (Collider col in hits)
        {
            Transform enemy = FindEnemyRoot(col.transform);

            if (enemy == null)
                continue;

            // Ignore colliders belonging to the player.
            if (enemy.IsChildOf(transform.root))
                continue;

            // Avoid duplicate targets.
            if (targets.Contains(enemy))
                continue;

            if (!enemy.gameObject.activeInHierarchy)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    enemy.position
                );

            if (distance < minDistance ||
                distance > maxDistance)
                continue;

            Vector3 point = GetTargetPoint(enemy);

            Vector3 viewport =
                mainCamera.WorldToViewportPoint(point);

            // Target must be in front of the camera.
            if (viewport.z <= 0f)
                continue;

            // Aim point must be visible on screen.
            if (viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f)
                continue;

            if (!HasClearSight(enemy))
                continue;

            targets.Add(enemy);
        }

        // Sort by distance from screen center.
        return targets.OrderBy(enemy =>
        {
            Vector3 viewport =
                mainCamera.WorldToViewportPoint(
                    GetTargetPoint(enemy)
                );

            return Vector2.Distance(
                new Vector2(viewport.x, viewport.y),
                new Vector2(0.5f, 0.5f)
            );
        }).ToList();
    }

    // =========================
    // TARGET VALIDATION
    // =========================

    private bool CanMaintainTarget(
        Transform target,
        out string reason
    )
    {
        reason = null;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            reason = "Target destroyed or disabled";
            return false;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        if (distance > maxDistance + releaseDistanceBuffer)
        {
            reason = "Target out of range";
            return false;
        }

        // We don't check minimum distance or screen position
        // when maintaining an existing lock.
        if (!HasClearSight(target))
        {
            reason = "Line of sight blocked";
            return false;
        }

        return true;
    }

    private bool HasClearSight(Transform target)
    {
        if (target == null || mainCamera == null)
            return false;

        Vector3 origin =
            mainCamera.transform.position;

        Vector3 targetPosition =
            GetTargetPoint(target);

        Vector3 direction =
            targetPosition - origin;

        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction.normalized,
            distance,
            obstacleLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            // Ignore the player's own colliders.
            if (hit.transform.IsChildOf(transform.root))
                continue;

            // Ignore the target's own colliders.
            if (hit.transform.IsChildOf(target))
                continue;

            // An obstacle blocks the view.
            return false;
        }

        return true;
    }

    // =========================
    // CLEANUP
    // =========================

    private void OnDisable()
    {
        ClearTarget();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            maxDistance
        );
    }
}