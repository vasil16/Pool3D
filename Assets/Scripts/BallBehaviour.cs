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

    private void OnCollisionEnter(Collision collision)
    {
        //if (collision.gameObject.CompareTag("outer"))
        //{
        //    GetComponent<Rigidbody>().isKinematic = true;
        //    transform.position = initPos;
        //    GetComponent<Rigidbody>().isKinematic = false;
        //}

        //else

        if (ballType == BallType.white)
        {
            if (collision.gameObject.CompareTag("playBall"))
            {
                if (!PoolMain.instance.spun && PoolMain.instance.hasSpin)
                {
                    Debug.Log("spinn power " + PoolMain.instance.hitPower + "  dir " + PoolMain.instance.spinMark.transform.position);
                    GetComponent<Rigidbody>().AddForce((transform.position - PoolMain.instance.spinMark.transform.position).normalized * PoolMain.instance.hitPower * 0.10f,  ForceMode.Force);
                    PoolMain.instance.spun = true;
                }
                else if (!PoolMain.instance.spun)
                {                    
                    if(!PoolMain.instance.firstBreak)
                    {
                        Debug.Log("cut on  " + gameObject.name+ " with "+ collision.gameObject.name);
                        StartCoroutine(CutOff());
                        PoolMain.instance.spun = true;
                    }
                }
            }
        }
    }


    IEnumerator CutOff()
    {
        yield return new WaitForSeconds(0.1f);
        Vector3 relVelocity = gameObject.GetComponent<Rigidbody>().velocity;
        gameObject.GetComponent<Rigidbody>().velocity = (relVelocity * 0.07f);
    }
}
