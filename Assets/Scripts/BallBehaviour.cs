using System.Collections;
using UnityEngine;

public class BallBehaviour : MonoBehaviour
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
    GameController playerController;
    public BallType ballType;

    private void Awake()
    {
        playerController = GameController.instance;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag is "playBall" or "cueBall")
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
            //if (ballType == BallType.white)
            //{
            //    if (!playerController.spun && playerController.hasSpin)
            //    {
            //        Debug.Log("spinn power " + playerController.hitPower + "  dir " + playerController.spinMark.transform.position);
            //        //GetComponent<Rigidbody>().AddForce((transform.position - playerController.spinMark.transform.position).normalized * playerController.hitPower * 0.10f, ForceMode.Force);
            //        playerController.spun = true;
            //    }
            //    else if (!playerController.spun)
            //    {
            //        if (!playerController.firstBreak)
            //        {
            //            Debug.Log("cut on  " + gameObject.name + " with " + collision.gameObject.name);
            //            //GetComponent<Rigidbody>().linearVelocity *= 0.4f;
            //            //StartCoroutine(CutOff());
            //            //PoolMain.instance.spun = true;
            //        }
            //    }
            //}
        }
        else if (collision.gameObject.CompareTag("pocket"))
        {
            GameManager.instance.PlaySound(cushionHit);
        }

    }


    IEnumerator CutOff()
    {
        yield return new WaitForSeconds(0.1f);
        Vector3 relVelocity = gameObject.GetComponent<Rigidbody>().linearVelocity;
        gameObject.GetComponent<Rigidbody>().linearVelocity = (relVelocity * 0.06f);
    }
}
