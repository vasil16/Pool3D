using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoolMain : MonoBehaviour
{
    [SerializeField] List<GameObject> balls;
    [SerializeField] List<Transform> placeableBalls, shadows;
    [SerializeField] List<Texture> ballTexture;

    [SerializeField] Transform tableFootSpot;
    [SerializeField] float ballRadius, tableSurfaceY;

    MaterialPropertyBlock _propBlock;

    void Start()
    {
        if (ballRadius <= 0)
        {
            Debug.LogError("ballRadius must be strictly greater than 0. Check your inspector.");
            return;
        }

        _propBlock = new MaterialPropertyBlock();
        SetBallTextures();
        PlayerPrefs.DeleteAll();
    }

    void SetBallTextures()
    {
        for (int i = 0; i < balls.Count; i++)
        {
            Renderer ballRenderer = balls[i].GetComponent<Renderer>();
            Texture newTexture = ballTexture[i];

            _propBlock.Clear();
            _propBlock.SetTexture("_BaseMap", newTexture);
            ballRenderer.SetPropertyBlock(_propBlock);
        }

        RackBalls(placeableBalls, tableFootSpot, ballRadius);
    }

    public void RackBalls(List<Transform> ballsToRack, Transform footSpot, float radius, float rackDirection = 1f)
    {
        if (ballsToRack.Count != 15)
        {
            Debug.LogError("You must pass exactly 15 balls to rack.");
            return;
        }

        float colSpacing = radius * 2f;
        float rowSpacing = radius * Mathf.Sqrt(3f);
        int currentBallIndex = 0;

        for (int row = 0; row < 5; row++)
        {
            float startX = -((float)row / 2f) * colSpacing;
            float zPosition = row * rowSpacing * rackDirection;

            for (int col = 0; col <= row; col++)
            {
                float xPosition = startX + (col * colSpacing);
                Transform targetBall = ballsToRack[currentBallIndex];

                Rigidbody rb = targetBall.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Vector3 localOffset = (footSpot.right * xPosition) + (footSpot.forward * zPosition);
                targetBall.position = footSpot.position + localOffset;

                currentBallIndex++;
            }
        }
    }

    void LateUpdate()
    {
        if (balls.Count != shadows.Count)
        {
            Debug.LogError("The number of balls and shadows must be identical.");
            return;
        }

        for (int i = 0; i < balls.Count; i++)
        {
            Vector3 ballPos = balls[i].transform.position;

            shadows[i].position = new Vector3(ballPos.x, tableSurfaceY, ballPos.z);
        }
    }
}