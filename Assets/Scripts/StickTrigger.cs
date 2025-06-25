using UnityEngine;
using DG.Tweening;

public class StickTrigger : MonoBehaviour
{
    bool up;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("enter");
        up = true;
        RotateUp(-10);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!up) return;
        RotateUp(10);
    }

    void RotateUp(float value)
    {
        transform.DORotateQuaternion(Quaternion.Euler (transform.rotation.x + value, transform.rotation.y, transform.rotation.z), 0.4f);
        up = false;
    }
}
