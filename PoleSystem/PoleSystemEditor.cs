using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PoleSystem))]
public class PoleSystemEditor : Editor
{
    private PoleSystem poleSystem;

    // Shift+Drag rotation states
    private bool isDraggingRotation = false;
    private Vector2 dragStartPos;
    private Vector3 dragStartRotation;
    private Vector3 dragStartPosWorld;
    private Vector3 dragStartNormal;

    // Serialized Properties for Undo/Redo & Prefab support
    private SerializedProperty polePrefabProp;
    private SerializedProperty terrainLayerProp;
    private SerializedProperty wireMaterialProp;
    
    private SerializedProperty wireWidthProp;
    private SerializedProperty wireSagProp;
    private SerializedProperty wireSagFactorProp;
    private SerializedProperty wireSegmentsProp;
    
    private SerializedProperty minDistanceProp;
    private SerializedProperty alignToNormalProp;
    private SerializedProperty poleRotationProp;
    
    private SerializedProperty enableRuntimeControlsProp;
    private SerializedProperty placementToggleKeyProp;
    private SerializedProperty undoKeyProp;
    private SerializedProperty clearKeyProp;

    private void OnEnable()
    {
        poleSystem = (PoleSystem)target;
        
        // Clean up preview if selection changes
        if (poleSystem != null)
        {
            poleSystem.DestroyPreview();
        }

        // Cache properties
        polePrefabProp = serializedObject.FindProperty("polePrefab");
        terrainLayerProp = serializedObject.FindProperty("terrainLayer");
        wireMaterialProp = serializedObject.FindProperty("wireMaterial");
        
        wireWidthProp = serializedObject.FindProperty("wireWidth");
        wireSagProp = serializedObject.FindProperty("wireSag");
        wireSagFactorProp = serializedObject.FindProperty("wireSagFactor");
        wireSegmentsProp = serializedObject.FindProperty("wireSegments");
        
        minDistanceProp = serializedObject.FindProperty("minDistance");
        alignToNormalProp = serializedObject.FindProperty("alignToNormal");
        poleRotationProp = serializedObject.FindProperty("poleRotation");
        
        enableRuntimeControlsProp = serializedObject.FindProperty("enableRuntimeControls");
        placementToggleKeyProp = serializedObject.FindProperty("placementToggleKey");
        undoKeyProp = serializedObject.FindProperty("undoKey");
        clearKeyProp = serializedObject.FindProperty("clearKey");
    }

