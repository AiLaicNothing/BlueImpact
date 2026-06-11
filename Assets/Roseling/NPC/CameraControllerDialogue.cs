using UnityEngine;
using System.Collections;

public class CameraControllerDialogue : MonoBehaviour
{
    public Transform targetPosition;
    public Transform lookAtTarget;

    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;

    void LateUpdate()
    {
        if (targetPosition != null)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition.position, Time.deltaTime * moveSpeed);
        }

        if (lookAtTarget != null)
        {
            Vector3 direction = lookAtTarget.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void MoveTo(Transform pos, Transform lookAt)
    {
        targetPosition = pos;
        lookAtTarget = lookAt;
    }

}
