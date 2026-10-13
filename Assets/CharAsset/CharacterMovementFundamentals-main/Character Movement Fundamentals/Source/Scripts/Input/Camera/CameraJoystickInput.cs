
using UnityEngine;

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

        private void Awake()
        {
            if (playerInputHandler == null)
            {
                playerInputHandler =
                    FindObjectOfType<PlayerInputHandler>();
            }

            if (playerInputHandler == null)
            {
                Debug.LogError(
                    "CameraJoystickInput: No se encontró PlayerInputHandler.",
                    this
                );
            }
        }

        public override float GetHorizontalCameraInput()
        {
            if (playerInputHandler == null)
                return 0f;

            float input = playerInputHandler.gamepadLookInput.x;

            if (Mathf.Abs(input) < deadZoneThreshold)
                input = 0f;

            if (invertHorizontalInput)
                input = -input;

            return input * horizontalSensitivity;
        }

        public override float GetVerticalCameraInput()
        {
            if (playerInputHandler == null)
                return 0f;

            float input = playerInputHandler.gamepadLookInput.y;

            if (Mathf.Abs(input) < deadZoneThreshold)
                input = 0f;

            if (invertVerticalInput)
                input = -input;

            return input * verticalSensitivity;
        }
    }
}
