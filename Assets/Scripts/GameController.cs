using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    public List<GameObject> balls, cpuBalls;
    [SerializeField] Vector4 clampTableBreak, clampTableNormal;
    [SerializeField] private Vector3 cueOgPos, cueOgRot, spinMarkOffset;
    [SerializeField] private Vector2 deltaPosition, deltaPos;
    [SerializeField] private GameObject targetBall, cueBall, powerBar, aimDock, spinObj;
    [SerializeField] private float  powerMultiplier, cueBallRadius, ballRadius, ballYpos, dockYpos, slingDuration;
    [SerializeField] private Transform forceAt;
    [SerializeField] private LineRenderer lineCue, linePath;
    [SerializeField] private CameraController poolCam;
    //[SerializeField] private PowerControl power;
    [SerializeField] CuePowerControl power;
    [SerializeField] private RectTransform spinRect, circleRect, spinIndicator;
    
    [SerializeField] private GameObject[] pockets;
    [SerializeField] public AudioClip cueHit, rolling;
    [SerializeField] Text fpsText;

    public GameObject cue, spinMark, cueAnchor;
    public bool isBreak, spun, hasSpin, isWaiting, pocketed, ballAssigned, updown, isFoul, firstBreak, gameOver, touchDisabled, firstHit;
    private int rand;

    private Rigidbody ballR;

    public float hitPower, dockOffset;
    public float maxHitPower;

    public bool cpuMode;

    public GameManager manager;

    private void Awake()
    {
        Application.targetFrameRate = 120;
        instance = this;
    }

    private void OnEnable()
    {
        
        EventHandler.AddSpin += HandleSpinControl;
        EventHandler.MoveCueBall += MoveCueBall;
    }

    private void OnDestroy()
    {
        
        EventHandler.AddSpin -= HandleSpinControl;
        EventHandler.MoveCueBall -= MoveCueBall;
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

    //void OnDrawGizmos()
    //{
    //    // Draw the direction to the pocket in red
    //    if (lockedPocket != null && cueBall != null)
    //    {
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawLine(cueBall.transform.position, hitPoint);
    //    }

    //    // Draw the pocket direction from the ball to the pocket in green
    //    if (lastPocketDirection != Vector3.zero && cueBall != null)
    //    {
    //        Gizmos.color = Color.green;
    //        Gizmos.DrawLine(lockedBall.transform.position, lastPocketDirection);
    //    }

    //    // Draw the cue ball hitting direction in blue
    //    if (lastHittingDirection != Vector3.zero && cueBall != null)
    //    {
    //        Gizmos.color = Color.blue;
    //        Gizmos.DrawLine(cueBall.transform.position, lastHittingDirection);
    //    }
    //}


    #endregion

    void Update()
    {
        //Application.targetFrameRate = 120;
        //fpsText.text = 1 / Time.deltaTime+"";
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
        poolCam.transform.DORotateQuaternion(Quaternion.Euler(poolCam.transform.eulerAngles.x, cueAnchor.transform.eulerAngles.y, poolCam.transform.eulerAngles.z),1f).SetEase(Ease.InOutCubic);
        if (firstBreak)
        {
            //LookAt(balls[0]);
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

    [SerializeField] RectTransform spinCotrolUI;

    void HandleTouchInput()
    {                
        if (GameManager.instance.gameState == GameManager.GameState.Aim)
        {
            RenderTrajectory();
            return;
        }        
    }

    void MoveCueBall(Vector2 screenDelta)
    {
        screenDelta *= 0.003f;

        Vector3 camRight = Camera.main.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 move = camRight * screenDelta.x + camForward * screenDelta.y;

        Vector3 invertMove = camRight * screenDelta.y + camForward * -screenDelta.x;
        cueBall.transform.localPosition += move;
        cueBall.transform.RotateAroundLocal(invertMove, .1f);

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

    void HandleBreak()
    {
        foreach (GameObject ball in balls)
        {
            ball.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    void HandleSpinControl(Vector2 pos)
    {
        if (!spinCotrolUI.gameObject.active) return;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(circleRect, pos, null, out localPoint);
        hasSpin = true;

        Vector2 center = circleRect.rect.center;
        float radius = circleRect.rect.width / 2;

        if (Vector2.Distance(center, localPoint) <= radius)
        {
            spinRect.transform.position = pos;
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

    #region CpuPlay
    public Transform lockedPocket;
    public Vector3 hitPoint;
    Transform lockedBall;

    public List<CpuShotOption> debugShotOptions = new List<CpuShotOption>();
    [SerializeField] private LayerMask ballLayerMask; // Set this to your Ball layer in Inspector

    IEnumerator HandleCpuCueBallPlace()
    {
        cueBall.transform.localPosition = new Vector3(1.2f, .768f, -.5f);
        cueAnchor.transform.SetParent(cueBall.transform);
        cueAnchor.transform.localPosition = Vector3.zero;
        cueAnchor.transform.SetParent(null);
        StartCoroutine(HandleCpuPlay());
        yield return null;
    }

    IEnumerator HandleCpuPlay()
    {
        GameManager.instance.gameState = GameManager.GameState.Waiting;
        EventHandler.WaitCPU?.Invoke();
        yield return new WaitUntil(() => poolCam.doneCameraMove);
        poolCam.doneCameraMove = false;

        if (firstBreak)
        {
            hitPower = MAX_POWER; // 100
            yield return new WaitForSeconds(1.7f);

            // Lerp normalized to 0-100
            float breakT = Mathf.InverseLerp(0f, MAX_POWER, hitPower);
            float breakMove = Mathf.Lerp(-0.1f, -.06f, breakT);

            yield return cue.transform.DOLocalMove(new Vector3(cue.transform.localPosition.x + breakMove, cue.transform.localPosition.y, cue.transform.localPosition.z), .7f).WaitForCompletion();
            StartCoroutine(PlayShot());
            yield break;
        }

        yield return new WaitForSeconds(1f);
        CpuShotOption bestShot = EvaluateAllPossibleShots();

        if (bestShot == null)
        {
            Debug.LogError("No valid CPU shots found.");
            yield break;
        }

        lockedBall = bestShot.ball;
        lockedPocket = bestShot.pocket;
        hitPoint = bestShot.hitPoint;

        Vector3 cueBallPos = GetFlatPosition(cueBall.transform.position);
        Vector3 aimDirection = (GetFlatPosition(hitPoint) - cueBallPos).normalized;

        cue.SetActive(true);
        Quaternion newRotation = Quaternion.LookRotation(aimDirection);
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

        yield return new WaitForSeconds(0.5f);

        // Calculate conditional hitPower (Scale 0 - 100)
        Vector3 ballPos = GetFlatPosition(lockedBall.position);
        Vector3 pocketPos = GetFlatPosition(lockedPocket.position);

        Vector3 cueToGhost = (GetFlatPosition(hitPoint) - cueBallPos).normalized;
        Vector3 ballToPocket = (pocketPos - ballPos).normalized;

        float cueDist = Vector3.Distance(cueBallPos, GetFlatPosition(hitPoint));
        float ballDist = Vector3.Distance(ballPos, pocketPos);
        float cutDot = Mathf.Clamp01(Vector3.Dot(cueToGhost, ballToPocket));

        hitPower = DeduceShotPower(cueDist, ballDist, cutDot);

        // Animate cue pull back mapped strictly to [0, 100]
        float t = Mathf.Clamp01(hitPower / 100f);
        float pullBackOffset = Mathf.Lerp(0f, -0.1f, t);

        Vector3 startPos = cue.transform.localPosition;
        Vector3 targetPullbackPos = new Vector3(startPos.x + pullBackOffset, startPos.y, startPos.z);

        yield return cue.transform.DOLocalMove(targetPullbackPos, 0.4f) .SetEase(Ease.OutQuad).WaitForCompletion(); StartCoroutine(PlayShot());
    }

    const float MIN_POWER = 15f;
    const float MAX_POWER = 100f;

    // Call this to calculate hitPower based on shot conditions
    float DeduceShotPower(float cueDist, float ballDist, float cutDot)
    {
        // Condition 1 & 2: Distance travel requirement
        // Assuming table distance units scale such that combined dist adds base power requirement
        float distancePower = (cueDist * 8f) + (ballDist * 10f);

        // Base minimum force to guarantee the ball reaches the pocket edge
        float requiredBasePower = MIN_POWER + distancePower;

        // Condition 3: Cut Angle Energy Transfer Loss
        // Cut dot is between 0.3 (steep cut) and 1.0 (straight shot)
        // Energy transfer efficiency scales with cutDot. Thin cuts require more force.
        float cutAngleMultiplier = 1f / Mathf.Max(cutDot, 0.25f);

        float calculatedPower = requiredBasePower * cutAngleMultiplier;

        return Mathf.Clamp(calculatedPower, MIN_POWER, MAX_POWER);
    }

    CpuShotOption EvaluateAllPossibleShots()
    {
        List<GameObject> cpuPlayableBalls = (manager.player2.pocketedBalls.Count == 7)
            ? new List<GameObject> { balls[7] }
            : (ballAssigned ? cpuBalls : balls);

        debugShotOptions.Clear();
        List<CpuShotOption> shotOptions = new List<CpuShotOption>();

        Vector3 cueBallPos = GetFlatPosition(cueBall.transform.position);

        foreach (GameObject ball in cpuPlayableBalls)
        {
            if (!ball.activeInHierarchy) continue;

            Vector3 ballPos = GetFlatPosition(ball.transform.position);

            foreach (GameObject pocket in pockets)
            {
                Vector3 pocketPos = GetFlatPosition(pocket.transform.position);

                if (!IsShotPossible(ball, pocket)) continue;

                Vector3 targetHitPoint = HitPoint(ballPos, pocketPos);
                Vector3 cueToGhost = (targetHitPoint - cueBallPos).normalized;
                Vector3 ballToPocket = (pocketPos - ballPos).normalized;

                float alignment = Vector3.Dot(cueToGhost, ballToPocket);
                float cueDist = Vector3.Distance(cueBallPos, targetHitPoint);
                float pocketDist = Vector3.Distance(ballPos, pocketPos);

                float score = (alignment * 150f) - (cueDist * 2f) - (pocketDist * 2.5f);

                CpuShotOption option = new CpuShotOption(ball.transform, pocket.transform, targetHitPoint, cueDist, pocketDist, alignment, alignment, score);
                shotOptions.Add(option);
                debugShotOptions.Add(option);
            }
        }

        if (shotOptions.Count > 0)
        {
            shotOptions.Sort((a, b) => b.score.CompareTo(a.score));
            return shotOptions[0];
        }

        // Best Fallback Evaluation
        CpuShotOption bestFallback = null;
        float highestFallbackScore = float.NegativeInfinity;

        foreach (GameObject ball in cpuPlayableBalls)
        {
            if (!ball.activeInHierarchy) continue;
            Vector3 ballPos = GetFlatPosition(ball.transform.position);

            foreach (GameObject pocket in pockets)
            {
                Vector3 pocketPos = GetFlatPosition(pocket.transform.position);
                Vector3 dir = (pocketPos - ballPos).normalized;
                Vector3 fallbackHitPoint = ballPos - dir * (2f * ballRadius);

                float cueToBallDist = Vector3.Distance(cueBallPos, fallbackHitPoint);
                float ballToPocketDist = Vector3.Distance(ballPos, pocketPos);
                float alignment = Vector3.Dot((ballPos - cueBallPos).normalized, dir);

                float fallbackScore = (alignment * 50f) - cueToBallDist - ballToPocketDist;

                if (fallbackScore > highestFallbackScore)
                {
                    highestFallbackScore = fallbackScore;
                    bestFallback = new CpuShotOption(ball.transform, pocket.transform, fallbackHitPoint, cueToBallDist, ballToPocketDist, alignment, alignment, fallbackScore);
                }
            }
        }

        if (bestFallback != null) debugShotOptions.Add(bestFallback);
        return bestFallback;
    }

    bool IsShotPossible(GameObject ball, GameObject pocket)
    {
        Vector3 ballPos = GetFlatPosition(ball.transform.position);
        Vector3 pocketPos = GetFlatPosition(pocket.transform.position);
        Vector3 cueBallPos = GetFlatPosition(cueBall.transform.position);

        Vector3 ballToPocket = (pocketPos - ballPos).normalized;

        // Check path from Object Ball to Pocket
        if (Physics.SphereCast(ballPos, ballRadius * 0.95f, ballToPocket, out RaycastHit hitPocket, Vector3.Distance(ballPos, pocketPos), ballLayerMask))
        {
            if (hitPocket.collider.gameObject != pocket)
                return false;
        }

        // Check path from Cue Ball to Ghost Ball HitPoint
        Vector3 targetHitPoint = HitPoint(ballPos, pocketPos);
        Vector3 cueToGhost = (targetHitPoint - cueBallPos).normalized;
        float cueToGhostDist = Vector3.Distance(cueBallPos, targetHitPoint);

        // Offset SphereCast start position outside cue ball to prevent self-intersection
        Vector3 castStart = cueBallPos + (cueToGhost * cueBallRadius);

        if (Physics.SphereCast(castStart, cueBallRadius * 0.95f, cueToGhost, out RaycastHit hitCue, cueToGhostDist - cueBallRadius, ballLayerMask))
        {
            if (hitCue.collider.transform != ball.transform)
                return false;
        }

        // Cut angle check: reject impossible angles (> 72 degrees cut)
        float dot = Vector3.Dot(cueToGhost, ballToPocket);
        return dot > 0.3f;
    }

    Vector3 HitPoint(Vector3 ballPos, Vector3 pocketPos)
    {
        Vector3 ballToPocket = (pocketPos - ballPos).normalized;
        return ballPos - ballToPocket * (cueBallRadius + ballRadius);
    }

    Vector3 GetFlatPosition(Vector3 pos)
    {
        return new Vector3(pos.x, 0f, pos.z);
    }
    #endregion

    #region GameMech    

    public IEnumerator PlayShot()
    {
        Debug.Log("h");
        if (hitPower <= 5) yield break;
        Debug.Log("h1");
        GameManager.instance.gameState = GameManager.GameState.Hit;
        spinObj.SetActive(false);
        manager.placeBallButton.SetActive(false);
        Vector3 startPos = cue.transform.localPosition;

        //slingDuration = Mathf.Lerp(0.4f, 0.24f, hitPower / power.maxValue);

        yield return (cue.transform.DOLocalMove(cueOgPos, slingDuration).SetEase(Ease.OutSine)).WaitForCompletion();

        isWaiting = true;
        powerBar.SetActive(false);

        Vector3 direction = cueAnchor.transform.right.normalized;
        cue.SetActive(false);
        GameManager.instance.PlaySound(cueHit);
        Vector3 offset = forceAt.position - spinMark.transform.position;
        Vector3 spinDirection = Vector3.Cross(direction, offset.normalized);

        ballR.AddForceAtPosition(direction * hitPower * powerMultiplier, spinMark.transform.position, ForceMode.Impulse);

        //ballR.AddTorque(spinDirection * hitPower * powerMultiplier, ForceMode.Impulse);
        DisableLine();
        StartCoroutine(ResetCue());
    }

    public void PlaySyncedShot(Vector3 direction, float power, Vector3 spin)
    {
        spinObj.SetActive(false);
        cue.SetActive(false);
        powerBar.SetActive(false);

        GameManager.instance.PlaySound(cueHit);

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
        //power.value = 0;

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

        EventHandler.ResetCam?.Invoke();
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
            GameManager.instance.gameState = GameManager.GameState.Break;
            HandleBreak();
            poolCam.PlaceCamera();
        }
        isFoul = false;
        yield return null;
    }

    public void PlaceCueBall()
    {
        DisableLine();
        cue.SetActive(false);
        spinObj.SetActive(false);
        powerBar.SetActive(false);
        manager.startPanel.SetActive(true);
        manager.placeBallPop.SetActive(true);
        GameManager.instance.gameState = GameManager.GameState.Break;
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
    public float visualInaccuracyOffset = 0.05f;

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
            GameObject hitObject = hit.collider.gameObject;
            int hitLayerValue = 1 << hitObject.layer;

            if ((playBallLayer.value & hitLayerValue) != 0)
            {
                Vector3 hitBallCenter = hit.collider.transform.position;
                float combinedRadius = cueBallRadius + ballRadius;

                float exactContactDistance = SolveContactDistance(currentPosition, currentDirection, hitBallCenter, combinedRadius);
                Vector3 cueBallCenterAtImpact = currentPosition + currentDirection * exactContactDistance;

                linePoints.Add(cueBallCenterAtImpact);

                Vector3 objectBallTravelDir = (hitBallCenter - cueBallCenterAtImpact).normalized;

                if (linePath != null)
                    RenderTargetBallPath(hitBallCenter, objectBallTravelDir);

                if (aimDock != null)
                {
                    aimDock.SetActive(true);
                    aimDock.transform.position = currentPosition + currentDirection * (exactContactDistance - aimWidth);
                }
            }
            else
            {
                linePoints.Add(currentPosition + currentDirection * hit.distance);
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

        float pathLength = 0.2f;
        if (Physics.SphereCast(startPos, ballRadius, direction, out RaycastHit targetHit, pathLength, collisionLayers))
            pathLength = targetHit.distance;

        linePath.positionCount = 2;
        linePath.SetPosition(0, startPos);
        linePath.SetPosition(1, startPos + direction * pathLength);
    }

    float SolveContactDistance(Vector3 origin, Vector3 dir, Vector3 targetCenter, float combinedRadius)
    {
        Vector3 originToTarget = targetCenter - origin;
        float tClosest = Vector3.Dot(originToTarget, dir);
        float perpDistSq = originToTarget.sqrMagnitude - tClosest * tClosest;
        float halfChordSq = combinedRadius * combinedRadius - perpDistSq;
        if (halfChordSq < 0f) halfChordSq = 0f; // guard float error right at grazing contact
        return tClosest - Mathf.Sqrt(halfChordSq);
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

