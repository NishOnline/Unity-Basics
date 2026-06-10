using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomPlacer : MonoBehaviour
{
    [Header("Prefabs to Place")]
    [Tooltip("Select 3D models like pamphlets, fruit peels, banana peels, wrappers, etc.")]
    public GameObject[] prefabsToPlace;

    [Header("Placement Settings")]
    [Range(0.1f, 50f)]
    public float radius = 2.0f;
    [Range(1, 100)]
    public int amountToPlace = 10;

    [Header("Scale & Rotation")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public bool alignToNormal = true;
    [Tooltip("If checked, randomizes rotation around the X axis.")]
    public bool randomXRotation = false;
    [Tooltip("If checked, randomizes rotation around the Y axis.")]
    public bool randomYRotation = true;
    [Tooltip("If checked, randomizes rotation around the Z axis.")]
    public bool randomZRotation = false;
    [Tooltip("The specific rotation values used for X, Y, and Z if they are not randomized.")]
    public Vector3 fixedRotation = Vector3.zero;

    [Header("Raycast Settings")]
    public LayerMask placementMask = ~0; // Default to everything
    [Tooltip("Height above target point to start raycasting downward.")]
    public float raycastHeightOffset = 10f;
    [Tooltip("Distance to raycast downward.")]
    public float raycastDistance = 20f;
    [Tooltip("Offset applied along the normal/Y-axis to prevent clipping.")]
    public float verticalOffset = 0.02f;

    [Header("Parenting")]
    [Tooltip("If null, placed objects will be children of this GameObject.")]
    public Transform parentOverride;

    [HideInInspector]
    public bool isPlacementModeActive = false;

    // Track placed objects to allow clearing them
    [HideInInspector]
    public List<GameObject> placedObjects = new List<GameObject>();

    /// <summary>
    /// Places random objects inside the circle centered at 'center'.
    /// </summary>
    public void PlaceObjects(Vector3 center, Vector3 normal)
    {
        if (prefabsToPlace == null || prefabsToPlace.Length == 0)
        {
            Debug.LogWarning("RandomPlacer: No prefabs assigned to place.");
            return;
        }

        if (normal == Vector3.zero)
        {
            normal = Vector3.up;
        }

        if (raycastDistance <= raycastHeightOffset)
        {
            Debug.LogWarning($"RandomPlacer: raycastDistance ({raycastDistance}) is less than or equal to raycastHeightOffset ({raycastHeightOffset}). The raycast might not reach the surface. Consider increasing raycastDistance.");
        }

#if UNITY_EDITOR
        // Start grouping Undos
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Place Random Litter");
        int undoGroup = Undo.GetCurrentGroup();
#endif

        Transform parentTransform = parentOverride != null ? parentOverride : transform;
        int successfullyPlaced = 0;
        int missedRaycasts = 0;

        // Calculate rotation to align the disk brush to the surface normal
        Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, normal);

        for (int i = 0; i < amountToPlace; i++)
        {
            // Get a random point within the 2D circle
            Vector2 randomPoint2D = Random.insideUnitCircle * radius;
            Vector3 localOffset = new Vector3(randomPoint2D.x, 0f, randomPoint2D.y);

            // Rotate offset to lie tangent to the surface normal
            Vector3 alignedOffset = surfaceRotation * localOffset;

            // Calculate raycast start position.
            // We start offset along the normal.
            Vector3 rayStart = center + alignedOffset + normal * raycastHeightOffset;
            Ray ray = new Ray(rayStart, -normal);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, placementMask))
            {
                // Select a random prefab
                GameObject selectedPrefab = prefabsToPlace[Random.Range(0, prefabsToPlace.Length)];
                if (selectedPrefab == null) continue;

                GameObject newObject = null;

#if UNITY_EDITOR
                // Safely instantiate model assets (like FBX, GLB) or standard prefabs in Editor
                try
                {
                    if (PrefabUtility.IsPartOfPrefabAsset(selectedPrefab))
                    {
                        newObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                    }
                    else
                    {
                        newObject = Instantiate(selectedPrefab);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"RandomPlacer: PrefabUtility.InstantiatePrefab failed for '{selectedPrefab.name}'. Falling back to standard Instantiate. Info: {ex.Message}");
                    newObject = Instantiate(selectedPrefab);
                }
#else
                newObject = Instantiate(selectedPrefab);
#endif

                if (newObject != null)
                {
                    // Calculate position with optional height offset
                    Vector3 position = hit.point + hit.normal * verticalOffset;
                    newObject.transform.position = position;

                    // Calculate rotation
                    float rotX = randomXRotation ? Random.Range(0f, 360f) : fixedRotation.x;
                    float rotY = randomYRotation ? Random.Range(0f, 360f) : fixedRotation.y;
                    float rotZ = randomZRotation ? Random.Range(0f, 360f) : fixedRotation.z;

                    Quaternion rotation = Quaternion.Euler(rotX, rotY, rotZ);

                    if (alignToNormal)
                    {
                        // Align to surface normal, then apply the randomized rotation relative to the aligned frame
                        Quaternion alignRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                        rotation = alignRotation * rotation;
                    }

                    newObject.transform.rotation = rotation;

                    // Randomize Scale relative to prefab's original scale
                    float randomScale = Random.Range(minScale, maxScale);
                    newObject.transform.localScale = selectedPrefab.transform.localScale * randomScale;

                    // Set Parent
                    newObject.transform.SetParent(parentTransform);

                    // Track object
                    placedObjects.Add(newObject);

#if UNITY_EDITOR
                    // Register with Undo
                    Undo.RegisterCreatedObjectUndo(newObject, "Place Random Litter");
                    EditorUtility.SetDirty(newObject);
#endif
                    successfullyPlaced++;
                }
            }
            else
            {
                missedRaycasts++;
            }
        }

#if UNITY_EDITOR
        // Clean up lists on the placer
        Undo.RecordObject(this, "Place Random Litter");
        EditorUtility.SetDirty(this);
        Undo.CollapseUndoOperations(undoGroup);
#endif

        if (successfullyPlaced > 0)
        {
            Debug.Log($"RandomPlacer: Successfully placed {successfullyPlaced} objects.");
        }
        
        if (missedRaycasts > 0)
        {
            Debug.LogWarning($"RandomPlacer: {missedRaycasts} out of {amountToPlace} placement raycasts did not hit any collider. Ensure your placement surface has a collider and is in the selected Layer Mask.");
        }
    }

    /// <summary>
    /// Clears all objects placed by this script.
    /// </summary>
    public void ClearPlacedObjects()
    {
#if UNITY_EDITOR
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clear Placed Litter");
        int undoGroup = Undo.GetCurrentGroup();

        // Record state of RandomPlacer component for Undo
        Undo.RecordObject(this, "Clear Placed Litter");
#endif

        // Clean up references
        placedObjects.RemoveAll(item => item == null);

        int count = placedObjects.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            GameObject obj = placedObjects[i];
            if (obj != null)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(obj);
#else
                Destroy(obj);
#endif
            }
        }

        placedObjects.Clear();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        Undo.CollapseUndoOperations(undoGroup);
#endif

        Debug.Log($"RandomPlacer: Cleared {count} placed objects.");
    }
}

