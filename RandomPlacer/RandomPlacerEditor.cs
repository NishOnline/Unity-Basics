using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RandomPlacer))]
public class RandomPlacerEditor : Editor
{
    private RandomPlacer placer;

    private SerializedProperty prefabsToPlaceProp;
    private SerializedProperty radiusProp;
    private SerializedProperty amountToPlaceProp;
    private SerializedProperty minScaleProp;
    private SerializedProperty maxScaleProp;
    private SerializedProperty alignToNormalProp;
    private SerializedProperty randomXRotationProp;
    private SerializedProperty randomYRotationProp;
    private SerializedProperty randomZRotationProp;
    private SerializedProperty fixedRotationProp;
    private SerializedProperty placementMaskProp;
    private SerializedProperty raycastHeightOffsetProp;
    private SerializedProperty raycastDistanceProp;
    private SerializedProperty verticalOffsetProp;
    private SerializedProperty parentOverrideProp;

    private Vector2 mouseDownPosition;

    private void OnEnable()
    {
        placer = (RandomPlacer)target;

        prefabsToPlaceProp = serializedObject.FindProperty("prefabsToPlace");
        radiusProp = serializedObject.FindProperty("radius");
        amountToPlaceProp = serializedObject.FindProperty("amountToPlace");
        minScaleProp = serializedObject.FindProperty("minScale");
        maxScaleProp = serializedObject.FindProperty("maxScale");
        alignToNormalProp = serializedObject.FindProperty("alignToNormal");
        randomXRotationProp = serializedObject.FindProperty("randomXRotation");
        randomYRotationProp = serializedObject.FindProperty("randomYRotation");
        randomZRotationProp = serializedObject.FindProperty("randomZRotation");
        fixedRotationProp = serializedObject.FindProperty("fixedRotation");
        placementMaskProp = serializedObject.FindProperty("placementMask");
        raycastHeightOffsetProp = serializedObject.FindProperty("raycastHeightOffset");
        raycastDistanceProp = serializedObject.FindProperty("raycastDistance");
        verticalOffsetProp = serializedObject.FindProperty("verticalOffset");
        parentOverrideProp = serializedObject.FindProperty("parentOverride");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Prefabs Section
        EditorGUILayout.LabelField("Prefabs Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(prefabsToPlaceProp, true);

        EditorGUILayout.Space(10);

        // 2. Placement Area Settings
        EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);
        DrawPropertyWithReset(radiusProp, 2.0f);
        DrawPropertyWithReset(amountToPlaceProp, 10);

        EditorGUILayout.Space(10);

        // 3. Scale & Rotation Settings
        EditorGUILayout.LabelField("Scale & Rotation Randomization", EditorStyles.boldLabel);
        DrawPropertyWithReset(minScaleProp, 0.8f);
        DrawPropertyWithReset(maxScaleProp, 1.2f);
        DrawPropertyWithReset(alignToNormalProp, true);
        DrawPropertyWithReset(randomXRotationProp, false);
        DrawPropertyWithReset(randomYRotationProp, true);
        DrawPropertyWithReset(randomZRotationProp, false);
        DrawPropertyWithReset(fixedRotationProp, Vector3.zero);

        EditorGUILayout.Space(10);

        // 4. Raycast Settings
        EditorGUILayout.LabelField("Raycasting & Offsets", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(placementMaskProp);
        DrawPropertyWithReset(raycastHeightOffsetProp, 10.0f);
        DrawPropertyWithReset(raycastDistanceProp, 20.0f);
        DrawPropertyWithReset(verticalOffsetProp, 0.02f);

        EditorGUILayout.Space(10);

        // 5. Parenting
        EditorGUILayout.LabelField("Parent Hierarchy Override", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(parentOverrideProp);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);

        // Display current placement mode status
        GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
        if (placer.isPlacementModeActive)
        {
            statusStyle.normal.textColor = new Color(0f, 0.8f, 0.5f); // Beautiful green
            EditorGUILayout.LabelField("Placement Mode: ACTIVE (Hover in Scene View to paint)", statusStyle);
        }
        else
        {
            statusStyle.normal.textColor = Color.gray;
            EditorGUILayout.LabelField("Placement Mode: INACTIVE", statusStyle);
        }

        // Toggle Placement Mode button
        string toggleButtonText = placer.isPlacementModeActive ? "Disable Placement Mode" : "Enable Placement Mode";
        if (GUILayout.Button(toggleButtonText, GUILayout.Height(30)))
        {
            placer.isPlacementModeActive = !placer.isPlacementModeActive;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(5);

        // Clear Placed Objects button
        GUI.enabled = placer.placedObjects.Count > 0;
        if (GUILayout.Button($"Clear Placed Objects ({placer.placedObjects.Count})", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Placed Objects", "Are you sure you want to delete all objects spawned by this placer?", "Yes", "No"))
            {
                placer.ClearPlacedObjects();
                SceneView.RepaintAll();
            }
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Assign your litter/3D model prefabs to the 'Prefabs To Place' array above.\n" +
            "2. Enable Placement Mode.\n" +
            "3. Hover your mouse over colliders in the Scene View to see the placement brush.\n" +
            "4. Adjust brush radius using '+' / '-' keys or '[' / ']' keys.\n" +
            "5. Press Enter or Right-Click (release quickly) to place the litter.\n" +
            "6. Press Ctrl+Z (Cmd+Z on Mac) to undo any placement batch.\n" +
            "7. Click 'Reset' next to fields to restore their default settings.",
            MessageType.Info
        );
    }

    private void DrawPropertyWithReset(SerializedProperty prop, float defaultValue)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop);
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            prop.floatValue = defaultValue;
            GUI.FocusControl(null);
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
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPropertyWithReset(Ser...

<...etc...>
