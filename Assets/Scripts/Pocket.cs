using UnityEngine;

public class Pocket : MonoBehaviour
{
    [SerializeField] AudioClip pocketClip;
    private void OnTriggerEnter(Collider other)
    {
        PoolMain.instance.gameAudio.PlayOneShot(pocketClip);
        BallBehaviour pocketedBall = other.GetComponent<BallBehaviour>();
        other.attachedRigidbody.constraints = RigidbodyConstraints.None;

        if (other.CompareTag("cueBall"))
        {
            PoolMain.instance.isFoul = true;
        }

        else if (pocketedBall.ballType == BallBehaviour.BallType.black)
        {
            if(PoolMain.instance.firstBreak)
            {
                GameLogic.instance.GameCompleteEvent(GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer));
                return;
            }
            if(GameLogic.instance.players[GameLogic.instance.currentPlayer].pocketedBalls.Count == 7)
            {
                GameLogic.instance.GameCompleteEvent(GameLogic.instance.currentPlayer);
            }
            else
            {
                GameLogic.instance.GameCompleteEvent(GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer));
            }
        }

        else
        {
            other.gameObject.SetActive(false);
            GameLogic.instance.pocketedBalls.Add(other.gameObject);
            PoolMain.instance.balls.Remove(other.gameObject);
            if (PoolMain.instance.isBreak)
            {
                PoolMain.instance.pocketed = true;
            }
            else
            {
                if (PoolMain.instance.firstPot)
                {                    
                    PoolMain.instance.firstPot = false;
                    GameLogic.instance.players[GameLogic.instance.currentPlayer].BallType = pocketedBall.ballType;
                    GameLogic.instance.players[GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer)].BallType = pocketedBall.ballType == BallBehaviour.BallType.stripe ? BallBehaviour.BallType.solid : BallBehaviour.BallType.stripe;
                    PoolMain.instance.player1Txt.text =  GameLogic.instance.player1.BallType+"";
                    PoolMain.instance.player2Txt.text = GameLogic.instance.player2.BallType + "";
                    PoolMain.instance.pocketed = true;
                    GameLogic.instance.SetBallImages();
                    foreach (GameObject gBall in GameLogic.instance.pocketedBalls)
                    {
                        if(gBall.GetComponent<BallBehaviour>().ballType == GameLogic.instance.players[GameLogic.instance.currentPlayer].BallType)
                        {
                            GameLogic.instance.players[GameLogic.instance.currentPlayer].pocketedBalls.Add(gBall);
                            GameLogic.instance.players[GameLogic.instance.currentPlayer].DisableBallImage(gBall.GetComponent<BallBehaviour>().ballCode);
                        }
                        else
                        {
                            GameLogic.instance.players[GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer)].pocketedBalls.Add(gBall);
                            GameLogic.instance.players[GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer)].DisableBallImage(gBall.GetComponent<BallBehaviour>().ballCode);
                        }
                    }
                    
                    return;
                }

                else
                if (pocketedBall.ballType == GameLogic.instance.players[GameLogic.instance.currentPlayer].BallType)
                {
                    PoolMain.instance.pocketed = true;
                    GameLogic.instance.players[GameLogic.instance.currentPlayer].pocketedBalls.Add(pocketedBall.gameObject);
                    GameLogic.instance.players[GameLogic.instance.currentPlayer].DisableBallImage(pocketedBall.ballCode);
                }
                else
                {
                    GameLogic.instance.players[GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer)].pocketedBalls.Add(pocketedBall.gameObject);
                    GameLogic.instance.players[GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer)].DisableBallImage(pocketedBall.ballCode);
                }
            }
        }
    }
}
