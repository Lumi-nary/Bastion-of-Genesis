using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{

    // Components
    private CinemachineCamera m_CMCamera;
    private InputSystem_Actions playerControls;
    private InputAction moveAction;
    private InputAction zoomAction;
    private InputAction dragAction;


    [Header("Camera Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float dragSpeed = 0.7f;
    [SerializeField] private bool useUnscaledTimeforDrag = true;

    [Header("Edge Panning Settings")]
    [SerializeField] private bool enableEdgePanning = true;
    [SerializeField] private int screenBorderThickness = 50;

    [Header("Zoom Settings")]
    [SerializeField][Range(1.01f, 2f)] private float zoomMultiplier = 1.2f;
    [SerializeField] private float minOrthographicSize = 2f;
    [SerializeField] private float maxOrthographicSize = 15f;
    [SerializeField] private float zoomSmoothing = 5f;

    private PlacementSystem placementSystem;

    // State Variables
    private bool isDragging;
    private Vector2 lastMousePosition;
    private Vector3 moveDirection;
    private float targetOrthographicSize;

    private void Awake()
    {
        // Cache the Cinemachine component
        m_CMCamera = GetComponentInChildren<CinemachineCamera>();
        if (m_CMCamera == null)
        {
            Debug.LogError("CinemachineCamera component not found on this GameObject.");
            // Disable the script if the camera is not found to prevent further errors
            enabled = false;
            return;
        }

        // Initialize the target size for smooth zooming
        targetOrthographicSize = m_CMCamera.Lens.OrthographicSize;

        // Setup Input Actions
        playerControls = new InputSystem_Actions();
        moveAction = playerControls.Camera.Movement;
        zoomAction = playerControls.Camera.Zoom;
        dragAction = playerControls.Camera.Drag;

        placementSystem = PlacementSystem.Instance;
    }

    private void OnEnable()
    {
        playerControls.Camera.Enable();

        // Subscribe Drag Action
        dragAction.started += StartDrag;
        dragAction.canceled += EndDrag;
        zoomAction.performed += HandleZoomInput;
    }

    private void OnDisable()
    {
        dragAction.started -= StartDrag;
        dragAction.canceled -= EndDrag;
        zoomAction.performed -= HandleZoomInput;

        playerControls.Camera.Disable();
    }

    void Update()
    {
        HandleMovement();
        SmoothZoom();
    }

    private void HandleMovement()
    {
        // Read mouse position once per frame
        Vector2 currentMousePosition = Mouse.current.position.ReadValue();

        // Priority 1: Mouse Dragging
        if (isDragging)
        {
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            moveDirection = new Vector3(-mouseDelta.x, -mouseDelta.y, 0);

            // Apply drag movement. Using unscaled time makes it feel more responsive during frame drops.
            float deltaTime = useUnscaledTimeforDrag ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.position += moveDirection * dragSpeed * m_CMCamera.Lens.OrthographicSize * deltaTime;

            lastMousePosition = currentMousePosition;
            return; // Exit early since dragging takes priority
        }

        // Priority 2: Keyboard Input
        Vector2 keyboardInput = moveAction.ReadValue<Vector2>();
        moveDirection = new Vector3(keyboardInput.x, keyboardInput.y, 0);

        // Priority 3: Edge Panning (if no keyboard input)
        if (enableEdgePanning && keyboardInput == Vector2.zero)
        {
            HandleEdgePanning(currentMousePosition);
        }

        // Apply final movement
        transform.position += moveDirection.normalized * moveSpeed * m_CMCamera.Lens.OrthographicSize * Time.deltaTime;
    }

    private void HandleEdgePanning(Vector2 mousePosition)
    {
        Vector2 edgeMoveDirection = Vector2.zero;

        if (mousePosition.y >= Screen.height - screenBorderThickness) edgeMoveDirection.y = 1;
        else if (mousePosition.y <= screenBorderThickness) edgeMoveDirection.y = -1;

        if (mousePosition.x >= Screen.width - screenBorderThickness) edgeMoveDirection.x = 1;
        else if (mousePosition.x <= screenBorderThickness) edgeMoveDirection.x = -1;

        // Update move direction if edge panning is active
        moveDirection = new Vector3(edgeMoveDirection.x, edgeMoveDirection.y, 0);
    }

    private void SmoothZoom()
    {
        // Lerp towards the target size for a smooth visual effect
        m_CMCamera.Lens.OrthographicSize = Mathf.Lerp(
            m_CMCamera.Lens.OrthographicSize,
            targetOrthographicSize,
            Time.deltaTime * zoomSmoothing
        );
    }

    private void HandleZoomInput(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<Vector2>().y;

        if (scrollValue > 0) // Scrolled Up -> Zoom In
        {
            targetOrthographicSize /= zoomMultiplier;
        }
        else if (scrollValue < 0) // Scrolled Down -> Zoom Out
        {
            targetOrthographicSize *= zoomMultiplier;
        }

        // Clamp the target size
        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
    }

    private void StartDrag(InputAction.CallbackContext context)
    {
        isDragging = true;
        // Read mouse position at the exact moment dragging starts
        lastMousePosition = Mouse.current.position.ReadValue();
    }

    private void EndDrag(InputAction.CallbackContext context)
    {
        isDragging = false;
    }
}
