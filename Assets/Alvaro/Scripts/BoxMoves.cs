using UnityEngine;

public class BoxMoves : MonoBehaviour
{
    [SerializeField] Transform poseA;
    [SerializeField] Transform poseB;
    [SerializeField] float moveSpeed = 1f;

    void FixedUpdate()
    {
        float t = Mathf.PingPong(Time.time * moveSpeed, 1f);
        Vector3 lerpPos = Vector3.Lerp(poseA.position, poseB.position, t);
        transform.position = lerpPos + Vector3.up;
    }
}