using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIItemEntry : MonoBehaviour
{

    [SerializeField] Vector2 finalPos, actualPos;
    [SerializeField] float startDelay, duration;
    private RectTransform rect;
    float runTime;
    bool completed;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        actualPos = rect.anchoredPosition;
        //StartCoroutine(MoveToPos());
    }

    void Update()
    {
        runTime += Time.deltaTime;
        if(!completed && runTime>startDelay)
        {
            float t = (runTime - startDelay) / duration;
            t = Mathf.Clamp01(t);

            rect.anchoredPosition = Vector2.Lerp(actualPos, finalPos, t);
            if (t >= 1.0f) // Check if the movement is complete
            {
                // Execute the next line of code after movement completes
                GetComponent<UIItemExit>().enabled = true;

                // Disable this script
                completed = true; // Stop further movement
                this.enabled = false; // Disable the script
            }
        }

    }

    IEnumerator MoveToPos()
    {
        yield return new WaitForSeconds(startDelay);
        float time = 0;
        while(time<=duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            rect.anchoredPosition = Vector2.Lerp(actualPos, finalPos, t);
            yield return null;
        }
        GetComponent<UIItemExit>().enabled = true;
    }

}
