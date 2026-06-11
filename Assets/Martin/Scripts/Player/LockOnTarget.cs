using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class LockOnTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineFreeLook freeLook;
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

    void Start()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        if (aimIcon == null)
        {
            //PlayerUI hud = FindFirstObjectByType<PlayerUI>();
            //if (hud != null)
            //    aimIcon = hud.aimIcon;
        }

        // Disable default Cinemachine input
        freeLook.m_XAxis.m_InputAxisName = "";
        freeLook.m_YAxis.m_InputAxisName = "";
    }

    void Update()
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
            // Free look using Input System
            freeLook.m_XAxis.m_InputAxisValue = input.lookInput.x;
            freeLook.m_YAxis.m_InputAxisValue = input.lookInput.y;
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


    void HandleLockInput()
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

    void UpdateLockCamera()
    {

        if (!currentTarget) { ClearTarget(); return; }

        Vector3 dirToTarget = currentTarget.position - freeLook.Follow.position;

        float targetX = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
        freeLook.m_XAxis.Value = Mathf.LerpAngle(freeLook.m_XAxis.Value, targetX, Time.deltaTime * lockSpeed);

        // Calculamos el ángulo vertical (pitch)
        float distanceXZ = new Vector2(dirToTarget.x, dirToTarget.z).magnitude;
        float angleY = Mathf.Atan2(dirToTarget.y, distanceXZ) * Mathf.Rad2Deg;

        // Mapeo de ángulo a rango 0-1 del FreeLook
        // Normalmente: -45 grados es arriba (0), 45 grados es abajo (1). 
        // Ajusta estos valores según los límites que tengas en el Inspector de tu FreeLook.
        float minAngle = -40f;
        float maxAngle = 40f;

        // InverseLerp nos da un valor de 0 a 1 basado en el ángulo actual
        // Nota: El eje Y en FreeLook suele estar invertido (0 arriba, 1 abajo)
        float targetYNormalized = Mathf.InverseLerp(maxAngle, minAngle, angleY);

        // Aplicar con suavizado
        freeLook.m_YAxis.Value = Mathf.Lerp(freeLook.m_YAxis.Value, targetYNormalized, Time.deltaTime * lockSpeed);
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

    void AssignTarget()
    {
        validTarget = GetValidTargets();

        if (validTarget.Count == 0) return;

        currentIndex = 0;
        currentTarget = validTarget[currentIndex];

        isTargeting = true;

        if (inputProvider != null) inputProvider.enabled = false;
    }

    void ClearTarget()
    {
        isTargeting = false;
        currentTarget = null;


        if (inputProvider != null)
        {
            inputProvider.enabled = true;
        }
    }

    List<Transform> GetValidTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, maxDistance);

        List<Transform> targets = new List<Transform>();

        foreach (Collider col in hits)
        {
            if (!col.CompareTag(enemyTag)) continue;

            Transform enemy = col.transform;

            float dist = Vector3.Distance(transform.position, enemy.position);

            if (dist < minDistance || dist > maxDistance) continue;

            // BETTER CAMERA CHECK
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 toEnemy = (enemy.position - mainCamera.transform.position).normalized;

            float dot = Vector3.Dot(camForward, toEnemy);

            if (dot < 0.5f) continue;

            // SCREEN CHECK
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(enemy.position);

            if (viewportPos.z <= 0) continue;

            if (viewportPos.x < 0.1f || viewportPos.x > 0.9f) continue;

            if (viewportPos.y < 0.1f || viewportPos.y > 0.9f) continue;

            // OBSTACLE CHECK
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

        // SORT BY SCREEN CENTER
        targets = targets.OrderBy(t =>
        {
            Vector3 viewPos = mainCamera.WorldToViewportPoint(t.position);

            return Vector2.Distance(new Vector2(viewPos.x, viewPos.y), new Vector2(0.5f, 0.5f));
        }).ToList();

        return targets;
    }
    bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        return GetValidTargets().Contains(target);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}

