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

    public BallType ballType;
    [SerializeField] public int ballCode;
    [SerializeField] AudioClip ballHit, cushionHit;

    GamePlayController playerController;

    private void Awake()
    {
        playerController = GamePlayController.instance;
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
            //if (!playerController.firstBreak)
            //{
            //    if(playerController.gameAudio.isPlaying)
            //    {
            //        playerController.gameAudio.Stop();
            //    }
            //    playerController.gameAudio.PlayOneShot(ballHit);
            //}
            

            if (ballType == BallType.white)
            {
                if (!playerController.spun && playerController.hasSpin)
                {
                    Debug.Log("spinn power " + playerController.hitPower + "  dir " + playerController.spinMark.transform.position);
                    GetComponent<Rigidbody>().AddForce((transform.position - playerController.spinMark.transform.position).normalized * playerController.hitPower * 0.10f, ForceMode.Force);
                    playerController.spun = true;
                }
                else if (!playerController.spun)
                {
                    if (!playerController.firstBreak)
                    {
                        Debug.Log("cut on  " + gameObject.name + " with " + collision.gameObject.name);
                        //StartCoroutine(CutOff());
                        //PoolMain.instance.spun = true;
                    }
                }
            }
        }
        else if (collision.gameObject.CompareTag("pocket"))
        {
            playerController.gameAudio.PlayOneShot(cushionHit);
        }

    }


    IEnumerator CutOff()
    {
        yield return new WaitForSeconds(0.1f);
        Vector3 relVelocity = gameObject.GetComponent<Rigidbody>().linearVelocity;
        gameObject.GetComponent<Rigidbody>().linearVelocity = (relVelocity * 0.06f);
    }
}
