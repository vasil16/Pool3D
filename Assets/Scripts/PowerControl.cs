using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PowerControl :  Slider, IPointerUpHandler, IPointerDownHandler
{
    float modValue;
    Color ogColor = new Color(101f / 255f, 183f / 255f, 98f / 255f, 1f);
    Color finalColor = new Color(222f / 255f, 0f / 255f, 7f / 255f, 1f);

    public override void OnPointerDown(PointerEventData eventData)
    {
        GamePlayController.instance.touchDisabled = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Pointer Up on Slider");
        targetGraphic.color = ogColor;
        OnSliderPointerUp();
    }

    private void OnSliderPointerUp()
    {
        GamePlayController.instance.touchDisabled = false;
        Debug.Log("Slider pointer up event handled.");        
        StartCoroutine(GamePlayController.instance.PlayShot());
    }

    public void sliderMech(float val)
    {
        modValue = Mathf.Lerp(-0.0308f, -0.097f, val / maxValue);
        float t = val / maxValue;
        targetGraphic.color = Color.Lerp(ogColor, finalColor, t);
        GamePlayController.instance.cue.transform.localPosition = new Vector3(modValue, GamePlayController.instance.cue.transform.localPosition.y, GamePlayController.instance.cue.transform.localPosition.z);
        GamePlayController.instance.hitPower = val;
    }
}
