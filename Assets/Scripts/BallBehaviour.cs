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

    Vector3 initPos;

    public BallType ballType;
    [SerializeField] public int ballCode;

    private void Awake()
    {
        initPos = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("outer"))
        {
            GetComponent<Rigidbody>().isKinematic = true;
            transform.position = initPos;
            GetComponent<Rigidbody>().isKinematic = false;
        }

        else

        if (ballType == BallType.white)
        {
            if (collision.gameObject.CompareTag("playBall"))
            {
                if (!PoolMain.instance.spun && PoolMain.instance.hasSpin)
                {
                    Debug.Log("spinn power " + PoolMain.instance.hitPower + "  dir " + PoolMain.instance.spinMark.transform.position);
                    //GetComponent<Rigidbody>().AddForceAtPosition(PoolMain.instance.spinMark.transform.localPosition.normalized * PoolMain.instance.hitPower * 0.20f, PoolMain.instance.spinMark.transform.localPosition, ForceMode.Force);
                    GetComponent<Rigidbody>().AddForce((transform.position - PoolMain.instance.spinMark.transform.position).normalized * PoolMain.instance.hitPower * 0.20f,  ForceMode.Force);
                    PoolMain.instance.spun = true;
                }
                else
                if (!PoolMain.instance.spun)
                {
                    Debug.Log("cut speed");
                    //GetComponent<Rigidbody>().AddForce(GetComponent<Rigidbody>().velocity * -0.7f);
                    GetComponent<Rigidbody>().drag = 3f;
                    PoolMain.instance.spun = true;
                }
            }
        }
    }
}
