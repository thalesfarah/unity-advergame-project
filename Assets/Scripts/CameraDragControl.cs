using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraDragControl : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController inputController;
    [SerializeField] private CinemachineCamera virtualCamera;

    [SerializeField] private float zoomSensitivity = 0.05f;
    [SerializeField] private float minFOV = 20f;
    [SerializeField] private float maxFOV = 60f;

    void Update()
    {
        HandleRotationInput();
        HandleZoomInput();
    }

    private void HandleRotationInput()
    {
        bool isPointerPressed = false;

        // Verifica Mouse
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPointerPressed = true;
        }

        // Verifica Touch (Celular)
        // Mudamos para detectar se há QUALQUER toque pressionado
        if (Touchscreen.current != null && Touchscreen.current.touches.Count == 1)
        {
            // Verificamos a fase do toque diretamente
            var touch = Touchscreen.current.touches[0];
            if (touch.press.isPressed)
            {
                isPointerPressed = true;
            }
        }

        if (inputController != null)
        {
            inputController.enabled = isPointerPressed;
        }
    }

    private void HandleZoomInput()
    {
        float currentFOV = virtualCamera.Lens.FieldOfView;

        // Zoom Mouse
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.1f)
            {
                currentFOV -= (scroll * zoomSensitivity * 0.1f);
            }
        }

        // Zoom Touch (Pinch)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            var t0 = Touchscreen.current.touches[0];
            var t1 = Touchscreen.current.touches[1];

            // No novo Input System, checamos se os dedos estão em movimento ou parados na tela
            if (t0.isInProgress && t1.isInProgress)
            {
                Vector2 pos0 = t0.position.ReadValue();
                Vector2 pos1 = t1.position.ReadValue();
                Vector2 delta0 = t0.delta.ReadValue();
                Vector2 delta1 = t1.delta.ReadValue();

                Vector2 prevPos0 = pos0 - delta0;
                Vector2 prevPos1 = pos1 - delta1;

                float prevDist = Vector2.Distance(prevPos0, prevPos1);
                float currentDist = Vector2.Distance(pos0, pos1);

                if (Mathf.Abs(currentDist - prevDist) > 0.01f)
                {
                    float deltaDist = currentDist - prevDist;
                    currentFOV -= deltaDist * zoomSensitivity;
                }
            }
        }

        virtualCamera.Lens.FieldOfView = Mathf.Clamp(currentFOV, minFOV, maxFOV);
    }
}