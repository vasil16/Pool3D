using UnityEngine;

public class BlobShadow : MonoBehaviour
{
    [Tooltip("Drag the pool ball transform here")]
    [SerializeField] Transform targetBall;

    [Tooltip("Y-axis position of your pool table surface, plus a tiny offset to prevent Z-fighting")]
    [SerializeField] float tableSurfaceY = -0.48f;

    void LateUpdate()
    {
        if (targetBall == null) return;

        // Follow the ball's X and Z, but lock the Y to just above the table surface
        transform.position = new Vector3(targetBall.position.x, tableSurfaceY, targetBall.position.z);

        // Force the rotation to stay perfectly flat against the table
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}