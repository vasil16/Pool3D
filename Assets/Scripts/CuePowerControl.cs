using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CuePowerControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Refs")]
    public RectTransform cueImage; 

    [Header("Power Range")]
    public float maxDragDistance = 300f;

    [Header("Cue Local Y Limits")]
    public float cueYAtZeroPower = -50f;
    public float cueYAtFullPower = -384f;

    [Header("Colors")]
    public UnityEngine.UI.Image cueGraphic;
    Color ogColor = new Color(101f / 255f, 183f / 255f, 98f / 255f, 1f);
    Color finalColor = new Color(222f / 255f, 0f / 255f, 7f / 255f, 1f);

    float dragStartY;
    float currentPower;
    bool dragging;
    public float returnDuration = 0.15f;

    public void OnPointerDown(PointerEventData eventData)
    {
        GameController.instance.touchDisabled = true;
        dragStartY = eventData.position.y;
        currentPower = 0f;
        dragging = true;
        ApplyPower(0f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        float delta = dragStartY - eventData.position.y;
        float t = Mathf.Clamp01(delta / maxDragDistance);

        currentPower = t;
        ApplyPower(t);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        GameController.instance.touchDisabled = false;
        if (cueGraphic != null) cueGraphic.color = ogColor;

        GameController.instance.hitPower = currentPower * GameController.instance.maxHitPower;
        StartCoroutine(ReturnCueThenShoot());
    }

    void ApplyPower(float t)
    {
        float localY = Mathf.Lerp(cueYAtZeroPower, cueYAtFullPower, t);
        Vector3 pos = cueImage.localPosition;
        cueImage.localPosition = new Vector3(pos.x, localY, pos.z);


        float modValue = Mathf.Lerp(0f, -0.1f, t);
        GameController.instance.cue.transform.localPosition = new Vector3(modValue, GameController.instance.cue.transform.localPosition.y, GameController.instance.cue.transform.localPosition.z);


        if (cueGraphic != null)
            cueGraphic.color = Color.Lerp(ogColor, finalColor, t);
    }

    IEnumerator ReturnCueThenShoot()
    {
        Vector3 startPos = cueImage.localPosition;
        Vector3 endPos = new Vector3(startPos.x, cueYAtZeroPower, startPos.z);
        float t = 0f;

        while (t < returnDuration)
        {
            t += Time.deltaTime;
            cueImage.localPosition = Vector3.Lerp(startPos, endPos, t / returnDuration);
            yield return null;
        }
        cueImage.localPosition = endPos;

        yield return GameController.instance.PlayShot();
    }
}