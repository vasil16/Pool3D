using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PowerControl : Slider, IPointerUpHandler, IPointerDownHandler
{
    float modValue;
    Color ogColor = new Color(101f / 255f, 183f / 255f, 98f / 255f, 1f);
    Color finalColor = new Color(222f / 255f, 0f / 255f, 7f / 255f, 1f);

    public override void OnPointerDown(PointerEventData eventData)
    {
        GameController.instance.touchDisabled = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Pointer Up on Slider");
        //targetGraphic.color = ogColor;
        OnSliderPointerUp();
    }

    private void OnSliderPointerUp()
    {
        GameController.instance.touchDisabled = false;
        Debug.Log("Slider pointer up event handled.");
        StartCoroutine(GameController.instance.PlayShot());
    }

    public void sliderMech(float val)
    {
        modValue = Mathf.Lerp(0f, -0.097f, val / maxValue);
        float t = val / maxValue;
        targetGraphic.color = Color.Lerp(ogColor, finalColor, t);
        GameController.instance.cue.transform.localPosition = new Vector3(modValue, GameController.instance.cue.transform.localPosition.y, GameController.instance.cue.transform.localPosition.z);
        GameController.instance.hitPower = val;
    }
}


//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class PowerControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
//{
//    [Tooltip("Assign your visual UI Slider here. Do NOT attach this script to the Slider directly if it intercepts raycasts.")]
//    [SerializeField] private Slider powerSlider;

//    [Tooltip("How many screen pixels you need to drag to reach maximum power.")]
//    [SerializeField] private float maxDragDistance = 300f;

//    private Vector2 pointerStartPos;
//    private Color ogColor = new Color(101f / 255f, 183f / 255f, 98f / 255f, 1f);
//    private Color finalColor = new Color(222f / 255f, 0f / 255f, 7f / 255f, 1f);

//    public void OnPointerDown(PointerEventData eventData)
//    {
//        GameController.instance.touchDisabled = true;
//        pointerStartPos = eventData.position;
//        UpdatePower(0f); // Reset power to 0 at the exact moment of touch
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        // Assuming dragging downwards increases power. 
//        // If dragging in ANY direction should increase power, use: Vector2.Distance(pointerStartPos, eventData.position);
//        float dragDistance = pointerStartPos.y - eventData.position.y;

//        // Prevent negative power if they drag the wrong way
//        dragDistance = Mathf.Max(0, dragDistance);

//        // Normalize the drag against your defined max distance
//        float normalizedDrag = Mathf.Clamp01(dragDistance / maxDragDistance);
//        float calculatedPower = normalizedDrag * powerSlider.maxValue;

//        UpdatePower(calculatedPower);
//    }

//    public void OnPointerUp(PointerEventData eventData)
//    {
//        if (powerSlider.targetGraphic != null)
//        {
//            powerSlider.targetGraphic.color = ogColor;
//        }

//        GameController.instance.touchDisabled = false;

//        // Shoot
//        StartCoroutine(GameController.instance.PlayShot());

//        // Reset visually after shot
//        UpdatePower(0f);
//    }

//    private void UpdatePower(float val)
//    {
//        powerSlider.value = val;
//        float t = val / powerSlider.maxValue;

//        if (powerSlider.targetGraphic != null)
//        {
//            powerSlider.targetGraphic.color = Color.Lerp(ogColor, finalColor, t);
//        }

//        float modValue = Mathf.Lerp(0f, -0.097f, t);
//        Transform cueTransform = GameController.instance.cue.transform;

//        // Avoid repeatedly calling instance and transform getters, cache them logically if called every frame
//        cueTransform.localPosition = new Vector3(modValue, cueTransform.localPosition.y, cueTransform.localPosition.z);

//        GameController.instance.hitPower = val;
//    }
//}