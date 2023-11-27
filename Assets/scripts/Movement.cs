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
    private Transform direction;
    private Rigidbody rb;
    private float thrustForceMagnitude;
    private double forceTimeScalar;
    public float maximumThrustForce;
    public float forceIncreaseRate;
    public float currentForceOutput;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Transform direction = rb.transform;
        forceTimeScalar = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 curPosition = transform.position;


        mouseChange.x = UnityEngine.Input.GetAxis("Mouse X");
        mouseChange.y = UnityEngine.Input.GetAxis("Mouse Y");
        float horizontalInput = UnityEngine.Input.GetAxis("Horizontal");

        float roll = mouseChange.x;
        float pitch = mouseChange.y;
        float yaw = (float)0.1 * horizontalInput;

        if (pitch >  90) { pitch = 90; }
        if(pitch < -90) { pitch = -90; }

        Quaternion rotationQuat = rb.transform.rotation;
        //rotationQuat *= Quaternion.AngleAxis(pitch, new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw)));
        //rotationQuat *= Quaternion.AngleAxis(roll, new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw)));

        rotationQuat *= Quaternion.Euler(pitch, yaw, roll);
        
        rb.transform.rotation = rotationQuat;
        direction = rb.transform;
        Vector3 curForwardDir = -direction.forward;
        Vector3 curRightDir = -direction.right;
        Vector3 curUpDir = direction.up;


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

        Vector3 liftForce = curUpDir * Mathf.Pow(Vector3.Distance(rb.velocity, Vector3.zero), 2) / 2 * (float)78.04 * (float)1.293;
        rb.AddForceAtPosition(liftForce, rb.centerOfMass);

        currentForceOutput = Mathf.Abs(thrustForceMagnitude);
    }
}
