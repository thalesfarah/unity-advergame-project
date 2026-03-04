using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraDragControl : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController inputController;
    [SerializeField] private CinemachineCamera virtualCamera;

    [SerializeField] private float zoomSensitivity = 2f;
    [SerializeField] private float minFOV = 20f;
    [SerializeField] private float maxFOV;


    void Update()
    {
        // No novo Input System, checamos o estado do botão esquerdo assim:
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            inputController.enabled = true;
        }
        else
        {
            inputController.enabled = false;
        }
        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
        if (scrollDelta.y != 0)
        {
            // Pegamos o FOV atual da lente
            float currentFOV = virtualCamera.Lens.FieldOfView;

            // Calculamos o novo FOV baseado na direção do scroll (positivo ou negativo)
            // Dividimos por um valor base (ex: 120) para normalizar a velocidade entre diferentes mouses
            currentFOV -= (scrollDelta.y * zoomSensitivity);

            // Aplicamos o Clamp para não inverter a câmera ou aproximar demais
            virtualCamera.Lens.FieldOfView = Mathf.Clamp(currentFOV, minFOV, maxFOV);
        }
    }
}