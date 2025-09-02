using System.Collections;
using UnityEngine;
using Fusion;

public class Pocket : MonoBehaviour
{
    [SerializeField] AudioClip pocketClip;

    GamePlayController playerController;

    private void Awake()
    {
        playerController = GamePlayController.instance;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!playerController.isFoul) GameManager.instance.PlaySound(pocketClip);
        BallBehaviour pocketedBall = other.gameObject.GetComponent<BallBehaviour>();
        other.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

        if (other.gameObject.CompareTag("cueBall"))
        {
            playerController.isFoul = true;
            StartCoroutine(GameManager.instance.Popup("Foul!! Cue ball pocketed"));
        }
        else if (pocketedBall.ballType == BallBehaviour.BallType.black)
        {
            if (playerController.firstBreak)
            {
                GameManager.instance.GameCompleteEvent(GameManager.instance.GetOpponent(GameManager.instance.currentPlayer));
                return;
            }
            if (GameManager.instance.players[GameManager.instance.currentPlayer].pocketedBalls.Count == 7)
            {
                StartCoroutine(Wait());
            }
            else
            {
                GameManager.instance.GameCompleteEvent(GameManager.instance.GetOpponent(GameManager.instance.currentPlayer));
            }
        }
        else
        {
            other.gameObject.SetActive(false);
            GameManager.instance.pocketedBalls.Add(other.gameObject);
            playerController.balls.Remove(other.gameObject);
            playerController.pocketed = true;
            if (playerController.firstBreak)
            {
                
            }
            else
            {
                if (!playerController.ballAssigned)
                {
                    HandleFirstBallAssignment(pocketedBall);
                    return;
                }
                else
                {
                    HandleSubsequentBallPocket(pocketedBall);
                }
            }
        }
    }

    private void HandleFirstBallAssignment(BallBehaviour pocketedBall)
    {
        playerController.ballAssigned = true;

        // Set ball types locally
        GameManager.instance.players[GameManager.instance.currentPlayer].BallType = pocketedBall.ballType;
        GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].BallType =
            pocketedBall.ballType == BallBehaviour.BallType.stripe ? BallBehaviour.BallType.solid : BallBehaviour.BallType.stripe;

        // SYNC BALL ASSIGNMENT TO ALL PLAYERS IN ONLINE MODE
        if (GameManager.instance.gameMode == GameManager.GameMode.online)
        {
            SyncBallAssignmentThroughNetworkPlayers();
        }

        // CPU ball setup (offline only)
        if (GameManager.instance.gameMode != GameManager.GameMode.online)
        {
            if (GameManager.instance.players[GameManager.instance.currentPlayer].name == "CPU")
            {
                foreach (GameObject ball in playerController.balls)
                {
                    if (ball.GetComponent<BallBehaviour>().ballType == pocketedBall.ballType)
                    {
                        playerController.cpuBalls.Add(ball);
                    }
                }
            }            
        }

        GameManager.instance.SetBallImages();

        // Process already pocketed balls
        foreach (GameObject gBall in GameManager.instance.pocketedBalls)
        {
            BallBehaviour ballBehaviour = gBall.GetComponent<BallBehaviour>();
            if (ballBehaviour.ballType == GameManager.instance.players[GameManager.instance.currentPlayer].BallType)
            {
                GameManager.instance.players[GameManager.instance.currentPlayer].pocketedBalls.Add(gBall);
                GameManager.instance.DisableBallImage(ballBehaviour.ballCode);
            }
            else
            {
                GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].pocketedBalls.Add(gBall);
                GameManager.instance.DisableBallImage(ballBehaviour.ballCode);
            }
        }

        // SYNC POCKETED BALLS TO OTHER PLAYERS
        if (GameManager.instance.gameMode == GameManager.GameMode.online)
        {
            SyncPocketedBallsThroughNetworkPlayers();
        }
    }

    private void HandleSubsequentBallPocket(BallBehaviour pocketedBall)
    {
        if (pocketedBall.ballType == GameManager.instance.players[GameManager.instance.currentPlayer].BallType)
        {
            playerController.pocketed = true;
            GameManager.instance.players[GameManager.instance.currentPlayer].pocketedBalls.Add(pocketedBall.gameObject);
            GameManager.instance.DisableBallImage(pocketedBall.ballCode);
        }
        else
        {
            GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].pocketedBalls.Add(pocketedBall.gameObject);
            GameManager.instance.DisableBallImage(pocketedBall.ballCode);
        }

        // SYNC POCKETED BALL TO OTHER PLAYERS IN ONLINE MODE
        if (GameManager.instance.gameMode == GameManager.GameMode.online)
        {
            SyncSinglePocketedBallThroughNetworkPlayers(pocketedBall);
        }
    }

    private void SyncBallAssignmentThroughNetworkPlayers()
    {
        var networkPlayers = FindObjectsOfType<NetworkPlayer>();
        
        if (networkPlayers.Length > 0)
        {
            foreach (var netPlayer in networkPlayers)
            {
                netPlayer.balllsAssigned = true;
                if (netPlayer.Object.HasStateAuthority)
                {
                    // Use the NEW RPC method that only handles ball assignment
                    netPlayer.RPC_SyncBallAssignmentOnly(
                        GameManager.instance.players[GameManager.Users.player1].name,
                        GameManager.instance.players[GameManager.Users.player2].name,
                        GameManager.instance.players[GameManager.Users.player1].BallType,
                        GameManager.instance.players[GameManager.Users.player2].BallType
                    );
                    break;
                }
            }
        }
    }

    private void SyncPocketedBallsThroughNetworkPlayers()
    {
        var networkPlayers = FindObjectsOfType<NetworkPlayer>();
        if (networkPlayers.Length > 0)
        {
            foreach (var netPlayer in networkPlayers)
            {
                if (netPlayer.Object.HasStateAuthority)
                {
                    // Sync all pocketed balls
                    foreach (GameObject gBall in GameManager.instance.pocketedBalls)
                    {
                        BallBehaviour ballBehaviour = gBall.GetComponent<BallBehaviour>();
                        int playerIndex = (ballBehaviour.ballType == GameManager.instance.players[GameManager.Users.player1].BallType) ? 0 : 1;
                        netPlayer.RPC_SyncPocketedBall(playerIndex, ballBehaviour.ballCode);
                    }
                    break;
                }
            }
        }
    }

    private void SyncSinglePocketedBallThroughNetworkPlayers(BallBehaviour pocketedBall)
    {
        var networkPlayers = FindObjectsOfType<NetworkPlayer>();
        if (networkPlayers.Length > 0)
        {
            foreach (var netPlayer in networkPlayers)
            {
                if (netPlayer.Object.HasStateAuthority)
                {
                    int playerIndex = (pocketedBall.ballType == GameManager.instance.players[GameManager.Users.player1].BallType) ? 0 : 1;
                    netPlayer.RPC_SyncPocketedBall(playerIndex, pocketedBall.ballCode);
                    break;
                }
            }
        }
    }

    IEnumerator Wait()
    {
        yield return new WaitUntil(playerController.BallStopped);
        if (!playerController.isFoul)
        {
            GameManager.instance.GameCompleteEvent(GameManager.instance.currentPlayer);
        }
        else
        {
            GameManager.instance.GameCompleteEvent(GameManager.instance.GetOpponent(GameManager.instance.currentPlayer));
        }
    }
}