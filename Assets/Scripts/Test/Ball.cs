using UnityEngine;


    public class Ball : MonoBehaviour
    {
        public int ballNumber;
        public string ballType;
        public bool isCueBall;

        public float radius = 0.028575f;
        public float mass = 0.17f;

        [HideInInspector] public Vector3 velocity;
        [HideInInspector] public Vector3 angularVelocity;
        [HideInInspector] public BallMotionState state = BallMotionState.Stationary;
        [HideInInspector] public bool pocketed;

        public void ResetBall(Vector3 position)
        {
            transform.position = position;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            state = BallMotionState.Stationary;
            pocketed = false;
            gameObject.SetActive(true);
        }

        public void SetPocketed()
        {
            pocketed = true;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            state = BallMotionState.Pocketed;
            gameObject.SetActive(false);
        }
    }
