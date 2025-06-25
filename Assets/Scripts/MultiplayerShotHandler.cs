using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

public class MultiplayerShotHandler : MonoBehaviour
{
    public static MultiplayerShotHandler instance;

    public List<GameObject> balls;

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
        cueBallRadius = cueBall.GetComponent<SphereCollider>().radius * cueBall.transform.localScale.x;
        ballRadius = balls[2].GetComponent<MeshRenderer>().bounds.extents.x;
    }


    void Update()
    {
        HandleTouchInput();
    }

    public void StartGame()
    {
        cueAnchor.transform.SetParent(cueBall.transform);
        cueAnchor.transform.localPosition = Vector3.zero;
        cueAnchor.transform.SetParent(null);
        //if your turn
        if (manager.IsLocalPlayersTurn())
        {
            cue.SetActive(true);
            spinObj.SetActive(true);
            powerBar.SetActive(true);
            if (firstBreak)
            {
                StartCoroutine(LookAtTarget(balls[0]));
            }
        }
        //if end
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
        if (manager.IsLocalPlayersTurn())
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
        if (manager.IsLocalPlayersTurn())
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
        for (int i = 0; i < balls.Count - 1; i++)
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

    public float aimWidth = 0.03f;

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
