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
    private Vector2 m_MouseChange;
    private Transform m_JetBody;
    private Rigidbody m_RigidBody;
    private float m_ThrustForceMagnitude;
    private double m_ThrustScalar;
    public float m_MaximumThrustForce;
    public float m_ForceIncreaseRate;
    public float m_ZeroLiftDragCoefficient;
    public float m_MaxLiftCoefficient;
    public float m_WingArea;
    public float m_AirDensity;
    public float m_StallAngle;
    public float m_WingSpan;

    public float m_MouseSensitivity;
    public bool m_LinearMouseMovement;

    public Vector3 m_MaxDeltaAngle;
    public Vector3 m_MinDeltaAngle;


    // Start is called before the first frame update
    void Start()
    {
        m_RigidBody = GetComponent<Rigidbody>();
        Transform m_JetBody = m_RigidBody.transform;
        m_ThrustScalar = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 curPosition = transform.position;

        m_RigidBody.transform.rotation = calculateRotation();
        m_JetBody = m_RigidBody.transform;
        Vector3 curForwardDir = Vector3.Normalize(-m_JetBody.forward);
        Vector3 curRightDir = Vector3.Normalize(-m_JetBody.right);
        Vector3 curUpDir = Vector3.Normalize(m_JetBody.up);


        float verticalInput = UnityEngine.Input.GetAxis("Vertical");
        m_ThrustScalar += m_ForceIncreaseRate * Time.deltaTime * verticalInput;
        if(m_ThrustScalar < 0)
            m_ThrustScalar = 0;
        if(m_ThrustScalar > 100)
            m_ThrustScalar = 100;
        
        m_ThrustForceMagnitude = m_MaximumThrustForce * (float)(m_ThrustScalar / 100.0);


        Vector3 ThrustForce = curForwardDir * m_ThrustForceMagnitude;
        m_RigidBody.AddForceAtPosition(ThrustForce, m_RigidBody.centerOfMass);

        float v2 = Mathf.Pow(Mathf.Max(Vector3.Distance(m_RigidBody.velocity, Vector3.zero) * Vector3.Dot(Vector3.Normalize(m_RigidBody.velocity), curForwardDir), 0), 2);
        float dynamicPressure = (float)0.5 * v2 * m_AirDensity;

        float liftCoefficient = calculateLiftCoefficient(m_RigidBody);
        Vector3 liftForce = curUpDir * liftCoefficient * dynamicPressure * m_WingArea;
        m_RigidBody.AddForceAtPosition(liftForce, m_RigidBody.centerOfMass);


        v2 = Mathf.Pow(Vector3.Distance(m_RigidBody.velocity, Vector3.zero), 2);
        dynamicPressure = 0.5f * v2 * m_AirDensity;

        float dragCoefficient = calculateDragCoefficient(liftCoefficient);
        Vector3 DragForce = Vector3.Normalize(-m_RigidBody.velocity) * dragCoefficient * dynamicPressure * m_WingArea;
        m_RigidBody.AddForceAtPosition(DragForce, m_RigidBody.centerOfMass);
    }

    private float lastMouseChangeTime;
    Quaternion calculateRotation()
    {
        Quaternion from, rot = m_RigidBody.transform.rotation;

        //m_MouseChange.x = UnityEngine.Input.GetAxis("Mouse X");
        //m_MouseChange.y = UnityEngine.Input.GetAxis("Mouse Y");

        m_MouseChange.x = 2 * (UnityEngine.Input.mousePosition.x - UnityEngine.Screen.width / 2) / UnityEngine.Screen.width;
		m_MouseChange.y = 2 * (UnityEngine.Input.mousePosition.y - UnityEngine.Screen.height / 2) / UnityEngine.Screen.height;


        if(!m_LinearMouseMovement)
        {
            m_MouseChange.x = m_MouseChange.x * m_MouseChange.x * Mathf.Sign(m_MouseChange.x);
		    m_MouseChange.y = m_MouseChange.y * m_MouseChange.y * Mathf.Sign(m_MouseChange.y);
        }

        m_MouseChange *= Mathf.Clamp01(m_MouseSensitivity);

        float pitch, yaw, roll;
        {
            if(m_MouseChange.y > 0)
                pitch = Mathf.Lerp(0, m_MaxDeltaAngle.x * Time.deltaTime, m_MouseChange.y);
            else
                pitch = Mathf.Lerp(0, m_MinDeltaAngle.x * Time.deltaTime, -m_MouseChange.y);
            
            float horizontalInput = UnityEngine.Input.GetAxis("Horizontal");
            yaw = Mathf.Clamp(horizontalInput, m_MinDeltaAngle.y * Time.deltaTime, m_MaxDeltaAngle.y * Time.deltaTime);

			if(m_MouseChange.x > 0)
				roll = Mathf.Lerp(0, m_MaxDeltaAngle.z * Time.deltaTime, m_MouseChange.x);
			else
				roll = Mathf.Lerp(0, m_MinDeltaAngle.z * Time.deltaTime, -m_MouseChange.x);

		}


		if(UnityEngine.Input.GetAxis("Mouse X") != 0)
        {
            lastMouseChangeTime = Time.time;
        }

        from = rot * Quaternion.Euler(pitch, yaw, roll);
        
        if(Time.time - lastMouseChangeTime > 1)
        { //roll calculations
            Vector3 angles = rot.eulerAngles;
            float phi = (angles.z < 90 || angles.z > 270) ? 0 : 180;
            Quaternion to = Quaternion.Euler(pitch + angles.x, yaw + angles.y, phi);


            float timeScalar = 0.1f * Mathf.Clamp01(0.2f * (Time.time - lastMouseChangeTime));
            rot = Quaternion.RotateTowards(from, to, timeScalar * Mathf.Pow(Mathf.Sin(2 * (rot.eulerAngles.z * Mathf.Deg2Rad)), 2));
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
        cosAngleToCoefficientRatio = m_MaxLiftCoefficient / Mathf.Cos(m_StallAngle);

        float signOfAlpha = -Mathf.Sign(Vector3.Dot(Vector3.Normalize(body.velocity), body.transform.up));
        float alpha = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(Vector3.Normalize(body.velocity), -body.transform.forward));
        if (alpha < m_StallAngle)
        {
            Cl = alpha;
        }
        else if (alpha >= m_StallAngle && alpha < 2 * m_StallAngle)
        {
            Cl = (2 * m_StallAngle - alpha);
        }
        else
        {
            Cl = m_StallAngle * 0.5f;
        }
        

        Cl = Cl / m_StallAngle;
        Cl = Mathf.Pow(Cl, 2);
        Cl = Cl * signOfAlpha;
        Cl = Cl * m_MaxLiftCoefficient;

        return Cl;
    }

    float calculateDragCoefficient(float liftCoefficient)
    {
        float Cd0, Cd;
        Cd0 = m_ZeroLiftDragCoefficient;

        float AspectRatio = m_WingSpan * m_WingSpan / m_WingArea;
        float OswaldEfficiency = (float)0.5;

        float numerator = Mathf.Max(Mathf.Pow(liftCoefficient, 2), 0);
        float denominator = Mathf.PI * OswaldEfficiency * AspectRatio;

        Cd = Cd0 + numerator / denominator;

        return Cd;
    }
}
