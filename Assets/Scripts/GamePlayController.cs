using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

public class GamePlayController : MonoBehaviour
{
    public static GamePlayController instance;

    public List<GameObject> balls, cpuBalls;

    [SerializeField] Vector4 clampTableBreak, clampTableNormal;
    [SerializeField] private Vector3 cueOgPos, cueOgRot, spinMarkOffset;
    [SerializeField] private Vector2 deltaPosition, deltaPos;
    [SerializeField] private GameObject targetBall, cueBall, powerBar, aimDock, spinObj;
    [SerializeField] private float rotationSpeed = 0.1f, powerMultiplier, cueBallRadius, ballRadius, ballYpos, dockYpos;
    [SerializeField] private Camera povCam;
    [SerializeField] private Transform forceAt;
    [SerializeField] private LineRenderer lineCue, linePath;
    [SerializeField] private PoolCamBehaviour poolCam;
    [SerializeField] private PowerControl power;
    [SerializeField] private RectTransform spinRect, circleRect, spinIndicator;
    [SerializeField] public TextMeshProUGUI player1Txt, player2Txt;
    [SerializeField] public GameObject[] pockets;
    [SerializeField] LayerMask closeMask;
    [SerializeField] public AudioSource gameAudio;
    [SerializeField] public AudioClip cueHit, rolling;

    public GameObject cue, spinMark, cueAnchor;
    public bool isBreak, spun, hasSpin, isWaiting, pocketed, ballAssigned, updown, isFoul, firstBreak, gameOver, touchDisabled, firstHit;
    private bool looked;
    private int rand;

    private Rigidbody ballR;

    private Ray pRay;
    private RaycastHit bHit;

    public float time, duration, hitPower, dockOffset;

    public bool cpuMode;

    public GameManager manager;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        cueOgPos = cue.transform.localPosition;
        ballR = cueBall.GetComponent<Rigidbody>();
        cueBallRadius = cueBall.GetComponent<SphereCollider>().radius * cueBall.transform.localScale.x;
        ballRadius = balls[2].GetComponent<MeshRenderer>().bounds.extents.x;
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

    void OnDrawGizmos()
    {
        // Draw the direction to the pocket in red
        if (lockedPocket != null && cueBall != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cueBall.transform.position, hitPoint);
        }

        // Draw the pocket direction from the ball to the pocket in green
        if (lastPocketDirection != Vector3.zero && cueBall != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(lockedBall.transform.position, lastPocketDirection);
        }

