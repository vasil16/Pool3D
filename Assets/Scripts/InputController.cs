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

    [SerializeField] PoolCamBehaviour camB;

    private Vector2 touchStartPos;
    private float touchStartTime;

    [Header("Swipe Settings")]
    public float swipeMinDistance = 100f;   
    public float swipeMinSpeed = 500f;      
    public float swipeDurationThreshold =.5f;

    [Header("Hold Settings")]
    public float holdThreshold = 0.3f; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = new GameInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        inputActions.Gameplay.Tap.performed += HandleTap;

        inputActions.Gameplay.Drag.performed += HandleDrag;

        inputActions.Gameplay.Touch.performed += HandleTouch;

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


    private void HandleTap(InputAction.CallbackContext ctx)
    {
        if(camB.gameState == PoolCamBehaviour.GameState.Aim)
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

    private void HandleDrag(InputAction.CallbackContext ctx)
    {
        delta = ctx.ReadValue<Vector2>();
        //Debug.Log($"[InputController] Drag delta: {delta}");

    }

    private void HandleTouch(InputAction.CallbackContext ctx)
    {
        if (camB.gameState == PoolCamBehaviour.GameState.Waiting) return;
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
                //Debug.Log("over drag");
                EventHandler.RotateCameraBreak?.Invoke(delta);
            }
        }
        else
        {
            if(camB.gameState==PoolCamBehaviour.GameState.Aim || camB.gameState == PoolCamBehaviour.GameState.Hit)
            {
                EventHandler.DragAim?.Invoke(delta);
            }
            else if (camB.gameState == PoolCamBehaviour.GameState.Break)
            {
                EventHandler.MoveCueBall?.Invoke(delta);
            }
        }
    }

    private void HandleTouchStart(InputAction.CallbackContext ctx)
    {
        if (camB.gameState == PoolCamBehaviour.GameState.Waiting) return;
        touchStartPos = inputActions.Gameplay.Touch.ReadValue<Vector2>();
        touchStartTime = Time.time;

        // ---------------------------
        // HOLD LOGIC (start)
        //Debug.Log($"[InputController] Hold started at: {touchStartPos}");
        // ---------------------------
    }

    private void HandleTouchEnd(InputAction.CallbackContext ctx)
    {
        if (camB.gameState == PoolCamBehaviour.GameState.Waiting) return;
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
            if (camB.gameState==PoolCamBehaviour.GameState.Break)
            {
                EventHandler.SwipeCueBall?.Invoke(direction);
            }
            else if (camB.gameState == PoolCamBehaviour.GameState.Aim || camB.gameState == PoolCamBehaviour.GameState.Hit)
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
