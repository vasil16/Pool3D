using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PoolMain : MonoBehaviour
{
    public static PoolMain instance;

    public List<GameObject> balls, cpuBalls;
    public List<Texture> ballTexture;

    [SerializeField] Vector4 clampTableBreak,clampTableNormal;
    [SerializeField] private Vector3 lineRendererOffset, cueOgPos, cueOgRot;
    [SerializeField] private Vector2 deltaPosition, deltaPos;
    [SerializeField] private GameObject targetBall, cueBall, powerBar, aimDock, spinObj;
    [SerializeField] private float rotationSpeed = 0.1f, powerMultiplier, maxDistance = 3, secMax = 2, cueBallRadius,ballRadius, ballYpos, dockYpos, extensionLength;
    [SerializeField] private Camera povCam;
    [SerializeField] private Transform forceAt;
    [SerializeField] private LineRenderer lineRenderer, linePlay;
    [SerializeField] private PoolCamBehaviour poolCam;
    [SerializeField] private PowerControl power;
    [SerializeField] private RectTransform spinRect, circleRect, spinIndicator;
    [SerializeField] public TextMeshProUGUI player1Txt, player2Txt;
    [SerializeField] public GameObject[] playerIndicator, pockets;
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

    public float time, duration, hitPower, speedReductVal, dockOffset;

    public bool cpuMode;

    MaterialPropertyBlock _propBlock;


    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        _propBlock = new MaterialPropertyBlock();
        SetBallTextures();
        cueOgPos = cue.transform.localPosition;
        ballR = cueBall.GetComponent<Rigidbody>();        

        cueBallRadius = cueBall.GetComponent<MeshRenderer>().bounds.extents.x;
        ballRadius = balls[2].GetComponent<MeshRenderer>().bounds.extents.x;

        PlayerPrefs.DeleteAll();
    }

    void SetBallTextures()
    {
        for (int i=0; i< balls.Count;i++)
        {
            Renderer ballRenderer = balls[i].GetComponent<Renderer>();

            // Apply a unique color to each ball (for example purposes)
            Texture newTexture = ballTexture[i];
            SetBallTexture(ballRenderer, newTexture);
        }
    }

    void SetBallTexture(Renderer renderer, Texture newTexture)
    {
        _propBlock.Clear();

        // Set the texture property
        _propBlock.SetTexture("_BaseMap", newTexture);

        renderer.SetPropertyBlock(_propBlock);
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
        if (!GameLogic.instance) return;
        if (GameLogic.instance.players[GameLogic.instance.currentPlayer].name == "CPU")
        {
            return;
        }
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
        
        if (GameLogic.instance.players[GameLogic.instance.currentPlayer].name == "CPU")
        {
            cpuMode = true;
            StartCoroutine(HandleCpuPlay());
        }
        else
        {
            spinObj.SetActive(true);
            powerBar.SetActive(true);
            if (firstBreak)
            {
                Debug.Log("fbr");
                StartCoroutine(LookAtTarget(balls[0]));
            }
        }
    }

    #region InputHandle
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
        foreach(GameObject ball in balls)
        {
            ball.GetComponent<Rigidbody>().isKinematic = true;
        }
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

    #endregion

    #region CpuPlay
    public Transform lockedPocket;
    public Vector3 hitPoint;
    bool playableBallFound;

    Vector3 lastPocketDirection; // Store the direction to the pocket
    Vector3 lastHittingDirection; // Store the cue ball to ball direction

    Transform lockedBall;

    IEnumerator HandleCpuPlay()
    {
        poolCam.gameState = PoolCamBehaviour.GameState.Waiting;
        if (firstBreak)
        {
            Debug.Log("fbr");
            hitPower = power.maxValue;
            StartCoroutine(Hit());
        }
        else
        {

            //lockedBall = null;
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
                    //int randomIndex = Random.Range(0, balls.Count-1);
                    int randomIndex = random.Next(0, cpuBalls.Count);
                    lockedBall = cpuBalls[randomIndex].transform;
                }


                if (lockedBall.gameObject.activeInHierarchy && BallPlayable(lockedBall.gameObject))
                {
                    if (lockedBall.GetComponent<BallBehaviour>().ballType == BallBehaviour.BallType.black && GameLogic.instance.player2.pocketedBalls.Count != 7)
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

                Vector3 pocketDirection = (lockedPocket.transform.position - lockedBall.GetComponent<Rigidbody>().worldCenterOfMass).normalized;

                hitPoint = lockedBall.transform.position - (pocketDirection * (ballRadius+cueBallRadius));

                Vector3 cueDirection = (hitPoint - cueBall.transform.position).normalized;
                cueDirection.y = 0; 

                Quaternion newRotation = Quaternion.LookRotation(cueDirection);
                cueAnchor.transform.rotation = Quaternion.Euler(0, newRotation.eulerAngles.y - 90, 0);
            }

            yield return new WaitForSeconds(1f);
            hitPower = 80;

            yield return new WaitForSeconds(0.6f);

            StartCoroutine(Hit());
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

            //if (!IsPocketInFrontOfBallAndCue(ball, activePocket))
            //{
            //    Debug.Log("no pockets for " + ball.name + " for " + activePocket.name);
            //    continue;
            //}

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
    //bool IsPocketInFrontOfBallAndCue(GameObject ball, GameObject activePocket)
    //{
    //    // Get the cue ball, selected ball, and pocket positions


    //    Vector3 cueBallPosition = cueBall.transform.position;
    //    Vector3 ballPosition = ball.transform.position;
    //    Vector3 pocketPosition = activePocket.transform.position;

    //    // Vector from the selected ball to the pocket
    //    Vector3 ballToPocket = pocketPosition - ballPosition;

    //    // Vector from the cue ball to the selected ball
    //    Vector3 cueToBall = ballPosition - cueBallPosition;

    //    // Check if the pocket is in front of the selected ball (based on relative positions along the X and Z axes)
    //    bool isPocketInFront = (pocketPosition.x > ballPosition.x && pocketPosition.z > ballPosition.z);  // Comparing both X and Z axes

    //    // If the pocket is in front of the ball
    //    if (isPocketInFront)
    //    {
    //        // The selected ball should be in front of the cue ball along both X and Z axes, with the distance check
    //        return (ballPosition.x > cueBallPosition.x && ballPosition.z > cueBallPosition.z &&
    //                (ballPosition.x - cueBallPosition.x > 0.7f) && (ballPosition.z - cueBallPosition.z > 0.7f));
    //    }
    //    // If the pocket is behind the selected ball
    //    else
    //    {
    //        // The cue ball should be in front of the selected ball along both X and Z axes, with the distance check
    //        return (cueBallPosition.x > ballPosition.x && cueBallPosition.z > ballPosition.z &&
    //                (cueBallPosition.x - ballPosition.x > 0.7f) && (cueBallPosition.z - ballPosition.z > 0.7f));
    //    }
    //}


    #endregion

    #region GameMech
    float slingDuration;
    public IEnumerator Hit()
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
        
        speedReductVal = 0.6f;
        power.maxValue = 160;
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
        if (GameLogic.instance.players[GameLogic.instance.currentPlayer].name == "CPU")
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
        GameLogic.instance.startPanel.SetActive(true);
        GameLogic.instance.placeBallPop.SetActive(true);
        poolCam.gameState = PoolCamBehaviour.GameState.Break;
        isFoul = false;
        yield return null;
    }

    #endregion

    #region aimlinerender
    int points = 1;
    [SerializeField] RaycastHit hiit, lHit;
    [SerializeField] float collDistance;
    [SerializeField] Vector3 fallPoint, newDir;
    public Vector3 direction, returnVector;

    void RenderTrajectory()
    {
        Vector3 startPosition = cueBall.transform.position;
        Vector3 direction = cueAnchor.transform.right;

        Vector3 normalDir = direction.normalized;

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPosition);

        points = 1;

        //if(Physics.SphereCast(ballR.position, ballWidth,direction, out hiit, 180))
        if (ballR.SweepTest(direction, out hiit, 180))
        {
            points++;
            if (points > maxBounces) return;

            collDistance = hiit.distance;

            fallPoint = cueBall.transform.position + normalDir * collDistance;

            //simBall.transform.position = fallPoint;

            Vector3 fixedPosition = fallPoint - (direction * cueBallRadius);

            if (hiit.collider.CompareTag("playBall"))
            {
                lineRenderer.positionCount = points;
                lineRenderer.SetPosition(points - 1, fixedPosition);

                Vector3 dockPos = new Vector3(fallPoint.x, fixedPosition.y, fallPoint.z);

                aimDock.SetActive(true);
                aimDock.transform.position = dockPos;

                linePlay.positionCount = 2;
                Vector3 endPoint = new Vector3(hiit.collider.transform.position.x, fixedPosition.y, hiit.collider.transform.position.z);

                Vector3 newDir = (hiit.transform.GetComponent<Rigidbody>().worldCenterOfMass - hiit.point).normalized;

                linePlay.SetPosition(0, endPoint);
                if (Physics.Raycast(hiit.transform.position, newDir, out lHit, 0.2f))
                {
                    linePlay.SetPosition(1, lHit.point);
                    return;
                }
                Vector3 newPoint = endPoint + newDir * extensionLength;
                linePlay.SetPosition(1, new Vector3(newPoint.x, fixedPosition.y,newPoint.z));            
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


    public bool BallStopped()
    {
        foreach (GameObject ball in balls)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ball.activeInHierarchy && ballRb.velocity != Vector3.zero || ballRb.angularVelocity != Vector3.zero)
            {
                return false;
            }
        }
        return true;
    }
}
