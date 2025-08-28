using System.Collections;
using UnityEngine;
using DG.Tweening;


public class PoolCamBehaviour : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Transform cueStick, cueBall;
    [SerializeField] Vector3 ballFollowOffset, stickFollowOffset, cpuWaitPosition, cpuWaitRotation;
    [SerializeField] Vector3[] cpuWaitPositions, cpuWaitRotations;
    [SerializeField] int tCount;
    [SerializeField] float touchTime, minFov, maxFov, zoomSpeed, rotationAmount, rotationThreshold;
    [SerializeField] public GameState gameState;
    [SerializeField] SwipeDirection swipeDirection;

    GamePlayController playerController;


    private bool isZoomingIn = false;
    private bool isZoomingOut = false;

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

    private void OnEnable()
    {
        EventHandler.RotateCameraBreak += RotateCam;
        EventHandler.SwipeAim += SwipeAim;
        EventHandler.DragAim += DragAim;
        EventHandler.SwipeCueBall += SwipePlace;
        EventHandler.MoveCueBall += MovePlace;
        EventHandler.PlayableBallTapped += LookAt;
        EventHandler.ResetCam += ResetCam;
        EventHandler.WaitCPU += WaitCPU;
    }

    private void OnDestroy()
    {
        EventHandler.RotateCameraBreak -= RotateCam;
        EventHandler.SwipeAim -= SwipeAim;
        EventHandler.DragAim -= DragAim;
        EventHandler.SwipeCueBall -= SwipePlace;
        EventHandler.MoveCueBall -= MovePlace;
        EventHandler.PlayableBallTapped -= LookAt;
        EventHandler.ResetCam -= ResetCam;
        EventHandler.WaitCPU -= WaitCPU;
    }


    void Start()
    {
        stickFollowOffset = transform.position - cueStick.position;
        ballFollowOffset = transform.position - cueBall.position;
        playerController = GamePlayController.instance;
        minFov = cam.fieldOfView - 10;
        maxFov = cam.fieldOfView + 10;
    }


    //void Update()
    //{
    //    tCount = Input.touchCount;

    //    switch (gameState)
    //    {
    //        case GameState.Break:
    //            //Break();
    //            return;

    //        case GameState.Aim:
    //            playerController.RenderTrajectory();
    //            //Aim();
    //            break;

    //        case GameState.Waiting:
    //            //StartCoroutine(WaitCPU());
    //            return;

    //        case GameState.Reset:
    //            if (gameState != prevState)
    //            {
    //                //StartCoroutine(ResetCam());
    //            }
    //            break;
    //    }
    //    CameraAction();
    //    prevState = gameState;
    //}

    public void SetInitialCameraAnim()
    {
        StartCoroutine(SetCamera());
    }

    IEnumerator SetCamera()
    {
        yield return null;

        float duration = 1f, time = 0f;
        Vector3 startPos = transform.GetChild(0).localPosition;
        Vector3 endPos = new Vector3(-2.18f, 1.44f, 0);
        float velocity = 0f;

        //while (time < duration)
        //{
        //    time += Time.deltaTime;
        //    float smoothT = Mathf.SmoothDamp(0, 1, ref velocity, duration);
        //    transform.GetChild(0).localPosition = Vector3.Lerp(startPos, endPos, time/duration);
        //    yield return null; 
        //}

        transform.GetChild(0).DOLocalMove(endPos, 1f);

        //transform.GetChild(0).localPosition = endPos;
    }

    void CameraAction()
    {
        if (gameState == GameState.Waiting) return;
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (playerController.touchDisabled || Utils.IsPointerOverUIObject(touch.position)) return;

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
            }
        }
    }

    IEnumerator RotateEffect()
    {
        Ease ease = Ease.OutCubic;
        //ease = easeType;
        float duration = .8f;
        float rotationAmt = 15f;
        if (swipeDirection == SwipeDirection.Left)
        {
            cueStick.transform.DORotateQuaternion(Quaternion.Euler(0, transform.eulerAngles.y - rotationAmt,0), duration).SetEase(ease);
            transform.DORotateQuaternion (Quaternion.Euler(0, transform.eulerAngles.y - rotationAmt, transform.eulerAngles.z),duration).SetEase(ease);
        }
        else if (swipeDirection == SwipeDirection.Right)
        {
            cueStick.transform.DORotateQuaternion(Quaternion.Euler(0, transform.eulerAngles.y + rotationAmt, 0), duration).SetEase(ease);
            transform.DORotateQuaternion(Quaternion.Euler(0, transform.eulerAngles.y + rotationAmt, transform.eulerAngles.z), duration).SetEase(ease);
        }
        else if (swipeDirection == SwipeDirection.Up || swipeDirection == SwipeDirection.Down)
        {
            float z = transform.eulerAngles.z;
            if (z > 180f) z -= 360f;
            float direction = swipeDirection == SwipeDirection.Up ? 1f : -1f;
            float targetZ = Mathf.Clamp(z + rotationAmt * direction, -45f, 15f);
            transform.DORotateQuaternion(Quaternion.Euler(0, transform.eulerAngles.y, targetZ),duration).SetEase(ease);
        }
        yield return null;
        
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

    bool cut, dragRotationActive ,looked;
    public float swipeSpeedX, swipeSpeedY;

    void RotateCam(Vector2 delta)
    {
        dragRotationActive = true;
        cueStick.transform.rotation = Quaternion.Euler(0, cueStick.transform.eulerAngles.y + (delta.x * 1.4f * Time.deltaTime), 0);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + (delta.x * 1.4f * Time.deltaTime), transform.eulerAngles.z);
    }

    void LookAt(GameObject obj)
    {
        if (!looked)
        {
            looked = true;
            Debug.Log("here");
            float duration = .5f;
            Vector3 direction = obj.transform.position - cueStick.transform.position;
            direction.y = 0;
            direction.Normalize();

            Quaternion newRotation = Quaternion.LookRotation(direction);
            cueStick.transform.DORotateQuaternion(Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0), duration).SetEase(Ease.OutSine).OnComplete(() =>
            {
                transform.DORotateQuaternion(Quaternion.Euler(transform.eulerAngles.x, cueStick.eulerAngles.y, transform.eulerAngles.z), .3f);
                looked = false;
            });
        }
    }

    public void PlaceCamera()
    {
        transform.DOMove (cueBall.position + ballFollowOffset,1f).SetEase(Ease.InOutCubic);
    }

    void SwipePlace(Vector2 delta)
    {       
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            StartCoroutine(slideInOut(delta.x));
            cut = true;
        }
        else
        {
            StartCoroutine(slideUpDown(delta.y));
            cut = true;
        }        
    }

    void MovePlace(Vector2 delta)
    {
        transform.position = (cueBall.position + ballFollowOffset);
    }

    IEnumerator slideInOut(float delta)
    {
        float actualZPos = transform.position.z;
        float zOffset = delta < 0 ? 0.03f : -0.03f;
        zOffset += actualZPos;
        float duration = 0.25f, dur = 0.5f;
        float time = 0, t2 = 0;
        while (time <= duration)
        {
            time += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.Lerp(actualZPos, zOffset, time / duration));
            yield return null;
        }
        while (t2 <= dur)
        {
            t2 += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.Lerp(zOffset, actualZPos, t2 / dur));
            yield return null;
        }
        //transform.DOMoveZ(zOffset, .2f).SetEase(Ease.OutSine).OnComplete(() =>
        //{
        //    transform.DOMoveZ(actualZPos, .8f).SetEase(Ease.OutSine).OnComplete(() => cut = false);
        //});
        //yield return null;
        cut = false;
    }

    IEnumerator slideUpDown(float delta)
    {
        float actualXPos = transform.position.x;
        float xOffset = delta < 0 ? -0.03f : 0.03f;
        xOffset += actualXPos;
        float duration = 0.25f, dur = 0.5f;
        float time = 0, t2 = 0;
        while (time <= duration)
        {
            time += Time.deltaTime;
            transform.position = new Vector3(Mathf.Lerp(actualXPos, xOffset, time / duration), transform.position.y, transform.position.z);
            yield return null;
        }
        while (t2 <= dur)
        {
            t2 += Time.deltaTime;
            transform.position = new Vector3(Mathf.Lerp(xOffset, actualXPos, t2 / dur), transform.position.y, transform.position.z);
            yield return null;
        }
        cut = false;
    }

    void SwipeAim(Vector2 delta)
    {        
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            swipeDirection = delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            swipeDirection = delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
        StartCoroutine(RotateEffect());        
    }

    void DragAim(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > rotationThreshold || Mathf.Abs(delta.y) > rotationThreshold)
        {
            if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) && Mathf.Abs(delta.y) > 10 && gameState != GameState.Reset)
            {
                playerController.updown = true;
                float smoothRotation = delta.y * rotationAmount * Time.deltaTime;
                float z = transform.eulerAngles.z;
                if (z > 180f) z -= 360f;
                float rotationZ = Mathf.Clamp(z + smoothRotation, -45f, 15f);
                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, rotationZ);
            }
            else
            {
                playerController.updown = false;
                float smoothRotation = delta.x * rotationAmount * Time.deltaTime;
                cueStick.transform.rotation = Quaternion.Euler(0, cueStick.transform.eulerAngles.y + smoothRotation, 0);
                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + smoothRotation, transform.eulerAngles.z);
            }
        }
    }

    public bool doneCameraMove;

    public void WaitCPU()
    {
        //float time = 0;
        float duration = 1f;
        Vector3 currentPos = transform.position;
        Quaternion currentRot = transform.rotation;

        if (Vector3.Distance(cpuWaitPositions[1], cueBall.transform.position) < 1)
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


        transform.DORotate(cpuWaitRotation, duration).SetEase(Ease.OutSine);
        transform.DOMove(cpuWaitPosition, duration).SetEase(Ease.OutSine).OnComplete(()=>doneCameraMove=true);
    }

    void ResetCam()
    {
        StopAllCoroutines();
        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        //float time = 0;
        float duration = 0.3f;
        transform.DORotateQuaternion(Quaternion.Euler(0, cueStick.eulerAngles.y, 0), duration).SetEase(Ease.OutSine);
        transform.DOMove(cueStick.position + stickFollowOffset, duration).SetEase(Ease.OutSine).OnComplete(() => gameState = GameState.Aim);
        //gameState = GameState.Aim;
    }
}