using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraDragControl : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController inputController;
    [SerializeField] private CinemachineCamera virtualCamera;

    [SerializeField] private float zoomSensitivity = 0.05f; // Sensibilidade menor para celular
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

        // Mouse: Botão esquerdo
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPointerPressed = true;
        }
#elif UNITY_IOS || UNITY_ANDROID
        // Touch: Apenas se houver EXATAMENTE 1 dedo (para não rotacionar durante o Pinch Zoom)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count == 1)
        {
            if (Touchscreen.current.touches[0].press.isPressed)
            {
                isPointerPressed = true;
            }
        }
#endif
        if (inputController != null)
        {
            inputController.enabled = isPointerPressed;
        }
    }
    private void HandleZoomInput()
    {
        float currentFOV = virtualCamera.Lens.FieldOfView;

#if UNITY_EDITOR
        // 1. Scroll do Mouse
        if (Mouse.current != null)
        {
            Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
            if (scrollDelta.y != 0)
            {
                // Multiplicamos por um valor fixo pois o scrollDelta.y costuma ser alto (120/-120)
                currentFOV -= (scrollDelta.y * zoomSensitivity * 0.1f);
            }
        }
#elif UNITY_IOS || UNITY_ANDROID
        // 2. Pinch Zoom (Dois dedos)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            var t0 = Touchscreen.current.touches[0];
            var t1 = Touchscreen.current.touches[1];

            if (t0.isInProgress && t1.isInProgress)
            {
                Vector2 pos0 = t0.position.ReadValue();
                Vector2 pos1 = t1.position.ReadValue();
                Vector2 prevPos0 = pos0 - t0.delta.ReadValue();
                Vector2 prevPos1 = pos1 - t1.delta.ReadValue();

                float prevDist = Vector2.Distance(prevPos0, prevPos1);
                float currentDist = Vector2.Distance(pos0, pos1);
                float deltaDist = currentDist - prevDist;

                currentFOV -= deltaDist * zoomSensitivity;
            }
        }
#endif
        virtualCamera.Lens.FieldOfView = Mathf.Clamp(currentFOV, minFOV, maxFOV);
    }
}