        // Draw the cue ball hitting direction in blue
        if (lastHittingDirection != Vector3.zero && cueBall != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cueBall.transform.position, lastHittingDirection);
        }
    }


    #endregion

    void Update()
    {
        if (!manager || manager.players[manager.currentPlayer].name == "CPU") return;
        if(manager.gameMode==GameManager.GameMode.online)
        {

        }
        else
        {
            HandleTouchInput();
        }
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
            StartCoroutine(LookAtTarget(balls[0]));
        }        
    }

    public void StartCPUMode()
    {
        cueAnchor.transform.SetParent(cueBall.transform);
        cueAnchor.transform.localPosition = Vector3.zero;
        cueAnchor.transform.SetParent(null);
        cue.SetActive(true);
        cpuMode = true;
        StartCoroutine(HandleCpuPlay());             
    }


    #region InputHandle
    void HandleTouchInput()
    {
        if (touchDisabled) return;
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
            //if (touch.phase == TouchPhase.Ended && dragPower)
            //{
            //    StartCoroutine(PlayShot());
            //}

        }
    }

    void HandleBreak(Touch touch)
    {
        foreach (GameObject ball in balls)
        {
            ball.GetComponent<Rigidbody>().isKinematic = true;
        }

        if (touch.phase == TouchPhase.Moved)
        {
            Vector3 screenDelta = new Vector3(touch.deltaPosition.x, touch.deltaPosition.y, 0f);

            screenDelta *= 0.01f;

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 move = camRight * screenDelta.x + camForward * screenDelta.y;

            cueBall.transform.localPosition += move;

            //clamp
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

            Vector3 newSpinMarkPosition = new Vector3(spinMark.transform.localPosition.x, normalizedY * 0.03f, normalizedX * 0.03f * -1);
            spinMark.transform.localPosition = newSpinMarkPosition;
            newSpinMarkPosition = spinMark.transform.position;
            spinMark.transform.position = cueBall.GetComponent<MeshRenderer>().bounds.ClosestPoint(newSpinMarkPosition);
            spinIndicator.anchoredPosition = new Vector2(normalizedX * 50, normalizedY * 50);
        }
    }

    IEnumerator LookAtTarget(GameObject obj)
    {
        Debug.Log("here");
        time = 0;
        duration = .8f;
        targetBall = obj;
        Vector3 direction = targetBall.transform.position - cueAnchor.transform.position;
        direction.y = 0;
        direction.Normalize();

        Quaternion newRotation = Quaternion.LookRotation(direction);
        cueAnchor.transform.DORotateQuaternion(Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0), duration).SetEase(Ease.OutSine);


        //while (time < duration)
        //{
        //    time += Time.deltaTime;
        //    float t = Mathf.SmoothStep(0, 1, time / duration);
        //    cueAnchor.transform.rotation = Quaternion.Slerp(cueAnchor.transform.rotation, Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0), t);
        //    yield return null;
        //}
        yield return null;
        looked = false;
    }

    #endregion

    #region CpuPlay
    public Transform lockedPocket;
    public Vector3 hitPoint;
    bool playableBallFound;

    Vector3 lastPocketDirection;
    Vector3 lastHittingDirection;

    Transform lockedBall;

    IEnumerator HandleCpuCueBallPlace()
    {
        cueBall.transform.localPosition = new Vector3(1.2f, .768f, -.5f);
        cueAnchor.transform.SetParent(cueBall.transform);
        cueAnchor.transform.localPosition = Vector3.zero;
        cueAnchor.transform.SetParent(null);
        StartCoroutine(HandleCpuPlay());
        yield return null;
    }

    public List<CpuShotOption> debugShotOptions = new List<CpuShotOption>();

    IEnumerator HandleCpuPlay()
    {
        poolCam.gameState = PoolCamBehaviour.GameState.Waiting;
        yield return new WaitUntil(() => poolCam.doneCameraMove);
        poolCam.doneCameraMove = false;

        if (firstBreak)
        {
            hitPower = power.maxValue;
            yield return new WaitForSeconds(1.7f);
            StartCoroutine(PlayShot());
            yield break;
        }

        yield return new WaitForSeconds(1f);
        var bestShot = EvaluateAllPossibleShots();

        if (bestShot == null)
        {
            Debug.Log("No valid shots.");
            yield break;
        }

        lockedBall = bestShot.ball;
        lockedPocket = bestShot.pocket;
        hitPoint = bestShot.hitPoint;

        Debug.Log($"CPU selected: {lockedBall.name} -> {lockedPocket.name}, Score: {bestShot.score:F2}");

        Vector3 cueDirection = (hitPoint - cueBall.transform.position).normalized;
        cueDirection.y = 0;
        cue.SetActive(true);
        Quaternion newRotation = Quaternion.LookRotation(cueDirection);
        newRotation = Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0);

        float elapsedTime = 0f;
        float rotationDuration = 0.5f;
        Quaternion startRotation = cueAnchor.transform.rotation;

        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            cueAnchor.transform.rotation = Quaternion.Slerp(startRotation, newRotation, elapsedTime / rotationDuration);
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        //hitPower = 80f;
        Vector3 cueDir = (hitPoint - cueBall.transform.position).normalized;
        float basePower = 120f;
        float cueBallFactor = 1.2f;
        float ballToPocketFactor = 1.8f;
        float anglePenalty = Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(cueDir.normalized, (lockedPocket.position - lockedBall.position).normalized)));

        float cueDistance = Vector3.Distance(cueBall.transform.position, hitPoint);
        float ballDistance = Vector3.Distance(lockedBall.position, lockedPocket.position);

        // Main power equation
        float calculatedPower = basePower + (cueDistance * cueBallFactor) + (ballDistance * ballToPocketFactor) + (anglePenalty * 40f);

        // Clamp to prevent overhit
        hitPower = Mathf.Clamp(calculatedPower, 50f, 200f);
        yield return new WaitForSeconds(0.6f);
        StartCoroutine(PlayShot());
    }

    CpuShotOption EvaluateAllPossibleShots()
    {
        List<GameObject> cpuPlayableBalls;
        if (manager.player2.pocketedBalls.Count == 7)
        {
            cpuPlayableBalls =  new()
            {
                balls[7]
            };
        }
        else
        {
            cpuPlayableBalls = ballAssigned ? cpuBalls : balls;
        }
        debugShotOptions.Clear();
        List<CpuShotOption> shotOptions = new List<CpuShotOption>();

        foreach (GameObject ball in cpuPlayableBalls)
        {
            if (!ball.activeInHierarchy) continue;         

            foreach (GameObject pocket in pockets)
            {
                if (!IsShotPossible(ball, pocket)) continue;

                Vector3 cueToBall = (ball.transform.position - cueBall.transform.position).normalized;
                Vector3 ballToPocket = (pocket.transform.position - ball.transform.position).normalized;

                float cueAlignment = Vector3.Dot(cueToBall, ballToPocket);
                if (cueAlignment < 0.5f) continue;

                Vector3 hitPoint = HitPoint(ball.transform.position, pocket.transform.position);
                float cueDist = Vector3.Distance(cueBall.transform.position, hitPoint);

                Vector3 cueDir = (hitPoint - cueBall.transform.position).normalized;
                if (Physics.SphereCast(cueBall.transform.position, cueBallRadius * 0.95f, cueDir, out RaycastHit hit, cueDist))
                {
                    if (hit.collider.CompareTag("playBall") && hit.collider.transform != ball.transform)
                    {
                        Debug.Log($"⚠️ Blocked on final aim: {ball.name} to {pocket.name} by {hit.collider.name}");
                        continue; // reject this shot
                    }
                }

                float pocketDist = Vector3.Distance(ball.transform.position, pocket.transform.position);
                float alignment = Vector3.Dot(cueToBall, ballToPocket);

                float score = (alignment * 100f) + (cueAlignment * 80f) - (cueDist * 1.2f) - (pocketDist * 1.5f);

                CpuShotOption option = new CpuShotOption(ball.transform, pocket.transform, hitPoint, cueDist, pocketDist, alignment, cueAlignment, score);
                shotOptions.Add(option);
                debugShotOptions.Add(option);

                Debug.DrawLine(cueBall.transform.position, hitPoint, Color.green, 2f);
                Debug.DrawLine(ball.transform.position, pocket.transform.position, Color.yellow, 2f);
            }
        }

        if (shotOptions.Count > 0)
        {
            shotOptions.Sort((a, b) => b.score.CompareTo(a.score));
            return shotOptions[0];
        }
        else
        {
            foreach (GameObject ball in cpuPlayableBalls)
            {
                if (!ball.activeInHierarchy) continue;

                foreach (GameObject pocket in pockets)
                {
                    Vector3 dir = (pocket.transform.position - ball.transform.position).normalized;
                    Vector3 fallbackHitPoint = ball.transform.position - dir * (2 * ballRadius);

                    float cueToBallDist = Vector3.Distance(cueBall.transform.position, fallbackHitPoint);
                    float ballToPocketDist = Vector3.Distance(ball.transform.position, pocket.transform.position);
                    float alignment = Vector3.Dot((ball.transform.position - cueBall.transform.position).normalized, dir);
                    float cueAlign = alignment;

                    float fallbackScore = -1000f; // super low to mark it as fallback

                    var fallbackShot = new CpuShotOption(ball.transform, pocket.transform, fallbackHitPoint,
                        cueToBallDist, ballToPocketDist, alignment, cueAlign, fallbackScore);

                    debugShotOptions.Add(fallbackShot);
                    return fallbackShot;
                }
            }
        }
        return null;
    }

    bool IsShotPossible(GameObject ball, GameObject pocket)
    {
        // Ball to pocket
        Vector3 ballToPocket = (pocket.transform.position - ball.transform.position).normalized;
        RaycastHit[] pocketHits = ball.GetComponent<Rigidbody>().SweepTestAll(ballToPocket);

        foreach (RaycastHit hit in pocketHits)
        {
            if (hit.collider.CompareTag("playBall") || hit.collider.CompareTag("cushion"))
                return false;
        }

        // Cue to ball
        Vector3 cueDir = (ball.transform.position - cueBall.transform.position).normalized;
        float cueDist = Vector3.Distance(cueBall.transform.position, ball.transform.position);

        if (Physics.SphereCast(cueBall.transform.position, cueBallRadius * 0.95f, cueDir, out RaycastHit hitCue, cueDist))
        {
            if (hitCue.collider.CompareTag("playBall") && hitCue.transform.gameObject != ball)
                return false;
        }

        // Angle logic
        Vector3 cueToBall = (ball.transform.position - cueBall.transform.position).normalized;
        Vector3 ballToPocketDir = (pocket.transform.position - ball.transform.position).normalized;

        float dot = Vector3.Dot(cueToBall, ballToPocketDir);
        return dot > 0.3f;
    }


    Vector3 HitPoint(Vector3 ballPos, Vector3 pocketPos)
    {
        Vector3 ballToPocket = (pocketPos - ballPos).normalized;

        // Ghost ball position = where cue ball center should be to send object ball into pocket
        Vector3 ghostBallPos = ballPos - ballToPocket * (2f * ballRadius);

        return ghostBallPos;
    }


    #endregion

    #region GameMech

    float slingDuration;

    public IEnumerator PlayShot()
    {
        if (hitPower <= 5) yield break;

        poolCam.gameState = PoolCamBehaviour.GameState.Hit;
        spinObj.SetActive(false);
        Vector3 startPos = cue.transform.localPosition;

        slingDuration = Mathf.Lerp(0.4f, 0.24f, hitPower / power.maxValue);

        cue.transform.DOLocalMove(cueOgPos, slingDuration).SetEase(Ease.OutSine);

        isWaiting = true;
        powerBar.SetActive(false);

        Vector3 direction = cueAnchor.transform.right.normalized;
        cue.SetActive(false);
        gameAudio.PlayOneShot(cueHit);
        Vector3 offset = forceAt.position - spinMark.transform.position;
        Vector3 spinDirection = Vector3.Cross(direction, offset.normalized);

        ballR.AddForceAtPosition(direction * hitPower * .008f, spinMark.transform.position, ForceMode.Impulse);

        ballR.AddTorque(spinDirection * hitPower * 0.008f, ForceMode.Impulse);
        DisableLine();
        StartCoroutine(ResetCue());
    }

    public void PlaySyncedShot(Vector3 direction, float power, Vector3 spin)
    {
        spinObj.SetActive(false);
        cue.SetActive(false);
        powerBar.SetActive(false);

        gameAudio.PlayOneShot(cueHit);

        Vector3 offset = forceAt.position - spin;
        Vector3 spinDirection = Vector3.Cross(direction, offset.normalized);

        ballR.AddForceAtPosition(direction * power * .008f, spin, ForceMode.Impulse);
        ballR.AddTorque(spinDirection * power * 0.008f, ForceMode.Impulse);

        StartCoroutine(ResetCue());
    }


    IEnumerator ResetCue()
    {
        //dragPower = false;
        yield return new WaitForSeconds(2f);
        yield return new WaitUntil(BallStopped);
        yield return new WaitForSeconds(2f);
        ballR.linearVelocity = ballR.angularVelocity = Vector3.zero;
        spinIndicator.anchoredPosition = Vector2.zero;
        spinRect.anchoredPosition = Vector2.zero;
        spinMark.transform.localPosition = spinMarkOffset;
        spun = false;
        hasSpin = false;
        hitPower = 0;
        power.value = 0;

        if (gameOver) yield break;

        if (!pocketed || isFoul)
        {
            manager.SwitchTurn();
        }

        manager.SetIndicator();

        if (isFoul)
        {
            pocketed = false;
            StartCoroutine(FoulReset());            
            yield break;
        }

        pocketed = false;
        isBreak = false;
        firstBreak = false;
        firstHit = false;
        rand = UnityEngine.Random.Range(0, balls.Count - 1);
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
        if (manager.players[manager.currentPlayer].name == "CPU")
        {
            StartCoroutine(HandleCpuPlay());
        }
        else
        {
            spinObj.SetActive(true);
            powerBar.SetActive(true);
        }
    }

    IEnumerator FoulReset()
    {
        firstBreak = false;
        firstHit = false;
        cueBall.GetComponent<Rigidbody>().isKinematic = true;
        cueBall.transform.localPosition = new Vector3(0.955f, ballYpos, 0f);
        cueBall.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        poolCam.transform.rotation = Quaternion.Euler(0, 0, 0);
        cueBall.GetComponent<Rigidbody>().isKinematic = false;
        if (manager.players[manager.currentPlayer].name =="CPU")
        {
            StartCoroutine(HandleCpuCueBallPlace());
        }
        else
        {
            manager.startPanel.SetActive(true);
            manager.placeBallPop.SetActive(true);
            poolCam.gameState = PoolCamBehaviour.GameState.Break;
        }
        isFoul = false;
        yield return null;
    }

    public bool CueBallValid()
    {
        for(int i=0;i<balls.Count-1;i++)
        {
            if (balls[i].GetComponent<MeshRenderer>().bounds.Contains(cueBall.transform.position))
            {
                Debug.Log("issue");
                return false;
            }
        }
        return true;
    }

    #endregion

    #region aimlinerender
    [SerializeField] float collDistance;
    [SerializeField] Vector3 fallPoint, newDir;
    public Vector3 direction, returnVector;


    private List<Vector3> linePoints = new List<Vector3>();
    public float maxStepDistance = 10f;
    public float targetExtensionLength = 0.01f;

    [Header("Layer Masks")]
    [Tooltip("Layers that the trajectory prediction should interact with (Balls, Cushions)")]
    public LayerMask collisionLayers;
    [Tooltip("The specific layer assigned to cushions/table edges")]
    public LayerMask cushionLayer;
    [Tooltip("The specific layer assigned to playable balls (excluding cue ball initially)")]
    public LayerMask playBallLayer;

    public float aimWidth= 0.03f;

    public void DisableLine()
    {
        lineCue.positionCount = 0;
        linePath.positionCount = 0;
        aimDock.SetActive(false);
    }

    [Range(-0.2f, 0.2f)]
    public float visualInaccuracyOffset = 0.05f; // +ve shifts right, -ve shifts left

    public void RenderTrajectory()
    {
        linePoints.Clear();
        if (linePath != null) linePath.positionCount = 0;
        if (aimDock != null) aimDock.SetActive(false);

        if (cueBall == null || cueAnchor == null || lineCue == null)
            return;

        Vector3 currentPosition = cueBall.transform.position;
        Vector3 currentDirection = cueAnchor.transform.right.normalized;

        linePoints.Add(currentPosition);

        if (Physics.SphereCast(currentPosition, cueBallRadius, currentDirection, out RaycastHit hit, maxStepDistance, collisionLayers))
        {
            Vector3 cueBallSurfaceContactPoint = currentPosition + currentDirection * hit.distance;
            Debug.DrawRay(hit.point, hit.point - hit.collider.transform.position, Color.red);
            linePoints.Add(cueBallSurfaceContactPoint);

            GameObject hitObject = hit.collider.gameObject;
            int hitLayerValue = 1 << hitObject.layer;

            if ((playBallLayer.value & hitLayerValue) != 0)
            {
                Vector3 hitBallCenter = hit.collider.transform.position;
                Vector3 cueBallCenterAtImpact = currentPosition + currentDirection * hit.distance;

                // The correct travel direction
                Vector3 objectBallTravelDir = (hitBallCenter - cueBallCenterAtImpact).normalized;

                // Apply visual offset (left or right)
                Vector3 sideOffset = Vector3.Cross(Vector3.up, objectBallTravelDir).normalized;
                Vector3 adjustedDirection = (objectBallTravelDir + sideOffset * visualInaccuracyOffset).normalized;

                if (linePath != null)
                {
                    RenderTargetBallPath(hitBallCenter, objectBallTravelDir);
                    //RenderTargetBallPath(hit.collider.transform.position, hit.point);
                }

                if (aimDock != null)
                {
                    aimDock.SetActive(true);
                    Vector3 dockPos = currentPosition + currentDirection * (hit.distance - aimWidth);
                    aimDock.transform.position = dockPos;
                }
            }
        }
        else
        {
            linePoints.Add(currentPosition + currentDirection * maxStepDistance);
        }

        lineCue.positionCount = linePoints.Count;
        lineCue.SetPositions(linePoints.ToArray());
    }

    void RenderTargetBallPath(Vector3 startPos, Vector3 direction)
    {
        if (linePath == null) return;
        linePath.positionCount = 2;
        linePath.SetPosition(0, startPos);
        linePath.SetPosition(1, startPos + direction * 0.2f);

        if (Physics.SphereCast(startPos, ballRadius, direction, out RaycastHit targetHit, .1f, collisionLayers))
        {
            Vector3 targetBallSurfaceContact = startPos + direction * .2f;
            linePath.SetPosition(1, targetBallSurfaceContact);
        }
    }

    #endregion

    #region helpers

    //public float velocityThreshold = 0.01f;
    //public float settleTime = 0.5f;
    //public float checkInterval = 0.1f;

    //public event Action OnAllBallsStopped;

    //private Coroutine checkRoutine;

    //public void BeginMonitoring()
    //{
    //    if (checkRoutine != null)
    //        StopCoroutine(checkRoutine);

    //    checkRoutine = StartCoroutine(CheckBallsRoutine());
    //}

    //private IEnumerator CheckBallsRoutine()
    //{
    //    float timer = 0f;

    //    while (true)
    //    {
    //        bool allBelowThreshold = true;

    //        foreach (GameObject ball in balls)
    //        {
    //            Rigidbody rb = ball.GetComponent<Rigidbody>();
    //            if (!rb || !rb.gameObject.activeInHierarchy)
    //                continue;

    //            if (rb.velocity.sqrMagnitude > velocityThreshold * velocityThreshold ||
    //                rb.angularVelocity.sqrMagnitude > velocityThreshold * velocityThreshold)
    //            {
    //                allBelowThreshold = false;
    //                break;
    //            }
    //        }

    //        if (allBelowThreshold)
    //        {
    //            timer += checkInterval;
    //            if (timer >= settleTime)
    //                break;
    //        }
    //        else
    //        {
    //            timer = 0f;
    //        }

    //        yield return new WaitForSeconds(checkInterval);
    //    }

    //    checkRoutine = null;
    //    OnAllBallsStopped?.Invoke();
    //}

    //public void StopMonitoring()
    //{
    //    if (checkRoutine != null)
    //    {
    //        StopCoroutine(checkRoutine);
    //        checkRoutine = null;
    //    }
    //}

    public bool BallStopped()
    {
        foreach (GameObject ball in balls)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ball.activeInHierarchy && ballRb.linearVelocity != Vector3.zero || ballRb.angularVelocity != Vector3.zero)
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}

#region helperClass

[System.Serializable]
public class CpuShotOption
{
    public string ballName;
    public string pocketName;
    public Transform ball;
    public Transform pocket;
    public Vector3 hitPoint;
    public float cueToBallDistance;
    public float ballToPocketDistance;
    public float alignment;
    public float cueAlignment;
    public float score;

    public CpuShotOption(
        Transform ball, Transform pocket, Vector3 hitPoint,
        float cueToBallDistance, float ballToPocketDistance,
        float alignment, float cueAlignment, float score)
    {
        this.ball = ball;
        this.pocket = pocket;
        this.hitPoint = hitPoint;
        this.ballName = ball.name;
        this.pocketName = pocket.name;
        this.cueToBallDistance = cueToBallDistance;
        this.ballToPocketDistance = ballToPocketDistance;
        this.alignment = alignment;
        this.cueAlignment = cueAlignment;
        this.score = score;
    }
}
#endregion

