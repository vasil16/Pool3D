using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PoolMain : MonoBehaviour
{
    public static PoolMain instance;

    public List<GameObject> balls;

    [SerializeField] Vector4 clampTableBreak,clampTableNormal;
    [SerializeField] private Vector3 lineRendererOffset, cueOgPos, cueOgRot;
    [SerializeField] private Vector2 deltaPosition, deltaPos;
    [SerializeField] private GameObject targetBall, cueBall, powerBar, aimDock, spinObj;
    [SerializeField] private float rotationSpeed = 0.1f, powerMultiplier, maxDistance = 3, secMax = 2, ballWidth, ballYpos;
    [SerializeField] private Camera povCam;
    [SerializeField] private Transform forceAt;
    [SerializeField] private LineRenderer lineRenderer, linePlay;
    [SerializeField] private PoolCamBehaviour poolCam;
    [SerializeField] private PowerControl power;
    [SerializeField] private RectTransform spinRect, circleRect, spinIndicator;
    [SerializeField] public TextMeshProUGUI player1Txt, player2Txt;
    [SerializeField] public GameObject[] playerIndicator;
    [SerializeField] Rigidbody simCueBall;
    [SerializeReference] GameObject simBall;
    [SerializeField] LayerMask closeMask;
    [SerializeField] public AudioSource gameAudio;
    [SerializeField] public AudioClip cueHit, rolling;

    public GameObject cue, spinMark, cueAnchor;
    public bool isBreak = true;
    public bool dragPower, spun, hasSpin, isWaiting, pocketed, firstPot, updown, isFoul, firstBreak, gameOver;
    private bool looked;
    private int maxBounces = 2;
    private int rand;

    private Rigidbody ballR;

    private Ray pRay;
    private RaycastHit bHit;

    public float time, duration, hitPower, speedReductVal;


    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        cueOgPos = cue.transform.localPosition;
        ballR = cueBall.GetComponent<Rigidbody>();
        //ballWidth = cueBall.GetComponent<MeshRenderer>().bounds.size.x/2;
        ballWidth = cueBall.GetComponent<SphereCollider>().radius;
        //ballWidth = 0.0345f;
        Application.targetFrameRate = 60;
        //power.maxValue = Random.Range(190, 208);
        PlayerPrefs.DeleteAll();
    }

    #region helperGizmos
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
    #endregion

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
            if (Physics.Raycast(pRay, out bHit, closeMask) && bHit.collider.gameObject.CompareTag("playBall") && !looked)
            {
                StartCoroutine(LookAtTarget(bHit.collider.gameObject));
                looked = true;
            }
            if (touch.phase == TouchPhase.Ended && dragPower)
            {
                StartCoroutine(Hit());
            }

        }
    }

    void HandleBreak(Touch touch)
    {
        if (touch.phase == TouchPhase.Moved)
        {
            Vector3 newPos = touch.deltaPosition * 0.1f * Time.deltaTime;
            cueBall.transform.localPosition += new Vector3(newPos.y, 0, newPos.x*-1);

            if (firstBreak)
            {
                float clampedX = Mathf.Clamp(cueBall.transform.localPosition.x, clampTableBreak.x, clampTableBreak.y);
                float clampedZ = Mathf.Clamp(cueBall.transform.localPosition.z, clampTableBreak.z, clampTableBreak.w);
                cueBall.transform.localPosition = new Vector3(clampedX, cueBall.transform.localPosition.y, clampedZ);
            }
            else
            {
                float clampedX = Mathf.Clamp(cueBall.transform.localPosition.x, clampTableNormal.x, clampTableNormal.y);
                float clampedZ = Mathf.Clamp(cueBall.transform.localPosition.z, clampTableNormal.z, clampTableNormal.w);
                cueBall.transform.localPosition = new Vector3(clampedX, cueBall.transform.localPosition.y, clampedZ);
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
        
        poolCam.gameState = PoolCamBehaviour.GameState.Hit;
        float time = 0;
        spinObj.SetActive(false);
        firstBreak = false;
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

        Vector3 direction = cueAnchor.transform.right.normalized;
        cue.SetActive(false);
        gameAudio.PlayOneShot(cueHit);
        ballR.AddForceAtPosition(direction * hitPower, forceAt.position, ForceMode.Force);
        StartCoroutine(ResetCue());
    }

    IEnumerator ResetCue()
    {
        dragPower = false;        
        yield return new WaitForSeconds(2f);

        ballR.constraints = RigidbodyConstraints.None;
        yield return new WaitUntil(BallStopped);
        yield return new WaitForSeconds(2f);
        
        ballR.drag = 0.23f;
        spinObj.SetActive(true);
        speedReductVal = 0.6f;
        power.maxValue = 170;
        spinIndicator.anchoredPosition = Vector2.zero;
        spinRect.anchoredPosition = Vector2.zero;
        spinMark.transform.localPosition = Vector3.zero;
        spun = false;
        hasSpin = false;
        hitPower = 0;
        power.value = 0;

        if (gameOver) yield break;

        if (!pocketed || isFoul)
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
        cueBall.GetComponent<Rigidbody>().isKinematic = true;
        cueBall.transform.localPosition = new Vector3(0.955f, ballYpos, 0f);
        cueBall.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        poolCam.transform.rotation = Quaternion.Euler(0, 0, 0);
        cueBall.GetComponent<Rigidbody>().isKinematic = false;
        GameLogic.instance.startPanel.SetActive(true);
        GameLogic.instance.placeBallPop.SetActive(true);
        poolCam.gameState = PoolCamBehaviour.GameState.Break;
        isFoul = false;
        yield return null;
    }

    #region aimlinerender
    int points = 1;
    [SerializeField] RaycastHit hiit, lHit;
    [SerializeField] float collDistance;
    [SerializeField] Vector3 fallPoint, newDir;
    bool directionSet;
    public Vector3 direction, returnVector;

    void RenderTrajectory()
    {
        Vector3 startPosition = cueBall.transform.position;
        Vector3 direction = cueAnchor.transform.right;

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPosition);

        points = 1;

        if(Physics.SphereCast(ballR.position, ballWidth,direction, out hiit, 180))
        //if (ballR.SweepTest(direction, out hiit, 180))
        {
            points++;
            if (points > maxBounces) return;

            collDistance = hiit.distance;

            fallPoint = cueBall.transform.position + direction * collDistance;

            //simBall.transform.position = fallPoint;

            Vector3 fixedPosition = fallPoint - (direction * ballWidth);

            if (hiit.collider.CompareTag("playBall"))
            {
                lineRenderer.positionCount = points;
                lineRenderer.SetPosition(points - 1, fixedPosition);

                Vector3 dockPos = new Vector3(fixedPosition.x, 0.7785423f, fixedPosition.z);

                aimDock.SetActive(true);
                aimDock.transform.position = dockPos;

                Vector3 newStart = fallPoint;

                linePlay.positionCount = 2;
                Vector3 startPoint = new Vector3(newStart.x, fallPoint.y, newStart.z);
                Vector3 endPoint = new Vector3(hiit.collider.transform.position.x, fallPoint.y, hiit.collider.transform.position.z);

                Vector3 newDir = (endPoint - startPoint).normalized; // Calculate direction vector
                float extensionLength = 0.2f; // Adjust this value to change the extension length

                linePlay.SetPosition(0, endPoint);
                //linePlay.SetPosition(1, endPoint + newDir * extensionLength);
                if (Physics.Raycast(hiit.transform.position, newDir, out lHit, 0.2f))
                {
                    Debug.Log("hhh");
                    linePlay.SetPosition(1, lHit.point);
                    return;
                }

                linePlay.SetPosition(1, endPoint + newDir * extensionLength);
                //    else
                //        //linePlay.SetPosition(1, hitRay.GetPoint(2f));
                //        linePlay.SetPosition(1, endPoint + newDir * extensionLength);                
            }
            else
            {
                aimDock.SetActive(false);
                lineRenderer.positionCount = points;
                lineRenderer.SetPosition(points - 1, fallPoint);
                linePlay.positionCount = 0;
            }
        }
        else
        {
            aimDock.SetActive(false);
            lineRenderer.positionCount = points;
            lineRenderer.SetPosition(points - 1, fallPoint);
            linePlay.positionCount = 0;
        }
    }

    IEnumerator SimulateDirection(Transform hitBall)
    {
        Debug.Log("run cc");
        ballR.isKinematic = true;
        simCueBall.transform.position = cueBall.transform.position;
        simCueBall.transform.rotation = cueBall.transform.rotation;
        simBall.transform.position = hitBall.position;
        simBall.transform.rotation = hitBall.rotation;
        simBall.GetComponent<Rigidbody>().isKinematic = true;
        yield return new WaitForSeconds(0.2f);
        // Ensure simBall is not kinematic and simulate a force
        simBall.GetComponent<Rigidbody>().isKinematic = false;
        simCueBall.AddForceAtPosition(direction * 85, forceAt.position, ForceMode.Force);
        yield return new WaitUntil(CheckKinematic);

        returnVector = simBall.transform.position; // Update returnVector based on simulated ball position
        linePlay.SetPosition(1, returnVector);
        // Clean up
        ballR.isKinematic = false; // Reset kinematic state
        directionSet = false;
    }

    bool CheckKinematic()
    {
        if (simBall.GetComponent<Rigidbody>().isKinematic)
        {
            return true;
        }
        return false;
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
