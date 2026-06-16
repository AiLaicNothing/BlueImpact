using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class LockOnTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineCamera cameraRig;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private CinemachineInputAxisController inputProvider;

    [Header("UI")]
    [SerializeField] private Image aimIcon;

    [Header("Settings")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private Vector2 targetLockOffset;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float lockSpeed = 3f;

    private List<Transform> validTarget = new List<Transform>();
    private int currentIndex = 0;

    public bool isTargeting { get; private set; }
    public Transform CurrentTarget => currentTarget;

    private Transform currentTarget;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        }

        if (cameraRig == null)
        {
            cameraRig = FindFirstObjectByType<CinemachineCamera>();
        }

        if (orbitalFollow == null && cameraRig != null)
        {
            orbitalFollow = cameraRig.GetComponent<CinemachineOrbitalFollow>();
        }

        if (inputProvider == null && cameraRig != null)
        {
            inputProvider = cameraRig.GetComponent<CinemachineInputAxisController>();
        }
    }

    private void Start()
    {
        if (aimIcon == null)
        {
            P_TargetSelector_UI hud = FindFirstObjectByType<P_TargetSelector_UI>();

            if (hud != null)
            {
                aimIcon = hud.aimIcon;
            }
        }
    }

    private void Update()
    {
        HandleLockInput();

        if (isTargeting)
        {
            validTarget = GetValidTargets();

            if (validTarget.Count == 0)
            {
                ClearTarget();
                return;
            }

            if (CurrentTarget == null || !validTarget.Contains(currentTarget))
            {
                currentIndex = Mathf.Clamp(currentIndex, 0, validTarget.Count - 1);
                currentTarget = validTarget[currentIndex];
            }

            if (validTarget.Count > 1)
            {
                HandleTargetSwitch();
            }
        }

        if (isTargeting && !IsTargetValid(currentTarget))
        {
            ClearTarget();
            return;
        }

        if (isTargeting && currentTarget != null)
        {
            UpdateLockCamera();
        }
        else
        {
            if (inputProvider != null)
            {
                inputProvider.enabled = true;
            }
        }

        if (aimIcon != null)
        {
            aimIcon.gameObject.SetActive(isTargeting);
        }

        if (aimIcon != null && currentTarget != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(currentTarget.position + (Vector3)targetLockOffset);
            aimIcon.transform.position = screenPos;
        }
    }

    private void HandleLockInput()
    {
        if (!input.onLockTarget) return;

        if (isTargeting)
        {
            ClearTarget();
        }
        else
        {
            AssignTarget();
        }
    }

    private void UpdateLockCamera()
    {
        if (currentTarget == null || orbitalFollow == null)
        {
            ClearTarget();
            return;
        }

        // Use the camera's Follow target as the origin, but do not modify LookAt.
        Transform followTarget = cameraRig != null && cameraRig.Follow != null? cameraRig.Follow: transform;

        Vector3 dirToTarget = currentTarget.position - followTarget.position;

        float targetX = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
        orbitalFollow.HorizontalAxis.Value = Mathf.LerpAngle( orbitalFollow.HorizontalAxis.Value, targetX, Time.deltaTime * lockSpeed);

        float distanceXZ = new Vector2(dirToTarget.x, dirToTarget.z).magnitude;
        float angleY = Mathf.Atan2(dirToTarget.y, distanceXZ) * Mathf.Rad2Deg;

        float minAngle = -40f;
        float maxAngle = 40f;

        float targetYNormalized = Mathf.InverseLerp(maxAngle, minAngle, angleY);

        orbitalFollow.VerticalAxis.Value = Mathf.Lerp(orbitalFollow.VerticalAxis.Value, targetYNormalized, Time.deltaTime * lockSpeed);
    }

    private void HandleTargetSwitch()
    {
        if (Mathf.Abs(input.scrollInput) < 0.1f) return;

        if (input.scrollInput > 0)
        {
            currentIndex++;
        }
        else
        {
            currentIndex--;
        }

        if (currentIndex >= validTarget.Count)
        {
            currentIndex = 0;
        }

        if (currentIndex < 0)
        {
            currentIndex = validTarget.Count - 1;
        }

        currentTarget = validTarget[currentIndex];
    }

    private void AssignTarget()
    {
        validTarget = GetValidTargets();

        if (validTarget.Count == 0) return;

        currentIndex = 0;
        currentTarget = validTarget[currentIndex];
        isTargeting = true;

        if (inputProvider != null)
        {
            inputProvider.enabled = false;
        }
    }

    private void ClearTarget()
    {
        isTargeting = false;
        currentTarget = null;

        if (inputProvider != null)
        {
            inputProvider.enabled = true;
        }
    }

    private List<Transform> GetValidTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, maxDistance);

        List<Transform> targets = new List<Transform>();

        foreach (Collider col in hits)
        {
            if (!col.CompareTag(enemyTag)) continue;

            Transform enemy = col.transform;

            float dist = Vector3.Distance(transform.position, enemy.position);

            if (dist < minDistance || dist > maxDistance) continue;

            Vector3 camForward = mainCamera.transform.forward;
            Vector3 toEnemy = (enemy.position - mainCamera.transform.position).normalized;

            float dot = Vector3.Dot(camForward, toEnemy);

            if (dot < 0.5f) continue;

            Vector3 viewportPos = mainCamera.WorldToViewportPoint(enemy.position);

            if (viewportPos.z <= 0) continue;

            if (viewportPos.x < 0.1f || viewportPos.x > 0.9f) continue;

            if (viewportPos.y < 0.1f || viewportPos.y > 0.9f) continue;

            Vector3 origin = mainCamera.transform.position;
            Vector3 targetPos = enemy.position + Vector3.up;

            Vector3 dir = (targetPos - origin).normalized;
            float rayDistance = Vector3.Distance(origin, targetPos);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
            {
                continue;
            }

            targets.Add(enemy);
        }

        targets = targets.OrderBy(t =>
        {
            Vector3 viewPos = mainCamera.WorldToViewportPoint(t.position);
            return Vector2.Distance(new Vector2(viewPos.x, viewPos.y), new Vector2(0.5f, 0.5f));
        }).ToList();

        return targets;
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        return GetValidTargets().Contains(target);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}