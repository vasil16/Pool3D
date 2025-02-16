using System.Collections;
using UnityEngine;

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
        if(!playerController.isFoul)playerController.gameAudio.PlayOneShot(pocketClip);
        BallBehaviour pocketedBall = other.gameObject.GetComponent<BallBehaviour>();
        other.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

        if (other.gameObject.CompareTag("cueBall"))
        {
            playerController.isFoul = true;
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

            if (playerController.firstBreak)
            {
                playerController.pocketed = true;
            }
            else
            {
                if (playerController.firstPot)
                {
                    playerController.firstPot = false;
                    GameManager.instance.players[GameManager.instance.currentPlayer].BallType = pocketedBall.ballType;
                    GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].BallType = pocketedBall.ballType == BallBehaviour.BallType.stripe ? BallBehaviour.BallType.solid : BallBehaviour.BallType.stripe;

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
                    else
                    {
                        foreach (GameObject ball in playerController.balls)
                        {
                            if (ball.GetComponent<BallBehaviour>().ballType != pocketedBall.ballType && ball.GetComponent<BallBehaviour>().ballType != BallBehaviour.BallType.white && ball.GetComponent<BallBehaviour>().ballType != BallBehaviour.BallType.black)
                            {
                                playerController.cpuBalls.Add(ball);
                            }
                        }
                    }

                    playerController.player1Txt.text = GameManager.instance.player1.BallType + "";
                    playerController.player2Txt.text = GameManager.instance.player2.BallType + "";
                    playerController.pocketed = true;
                    GameManager.instance.SetBallImages();
                    foreach (GameObject gBall in GameManager.instance.pocketedBalls)
                    {
                        if (gBall.GetComponent<BallBehaviour>().ballType == GameManager.instance.players[GameManager.instance.currentPlayer].BallType)
                        {
                            GameManager.instance.players[GameManager.instance.currentPlayer].pocketedBalls.Add(gBall);
                            GameManager.instance.players[GameManager.instance.currentPlayer].DisableBallImage(gBall.GetComponent<BallBehaviour>().ballCode);
                        }
                        else
                        {
                            GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].pocketedBalls.Add(gBall);
                            GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].DisableBallImage(gBall.GetComponent<BallBehaviour>().ballCode);
                        }
                    }

                    return;
                }

                else
                if (pocketedBall.ballType == GameManager.instance.players[GameManager.instance.currentPlayer].BallType)
                {
                    playerController.pocketed = true;
                    GameManager.instance.players[GameManager.instance.currentPlayer].pocketedBalls.Add(pocketedBall.gameObject);
                    GameManager.instance.players[GameManager.instance.currentPlayer].DisableBallImage(pocketedBall.ballCode);
                }
                else
                {
                    GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].pocketedBalls.Add(pocketedBall.gameObject);
                    GameManager.instance.players[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].DisableBallImage(pocketedBall.ballCode);
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