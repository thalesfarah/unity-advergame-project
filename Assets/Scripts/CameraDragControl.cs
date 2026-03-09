using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraDragControl : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController inputController;
    [SerializeField] private CinemachineCamera virtualCamera;

    [SerializeField] private float zoomSensitivity = 0.1f; // Ajustado para ser mais suave no Scroll e Touch
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

        // Verifica entrada de Mouse
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPointerPressed = true;
        }

        // Verifica entrada de Touch (Celular)
        // Se houver pelo menos um toque na tela e ele estiver na fase de pressionado/movendo
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            if (Touchscreen.current.touches[0].press.isPressed)
            {
                isPointerPressed = true;
            }
        }

        // Ativa o controle do Cinemachine apenas enquanto houver pressão
        if (inputController != null)
        {
            inputController.enabled = isPointerPressed;
        }
    }

    private void HandleZoomInput()
    {
        float currentFOV = virtualCamera.Lens.FieldOfView;

        // 1. Zoom via Mouse Scroll
        if (Mouse.current != null)
        {
            Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
            if (scrollDelta.y != 0)
            {
                currentFOV -= (scrollDelta.y * zoomSensitivity);
            }
        }

        // 2. Zoom via Pinch (Pinça com dois dedos no celular)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];

            if (touch0.isInProgress && touch1.isInProgress)
            {
                // Posições atuais e anteriores para calcular a variação da distância
                Vector2 pos0 = touch0.position.ReadValue();
                Vector2 pos1 = touch1.position.ReadValue();
                Vector2 lastPos0 = pos0 - touch0.delta.ReadValue();
                Vector2 lastPos1 = pos1 - touch1.delta.ReadValue();

                float currentDist = Vector2.Distance(pos0, pos1);
                float lastDist = Vector2.Distance(lastPos0, lastPos1);

                float deltaDist = currentDist - lastDist;

                // Subtraímos o delta para que "abrir os dedos" diminua o FOV (Zoom In)
                currentFOV -= (deltaDist * zoomSensitivity);
            }
        }

        // Aplica o limite e atualiza a câmera
        virtualCamera.Lens.FieldOfView = Mathf.Clamp(currentFOV, minFOV, maxFOV);
    }
}