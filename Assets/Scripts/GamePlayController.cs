using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    [SerializeField] private LineRenderer lineRenderer, linePlay;
    [SerializeField] private PoolCamBehaviour poolCam;
    [SerializeField] private PowerControl power;
    [SerializeField] private RectTransform spinRect, circleRect, spinIndicator;
    [SerializeField] public TextMeshProUGUI player1Txt, player2Txt;
    [SerializeField] public GameObject[] playerIndicator, pockets;
    [SerializeField] LayerMask closeMask;
    [SerializeField] public AudioSource gameAudio;
    [SerializeField] public AudioClip cueHit, rolling;

    public GameObject cue, spinMark, cueAnchor;
    public bool isBreak = true;
    public bool dragPower, spun, hasSpin, isWaiting, pocketed, firstPot, updown, isFoul, firstBreak, gameOver, touchDisabled;
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
        //cueBallRadius = cueBall.GetComponent<MeshRenderer>().bounds.extents.x;
        cueBallRadius = cueBall.GetComponent<SphereCollider>().radius * cueBall.transform.localScale.x;
        ballRadius = balls[2].GetComponent<MeshRenderer>().bounds.extents.x;
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
            if (touch.phase == TouchPhase.Ended && dragPower)
            {
                StartCoroutine(PlayShot());
            }

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
            Vector3 newPos = touch.deltaPosition * 0.1f * Time.deltaTime;
            cueBall.transform.localPosition += new Vector3(newPos.y, 0, newPos.x * -1);

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

    IEnumerator HandleCpuPlay()
    {
        poolCam.gameState = PoolCamBehaviour.GameState.Waiting;
        yield return new WaitUntil(() => poolCam.doneCameraMove);
        poolCam.doneCameraMove = false;
        if (firstBreak)
        {
            Debug.Log("fbr");
            hitPower = power.maxValue;
            yield return new WaitForSeconds(1.7f);
            StartCoroutine(PlayShot());
        }
        else
        {
            yield return new WaitForSeconds(1f);
            do
            {
                Debug.Log("selecting ball");

                if (firstPot)
                {
                    System.Random random = new System.Random();
                    //int randomIndex = Random.Range(0, balls.Count-1);
                    int randomIndex = random.Next(0, balls.Count - 1);
                    lockedBall = balls[randomIndex].transform;
                }
                else
                {
                    //int randomIndex = Random.Range(0, cpuBalls.Count);
                    System.Random random = new System.Random();
                    int randomIndex = random.Next(0, cpuBalls.Count);
                    lockedBall = cpuBalls[randomIndex].transform;
                }

                if (lockedBall.gameObject.activeInHierarchy && BallPlayable(lockedBall.gameObject))
                {
                    if (lockedBall.GetComponent<BallBehaviour>().ballType == BallBehaviour.BallType.black && manager.player2.pocketedBalls.Count != 7)
                    {
                        playableBallFound = false;
                    }
                    else
                    {
                        playableBallFound = true;
                        Debug.Log("Playable ball found: " + lockedBall.gameObject.name);
                    }
                }
                yield return null;
            }
            while (!playableBallFound);

            if (lockedPocket == null)
            {
                Debug.Log("No pocket selected");
                direction = lockedBall.transform.position - cueBall.transform.position;
            }

            else
            {
                Debug.Log("Chosen ball: " + lockedBall.gameObject);
                Debug.Log("Chosen pocket: " + lockedPocket.gameObject);

                Vector3 pocketDirection = (lockedPocket.transform.position - lockedBall.transform.position).normalized;

                hitPoint = lockedBall.transform.position - (pocketDirection * (ballRadius + cueBallRadius));

                Vector3 cueDirection = (hitPoint - cueBall.transform.position).normalized;
                cueDirection.y = 0;
                cue.SetActive(true);
                Quaternion newRotation = Quaternion.LookRotation(cueDirection);
                newRotation = Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0);

                float rotationDuration = 0.5f; // Adjust for smoothness
                float elapsedTime = 0;

                Quaternion startRotation = cueAnchor.transform.rotation;

                while (elapsedTime < rotationDuration)
                {
                    elapsedTime += Time.deltaTime;
                    cueAnchor.transform.rotation = Quaternion.Slerp(startRotation, newRotation, elapsedTime / rotationDuration);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(1f);
            hitPower = 80;

            yield return new WaitForSeconds(0.6f);
            StartCoroutine(PlayShot());
        }
        yield return null;
    }

    Vector3 HitPoint(Vector3 ballPos, Vector3 pocketPos)
    {
        Vector3 pocketDirection = (pocketPos - ballPos).normalized;

        Vector3 newPoint = ballPos - (pocketDirection * (ballRadius + cueBallRadius));
        return newPoint;
    }


    public bool pos = false;

    bool BallPlayable(GameObject ball = null)
    {
        pos = false;
        for (int i = 0; i < 6; i++)
        {
            GameObject activePocket = pockets[i];

            Vector3 pocketDirection = (activePocket.transform.position - ball.transform.position).normalized;
            RaycastHit[] hitResultsPocket = ball.GetComponent<Rigidbody>().SweepTestAll(pocketDirection);

            bool isPocketBlocked = false;
            foreach (RaycastHit hit in hitResultsPocket)
            {
                if (hit.collider.CompareTag("playBall") || hit.collider.CompareTag("cushion"))
                {
                    Debug.Log("Pocket blocked for " + ball.name + " for " + activePocket.name + " by " + hit.transform.gameObject.name);
                    isPocketBlocked = true;
                    break;
                }
            }

            if (isPocketBlocked)
            {
                continue;
            }

            Vector3 hittingDirection = (HitPoint(ball.transform.position, activePocket.transform.position) - cueBall.transform.position).normalized;
            RaycastHit[] hitResultsCue = Physics.SphereCastAll(cueBall.transform.position, cueBallRadius, hittingDirection);

            bool isCueBlocked = false;
            foreach (RaycastHit hit in hitResultsCue)
            {
                if ((hit.collider.CompareTag("playBall") && hit.transform.gameObject.name != ball.gameObject.name))
                {
                    Debug.Log("cue ball blocked for " + ball.name + " for " + activePocket.name + " by " + hit.transform.gameObject.name);
                    isCueBlocked = true;
                    break;
                }
            }

            if (isCueBlocked)
            {
                continue;
            }

            if (!IsPocketInFrontOfBallAndCue(ball, activePocket))
            {
                Debug.Log("no pockets for " + ball.name + " for " + activePocket.name);
                continue;
            }

            lastPocketDirection = activePocket.transform.position;
            lastHittingDirection = ball.transform.position;
            lockedPocket = activePocket.transform;
            pos = true;

            Debug.Log("Pocket chosen for " + ball.name + ": " + activePocket.name);
            return true;
        }
        return pos;
    }


    bool IsPocketInFrontOfBallAndCue(GameObject ball, GameObject activePocket)
    {
        Vector3 ballToPocket;
        Vector3 cueToBall;

        ballToPocket = (activePocket.transform.position - ball.transform.position).normalized;
        cueToBall = (ball.transform.position - cueBall.transform.position).normalized;

        bool isPocketInFrontOfBall = Vector3.Dot(ballToPocket, (ball.transform.position - cueBall.transform.position).normalized) > 0;

        // If the pocket is in front of the ball, check if the ball is in front of the cue ball
        if (isPocketInFrontOfBall)
        {
            Debug.Log("dir first");
            return Vector3.Dot(cueToBall, (activePocket.transform.position - ball.transform.position).normalized) > 0;
        }
        else
        {
            Debug.Log("dir second");
            // If the pocket is behind the ball, check if the cue ball is in front of the selected ball
            return Vector3.Dot(cueToBall, ballToPocket) > 0;
        }
    }

    #endregion

    #region GameMech
    float slingDuration;
    public IEnumerator PlayShot()
    {
        if (hitPower <= 5) yield break;

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

        Vector3 direction = cueAnchor.transform.right.normalized;
        cue.SetActive(false);
        gameAudio.PlayOneShot(cueHit);
        //forceAt.position = spinMark.transform.position;
        Vector3 offset = forceAt.position - spinMark.transform.position;
        Vector3 spinDirection = Vector3.Cross(direction, offset.normalized);
        ballR.AddForceAtPosition(direction * hitPower * .008f, spinMark.transform.position, ForceMode.Impulse);
        ballR.AddTorque(spinDirection * hitPower * 0.008f, ForceMode.Impulse);
        StartCoroutine(ResetCue());
    }


    IEnumerator ResetCue()
    {
        dragPower = false;
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
            manager.currentPlayer = manager.GetOpponent(manager.currentPlayer);
        }

        playerIndicator[(int)manager.currentPlayer].SetActive(true);
        playerIndicator[(int)manager.GetOpponent(manager.currentPlayer)].SetActive(false);

        if (isFoul)
        {
            pocketed = false;
            StartCoroutine(FoulReset());            
            yield break;
        }

        pocketed = false;
        isBreak = false;
        firstBreak = false;
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

    #endregion

    #region aimlinerender
    [SerializeField] float collDistance;
    [SerializeField] Vector3 fallPoint, newDir;
    public Vector3 direction, returnVector;


    private List<Vector3> linePoints = new List<Vector3>();
    public float maxStepDistance = 10f;      // Max distance for each raycast step
    public float targetExtensionLength = 0.2f; // How far to draw the target ball line

    [Header("Layer Masks")] // Optional header
    [Tooltip("Layers that the trajectory prediction should interact with (Balls, Cushions)")]
    public LayerMask collisionLayers; // Assign in Inspector (MUST INCLUDE BALLS AND CUSHIONS)
    [Tooltip("The specific layer assigned to cushions/table edges")]
    public LayerMask cushionLayer;    // Assign in Inspector (Cushions ONLY)
    [Tooltip("The specific layer assigned to playable balls (excluding cue ball initially)")]
    public LayerMask playBallLayer;   // Assign in Inspector (Playable Balls ONLY)

    void RenderTrajectory()
    {
        // 1. Reset lines and dock at the very beginning
        linePoints.Clear();
        if (linePlay != null) linePlay.positionCount = 0;
        if (aimDock != null) aimDock.SetActive(false);

        // Ensure essential components are assigned
        if (cueBall == null || cueAnchor == null || lineRenderer == null)
        {
            // Debug.LogError("RenderTrajectory: Essential components are not assigned.");
            return;
        }

        // 2. Get initial position and direction
        Vector3 currentPosition = cueBall.transform.position;
        Vector3 currentDirection = cueAnchor.transform.right.normalized;

        linePoints.Add(currentPosition); // Add the start point of the cue ball's path

        // 3. Perform a single SphereCast for the cue ball's path
        if (Physics.SphereCast(currentPosition, cueBallRadius, currentDirection, out RaycastHit hit, maxStepDistance, collisionLayers))
        {
            // Cue ball hit something. Its line stops at the contact point.
            Vector3 cueBallSurfaceContactPoint = currentPosition + currentDirection * hit.distance;
            linePoints.Add(cueBallSurfaceContactPoint);

            GameObject hitObject = hit.collider.gameObject;
            int hitLayerValue = 1 << hitObject.layer;

            if ((playBallLayer.value & hitLayerValue) != 0)
            {
                Vector3 hitBallCenter = hit.collider.transform.position;
                Vector3 cueBallCenterAtImpact = currentPosition + currentDirection * hit.distance;
                Vector3 collisionNormal = (hitBallCenter - cueBallCenterAtImpact).normalized;

                Vector3 velocityAlongNormal = Vector3.Project(currentDirection, collisionNormal);
                Vector3 newTargetBallDirection = velocityAlongNormal.normalized;

                if (linePlay != null) // Render target ball path if linePlay is assigned
                {
                    RenderTargetBallPath(hitBallCenter, newTargetBallDirection);
                }

                if (aimDock != null) // Activate and position aimDock if assigned
                {
                    aimDock.SetActive(true);
                    Vector3 targetBallSurfacePoint = hitBallCenter - collisionNormal * ballRadius;
                    aimDock.transform.position = targetBallSurfacePoint;
                }
            }
        }
        else 
        {
            linePoints.Add(currentPosition + currentDirection * maxStepDistance);
        }

        lineRenderer.positionCount = linePoints.Count;
        lineRenderer.SetPositions(linePoints.ToArray());
    }

    void RenderTargetBallPath(Vector3 startPos, Vector3 direction)
    {
        if (linePlay == null) return; // Safety check

        linePlay.positionCount = 2;
        linePlay.SetPosition(0, startPos);
        if (Physics.SphereCast(startPos, ballRadius, direction, out RaycastHit targetHit, targetExtensionLength, collisionLayers))
        {
            // Target ball hits something, line ends at contact point
            Vector3 targetBallSurfaceContact = startPos + direction * targetHit.distance;
            linePlay.SetPosition(1, targetBallSurfaceContact);
        }
        else
        {
            // Target ball doesn't hit anything, extend line by targetExtensionLength
            linePlay.SetPosition(1, startPos + direction * targetExtensionLength);
        }
    }

    #endregion


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
}
