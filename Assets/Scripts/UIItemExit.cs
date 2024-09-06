using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIItemExit : MonoBehaviour
{

    [SerializeField] Vector2 finalPos, actualPos;
    [SerializeField] float startDelay, duration;
    private RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        actualPos = rect.anchoredPosition;
        
    }

    public void ExitWithCallBack(Action callBack)
    {
        StartCoroutine(MoveToPos(callBack));
    }

    IEnumerator MoveToPos(Action callBack)
    {
        yield return new WaitForSeconds(startDelay);
        float time = 0;
        while(time<=duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duration);
            rect.anchoredPosition = Vector2.Lerp(actualPos, finalPos, t);
            yield return null;
        }
        callBack.Invoke();
    }

}
