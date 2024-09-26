using System.Collections;
using UnityEngine;

public class PoolCamBehaviour : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Transform cueStick, cueBall;
    [SerializeField] RectTransform dragRotateRect;
    [SerializeField] Vector3 ballFollowOffset, stickFollowOffset, followRotation, initialRotation, cpuWaitPosition, cpuWaitRotation;
    [SerializeField] Vector3[] cpuWaitPositions, cpuWaitRotations;
    [SerializeField] Vector2 touchDelta, touchStart, touchEnd, deltaPos;
    [SerializeField] int tCount;
    [SerializeField] float touchTime, longTouchThreshold, minFov, maxFov, zoomSpeed, rotationAmount, rotationThreshold;
    [SerializeField] public GameState gameState;
    [SerializeField] SwipeDirection swipeDirection;
    private GameState prevState = GameState.Break;

    bool timerRunning;

    private bool isZoomingIn = false;
    private bool isZoomingOut = false;


    enum Target
    {
        CueBall,
        CueStuck,
        Null
    }

    public enum GameState
    {
        Break,
        Hit,
        Waiting,
        Aim,
        Reset
    };

    enum SwipeDirection
    {
        None,
        Right,
        Left,
        Up,
        Down
    }


    void Start()
    {
        stickFollowOffset = transform.position - cueStick.position;
        ballFollowOffset = transform.position - cueBall.position;
    }


    void Update()
    {
        //if (isZoomingIn)
        //{
        //    ZoomIn();
        //}

        //if (isZoomingOut)
        //{
        //    ZoomOut();
        //}

        tCount = Input.touchCount;

        switch (gameState)
        {
            case GameState.Break:
                Break();
                return;

            case GameState.Hit:
                if (gameState != prevState)
                {
                    //StartCoroutine(FollowBall());
                }
                break;

            case GameState.Aim:
                StartCoroutine(FollowStick());
                break;

            case GameState.Waiting:
                StartCoroutine(AfterHit());
                return;

            case GameState.Reset:
                if (gameState != prevState)
                    StartCoroutine(ResetCam());
                break;
        }
        CameraAction();
        prevState = gameState;
    }

    void CameraAction()
    {
        if (gameState == GameState.Waiting) return;
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (PoolMain.instance.dragPower || Utils.IsPointerOverUIObject(touch.position)) return;

                if (tCount == 2)
                {
                    
                    Touch touch0 = Input.GetTouch(0);
                    Touch touch1 = Input.GetTouch(1);

                    if (touch0.phase == TouchPhase.Ended && touch1.phase == TouchPhase.Ended)
                    {

                    }
                    else
                    {
                        Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                        float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
                        float touchDeltaMag = (touch0.position - touch1.position).magnitude;

                        float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                        cam.fieldOfView += deltaMagnitudeDiff * zoomSpeed;
                        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFov, maxFov);

                        return;
                    }
                }

                else if (tCount == 1)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        StartCoroutine(StartTouchTimer());
                        touchStart = touch.position;
                    }

                    else if (touch.phase == TouchPhase.Moved)
                    {
                        deltaPos = touch.deltaPosition;

                        if (Mathf.Abs(deltaPos.x) > rotationThreshold || Mathf.Abs(deltaPos.y) > rotationThreshold)
                        {
                            if (Mathf.Abs(deltaPos.y) > Mathf.Abs(deltaPos.x) && Mathf.Abs(deltaPos.y)>10)
                            {
                                PoolMain.instance.updown = true;

                                float smoothRotation = deltaPos.y * rotationAmount;
                                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + smoothRotation);
                                return;
                            }
                            else
                            {
                                PoolMain.instance.updown = false;
                                float smoothRotation = deltaPos.x * rotationAmount;
                                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, PoolMain.instance.cueAnchor.transform.eulerAngles.y + smoothRotation, 0);
                                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + smoothRotation, transform.eulerAngles.z);
                                return;
                            }
                        }
                    }

                    else if (touch.phase == TouchPhase.Ended)
                    {
                        if (tCount == 2) return;
                        EndTouchTimer();
                        touchEnd = touch.position;
                        if (touchTime > 0.01 &&  touchTime < longTouchThreshold)
                        {
                            touchDelta = touchEnd - touchStart;

                            if (Mathf.Abs(touchDelta.x) > 80f || Mathf.Abs(touchDelta.y) > 80f)
                            {
                                if (Mathf.Abs(touchDelta.x) > Mathf.Abs(touchDelta.y))
                                {
                                    swipeDirection = touchDelta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
                                }
                                else
                                {
                                    swipeDirection = touchDelta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
                                }
                                touchDelta = touchStart = touchEnd = Vector2.zero;
                                StartCoroutine(MoveEffect());
                            }
                        }

                    }
                }
            }
        }
    }

    IEnumerator StartTouchTimer()
    {
        touchTime = 0;
        timerRunning = true;
        while (timerRunning)
        {
            touchTime += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(2);
    }

    void EndTouchTimer()
    {
        timerRunning = false;
    }

    IEnumerator MoveEffect()
    {
        float time = 1;
        while (time >= 0)
        {
            time -= Time.deltaTime;
            if (swipeDirection == SwipeDirection.Left)
            {
                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y - (time * 0.8f), 0);
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y - (time * 0.8f), transform.eulerAngles.z);
            }
            else if (swipeDirection == SwipeDirection.Right)
            {
                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + (time * 0.8f), 0);
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + (time * 0.8f), transform.eulerAngles.z);
            }
            else if (swipeDirection == SwipeDirection.Up)
            {
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z + (time * 0.8f));
            }
            else if (swipeDirection == SwipeDirection.Down)
            {
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z - (time * 0.8f));
            }
            yield return null;
        }
    }

    #region Zoom

    public void ZoomIn()
    {
        //cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, minFov, zoomSpeed * Time.deltaTime);
        cam.fieldOfView -= 5 * Time.smoothDeltaTime;
    }

    public void ZoomOut()
    {
        //cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, maxFov, zoomSpeed * Time.deltaTime);
        cam.fieldOfView += 5 * Time.smoothDeltaTime;
    }

    public void StartZoomIn()
    {
        isZoomingIn = true;
        isZoomingOut = false;
    }

    public void StopZoomIn()
    {
        isZoomingIn = false;
    }

    public void StartZoomOut()
    {
        isZoomingIn = false;
        isZoomingOut = true;
    }

    public void StopZoomOut()
    {
        isZoomingOut = false;
    }

    #endregion

    bool cut;
    public float swipeSpeedX, swipeSpeedY;

    void Break()
    {
        transform.position = (cueBall.position + ballFollowOffset);
        foreach (Touch touch in Input.touches)
        {
            deltaPos = touch.deltaPosition;
            if (Utils.IsPointerOverUIObject(touch.position) && RectTransformUtility.RectangleContainsScreenPoint(dragRotateRect, touch.position))
            {
                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, PoolMain.instance.cueAnchor.transform.eulerAngles.y + (deltaPos.x * rotationAmount), 0);
                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + (deltaPos.x * rotationAmount), transform.eulerAngles.z);
                return;
            }
            if(touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                StartCoroutine(StartTouchTimer());
            }
            if (touch.phase == TouchPhase.Ended && !cut && PoolMain.instance.firstBreak)
            {
                EndTouchTimer();
                touchEnd = touch.position;
                touchDelta = touchEnd - touchStart;
                
                if (Mathf.Abs(touchDelta.x)> Mathf.Abs(touchDelta.y))
                {
                    swipeSpeedX = touchDelta.x / touchTime;
                    
                    if (Mathf.Abs(swipeSpeedX) > 300)
                    {
                        StartCoroutine(slideInOut());
                        cut = true;
                    }
                    return;
                }
                else
                {                    
                    swipeSpeedY = touchDelta.y / touchTime;
                                       
                    if (Mathf.Abs(swipeSpeedY) > 300)
                    {
                        StartCoroutine(slideUpDown());
                        cut = true;
                    }
                    return;
                }
            }
        }
    }

    IEnumerator slideInOut()
    {
        float actualZPos = transform.position.z;
        float zOffset = touchDelta.x < 0 ? 0.03f : -0.03f;
        zOffset += actualZPos;
        float duration = 0.1f, dur = 0.5f;
        float time = 0, t2 = 0;
        while (time<=duration)
        {
            time += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.Lerp(actualZPos,  zOffset, time / duration));
            yield return null;
        }
        while (t2 <= dur)
        {
            t2 += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.Lerp(zOffset, actualZPos, t2 / dur));
            yield return null;
        }
        cut = false;
    }

    IEnumerator slideUpDown()
    {
        float actualZPos = transform.position.x;
        float zOffset = touchDelta.y < 0 ? 0.03f : -0.03f;
        zOffset += actualZPos;
        float duration = 0.1f, dur = 0.4f;
        float time = 0, t2 = 0;
        while (time <= duration)
        {
            time += Time.deltaTime;
            transform.position = new Vector3(Mathf.Lerp(actualZPos, zOffset, time / duration), transform.position.y, transform.position.z);
            yield return null;
        }
        while (t2 <= dur)
        {
            t2 += Time.deltaTime;
            transform.position = new Vector3(Mathf.Lerp(zOffset, actualZPos, t2 / dur), transform.position.y, transform.position.z);
            yield return null;
        }
        cut = false;
    }

    IEnumerator FollowStick()
    {
        yield return null;
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, cueStick.eulerAngles.y, transform.eulerAngles.z);
    }

    IEnumerator AfterHit()
    {
        float time = 0;
        float duration =.8f;
        Vector3 currentPos = transform.position;
        Quaternion currentRot = transform.rotation;

        //for(int k =0; k<4;k++)
        //{
        //    if (Vector3.Distance(cpuWaitPositions[k], cueBall.transform.position) < 1)
        //    {
        //        cpuWaitPosition = cpuWaitPositions[k];
        //        cpuWaitRotation = cpuWaitRotations[k];
        //        break;
        //    }
        //}

        if(Vector3.Distance(cpuWaitPositions[1], cueBall.transform.position) <1)
        {
            cpuWaitPosition = cpuWaitPositions[1];
            cpuWaitRotation = cpuWaitRotations[1];
        }

        else if (Vector3.Distance(cpuWaitPositions[2], cueBall.transform.position) < 1)
        {
            cpuWaitPosition = cpuWaitPositions[2];
            cpuWaitRotation = cpuWaitRotations[2];
        }

        else if (Vector3.Distance(cpuWaitPositions[3], cueBall.transform.position) < 1)
        {
            cpuWaitPosition = cpuWaitPositions[3];
            cpuWaitRotation = cpuWaitRotations[3];
        }

        else
        {
            cpuWaitPosition = cpuWaitPositions[0];
            cpuWaitRotation = cpuWaitRotations[0];
        }

        while (time<=duration)
        {
            time += Time.deltaTime;
            transform.position = Vector3.LerpUnclamped(currentPos, cpuWaitPosition, time / duration);
            transform.rotation = Quaternion.SlerpUnclamped(currentRot, Quaternion.Euler(cpuWaitRotation), time / duration);
            yield return null;
        }
    }

    IEnumerator ResetCam()
    {
        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        float time = 0;
        float duration = 0.3f;
        while (time <= duration)
        {
            time += Time.smoothDeltaTime;
            float t = time / duration;
            transform.position = Vector3.Slerp(startPos, cueStick.position + stickFollowOffset, t);
            transform.rotation = Quaternion.Slerp(startRotation, Quaternion.Euler(0, cueStick.eulerAngles.y, 0), t);
            yield return null;
        }
        gameState = GameState.Aim;

    }
}



