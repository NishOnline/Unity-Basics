using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class Wire : MonoBehaviour
{
    [Header("Connections")]
    public Transform startConnector;
    public Transform endConnector;

    [Header("Wire Settings")]
    [Range(0.01f, 0.5f)]
    public float wireWidth = 0.04f;
    [Range(0f, 5f)]
    public float wireSag = 0.4f;
    [Tooltip("Additional sag based on the distance between poles (sag = wireSag + distance * wireSagFactor)")]
    [Range(0f, 0.1f)]
    public float wireSagFactor = 0.01f;
    [Range(5, 50)]
    public int segments = 20;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        InitializeLineRenderer();
    }

    private void Start()
    {
        UpdateWireCurve();
    }

    private void LateUpdate()
    {
        UpdateWireCurve();
    }

    private void OnValidate()
    {
        InitializeLineRenderer();
        UpdateWireCurve();
    }

    private void InitializeLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = wireWidth;
            lineRenderer.endWidth = wireWidth;
            lineRenderer.positionCount = segments + 1;
            
            // Set some default shadow and light-probe properties for wire performance
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
    }

    /// <summary>
    /// Computes and updates the LineRenderer positions to draw a sagging wire curve.
    /// </summary>
    public void UpdateWireCurve()
    {
        if (lineRenderer == null) return;

        if (startConnector == null || endConnector == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        Vector3 startPos = startConnector.position;
        Vector3 endPos = endConnector.position;

        float distance = Vector3.Distance(startPos, endPos);
        float totalSag = wireSag + (distance * wireSagFactor);

        if (lineRenderer.positionCount != segments + 1)
        {
            lineRenderer.positionCount = segments + 1;
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            
            // Linear interpolation between the two connectors
            Vector3 point = Vector3.Lerp(startPos, endPos, t);
            
            // Parabolic offset: y = -4 * sag * t * (1 - t)
            // This is 0 at t=0 and t=1, and peaks at t=0.5 with value -sag
            float sagOffset = -totalSag * 4f * t * (1f - t);
            point.y += sagOffset;

            lineRenderer.SetPosition(i, point);
        }
    }

    /// <summary>
    /// Helper to set wire configuration values.
    /// </summary>
    public void SetParameters(Transform start, Transform end, Material mat, float width, float sag, float sagFactor, int resolution)
    {
        startConnector = start;
        endConnector = end;
        wireWidth = width;
        wireSag = sag;
        wireSagFactor = sagFactor;
        segments = resolution;

        InitializeLineRenderer();
        if (lineRenderer != null && mat != null)
        {
            lineRenderer.sharedMaterial = mat;
        }
        UpdateWireCurve();
    }
}
