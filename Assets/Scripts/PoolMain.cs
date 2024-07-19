using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PoolMain : MonoBehaviour
{
    public static PoolMain instance;

    public List<GameObject> balls;

    [SerializeField] private Vector3 lineRendererOffset, cueOgPos, cueOgRot;
    [SerializeField] private Vector2 deltaPosition, deltaPos;
    [SerializeField] private GameObject targetBall, cueAnchor, cueBall, powerBar, aimDock, spinObj;
    [SerializeField] private float maxPower, rotationSpeed = 0.1f, powerMultiplier, maxDistance = 3, secMax = 2;
    [SerializeField] private Camera powerCam;
    [SerializeField] private Transform cueStick, forceAt;
    [SerializeField] private LineRenderer lineRenderer, linePlay;
    [SerializeField] private PoolCamBehaviour poolCam;
    [SerializeField] private PowerControl power;
    [SerializeField] private RectTransform spinRect, circleRect, spinIndicator;
    [SerializeField] public TextMeshProUGUI player1Txt, player2Txt;
    [SerializeField] public GameObject[] playerIndicator;

    public GameObject cue, spinMark;
    public bool isBreak = true;
    public bool dragPower, spun, hasSpin, isWaiting, pocketed, firstPot, updown, isFoul, firstBreak, gameOver;
    private bool looked;
    private int maxBounces = 2;
    private int rand;

    private Rigidbody ballR;
    //public LayerMask collisionMask;

    private Ray pRay;
    private RaycastHit bHit;

    public float time, duration, hitPower;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        cueOgPos = cue.transform.localPosition;
        ballR = cueBall.GetComponent<Rigidbody>();
        Application.targetFrameRate = 60;
    }

    //void OnDrawGizmos()
    //{
    //    if (powerCam != null && Input.touchCount > 0)
    //    {
    //        Touch touch = Input.GetTouch(0);
    //        Ray ray = powerCam.ScreenPointToRay(touch.position);
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawRay(ray.origin, ray.direction * 100);
    //    }
    //}

    void Update()
    {
        HandleTouchInput();
        if (isWaiting)
        {
            lineRenderer.positionCount = 0;
            linePlay.positionCount = 0;
            aimDock.SetActive(false);
            return;
        }

        RenderTrajectory();        
    }

    public void StartGame()
    {
        cueAnchor.transform.SetParent(cueBall.transform);
        cueAnchor.transform.localPosition = Vector3.zero;
        cueAnchor.transform.SetParent(null);
        cue.SetActive(true);
        spinObj.SetActive(true);
        powerBar.SetActive(true);
        if (firstBreak)
        {
            Debug.Log("fbr");
            StartCoroutine(LookAtTarget(balls[0]));
        }
    }

    void HandleBreak(Touch touch)
    {        
        if(touch.phase == TouchPhase.Moved)
        {
            Vector3 newPos = touch.deltaPosition * 0.1f * Time.deltaTime;
            cueBall.transform.localPosition += new Vector3(newPos.y * -1, 0, newPos.x * 1);

            if (firstBreak)
            {
                float clampedX = Mathf.Clamp(cueBall.transform.localPosition.x, 1.3f, 1.8f);
                float clampedZ = Mathf.Clamp(cueBall.transform.localPosition.z, -0.507f, 0.507f);
                cueBall.transform.localPosition = new Vector3(clampedX, cueBall.transform.localPosition.y, clampedZ);
            }
            else
            {
                float clampedX = Mathf.Clamp(cueBall.transform.localPosition.x, -0.279f, 1.8f);
                float clampedZ = Mathf.Clamp(cueBall.transform.localPosition.z, -0.507f, 0.507f);
                cueBall.transform.localPosition = new Vector3(clampedX, 0.3146f, clampedZ);                
            }
        }        
    }

    void HandleTouchInput()
    {
        foreach (Touch touch in Input.touches)
        {     
            if (Utils.IsPointerOverUIObject(touch.position))
            {                
                HandleSpinControl(touch);
                return;
            }

            if (poolCam.gameState == PoolCamBehaviour.GameState.Break)
            {
                HandleBreak(touch);
                return;
            }

            pRay = poolCam.GetComponentInChildren<Camera>().ScreenPointToRay(touch.position);
            if (Physics.Raycast(pRay, out bHit) && bHit.collider.gameObject.CompareTag("playBall") && !looked)
            {
                StartCoroutine(LookAtTarget(bHit.collider.gameObject));
                looked = true;
            }
            else
            {
                HandleCueControl(touch);
            }
        }
    }

    void HandleSpinControl(Touch touch)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(circleRect, touch.position))
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(circleRect, touch.position, null, out localPoint);
            hasSpin = true;

            Vector2 center = circleRect.rect.center;
            float radius = circleRect.rect.width / 2;

            if (Vector2.Distance(center, localPoint) <= radius)
            {
                spinRect.transform.position = touch.position;
            }
            else
            {
                Vector2 clampedPosition = spinRect.transform.position;
                spinRect.transform.position = clampedPosition;
            }
            Vector2 localSpinRectPoint = spinRect.anchoredPosition;

            float normalizedX = localSpinRectPoint.x / radius;
            float normalizedY = localSpinRectPoint.y / radius;

            Vector3 newSpinMarkPosition = new Vector3(normalizedY * 0.03f, spinMark.transform.localPosition.y, normalizedX * 0.03f * -1);
            spinMark.transform.localPosition = newSpinMarkPosition;
            spinIndicator.anchoredPosition = new Vector2(normalizedX * 50, normalizedY * 50);
        }
    }

    void HandleCueControl(Touch touch)
    {
        if (updown) return;
        if (touch.phase == TouchPhase.Moved)
        {
            deltaPosition = touch.deltaPosition;
            if (Mathf.Abs(deltaPosition.y) > 2f) return;
            float rotationAmount = deltaPosition.x  * rotationSpeed * Time.smoothDeltaTime;
            cueAnchor.transform.rotation *= Quaternion.Euler(0, rotationAmount, 0);
        }

        if (touch.phase == TouchPhase.Ended && dragPower)
        {
            StartCoroutine(Hit());
        }
    }

    IEnumerator LookAtTarget(GameObject obj)
    {
        time = 0;
        duration = 1.1f;

        targetBall = obj;
        Vector3 direction = targetBall.transform.position - cueAnchor.transform.position;
        direction.y = 0;
        direction.Normalize();

        Quaternion newRotation = Quaternion.LookRotation(direction);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duration);
            cueAnchor.transform.rotation = Quaternion.Slerp(cueAnchor.transform.rotation, Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0), t);
            yield return null;
        }
        looked = false;
    }

    float slingDuration;

    public IEnumerator Hit()
    {
        if (hitPower <= 5) yield break;
        firstBreak = false;
        poolCam.gameState = PoolCamBehaviour.GameState.Hit;
        float time = 0;
        spinObj.SetActive(false);

        Vector3 startPos = cue.transform.localPosition;

        slingDuration = Mathf.Lerp(0.4f, 0.24f, hitPower / power.maxValue);

        while (time <= slingDuration)
        {
            time += Time.smoothDeltaTime;
            float t = Mathf.SmoothStep(0, 1, time / slingDuration);
            cue.transform.localPosition = Vector3.Lerp(startPos, cueOgPos, t);
            yield return null;
        }

        isWaiting = true;
        powerBar.SetActive(false);

        Vector3 direction = cueStick.right.normalized;
        cue.SetActive(false);
        Debug.Log("power  " + hitPower);
        ballR.AddForceAtPosition(direction * hitPower, forceAt.position, ForceMode.Force);
        
        StartCoroutine(ResetCue());
    }

    IEnumerator ResetCue()
    {
        dragPower = false;
        
        yield return new WaitForSeconds(2f);
        yield return new WaitUntil(BallStopped);
        yield return new WaitForSeconds(2f);
        spinObj.SetActive(true);
        power.maxValue = 90;
        spinIndicator.anchoredPosition = Vector2.zero;
        spinRect.anchoredPosition = Vector2.zero;
        spinMark.transform.localPosition = Vector3.zero;
        spun = false;
        hasSpin = false;
        hitPower = 0;
        power.value = 0;

        if (gameOver) yield break;

        if (!pocketed)
        {
            GameLogic.instance.currentPlayer = GameLogic.instance.currentPlayer == GameLogic.CurrentPlayer.player1 ? GameLogic.CurrentPlayer.player2 : GameLogic.CurrentPlayer.player1;
        }

        playerIndicator[(int)GameLogic.instance.currentPlayer].SetActive(true);
        playerIndicator[(int)GameLogic.instance.GetOpponent(GameLogic.instance.currentPlayer)].SetActive(false);

        if (isFoul)
        {
            StartCoroutine(FoulReset());
            yield break;
        }

        pocketed = false;
        isBreak = false;
        rand = Random.Range(0, balls.Count - 1);
        targetBall = balls[rand];

        cueAnchor.transform.SetParent(cueBall.transform);
        cueAnchor.transform.localPosition = Vector3.zero;

        Vector3 direction = targetBall.transform.position - cueAnchor.transform.position;
        direction.y = 0;
        direction.Normalize();

        Quaternion newRotation = Quaternion.LookRotation(direction);
        cueAnchor.transform.rotation = Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0);

        Vector3 worldPosition = cueAnchor.transform.position;
        cueAnchor.transform.SetParent(null);
        cueAnchor.transform.position = worldPosition;
        cue.SetActive(true);
        cue.transform.localPosition = cueOgPos;

        poolCam.gameState = PoolCamBehaviour.GameState.Reset;
        isWaiting = false;
        powerBar.SetActive(true);
    }

    IEnumerator FoulReset()
    {
        cueBall.transform.localPosition = new Vector3(0.955f, 0.446f, 0f);
        cueBall.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        cueBall.GetComponent<Rigidbody>().isKinematic = false;
        GameLogic.instance.startPanel.SetActive(true);
        GameLogic.instance.placeBallPop.SetActive(true);
        poolCam.gameState = PoolCamBehaviour.GameState.Break;
        isFoul = false;
        yield return null;
    }

    #region aimlinerender
    int points = 1;
    [SerializeField] RaycastHit hiit;
    [SerializeField] float collDistance;
    [SerializeField] Vector3 fallPoint;


    void RenderTrajectory()
    {
        Vector3 startPosition = cueBall.transform.position;
        Vector3 direction = cueStick.right;

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPosition);

        points = 1;

        if (ballR.SweepTest(direction, out hiit, 180))
        {
            points++;
            if (points > maxBounces) return;

            if (hiit.collider.CompareTag("playBall"))
            {
                collDistance = hiit.distance;

                fallPoint = cueBall.transform.position + direction * collDistance;                

                lineRenderer.positionCount = points;
                lineRenderer.SetPosition(points - 1, fallPoint);

                aimDock.SetActive(true);                
                aimDock.transform.position = fallPoint;

                Vector3 newStart = hiit.point;

                Vector3 newDir = (hiit.collider.transform.position - hiit.point).normalized;

                Ray hitRay = new Ray(newStart, newDir);

                linePlay.positionCount = 2;
                linePlay.SetPosition(0, newStart);
                linePlay.SetPosition(1, hitRay.GetPoint(0.3f));
            }
            else
            {
                aimDock.SetActive(false);
                lineRenderer.positionCount = points;
                lineRenderer.SetPosition(points - 1, hiit.point);
                linePlay.positionCount = 0;
            }
        }
        else
        {

        }
    }
    #endregion

    bool BallStopped()
    {
        foreach (var ball in balls)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb.velocity != Vector3.zero || ballRb.angularVelocity != Vector3.zero)
            {
                return false;
            }
        }
        return true;
    }
}
