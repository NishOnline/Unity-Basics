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

    [Range(0f, 360f)]
    [Tooltip("The rotation angle (in degrees) around the vertical axis for newly placed poles.")]
    public float poleRotationY = 0f;

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

            // Left-click to place the pole (make sure we aren't clicking UI elements)
            if (Input.GetMouseButtonDown(0))
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
        // Continuous rotation with Q and E
        if (Input.GetKey(KeyCode.Q))
        {
            poleRotationY -= 90f * Time.deltaTime;
            poleRotationY = (poleRotationY % 360f + 360f) % 360f;
        }
        if (Input.GetKey(KeyCode.E))
        {
            poleRotationY += 90f * Time.deltaTime;
            poleRotationY = (poleRotationY % 360f + 360f) % 360f;
        }

        // Discrete rotation using Shift + Scroll Wheel
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                poleRotationY += scroll * 15f; // 15 degrees per scroll tick
                poleRotationY = (poleRotationY % 360f + 360f) % 360f;
            }
        }
    }

    /// <summary>
    /// Places a pole at the specified position and normal, setting up the connectors and wires.
    /// </summary>
    public Pole PlacePole(Vector3 position, Vector3 normal)
    {
        if (polePrefab == null)
        {
            Debug.LogError("[PoleSystem] Cannot place pole: Prefab is not assigned in the inspector!");
            return null;
        }

        CleanupNullPoles();

        // Enforce minimum distance constraint
        if (placedPoles.Count > 0)
        {
            float dist = Vector3.Distance(placedPoles[placedPoles.Count - 1].transform.position, position);
            if (dist < minDistance)
            {
                Debug.LogWarning($"[PoleSystem] Cannot place pole. Distance to previous pole is {dist:F2}m (Minimum allowed: {minDistance}m)");
                return null;
            }
        }

        // Instantiate the pole
        Quaternion baseRotation = alignToNormal ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
        Quaternion rotation = baseRotation * Quaternion.Euler(0f, poleRotationY, 0f);
        GameObject poleObj = Instantiate(polePrefab, position, rotation);
        poleObj.name = $"Pole_{placedPoles.Count + 1}";

        // Ensure the Pole component is present
        Pole newPole = poleObj.GetComponent<Pole>();
        if (newPole == null)
        {
            newPole = poleObj.AddComponent<Pole>();
        }
        // Force auto-detection of connectors if needed
        newPole.AutoDetectConnectors();

        // Connect wires from previous pole if it exists
        if (placedPoles.Count > 0)
        {
            Pole prevPole = placedPoles[placedPoles.Count - 1];
            CreateWiresBetweenPoles(prevPole, newPole);
        }

        placedPoles.Add(newPole);
        Debug.Log($"[PoleSystem] Successfully placed pole '{poleObj.name}' at {position}", poleObj);

#if UNITY_EDITOR
        // Support Undo in Editor Mode
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(poleObj, "Place Electric Pole");
        }
#endif

        return newPole;
    }

    /// <summary>
    /// Generates wire GameObjects between corresponding connectors of two poles.
    /// </summary>
    private void CreateWiresBetweenPoles(Pole fromPole, Pole toPole)
    {
        if (fromPole == null || toPole == null) return;

        // Draw wires between all available connector channels.
        // We use the maximum connector count to ensure everything gets wired,
        // and Pole.GetConnector() handles wrapping/clamping safely.
        int wireCount = Mathf.Max(fromPole.connectors.Length, toPole.connectors.Length);

        for (int i = 0; i < wireCount; i++)
        {
            Transform start = fromPole.GetConnector(i);
            Transform end = toPole.GetConnector(i);

            // Create wire GameObject under the newly placed pole for automatic cleanup on deletion
            GameObject wireObj = new GameObject($"Wire_{i + 1}");
            wireObj.transform.SetParent(toPole.transform);
            wireObj.transform.position = start.position;

            Wire wire = wireObj.AddComponent<Wire>();
            wire.SetParameters(
                start, 
                end, 
                wireMaterial, 
                wireWidth, 
                wireSag, 
                wireSagFactor, 
                wireSegments
            );
        }
    }

    /// <summary>
    /// Updates the position and visibility of the placement preview model.
    /// </summary>
    public void UpdatePreviewPosition(Vector3 position, Vector3 normal, bool visible)
    {
        if (polePrefab == null) return;

        if (!visible)
        {
            if (previewInstance != null)
            {
                previewInstance.SetActive(false);
            }
            return;
        }

        if (previewInstance == null)
        {
            CreatePreviewInstance();
        }

        if (previewInstance != null)
        {
            previewInstance.SetActive(true);
            previewInstance.transform.position = position;
            Quaternion baseRotation = alignToNormal ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
            previewInstance.transform.rotation = baseRotation * Quaternion.Euler(0f, poleRotationY, 0f);
        }
    }

    /// <summary>
    /// Spawns a colliderless and scriptless preview version of the pole prefab.
    /// </summary>
    private void CreatePreviewInstance()
    {
        if (polePrefab == null) return;

        // Spawn prefab
        previewInstance = Instantiate(polePrefab);
        previewInstance.name = "[Preview] Electric Pole";
        
        // Disable editing/saving on the preview object so it doesn't pollute the scene file
        previewInstance.hideFlags = HideFlags.DontSave;

        // Disable all colliders to prevent it from blocking raycasts
        Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Disable any scripts (Pole, etc.) to prevent unwanted execution
        MonoBehaviour[] scripts = previewInstance.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            // Make sure we don't disable UI or fundamental render scripts, just custom ones
            if (script is Pole)
            {
                script.enabled = false;
            }
        }
    }

    /// <summary>
    /// Destroys the preview pole instance.
    /// </summary>
    public void DestroyPreview()
    {
        if (previewInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(previewInstance);
            }
            else
            {
                DestroyImmediate(previewInstance);
            }
            previewInstance = null;
        }
    }

    /// <summary>
    /// Deletes the last placed pole and its wires.
    /// </summary>
    public void UndoLastPole()
    {
        CleanupNullPoles();

        if (placedPoles.Count == 0)
        {
            Debug.Log("[PoleSystem] Nothing to undo: No poles have been placed yet.");
            return;
        }

        int lastIndex = placedPoles.Count - 1;
        Pole lastPole = placedPoles[lastIndex];
        placedPoles.RemoveAt(lastIndex);

        if (lastPole != null)
        {
            Debug.Log($"[PoleSystem] Undoing placement of '{lastPole.name}'");
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.DestroyObjectImmediate(lastPole.gameObject);
                return;
            }
#endif
            Destroy(lastPole.gameObject);
        }
    }

    /// <summary>
    /// Clears all placed poles from the scene.
    /// </summary>
    public void ClearAllPoles()
    {
        CleanupNullPoles();

        if (placedPoles.Count == 0)
        {
            Debug.Log("[PoleSystem] No poles to clear.");
            return;
        }

        Debug.Log($"[PoleSystem] Clearing {placedPoles.Count} poles.");

        foreach (Pole pole in placedPoles)
        {
            if (pole != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.Undo.DestroyObjectImmediate(pole.gameObject);
                    continue;
                }
#endif
                Destroy(pole.gameObject);
            }
        }

        placedPoles.Clear();
    }

    /// <summary>
    /// Cleans up any null entries in the placed poles list.
    /// </summary>
    public void CleanupNullPoles()
    {
        placedPoles.RemoveAll(p => p == null);
    }

    private void OnValidate()
    {
        CleanupNullPoles();
    }
}
