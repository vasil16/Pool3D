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
        //if (GameLogic.instance.currentPlayer == GameLogic.CurrentPlayer.player2 && PoolMain.instance.cpuMode)
        //{
        //    PoolMain.instance.cpuReady = true;
        //}
        StartCoroutine(PoolMain.instance.Hit());
    }

    public void sliderMech(float val)
    {
        modValue = Mathf.Lerp(-0.0308f, -0.097f, val / maxValue);
        PoolMain.instance.cue.transform.localPosition = new Vector3(modValue, PoolMain.instance.cue.transform.localPosition.y, PoolMain.instance.cue.transform.localPosition.z);
        PoolMain.instance.hitPower = val;
    }
}
