using UnityEngine;
using System.Collections;

public class UIItemEntry : MonoBehaviour
{
    [SerializeField] private Vector2 finalPos;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rect;
    private Vector2 startPos;

    float dampFact = 0;
    Vector2 dampVec = Vector2.zero;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        StartCoroutine(AnimateMovement());
    }

    private IEnumerator AnimateMovement()
    {
        yield return new WaitForSeconds(startDelay);

        float elapsedTime = 0f;
        //while (elapsedTime < duration)
        //{
        //    elapsedTime += Time.deltaTime;
        //    float t = Mathf.Clamp01(elapsedTime / duration);
        //    float curveValue = movementCurve.Evaluate(t);
        //    //rect.anchoredPosition = Vector2.LerpUnclamped(startPos, finalPos, curveValue);
        //    rect.anchoredPosition = Vector2.SmoothDamp(rect.anchoredPosition, finalPos,ref dampVec, 0.8f);
        //    //rect.anchoredPosition = new Vector2(Mathf.SmoothDamp(startPos.x, finalPos.x, ref dampFact, 1), Mathf.SmoothDamp(startPos.y, finalPos.y, ref dampFact, 1));
        //    yield return null;
        //}
        while (Vector2.Distance(rect.anchoredPosition,finalPos)>0.4f)
        {
            rect.anchoredPosition = Vector2.SmoothDamp(rect.anchoredPosition, finalPos, ref dampVec, 0.3f);
            yield return null;
        }

        rect.anchoredPosition = finalPos;
        GetComponent<UIItemExit>().enabled = true;
        enabled = false;
    }
}