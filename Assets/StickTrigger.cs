using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickTrigger : MonoBehaviour
{

    bool overLap;

    void Start()
    {
        
    }

    void Update()
    {
        //if(HasColliderBelow())
        //{
        //    //StartCoroutine(RotateUp(true));
        //}
        //else
        //{
        //    //transform.parent.localRotation = Quaternion.Euler(0, 0, -5.927f);
        //}
        StartCoroutine(HasColliderBelow());
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("enter");
        overLap = true;
        
    }

    public void OnTriggerExit(Collider other)
    {
        overLap = false;
        float rotZ = transform.parent.eulerAngles.z;
    }

    public bool noOverLap()
    {
        return overLap;
    }

    IEnumerator RotateUp(bool up)
    {
        if(up)
        {
            while(overLap)
            {
                transform.parent.Rotate(Vector3.forward * -1);
                yield return null;
            }
        }
        else
        {
            transform.parent.Rotate(Vector3.forward);
            yield return null;            
        }
    }

    IEnumerator HasColliderBelow()
    {        
        if(GetComponent<Rigidbody>().SweepTest(Vector3.down,out RaycastHit hit))
        {
            //if (hit.transform.gameObject.tag is "playBall" or "cushion")
            //    Debug.Log("below");
            //    return true;
            while(hit.distance >0 && hit.distance < 0.02f)
            {
                transform.parent.Rotate(Vector3.forward);
                yield return null;
            }
        }
    }
}
