using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // Changed from Cinemachine to Unity.Cinemachine

public class CarMovement : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider FRCollider;
    public WheelCollider FLCollider;
    public WheelCollider RRCollider;
    public WheelCollider RLCollider;

    [Header("Wheel Meshes")]
    public Transform FLTire;
    public Transform FRTire;
    public Transform RLTire;
    public Transform RRTire;

    [Header("Car Settings")]
    public float MaxTorque = 150f;
    public float MaxTurn = 30f;
    public float BrakeForce = 3000f;
    public float DownForce = 50f; // Add downforce to keep car grounded
    public float AntiRollForce = 5000f; // Prevent car from rolling over

    [Header("Wheel Visual Offset")]
    public Vector3 WheelRotationOffset;

    [Header("Engine Sound")]
    public AudioSource EngineAudio;
    public float MinPitch = 0.8f;
    public float MaxPitch = 2f;
    public float MaxSpeedForPitch = 120f;

    [Header("Enter / Exit Car")]
    public Transform driverSeat;
    public CinemachineCamera carFreeLookCamera; // Changed to CinemachineCamera (works for FreeLook)
    public GameObject player;
    public Camera playerCamera;
    public PlayerMovement playerMovement;
    public float enterDistance = 3f;

    Rigidbody rb;
    bool isDriving = false;

    float accel;
    float steer;
    float brake;

    Vector3 wheelPos;
    Quaternion wheelRot;

    // ---------------- INIT ----------------
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (EngineAudio != null)
            EngineAudio.Play();
    }

    // ---------------- UPDATE ----------------
    void Update()
    {
        UpdateWheel(FLCollider, FLTire);
        UpdateWheel(FRCollider, FRTire);
        UpdateWheel(RLCollider, RLTire);
        UpdateWheel(RRCollider, RRTire);

        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isDriving)
                ExitCar();
            else
                TryEnterCar();
        }
    }

    // ---------------- PHYSICS ----------------
    void FixedUpdate()
    {
        if (!isDriving) return;

        accel = Input.GetAxis("Vertical");
        steer = Input.GetAxis("Horizontal");
        brake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        RLCollider.motorTorque = accel * MaxTorque;
        RRCollider.motorTorque = accel * MaxTorque;

        FLCollider.steerAngle = steer * MaxTurn;
        FRCollider.steerAngle = steer * MaxTurn;

        ApplyBrakes(brake * BrakeForce);

        // Apply downforce to keep car stable at high speeds
        rb.AddForce(-transform.up * DownForce * rb.linearVelocity.magnitude);

        // Apply anti-roll to prevent flipping
        ApplyAntiRoll(FLCollider, FRCollider);
        ApplyAntiRoll(RLCollider, RRCollider);

        UpdateEngineSound();
    }

    // ---------------- ENTER CAR ----------------
    void TryEnterCar()
    {
        if (Vector3.Distance(player.transform.position, transform.position) > enterDistance)
            return;

        isDriving = true;

        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        playerMovement.canMove = false;

        player.transform.SetParent(transform);
        player.transform.position = driverSeat.position;
        player.transform.rotation = driverSeat.rotation;

        playerCamera.enabled = false;
        
        // Enable Cinemachine FreeLook camera
        if (carFreeLookCamera != null)
        {
            carFreeLookCamera.gameObject.SetActive(true);
            // Lock cursor for car camera control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ---------------- EXIT CAR ----------------
    void ExitCar()
    {
        isDriving = false;

        // Stop the car completely
        RLCollider.motorTorque = 0;
        RRCollider.motorTorque = 0;
        RLCollider.brakeTorque = 0;
        RRCollider.brakeTorque = 0;
        FLCollider.brakeTorque = 0;
        FRCollider.brakeTorque = 0;
        
        // Stop car rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        
        // Disable CharacterController before unparenting
        cc.enabled = false;

        // Unparent player
        player.transform.SetParent(null);

        // Calculate exit position - try multiple positions
        Vector3 exitPos = transform.position;
        bool foundGround = false;
        
        // Try right side first
        Vector3 testPos = transform.position + transform.right * 3f;
        if (FindGroundPosition(testPos, out Vector3 groundPos))
        {
            exitPos = groundPos;
            foundGround = true;
        }
        
        // Try left side if right failed
        if (!foundGround)
        {
            testPos = transform.position - transform.right * 3f;
            if (FindGroundPosition(testPos, out groundPos))
            {
                exitPos = groundPos;
                foundGround = true;
            }
        }
        
        // Fallback: use car position
        if (!foundGround)
        {
            exitPos = transform.position + Vector3.up * 1.5f;
        }

        // Set player position and rotation
        player.transform.position = exitPos;
        player.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        StartCoroutine(EnableControllerAndMovement(cc));

        // Disable Cinemachine FreeLook camera
        if (carFreeLookCamera != null)
        {
            carFreeLookCamera.gameObject.SetActive(false);
        }
        
        playerCamera.enabled = true;
        
        // Restore cursor state for player
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    bool FindGroundPosition(Vector3 testPosition, out Vector3 groundPosition)
    {
        // Start raycast from high above
        Vector3 rayStart = new Vector3(testPosition.x, testPosition.y + 10f, testPosition.z);
        
        // Cast down to find ground, ignoring car
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 20f);
        
        foreach (RaycastHit hit in hits)
        {
            // Skip if hit the car itself
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                continue;
                
            // Found valid ground
            groundPosition = hit.point + Vector3.up * 1f; // Place 1 unit above ground
            return true;
        }
        
        groundPosition = Vector3.zero;
        return false;
    }

    IEnumerator EnableControllerAndMovement(CharacterController cc)
    {
        // Wait for 2 physics frames to ensure player is settled
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        // Enable CharacterController
        cc.enabled = true;
        
        // Wait one more frame
        yield return null;
        
        // Enable movement
        playerMovement.canMove = true;
    }

    // ---------------- HELPERS ----------------
    void ApplyBrakes(float force)
    {
        FLCollider.brakeTorque = force;
        FRCollider.brakeTorque = force;
        RLCollider.brakeTorque = force;
        RRCollider.brakeTorque = force;
    }

    void UpdateWheel(WheelCollider col, Transform wheel)
    {
        col.GetWorldPose(out wheelPos, out wheelRot);
        wheel.position = wheelPos;
        wheel.rotation = wheelRot * Quaternion.Euler(WheelRotationOffset);
    }

    void UpdateEngineSound()
    {
        if (EngineAudio == null) return;

        float speed = rb.linearVelocity.magnitude * 3.6f;
        float t = Mathf.Clamp01(speed / MaxSpeedForPitch);
        EngineAudio.pitch = Mathf.Lerp(MinPitch, MaxPitch, t);
    }
    
    void ApplyAntiRoll(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;

        bool groundedL = leftWheel.GetGroundHit(out hit);
        if (groundedL)
            travelL = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;

        bool groundedR = rightWheel.GetGroundHit(out hit);
        if (groundedR)
            travelR = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;

        float antiRollForce = (travelL - travelR) * AntiRollForce;

        if (groundedL)
            rb.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
        if (groundedR)
            rb.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
    }
}
