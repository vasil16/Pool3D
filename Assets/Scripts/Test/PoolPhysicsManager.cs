using System;
using UnityEngine;
using UnityEngine.UI;
public class PoolPhysicsManager : MonoBehaviour
{
    [Header("Balls")]
    public Ball[] balls;
    public int cueBallIndex = 0;

    public Slider power;

    [Header("Table")]
    public Transform tableCenter;
    public float halfWidth = 1.12f;
    public float halfLength = 2.24f;

    [Header("Physics")]
    public float gravity = 9.81f;
    public float slidingFriction = 0.20f;
    public float rollingFriction = 0.015f;
    public float ballRestitution = 0.93f;
    public float cushionRestitution = 0.88f;
    public float stopSpeed = 0.01f;

    [Header("Pockets")]
    public Transform[] pockets;
    public float pocketRadius = 0.08f;
    public float pocketCaptureDepth = 0.05f;

    public event Action<Ball, Ball> OnBallBallCollision;
    public event Action<Ball> OnBallPocketed;
    public event Action OnShotFinished;

    bool shotActive;

    public static PoolPhysicsManager instance;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        // Reset all balls to their current positions
        if (balls != null)
        {
            foreach (var ball in balls)
            {
                if (ball != null)
                    ball.ResetBall(ball.transform.position);
            }
        }

