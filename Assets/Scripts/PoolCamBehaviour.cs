using System.Collections;
using UnityEngine;

public class PoolCamBehaviour : MonoBehaviour
{

    [SerializeField] Camera cam;
    [SerializeField] Transform cueStick, cueBall;
    [SerializeField] RectTransform dragRotateRect;
    [SerializeField] Vector3 ballFollowOffset, stickFollowOffset, followRotation, initialRotation;
    [SerializeField] Vector2 touchDelta, touchStart, touchEnd, deltaPos;
    [SerializeField] int tCount;
    [SerializeField] float touchTime, longTouchThreshold, minFov, maxFov, zoomSpeed, rotationAmount;
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
        if (isZoomingIn)
        {
            ZoomIn();
        }

        if (isZoomingOut)
        {
            ZoomOut();
        }

        tCount = Input.touchCount;

        switch (gameState)
        {
            case GameState.Break:
                Break();
                return;

            case GameState.Hit:

                if (gameState != prevState)
                {
                    StartCoroutine(FollowBall());
                }
                break;

            case GameState.Aim:
                StartCoroutine(FollowStick());
                break;

            case GameState.Waiting:
                StartCoroutine(AfterHit());
                break;

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
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (PoolMain.instance.dragPower || Utils.IsPointerOverUIObject(touch.position)) return;

                if (tCount == 2)
                {
                    //zoom
                    Debug.Log("zoomin");
                    Touch touch0 = Input.GetTouch(0);
                    Touch touch1 = Input.GetTouch(1);

                    Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                    Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                    float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
                    float touchDeltaMag = (touch0.position - touch1.position).magnitude;

                    float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                    cam.fieldOfView += deltaMagnitudeDiff * zoomSpeed;
                    cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFov, maxFov);

                    return;
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
                        if (Mathf.Abs(touch.deltaPosition.y) > Mathf.Abs(touch.deltaPosition.x))
                        {
                            if (Mathf.Abs(touch.deltaPosition.y) < 10) return;
                            PoolMain.instance.updown = true;

                            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + (deltaPos.y * 0.1f));
                        }
                        else
                        {
                            PoolMain.instance.updown = false;
                            {
                                Debug.Log("try swi");
                                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, PoolMain.instance.cueAnchor.transform.eulerAngles.y + (deltaPos.x * rotationAmount), 0);
                                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + (deltaPos.x * rotationAmount), transform.eulerAngles.z);
                            }
                        }
                    }

                    else if (touch.phase == TouchPhase.Ended)
                    {
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
        Debug.Log("moved");
        float time = 1;
        while (time >= 0)
        {
            time -= Time.deltaTime;
            if (swipeDirection == SwipeDirection.Left)
            {
                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y - (time * 0.5f), 0);
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y - (time * 0.5f), transform.eulerAngles.z);
            }
            else if (swipeDirection == SwipeDirection.Right)
            {
                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + (time * 0.5f), 0);
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + (time * 0.5f), transform.eulerAngles.z);
            }
            else if (swipeDirection == SwipeDirection.Up)
            {
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z + (time * 0.5f));
            }
            else if (swipeDirection == SwipeDirection.Down)
            {
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z - (time * 0.5f));
            }
            yield return null;
        }
    }

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



    IEnumerator FollowBall()
    {
        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        float time = 0;
        float duration = 0.8f;
        //while(time <= duration)
        //{
        //    time += Time.smoothDeltaTime;
        //    float t = time / duration;
        //    transform.position = Vector3.Slerp(startPos, ballFollowOffset, t);
        //    transform.rotation = Quaternion.Slerp(startRotation, Quaternion.Euler(followRotation), t);
        //    yield return null;
        //}
        yield break;
    }

    void Break()
    {
        transform.position = (cueBall.position + ballFollowOffset);
        foreach (Touch touch in Input.touches)
        {
            if (Utils.IsPointerOverUIObject(touch.position) && RectTransformUtility.RectangleContainsScreenPoint(dragRotateRect, touch.position))
            {
                PoolMain.instance.cueAnchor.transform.rotation = Quaternion.Euler(0, PoolMain.instance.cueAnchor.transform.eulerAngles.y + (touch.deltaPosition.x * rotationAmount), 0);
                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + (touch.deltaPosition.x * rotationAmount), transform.eulerAngles.z);
                return;
            }
        }
        //transform.rotation = Quaternion.Euler(Vector3.zero);
    }

    IEnumerator FollowStick()
    {
        yield return null;
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, cueStick.eulerAngles.y, transform.eulerAngles.z);
    }

    IEnumerator AfterHit()
    {
        while (gameState == GameState.Hit)
        {
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



