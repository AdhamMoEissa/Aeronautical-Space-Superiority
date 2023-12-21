using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.Windows;

public class Movement : MonoBehaviour
{
    private Vector2 mouseChange;
    private Transform jetBody;
    private Rigidbody rb;
    private float thrustForceMagnitude;
    private double forceTimeScalar;
    public float maximumThrustForce;
    public float forceIncreaseRate;
    public float zeroLiftDragCoefficient;
    public float maxLiftCoefficient;
    public float wingArea;
    public float airDensity;
    public float stallAngle;
    public float wingSpan;

    public float mouseSensitivity;

    public float maxRollAngle;
    public float maxPitchAngle;
    public float maxYawAngle;



    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Transform jetBody = rb.transform;
        forceTimeScalar = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 curPosition = transform.position;

        rb.transform.rotation = calculateRotation();
        jetBody = rb.transform;
        Vector3 curForwardDir = Vector3.Normalize(-jetBody.forward);
        Vector3 curRightDir = Vector3.Normalize(-jetBody.right);
        Vector3 curUpDir = Vector3.Normalize(jetBody.up);


        float verticalInput = UnityEngine.Input.GetAxis("Vertical");
        float upInput = UnityEngine.Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;

        if(verticalInput != 0)
            forceTimeScalar += forceIncreaseRate * Time.deltaTime;
        else
            forceTimeScalar = 0;

        if(forceTimeScalar > (UnityEngine.Input.GetKey(KeyCode.LeftShift) ? 100 : 50))
            forceTimeScalar = (UnityEngine.Input.GetKey(KeyCode.LeftShift) ? 100 : 50);

            thrustForceMagnitude = verticalInput * maximumThrustForce * (float)(forceTimeScalar / 100.0);


        Vector3 ThrustForce = curForwardDir * thrustForceMagnitude;
        rb.AddForceAtPosition(ThrustForce, rb.centerOfMass);

        float v2 = Mathf.Pow(Mathf.Max(Vector3.Distance(rb.velocity, Vector3.zero) * Vector3.Dot(Vector3.Normalize(rb.velocity), curForwardDir), 0), 2);
        float dynamicPressure = (float)0.5 * v2 * airDensity;

        float liftCoefficient = calculateLiftCoefficient(rb);
        Vector3 liftForce = curUpDir * liftCoefficient * dynamicPressure * wingArea;
        rb.AddForceAtPosition(liftForce, rb.centerOfMass);




        v2 = Mathf.Pow(Vector3.Distance(rb.velocity, Vector3.zero), 2);
        dynamicPressure = 0.5f * v2 * airDensity;

        float dragCoefficient = calculateDragCoefficient(liftCoefficient);
        Vector3 DragForce = Vector3.Normalize(-rb.velocity) * dragCoefficient * dynamicPressure * wingArea;
        rb.AddForceAtPosition(DragForce, rb.centerOfMass);
    }

    private float lastMouseChangeTime;

    Quaternion calculateRotation()
    {
        Quaternion from, rot = rb.transform.rotation;

        mouseChange.x = UnityEngine.Input.GetAxis("Mouse X");
        mouseChange.y = UnityEngine.Input.GetAxis("Mouse Y");
        mouseChange *= mouseSensitivity;
        float horizontalInput = UnityEngine.Input.GetAxis("Horizontal");

        float pitch = Mathf.Clamp(mouseChange.y, -maxPitchAngle, maxPitchAngle);
        
        float yaw = Mathf.Clamp(horizontalInput, -maxYawAngle, maxYawAngle);

        float roll = Mathf.Clamp(mouseChange.x, -maxRollAngle, maxRollAngle);


        if (roll != 0)
        {
            lastMouseChangeTime = Time.time;
        }

        from = rot * Quaternion.Euler(pitch, yaw, roll);
        
        if(Time.time - lastMouseChangeTime > 1)
        { //roll calculations
            Vector3 angles = rot.eulerAngles;
            float phi = (angles.z < 90 || angles.z > 270) ? 0 : 180;
            Quaternion to = Quaternion.Euler(pitch + angles.x, yaw + angles.y, phi);


            float timeScalar = 0.2f * Mathf.Clamp01(0.1f * (Time.time - lastMouseChangeTime));
            rot = Quaternion.RotateTowards(from, to, timeScalar * Mathf.Abs( Mathf.Pow(Mathf.Sin(2 * (rot.eulerAngles.z * Mathf.Deg2Rad)), 2) ));
        }
        else 
        {
            rot = from;
        }


        return rot;
    }

    float calculateLiftCoefficient(Rigidbody body)
    {
        float Cl, cosAngleToCoefficientRatio;
        cosAngleToCoefficientRatio = maxLiftCoefficient / Mathf.Cos(stallAngle);

        float signOfAlpha = -Mathf.Sign(Vector3.Dot(Vector3.Normalize(body.velocity), body.transform.up));
        float alpha = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(Vector3.Normalize(body.velocity), -body.transform.forward));
        if (alpha < stallAngle)
        {
            Cl = alpha;
        }
        else if (alpha >= stallAngle && alpha < 2 * stallAngle)
        {
            Cl = (2 * stallAngle - alpha);
        }
        else
        {
            Cl = stallAngle * 0.5f;
        }
        

        Cl = Cl / stallAngle;
        Cl = Mathf.Pow(Cl, 2);
        Cl = Cl * signOfAlpha;
        Cl = Cl * maxLiftCoefficient;

        return Cl;
    }

    float calculateDragCoefficient(float liftCoefficient)
    {
        float Cd0, Cd;
        Cd0 = zeroLiftDragCoefficient;

        float AspectRatio = wingSpan * wingSpan / wingArea;
        float OswaldEfficiency = (float)0.5;

        float numerator = Mathf.Max(Mathf.Pow(liftCoefficient, 2), 0);
        float denominator = Mathf.PI * OswaldEfficiency * AspectRatio;

        Cd = Cd0 + numerator / denominator;

        return Cd;
    }
}
