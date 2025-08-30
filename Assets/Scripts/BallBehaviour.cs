using System.Collections;
using UnityEngine;
using Fusion;
using static NetworkPlayersHandler;

public class BallBehaviour : NetworkBehaviour
{
    public enum BallType
    {
        stripe,
        solid,
        white,
        black
    }

    [SerializeField] public int ballCode;
    [SerializeField] AudioClip ballHit, cushionHit;
    GamePlayController playerController;
    public BallType ballType;

    // NETWORK PROPERTIES
    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private Vector3 NetworkVelocity { get; set; }
    [Networked] private Quaternion NetworkRotation { get; set; }

    private Rigidbody _rb;

    private void Awake()
    {
        playerController = GamePlayController.instance;
        _rb = GetComponent<Rigidbody>();
    }

    public override void Spawned()
    {
        Debug.Log($"Ball {gameObject.name} spawned on network");

        // Initialize network state with current physics state
        if (Object.HasStateAuthority)
        {
            NetworkPosition = _rb.position;
            NetworkVelocity = _rb.linearVelocity;
            NetworkRotation = _rb.rotation;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            if (Object.HasStateAuthority)
            {
                // Update network state from physics (server)
                NetworkPosition = _rb.position;
                NetworkVelocity = _rb.linearVelocity;
                NetworkRotation = _rb.rotation;
            }
            else
            {
                // Apply network state to physics (clients)
                _rb.position = NetworkPosition;
                _rb.linearVelocity = NetworkVelocity;
                _rb.rotation = NetworkRotation;

                // Keep physics awake to prevent sleeping
                _rb.WakeUp();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag is "playBall" or "cueBall")
        {
            if (!GameManager.instance) return;
            GameManager.instance.ballhitCount++;
            GameManager.instance.PlayBallSound(ballHit);
        }

        if (collision.gameObject.CompareTag("playBall"))
        {
            if (playerController.ballAssigned)
            {
                if (!playerController.firstHit)
                {
                    BallBehaviour ball = collision.gameObject.GetComponent<BallBehaviour>();
                    playerController.firstHit = true;
                    if (!GameManager.instance.CorrectBallPlayed(ball.ballType))
                    {
                        playerController.isFoul = true;
                        StartCoroutine(GameManager.instance.Popup("Foul!! Different ball played"));
                        Debug.Log("foul");
                    }
                }
            }
        }
        else if (collision.gameObject.CompareTag("pocket"))
        {
            GameManager.instance.PlaySound(cushionHit);
        }
    }

    IEnumerator CutOff()
    {
        yield return new WaitForSeconds(0.1f);
        Vector3 relVelocity = _rb.linearVelocity;
        _rb.linearVelocity = (relVelocity * 0.06f);
    }

    // NETWORK SYNC HELPER METHODS
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ForceSync()
    {
        // Force immediate synchronization
        if (Object.HasStateAuthority)
        {
            NetworkPosition = _rb.position;
            NetworkVelocity = _rb.linearVelocity;
            NetworkRotation = _rb.rotation;
        }
        else
        {
            _rb.position = NetworkPosition;
            _rb.linearVelocity = NetworkVelocity;
            _rb.rotation = NetworkRotation;
        }
        _rb.WakeUp();
    }

    // Called when ball is pocketed or needs to be reset
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetBall(Vector3 position, Quaternion rotation)
    {
        _rb.position = position;
        _rb.rotation = rotation;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        if (Object.HasStateAuthority)
        {
            NetworkPosition = position;
            NetworkRotation = rotation;
            NetworkVelocity = Vector3.zero;
        }
    }
}