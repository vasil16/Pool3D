using UnityEngine;


    public class BallEvents : MonoBehaviour
    {
        Ball ball;

        void Awake()
        {
            ball = GetComponent<Ball>();
        }

        public void HandleBallHit(Ball other)
        {
            Debug.Log($"Ball {ball.ballNumber} hit ball {other.ballNumber}");
        }

        public void HandlePocketed()
        {
            Debug.Log($"Ball pocketed: {ball.ballNumber} ({ball.ballType})");
        }
    }
