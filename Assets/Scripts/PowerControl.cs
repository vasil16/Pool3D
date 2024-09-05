using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PowerControl :  Slider, IPointerUpHandler
{
    float modValue;

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Pointer Up on Slider");
        OnSliderPointerUp();
    }

    private void OnSliderPointerUp()
    {
        Debug.Log("Slider pointer up event handled.");        
        StartCoroutine(PoolMain.instance.Hit());
    }

    public void sliderMech(float val)
    {
        modValue = Mathf.Lerp(-0.038f, -0.27f, val / maxValue);
        PoolMain.instance.cue.transform.localPosition = new Vector3(modValue, PoolMain.instance.cue.transform.localPosition.y, PoolMain.instance.cue.transform.localPosition.z);
        PoolMain.instance.hitPower = val;
    }
}