        // If you want to auto-read table center from a collider you can
        // add that logic here later, but for now we assume tableCenter
        // and halfWidth/halfLength are set correctly in the inspector.
    }

    /// <summary>
    /// Launches the cue ball with the given aim direction and power.
    /// </summary>
    ///


    void OnDrawGizmos()
    {
        // Draw table bounds
        Vector3 center = tableCenter != null ? tableCenter.position : Vector3.zero;
        center.y = 0.74f;
        Gizmos.color = Color.green;
        Vector3 size = new Vector3(halfWidth * 2f, 0.01f, halfLength * 2f);
        Gizmos.DrawWireCube(center, size);

        // Draw pocket zones
        if (pockets != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // orange
            foreach (var pocket in pockets)
            {
                if (pocket == null) continue;

                // Horizontal capture radius at pocket height
                Vector3 pocketPos = pocket.position;
                Gizmos.DrawWireSphere(pocketPos, pocketRadius);

                // Vertical capture depth as a small line
                Vector3 up = Vector3.up * pocketCaptureDepth;
                Gizmos.DrawLine(pocketPos - up, pocketPos + up);
            }
        }
    }

    public void shoo(int ballIndex)
    {
        Shoot(balls[ballIndex].transform.position - balls[cueBallIndex].transform.position, power.value);
    }


    public void Shoot(Vector3 aimDir, float power)
    {
        power *= 0.01f;
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude < 0.0001f)
            return;

        aimDir.Normalize();

        if (balls == null ||
          cueBallIndex < 0 ||
          cueBallIndex >= balls.Length ||
          balls[cueBallIndex] == null)
            return;

        Ball cue = balls[cueBallIndex];

        cue.velocity = aimDir * (power * 8f);
        cue.angularVelocity = Vector3.zero;
        cue.state = BallMotionState.Sliding;
        cue.pocketed = false;
        cue.gameObject.SetActive(true);

        // Reset other balls’ dynamic state (optional)
        for (int i = 0; i < balls.Length; i++)
        {
            Ball b = balls[i];
            if (b == null || i == cueBallIndex) continue;

            // Keep their positions, but clear motion state for a clean shot
            b.velocity = Vector3.zero;
            b.angularVelocity = Vector3.zero;
            if (!b.pocketed)
                b.state = BallMotionState.Stationary;
        }

        shotActive = true;
    }

    void FixedUpdate()
    {
        if (!shotActive || balls == null) return;

        float dt = Time.fixedDeltaTime;

        // 1. Advance all balls
        foreach (var ball in balls)
        {
            if (ball != null && !ball.pocketed)
                StepBall(ball, dt);
        }

        // 2. Resolve all ball-ball collisions
        ResolveAllBallBallCollisions();

        // 3. Resolve cushions for all balls
        ResolveAllCushions();

        // 4. Resolve pockets for all balls
        ResolveAllPockets();

        // 5. Sync transforms (here it's just ensuring we write positions)
        foreach (var ball in balls)
        {
            if (ball != null && !ball.pocketed)
                ball.transform.position = ball.transform.position;
        }

        // 6. Check if shot is finished (no moving balls)
        bool anyMoving = false;
        foreach (var ball in balls)
        {
            if (ball != null && !ball.pocketed &&
              ball.velocity.sqrMagnitude > stopSpeed * stopSpeed)
            {
                anyMoving = true;
                break;
            }
        }

        if (!anyMoving)
        {
            Debug.Log("done");
            shotActive = false;
            OnShotFinished?.Invoke();
        }
    }

    void StepBall(Ball ball, float dt)
    {
        if (ball.pocketed || ball.state == BallMotionState.Pocketed)
            return;

        float speed = ball.velocity.magnitude;
        if (speed < stopSpeed)
        {
            ball.velocity = Vector3.zero;
            ball.angularVelocity = Vector3.zero;
            ball.state = BallMotionState.Stationary;
            return;
        }

        Vector3 moveDir = ball.velocity.normalized;

        if (ball.state == BallMotionState.Sliding)
        {
            float dv = slidingFriction * gravity * dt;
            ball.velocity = Vector3.MoveTowards(ball.velocity, Vector3.zero, dv);

            if (ball.velocity.magnitude <= stopSpeed * 2f)
                ball.state = BallMotionState.Rolling;
        }
        else if (ball.state == BallMotionState.Rolling)
        {
            float dv = rollingFriction * gravity * dt;
            ball.velocity = Vector3.MoveTowards(ball.velocity, Vector3.zero, dv);

            if (ball.velocity.magnitude <= stopSpeed)
                ball.state = BallMotionState.Stationary;
        }

        ball.transform.position += moveDir * speed * dt;
    }

    void ResolveAllBallBallCollisions()
    {
        int count = balls.Length;
        for (int i = 0; i < count; i++)
        {
            Ball a = balls[i];
            if (a == null || a.pocketed) continue;

            for (int j = i + 1; j < count; j++)
            {
                Ball b = balls[j];
                if (b == null || b.pocketed) continue;

                ResolveBallBall(a, b);
            }
        }
    }

    void ResolveBallBall(Ball a, Ball b)
    {
        Vector3 delta = b.transform.position - a.transform.position;
        float dist = delta.magnitude;
        float minDist = a.radius + b.radius;

        if (dist <= 0.0001f || dist >= minDist)
            return;

        Vector3 n = delta / dist;
        float penetration = minDist - dist;

        a.transform.position -= n * (penetration * 0.5f);
        b.transform.position += n * (penetration * 0.5f);

        Vector3 relVel = b.velocity - a.velocity;
        float velAlongNormal = Vector3.Dot(relVel, n);

        if (velAlongNormal > 0f)
            return;

        float invMassA = 1f / a.mass;
        float invMassB = 1f / b.mass;

        float j = -(1f + ballRestitution) * velAlongNormal / (invMassA + invMassB);
        Vector3 impulse = j * n;

        a.velocity -= impulse * invMassA;
        b.velocity += impulse * invMassB;

        if (a.velocity.magnitude > stopSpeed)
            a.state = BallMotionState.Sliding;
        if (b.velocity.magnitude > stopSpeed)
            b.state = BallMotionState.Sliding;

        OnBallBallCollision?.Invoke(a, b);
    }

    void ResolveAllCushions()
    {
        foreach (var ball in balls)
        {
            if (ball != null && !ball.pocketed)
                ResolveCushions(ball);
        }
    }

    void ResolveCushions(Ball ball)
    {

        if (IsInPocketZone(ball))
            return;


        Debug.Log($"Ball {ball.ballNumber} cushion hit at {ball.transform.position}");

        Vector3 center = tableCenter != null ? tableCenter.position : Vector3.zero;

        float minX = center.x - halfWidth + ball.radius;
        float maxX = center.x + halfWidth - ball.radius;
        float minZ = center.z - halfLength + ball.radius;
        float maxZ = center.z + halfLength - ball.radius;

        Vector3 p = ball.transform.position;
        bool hit = false;

        if (p.x < minX)
        {
            p.x = minX;
            ball.velocity.x = Mathf.Abs(ball.velocity.x) * cushionRestitution;
            hit = true;
        }
        else if (p.x > maxX)
        {
            p.x = maxX;
            ball.velocity.x = -Mathf.Abs(ball.velocity.x) * cushionRestitution;
            hit = true;
        }

        if (p.z < minZ)
        {
            p.z = minZ;
            ball.velocity.z = Mathf.Abs(ball.velocity.z) * cushionRestitution;
            hit = true;
        }
        else if (p.z > maxZ)
        {
            p.z = maxZ;
            ball.velocity.z = -Mathf.Abs(ball.velocity.z) * cushionRestitution;
            hit = true;
        }

        ball.transform.position = p;

        if (hit && ball.velocity.magnitude > stopSpeed)
            ball.state = BallMotionState.Sliding;
    }

    bool IsInPocketZone(Ball ball)
    {
        if (pockets == null) return false;

        foreach (var pocket in pockets)
        {
            if (pocket == null) continue;

            Vector3 flatBall = ball.transform.position;
            Vector3 flatPocket = pocket.position;
            flatBall.y = 0f;
            flatPocket.y = 0f;

            // Use a bigger radius, e.g. 1.8–2.5 times pocketRadius
            if (Vector3.Distance(flatBall, flatPocket) <= pocketRadius * 2.0f)
                return true;
        }

        return false;
    }



    void ResolveAllPockets()
    {
        if (pockets == null || pockets.Length == 0)
            return;

        foreach (var ball in balls)
        {
            if (ball == null || ball.pocketed) continue;

            foreach (var pocket in pockets)
            {
                if (pocket == null) continue;
                ResolvePocket(ball, pocket);
            }
        }
    }

    void ResolvePocket(Ball ball, Transform pocket)
    {
        if (ball.pocketed) return;

        Vector3 flatBall = ball.transform.position;
        Vector3 flatPocket = pocket.position;

        // Flatten to XZ only
        flatBall.y = 0f;
        flatPocket.y = 0f;

        float d = Vector3.Distance(flatBall, flatPocket);

        if (d <= pocketRadius)
        {
            ball.SetPocketed();
            OnBallPocketed?.Invoke(ball);
        }
    }
}