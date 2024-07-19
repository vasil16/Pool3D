using UnityEngine;

public class Pocket : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BallBehaviour pocketedBall = other.GetComponent<BallBehaviour>();

        if (other.CompareTag("cueBall"))
        {
            other.attachedRigidbody.isKinematic = true;
            PoolMain.instance.isFoul = true;
            //PoolMain.instance.isBreak = true;
        }
        else if (pocketedBall.ballType == BallBehaviour.BallType.black)
        {
            Debug.Log("8b");
            if(GameLogic.instance.players[GameLogic.instance.currentPlayer].pocketedBalls.Count == 7)
            {
                Debug.Log("cd1");
                GameLogic.instance.GameCompleteEvent(GameLogic.instance.currentPlayer);
            }
            else
            {
                Debug.Log("cd2");
                GameLogic.instance.GameCompleteEvent(GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer));
            }
        }
        else
        {
            Debug.Log("swa");
            other.gameObject.SetActive(false);
            GameLogic.instance.pocketedBalls.Add(other.gameObject);
            PoolMain.instance.balls.Remove(other.gameObject);
            //other.gameObject.SetActive(false);
            if (PoolMain.instance.isBreak)
            {
                Debug.Log("pot on break");
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
                }
                else
                {
                    GameLogic.instance.players[GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer)].pocketedBalls.Add(pocketedBall.gameObject);
                }
                GameLogic.instance.DisableBallImage(pocketedBall.ballCode);
            }

        }
    }
}
