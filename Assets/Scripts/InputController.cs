using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController Instance;

    private GameInputActions inputActions;

    private Ray pRay;
    private RaycastHit bHit;
    [SerializeField] LayerMask closeMask;

    [SerializeField] RectTransform dragRotateRect, circleRect;

    [SerializeField] CameraController camB;

    // Swipe / Hold tracking
    private Vector2 touchStartPos;
    private float touchStartTime;

    [Header("Swipe Settings")]
    public float swipeMinDistance = 100f;   // in pixels
    public float swipeMinSpeed = 500f;      // px/sec
    public float swipeDurationThreshold =.5f;

    [Header("Hold Settings")]
    public float holdThreshold = 0.3f;      // seconds

    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = new GameInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        // Tap
        inputActions.Gameplay.Tap.performed += HandleTap;

        // Drag
        inputActions.Gameplay.Drag.performed += HandleDrag;

        // Touch position
        inputActions.Gameplay.Touch.performed += HandleTouch;

        // Press (finger down/up)
        inputActions.Gameplay.Press.started += HandleTouchStart;
        inputActions.Gameplay.Press.canceled += HandleTouchEnd;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Tap.performed -= HandleTap;
        inputActions.Gameplay.Drag.performed -= HandleDrag;
        inputActions.Gameplay.Touch.performed -= HandleTouch;
        inputActions.Gameplay.Press.started -= HandleTouchStart;
        inputActions.Gameplay.Press.canceled -= HandleTouchEnd;

        inputActions.Gameplay.Disable();
    }

    // 🔹 Tap
    private void HandleTap(InputAction.CallbackContext ctx)
    {
        if(GameManager.instance && GameManager.instance.gameState == GameManager.GameState.Aim)
        {
            Vector2 pos = inputActions.Gameplay.Touch.ReadValue<Vector2>();
            //Debug.Log($"[InputController] Tap at: {pos}");
            pRay = Camera.main.ScreenPointToRay(pos);
            if (Physics.Raycast(pRay, out bHit, closeMask) && bHit.collider.gameObject.CompareTag("playBall"))
            {
                EventHandler.PlayableBallTapped?.Invoke(bHit.collider.gameObject);
            }
        }
    }

    Vector2 delta;

    // 🔹 Drag
    private void HandleDrag(InputAction.CallbackContext ctx)
    {
        delta = ctx.ReadValue<Vector2>();
        //Debug.Log($"[InputController] Drag delta: {delta}");
    }

    // 🔹 Touch (position)
    private void HandleTouch(InputAction.CallbackContext ctx)
    {
        Vector2 pos = ctx.ReadValue<Vector2>();
        //Debug.Log($"[InputController] Touch at: {pos}");
        if(Utils.IsPointerOverUIObject(pos))
        {
            if(TappedOver(circleRect,pos))
            {
                //Debug.Log("over spin");
                EventHandler.AddSpin?.Invoke(pos);
            }
            else if (TappedOver(dragRotateRect, pos))
            {
                //Debug.Log("over spin");
                EventHandler.RotateCameraBreak?.Invoke(delta);
            }
        }
        else
        {
            if(GameManager.instance.gameState== GameManager.GameState.Aim || GameManager.instance.gameState == GameManager.GameState.Hit)
            {
                EventHandler.DragAim?.Invoke(delta);
            }
            else if (GameManager.instance.gameState == GameManager.GameState.Break)
            {
                EventHandler.MoveCueBall?.Invoke(delta);
            }
        }
    }

    // 🔹 Press Start (finger down)
    private void HandleTouchStart(InputAction.CallbackContext ctx)
    {
        touchStartPos = inputActions.Gameplay.Touch.ReadValue<Vector2>();
        touchStartTime = Time.time;

        // ---------------------------
        // HOLD LOGIC (start)
        //Debug.Log($"[InputController] Hold started at: {touchStartPos}");
        // ---------------------------
    }

    // 🔹 Press End (finger up)
    private void HandleTouchEnd(InputAction.CallbackContext ctx)
    {
        Vector2 touchEndPos = inputActions.Gameplay.Touch.ReadValue<Vector2>();
        float duration = Time.time - touchStartTime;
        Vector2 delta = touchEndPos - touchStartPos;

        // ---------------------------
        // SWIPE LOGIC
        float speed = delta.magnitude / duration;
        if (delta.magnitude > swipeMinDistance && speed > swipeMinSpeed && duration<swipeDurationThreshold)
        {
            Vector2 direction = delta.normalized;
            //Debug.Log($"[InputController] Swipe detected! Direction: {direction}, Speed: {speed}");
            if (Utils.IsPointerOverUIObject(touchStartPos)) return;
            if (GameManager.instance.gameState== GameManager.GameState.Break)
            {
                EventHandler.SwipeCueBall?.Invoke(direction);
            }
            else if (GameManager.instance.gameState == GameManager.GameState.Aim || GameManager.instance.gameState == GameManager.GameState.Hit)
            {
                EventHandler.SwipeAim?.Invoke(direction);
            }
        }
        // ---------------------------

        // ---------------------------
        // HOLD LOGIC (end)
        if (duration >= holdThreshold)
        {
            //Debug.Log($"[InputController] Hold ended at: {touchEndPos}, Duration: {duration:F2}s");
        }
        // ---------------------------
    }

    bool TappedOver(RectTransform transform, Vector2 position)
    {
        if (Utils.IsPointerOverUIObject(position) && RectTransformUtility.RectangleContainsScreenPoint(transform, position))
            return true;
        return false;
    }
}
