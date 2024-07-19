using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MatchCamera : MonoBehaviour
{
    public float referenceWidth = 1920f;
    public float referenceHeight = 1080f;
    public float referenceFoV = 60f; // Only used for perspective cameras

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        AdjustFoV();
    }


    void AdjustFoV()
    {
        float targetAspect = referenceWidth / referenceHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        if (cam.orthographic)
        {
            AdjustOrthographicSize(currentAspect, targetAspect);
        }
        else
        {
            AdjustPerspectiveFoV(currentAspect, targetAspect);
        }
    }

    void AdjustOrthographicSize(float currentAspect, float targetAspect)
    {
        float referenceOrthographicSize = 2.03f;
        if (currentAspect >= targetAspect)
        {
            cam.orthographicSize = referenceOrthographicSize;
        }
        else
        {
            float scaleFactor = targetAspect / currentAspect;
            cam.orthographicSize = referenceOrthographicSize * scaleFactor;
        }
    }

    void AdjustPerspectiveFoV(float currentAspect, float targetAspect)
    {
        if (currentAspect >= targetAspect)
        {
            cam.fieldOfView = referenceFoV;
        }
        else
        {
            float scaleFactor = currentAspect / targetAspect;
            cam.fieldOfView = CalcVerticalFoV(referenceFoV, scaleFactor);
        }
    }

    float CalcVerticalFoV(float horizontalFoV, float aspectRatio)
    {
        float horizontalFoVRad = Mathf.Deg2Rad * horizontalFoV;
        float verticalFoVRad = 2f * Mathf.Atan(Mathf.Tan(horizontalFoVRad / 2f) / aspectRatio);
        return Mathf.Rad2Deg * verticalFoVRad;
    }
}
