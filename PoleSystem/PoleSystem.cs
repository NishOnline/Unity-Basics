using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoleSystem : MonoBehaviour
{
    [Header("Prefabs & Materials")]
    [Tooltip("The electric pole prefab to place in the scene.")]
    public GameObject polePrefab;
    
    [Tooltip("The layer mask used for raycasting to find placement positions on the terrain.")]
    public LayerMask terrainLayer = ~0; // Default to everything

    [Tooltip("Material used for the line renderers representing the wires.")]
    public Material wireMaterial;

    [Header("Wire Customization")]
    [Range(0.01f, 0.2f)]
    public float wireWidth = 0.04f;
    [Range(0f, 5f)]
    public float wireSag = 0.4f;
    [Range(0f, 0.1f)]
    public float wireSagFactor = 0.01f;
    [Range(5, 50)]
    public int wireSegments = 20;

    [Header("Placement Rules")]
    [Tooltip("Min distance from the previous pole required to place a new one.")]
    public float minDistance = 3.0f;
    
    [Tooltip("Align the pole's vertical axis to the terrain surface normal? (Keep unchecked for standard upright poles)")]
    public bool alignToNormal = false;

    [Tooltip("The rotation angle (in degrees) in all 3 axes (X, Y, Z) for newly placed poles.")]
    public Vector3 poleRotation = Vector3.zero;

    [Header("Runtime Hotkeys")]
    public bool enableRuntimeControls = true;
    public KeyCode placementToggleKey = KeyCode.P;
    public KeyCode undoKey = KeyCode.U;
    public KeyCode clearKey = KeyCode.C;

    // Track placed poles. Marked Serialized so they persist in Editor.
    [SerializeField, HideInInspector]
    private List<Pole> placedPoles = new List<Pole>();

    // Placement Mode state
    [SerializeField, HideInInspector]
    private bool isPlacementModeActive = true;

    private GameObject previewInstance;
    private Camera mainCamera;

    private bool isRuntimeDraggingRotation = false;
    private Vector3 runtimeDragStartMousePos;
    private Vector3 runtimeDragStartRotation;

    public List<Pole> PlacedPoles => placedPoles;
    public bool IsPlacementModeActive
    {
        get => isPlacementModeActive;
        set
        {
            isPlacementModeActive = value;
            if (!isPlacementModeActive)
            {
                DestroyPreview();
            }
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        CleanupNullPoles();
    }

    private void Update()
    {
        // Toggle placement mode
        if (enableRuntimeControls && Input.GetKeyDown(placementToggleKey))
        {
            IsPlacementModeActive = !IsPlacementModeActive;
            Debug.Log($"[PoleSystem] Placement Mode: {IsPlacementModeActive}");
        }

        // Handle undo and clear hotkeys at runtime
        if (enableRuntimeControls)
        {
            if (Input.GetKeyDown(undoKey))
            {
                UndoLastPole();
            }
            if (Input.GetKeyDown(clearKey))
            {
                ClearAllPoles();
            }
        }

        // Handle runtime placement interaction
        if (Application.isPlaying && IsPlacementModeActive)
        {
            HandleRuntimePlacement();
            HandleRuntimeRotationInput();
            HandleRuntimeDragRotation();
        }
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    private void OnDestroy()
    {
        DestroyPreview();
    }

    /// <summary>
    /// Processes raycasting and mouse input for pole placement during Play Mode.
    /// </summary>
    private void HandleRuntimePlacement()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayer))
        {
            // Update preview pole position
            UpdatePreviewPosition(hit.point, hit.normal, true);

            // Left-click to place the pole (make sure we aren't clicking UI elements or holding Shift for drag-rotate)
            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                // Check if pointer is over UI before placing
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                PlacePole(hit.point, hit.normal);
            }
        }
        else
        {
            // If the raycast misses the terrain, hide the preview
            UpdatePreviewPosition(Vector3.zero, Vector3.up, false);
        }
    }

    /// <summary>
    /// Processes rotation inputs (Q/E or Shift+Scroll) at runtime.
    /// </summary>
    private void HandleRuntimeRotationInput()
    {
        float speed = 90f * Time.deltaTime;
        float change = 0f;
        
        // Continuous rotation with Q and E
        if (Input.GetKey(KeyCode.Q))
        {
            change -= speed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            change += speed;
        }

        // Discrete rotation using Shift + Scroll Wheel
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                change += scroll * 15f; // 15 degrees per scroll tick
            }
        }

        if (Mathf.Abs(change) > 0.001f)
        {
            if (Input.GetKey(KeyCode.X))
            {
                poleRotation.x = (poleRotation.x + change) % 360f;
                if (poleRotation.x < 0) poleRotation.x += 360f;
            }
            else if (Input.GetKey(KeyCode.Z))
            {
                poleRotation.z = (poleRotation.z + change) % 360f;
                if (poleRotation.z < 0) poleRotation.z += 360f;
            }
            else
            {
                poleRotation.y = (poleRotation.y + change) % 360f;
                if (poleRotation.y < 0) poleRotation.y += 360f;
            }
        }
    }

    /// <summary>
    /// Processes drag rotation at runtime when holding Shift and dragging.
    /// </summary>
    private void HandleRuntimeDragRotation()
    {
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (isShiftHeld)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isRuntimeDraggingRotation = true;
                runtimeDragStartMousePos = Input.mousePosition;
                runtimeDragStartRotation = poleRotation;
            }

            if (isRuntimeDraggingRotation && Input.GetMouseButton(0))
            {
                Vector3 mouseDelta = Input.mousePosition - runtimeDragStartMousePos;
                float scaleFactor = 0.5f;

                bool isC...

<...etc...>
