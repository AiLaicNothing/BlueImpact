using UnityEngine;

public class LerpRune : MonoBehaviour
{
    [SerializeField] Transform poseA;
    [SerializeField] Transform poseB;
    [SerializeField] float moveSpeed = 1f;

    [Header("Rotación - Objeto Llamativo")]
    [SerializeField] float rotSpeedY = 360f; // Rotación principal rápida
    [SerializeField] float rotSpeedX = 30f;  // Wobble lateral
    [SerializeField] float rotSpeedZ = 15f;  // Wobble de cabeceo

    [Header("Movimiento Visual")]
    [SerializeField] float bobHeight = 0.5f;      // Altura del bob vertical
    [SerializeField] float bobSpeed = 2f;         // Velocidad del bob
    [SerializeField] float pulseScale = 0.1f;     // Qué tanto crece/encoge

    private Vector3 initialPosition;
    private Vector3 initialScale;

    void Start()
    {
        initialPosition = transform.position;
        initialScale = transform.localScale;
    }

    void FixedUpdate()
    {
        // Movimiento entre poses
        float t = Mathf.PingPong(Time.time * moveSpeed, 1f);
        Vector3 lerpPos = Vector3.Lerp(poseA.position, poseB.position, t);

        // Bobbing vertical (flotación)
        float bobY = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = lerpPos + Vector3.up * bobY;

        // Rotación llamativa: eje Y constante + wobble
        float wobbleX = Mathf.Sin(Time.time * rotSpeedX * 0.1f) * 15f;
        float wobbleZ = Mathf.Cos(Time.time * rotSpeedZ * 0.1f) * 10f;

        transform.Rotate(
            wobbleX * Time.deltaTime,
            rotSpeedY * Time.deltaTime,
            wobbleZ * Time.deltaTime,
            Space.Self
        );

        // Escalado pulsante (crece y encoge suavemente)
        float pulse = 1f + Mathf.Sin(Time.time * bobSpeed * 0.5f) * pulseScale;
        transform.localScale = initialScale * pulse;
    }
}