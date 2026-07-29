using UnityEngine;

public static class PoolMath
{
    public static Vector3 GhostBallPosition(
        Vector3 objectBall,
        Vector3 pocket,
        float ballRadius)
    {
        Vector3 dir = (pocket - objectBall).normalized;
        return objectBall - dir * (ballRadius * 2f);
    }

    public static Vector3 ObjectBallDirection(
        Vector3 cueCentre,
        Vector3 objectBall)
    {
        return (objectBall - cueCentre).normalized;
    }

    public static float SolveCueContactDistance(
        Vector3 cueOrigin,
        Vector3 shotDirection,
        Vector3 objectCentre,
        float combinedRadius)
    {
        Vector3 toTarget = objectCentre - cueOrigin;

        float closest =
            Vector3.Dot(toTarget, shotDirection);

        float perpSq =
            toTarget.sqrMagnitude -
            closest * closest;

        float rSq =
            combinedRadius * combinedRadius;

        if (perpSq > rSq)
            return -1f;

        float halfChord =
            Mathf.Sqrt(rSq - perpSq);

        return closest - halfChord;
    }

    public static bool CuePathBlocked(
        Vector3 cueOrigin,
        Vector3 ghostBall,
        float cueRadius,
        LayerMask ballMask,
        Transform targetBall)
    {
        Vector3 dir =
            (ghostBall - cueOrigin).normalized;

        float distance =
            Vector3.Distance(
                cueOrigin,
                ghostBall);

        RaycastHit[] hits =
            Physics.SphereCastAll(
                cueOrigin,
                cueRadius * .98f,
                dir,
                distance);

        foreach (RaycastHit hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & ballMask) == 0)
                continue;

            if (hit.transform == targetBall)
                continue;

            return true;
        }

        return false;
    }

    public static bool ObjectPathBlocked(
        Vector3 objectBall,
        Vector3 pocket,
        float ballRadius,
        LayerMask mask,
        Transform self)
    {
        Vector3 dir =
            (pocket - objectBall).normalized;

        float dist =
            Vector3.Distance(
                objectBall,
                pocket);

        RaycastHit[] hits =
            Physics.SphereCastAll(
                objectBall,
                ballRadius * .98f,
                dir,
                dist);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == self)
                continue;

            if (hit.collider.CompareTag("playBall"))
                return true;

            if (hit.collider.CompareTag("cushion"))
                return true;
        }

        return false;
    }
}