using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class AdaptiveCanvasScaler : MonoBehaviour
{
    public Vector2 referenceResolution = new Vector2(1920, 1080);

    private CanvasScaler scaler;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        float targetAspect = referenceResolution.x / referenceResolution.y;
        float currentAspect = (float)Screen.width / Screen.height;

        // Wider screens -> preserve height
        // Taller screens -> preserve width
        scaler.matchWidthOrHeight = currentAspect > targetAspect ? 1f : 0f;
    }
}