    private void OnDisable()
    {
        if (poleSystem != null)
        {
            poleSystem.DestroyPreview();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Prefabs & Materials Section
        EditorGUILayout.LabelField("Prefabs & Materials", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(polePrefabProp);
        EditorGUILayout.PropertyField(terrainLayerProp);
        EditorGUILayout.PropertyField(wireMaterialProp);

        EditorGUILayout.Space(10);

        // 2. Wire Customization Section with Reset Buttons
        EditorGUILayout.LabelField("Wire Customization Settings", EditorStyles.boldLabel);
        DrawPropertyWithReset(wireWidthProp, 0.04f);
        DrawPropertyWithReset(wireSagProp, 0.4f);
        DrawPropertyWithReset(wireSagFactorProp, 0.01f);
        DrawPropertyWithReset(wireSegmentsProp, 20);

        EditorGUILayout.Space(10);

        // 3. Placement & Rotation Rules with Reset Buttons
        EditorGUILayout.LabelField("Placement & Rotation Rules", EditorStyles.boldLabel);
        DrawPropertyWithReset(minDistanceProp, 3.0f);
        DrawPropertyWithReset(alignToNormalProp, false);
        DrawPropertyWithReset(poleRotationProp, Vector3.zero);

        EditorGUILayout.Space(10);

        // 4. Runtime Configurations
        EditorGUILayout.LabelField("Runtime Keyboard Configurations", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableRuntimeControlsProp);
        EditorGUILayout.PropertyField(placementToggleKeyProp);
        EditorGUILayout.PropertyField(undoKeyProp);
        EditorGUILayout.PropertyField(clearKeyProp);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Editor Placement & Utility Tools", EditorStyles.boldLabel);

        // Display current mode status with stylized label
        GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
        if (poleSystem.IsPlacementModeActive)
        {
            statusStyle.normal.textColor = Color.green;
            EditorGUILayout.LabelField("Placement Mode: ACTIVE (Click in Scene View to place)", statusStyle);
        }
        else
        {
            statusStyle.normal.textColor = Color.gray;
            EditorGUILayout.LabelField("Placement Mode: INACTIVE", statusStyle);
        }

        // Toggle Placement Mode button
        string toggleButtonText = poleSystem.IsPlacementModeActive ? "Disable Placement Mode" : "Enable Placement Mode";
        if (GUILayout.Button(toggleButtonText, GUILayout.Height(30)))
        {
            poleSystem.IsPlacementModeActive = !poleSystem.IsPlacementModeActive;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(5);

        // Action Buttons Row
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Undo Last Pole", GUILayout.Height(25)))
        {
            Undo.RecordObject(poleSystem, "Undo Last Pole");
            poleSystem.UndoLastPole();
            EditorUtility.SetDirty(poleSystem);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(poleSystem.gameObject.scene);
            }
        }

        if (GUILayout.Button("Clear All Poles", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear All Poles", "Are you sure you want to delete all placed poles and wires?", "Yes", "No"))
            {
                Undo.RecordObject(poleSystem, "Clear All Poles");
                poleSystem.ClearAllPoles();
                EditorUtility.SetDirty(poleSystem);
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(poleSystem.gameObject.scene);
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Enable Placement Mode.\n" +
            "2. Hover over terrain in the Scene View to see the preview.\n" +
            "3. Use Q / E keys (or hold Shift + Scroll Wheel) to rotate the Y axis. Hold Ctrl (X-axis) or Alt (Z-axis) to rotate other axes.\n" +
            "4. Left-Click to place a pole and automatically span wires.\n" +
            "5. Click the 'Reset' buttons next to inputs to revert settings to defaults.", 
            MessageType.Info
        );
    }

    /// <summary>
    /// Helper to render a field alongside a small Reset button.
    /// </summary>
    private void DrawPropertyWithReset(SerializedProperty prop, float defaultValue)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop);
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            prop.floatValue = defaultValue;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPropertyWithReset(SerializedProperty prop, int defaultValue)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop);
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            prop.intValue = defaultValue;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPropertyWithReset(SerializedProperty prop, bool defaultValue)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop);
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            prop.boolValue = defaultValue;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPropertyWithReset(SerializedProperty prop, Vector3 defaultValue)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop);
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            prop.vector3Value = defaultValue;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI()
    {
        if (poleSystem == null) return;

        // Only run editor-time placement when not in Play mode
        if (Application.isPlaying) return;

        if (!poleSystem.IsPlacementModeActive) return;

        // Intercept input events
        Event e = Event.current;

        // Prevent selection clicking and handle layout
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlID);
        }

        // Perform raycast to find hover position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, poleSystem.terrainLayer);

        // Shift+Drag mouse rotation logic
        if (e.shift)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (hasHit)
                {
                    isDraggingRotation = true;
                    dragStartPos = e.mousePosition;
                    dragStartRotation = poleSystem.poleRotation;
                    dragStartPosWorld = hit.point;
                    dragStartNormal = hit.normal;
                    e.Use();
                }
            }

            if (isDraggingRotation)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 delta = e.mousePosition - dragStartPos;
                    Undo.RecordObject(poleSystem, "Rotate Pole via Drag");

                    float scaleFactor = 0.5f;
                    if (e.control)
                    {
                        poleSystem.poleRotation.x = (dragStartRotation.x + delta.y * scaleFactor) % 360f;
                        if (poleSystem.poleRotation.x < 0) poleSystem.poleRotation.x += 360f;
                    }
                    else if (e.alt)
                    {
                        poleSystem.poleRotation.z = (dragStartRotation.z + delta.x * scaleFactor) % 360f;
                        if (poleSystem.poleRotation.z < 0) poleSystem.poleRotation.z += 360f;
                    }
                    else
                    {
                        poleSystem.poleRotation.y = (dragStartRotation.y + delta.x * scaleFactor) % 360f;
                        if (poleSystem.poleRotation.y < 0) poleSystem.poleRotation.y += 360f;
                    }

                    SceneView.RepaintAll();
                    e.Use();
                }

                // Keep preview fixed at start drag position
                poleSystem.UpdatePreviewPosition(dragStartPosWorld, dragStartNormal, true);

                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    isDraggingRotation = false;
                    e.Use();
                }
                return; // Skip standard hover/placement while dragging
            }
        }
        else
        {
            isDraggingRotation = false;
        }

        // Standard hover & placement logic
        if (hasHit)
        {
            // Handle key rotation inputs in Scene View (Q/E)
            float editorDelta = 0f;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Q)
                {
                    editorDelta = -15f;
                    e.Use(); // Consume the event
                }
                else if (e.keyCode == KeyCode.E)
                {
                    editorDelta = 15f;
                    e.Use(); // Consume the event
                }
            }

            // Discrete rotation using Shift + Scroll Wheel in Editor (only if not dragging mouse)
            if (e.shift && e.type == EventType.ScrollWheel)
            {
                editorDelta = e.delta.y * 3f;
                e.Use();
            }

            if (Mathf.Abs(editorDelta) > 0.001f)
            {
                Undo.RecordObject(poleSystem, "Rotate Preview Pole");

                if (e.control)
                {
                    poleSystem.poleRotation.x = (poleSystem.poleRotation.x + editorDelta) % 360f;
                    if (poleSystem.poleRotation.x < 0) poleSystem.poleRotation.x += 360f;
                }
                else if (e.alt)
                {
                    poleSystem.poleRotation.z = (poleSystem.poleRotation.z + editorDelta) % 360f;
                    if (poleSystem.poleRotation.z < 0) poleSystem.poleRotation.z += 360f;
                }
                else
                {
                    poleSystem.poleRotation.y = (poleSystem.poleRotation.y + editorDelta) % 360f;
                    if (poleSystem.poleRotation.y < 0) poleSystem.poleRotation.y += 360f;
                }
                SceneView.RepaintAll();
            }

            // Update preview position
            poleSystem.UpdatePreviewPosition(hit.point, hit.normal, true);

            // Left-click to place pole (only if Shift is not held)
            if (e.type == EventType.MouseDown && e.button == 0 && !e.shift)
            {
                Undo.RecordObject(poleSystem, "Place Electric Pole");
                Pole placed = poleSystem.PlacePole(hit.point, hit.normal);
                if (placed != null)
                {
                    EditorUtility.SetDirty(poleSystem);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(poleSystem.gameObject.scene);
                }
                e.Use();
            }
        }
        else
        {
            poleSystem.UpdatePreviewPosition(Vector3.zero, Vector3.up, false);
        }

        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
        {
            SceneView.RepaintAll();
        }
    }
}