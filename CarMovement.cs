using UnityEngine;

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

    [Header("Wheel Visual Rotation Offset")]
    public Vector3 WheelRotationOffset;

    [Header("Speedometer")]
    public bool EnableSpeedometer = true;
    public float CurrentSpeed; // km/h

    [Header("Engine Sound")]
    public AudioSource EngineAudio;
    public float MinPitch = 0.8f;   // Idle
    public float MaxPitch = 2.0f;   // Max rev
    public float MaxSpeedForPitch = 120f; // km/h

    float CarAccel;
    float CarTurn;
    float BrakeInput;

    Vector3 pos;
    Quaternion rot;

    Rigidbody rb;

    // ---------------- INIT ----------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (EngineAudio != null)
            EngineAudio.Play();
    }

    // ---------------- PHYSICS ----------------
    private void FixedUpdate()
    {
        CarAccel   = Input.GetAxis("Vertical");
        CarTurn    = Input.GetAxis("Horizontal");
        BrakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // Motor (RWD)
        RLCollider.motorTorque = CarAccel * MaxTorque;
        RRCollider.motorTorque = CarAccel * MaxTorque;

        // Steering
        FRCollider.steerAngle = CarTurn * MaxTurn;
        FLCollider.steerAngle = CarTurn * MaxTurn;

        // Brakes
        ApplyBrakes(BrakeInput * BrakeForce);

        // Speedometer
        if (EnableSpeedometer)
            CurrentSpeed = rb.linearVelocity.magnitude * 3.6f;

        // Engine sound
        UpdateEngineSound();
    }

    // ---------------- VISUALS ----------------
    private void Update()
    {
        UpdateWheel(FLCollider, FLTire);
        UpdateWheel(FRCollider, FRTire);
        UpdateWheel(RLCollider, RLTire);
        UpdateWheel(RRCollider, RRTire);
    }

    void ApplyBrakes(float brakeForce)
    {
        FLCollider.brakeTorque = brakeForce;
        FRCollider.brakeTorque = brakeForce;
        RLCollider.brakeTorque = brakeForce;
        RRCollider.brakeTorque = brakeForce;
    }

    void UpdateWheel(WheelCollider collider, Transform wheelMesh)
    {
        collider.GetWorldPose(out pos, out rot);
        wheelMesh.position = pos;
        wheelMesh.rotation = rot * Quaternion.Euler(WheelRotationOffset);
    }

    // ---------------- ENGINE SOUND ----------------
    void UpdateEngineSound()
    {
        if (EngineAudio == null) return;

        float speedFactor = Mathf.Clamp01(CurrentSpeed / MaxSpeedForPitch);

        float targetPitch = Mathf.Lerp(MinPitch, MaxPitch, speedFactor);

        // Smooth pitch change
        EngineAudio.pitch = Mathf.Lerp(
            EngineAudio.pitch,
            targetPitch,
            Time.deltaTime * 5f
        );
    }
}
