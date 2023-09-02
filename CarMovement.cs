using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public WheelCollider FRCOLLIDER;
    public WheelCollider FLCOLLIDER;
    public WheelCollider RRCOLLIDER;
    public WheelCollider RLCOLLIDER;



    public float MaxTorque = 100;
    public float MaxTurn = 100;

    float CarAccel;
    float CarTurn;

    private void Update()
    {
        CarAccel = Input.GetAxis("Vertical");
        Debug.Log("Accel:"+ CarAccel);

        CarTurn = Input.GetAxis("Horizontal");
        Debug.Log("Turn:"+ CarAccel);

        RLCOLLIDER.motorTorque = CarAccel * MaxTorque;
        RRCOLLIDER.motorTorque = CarAccel * MaxTorque ;

        FRCOLLIDER.steerAngle = CarTurn * MaxTurn;
        FLCOLLIDER.steerAngle = CarTurn * MaxTurn;
    }
}
