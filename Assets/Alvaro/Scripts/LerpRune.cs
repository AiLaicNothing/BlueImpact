using System;
using UnityEngine;

public class LerpRune : MonoBehaviour
{
    [SerializeField] Transform poseA;
    [SerializeField] Transform poseB;
    [SerializeField] float speed;
    [SerializeField] float rotSpeed;
    void FixedUpdate()
    {
        float t = Mathf.PingPong(Time.time * speed, 1f);
        transform.position = Vector3.Lerp(poseA.position, poseB.position, t);
        transform.Rotate(0, rotSpeed * Time.deltaTime, 0, Space.Self);
    }
}
