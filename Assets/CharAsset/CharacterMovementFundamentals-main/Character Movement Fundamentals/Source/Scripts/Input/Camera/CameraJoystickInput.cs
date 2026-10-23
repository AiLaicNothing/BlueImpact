using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public class CameraJoystickInput : CameraInput
    {
        [Header("Referencia al jugador")]
        public PlayerInputHandler playerInputHandler;

        [Header("Sensibilidad")]
        public float horizontalSensitivity = 1f;
        public float verticalSensitivity = 1f;

        [Header("Configuración")]
        [Range(0f, 0.9f)]
        public float deadZoneThreshold = 0.15f;

        public bool invertHorizontalInput = false;
        public bool invertVerticalInput = false;

        [Header("Debug")]
        [Tooltip("Si está activo, lee el stick derecho directo del gamepad (prueba de aislamiento)")]
        public bool useDirectGamepadRead = false;
        public bool debugLog = true;

        private float nextLogTime;

        private void Awake()
        {
            if (playerInputHandler == null)
                playerInputHandler = FindObjectOfType<PlayerInputHandler>();

            if (playerInputHandler == null)
                Debug.LogError("CameraJoystickInput: No se encontró PlayerInputHandler.", this);
        }

        private Vector2 ReadLook()
        {
            Vector2 look;

            if (useDirectGamepadRead)
                look = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
            else
                look = playerInputHandler != null ? playerInputHandler.gamepadLookInput : Vector2.zero;

            if (debugLog && Time.unscaledTime >= nextLogTime)
            {
                nextLogTime = Time.unscaledTime + 1f;
                Debug.Log($"[CamJoy] handler={(playerInputHandler != null)} " +
                          $"direct={useDirectGamepadRead} look={look} " +
                          $"gamepad={(Gamepad.current != null ? Gamepad.current.name : "ninguno")}");
            }

            return look;
        }

        public override float GetHorizontalCameraInput()
        {
            float input = ReadLook().x;
            if (Mathf.Abs(input) < deadZoneThreshold) input = 0f;
            if (invertHorizontalInput) input = -input;
            return input * horizontalSensitivity;
        }

        public override float GetVerticalCameraInput()
        {
            float input = ReadLook().y;
            if (Mathf.Abs(input) < deadZoneThreshold) input = 0f;
            if (invertVerticalInput) input = -input;
            return input * verticalSensitivity;
        }
    }
}