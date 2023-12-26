using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraMovement : MonoBehaviour
{
    private Transform camTrans;
    private Camera cam;
    public GameObject objectToFollow;
    private Rigidbody objectRigidBody;
	private float scroll;
    public float scrollSensitivity;
    public float sensitivity;
    public float topSpeed;
    // Start is called before the first frame update
    void Start()
    {
        camTrans = GetComponent<Transform>();
        camTrans.position = objectToFollow.transform.position + new Vector3(0, 1, 3);
        camTrans.forward = objectToFollow.transform.forward;

		cam = GetComponent<Camera>();

		objectToFollow.TryGetComponent<Rigidbody>(out objectRigidBody);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        scroll += UnityEngine.Input.mouseScrollDelta.y * scrollSensitivity;
        if (scroll < 10)
            scroll = 10;
        if (scroll > 100)
            scroll = 100;

        camTrans.position = objectToFollow.transform.position + scroll * Vector3.Normalize(5 * objectToFollow.transform.forward + 2 * objectToFollow.transform.up);
        camTrans.LookAt(objectToFollow.transform.position + 3 * objectToFollow.transform.up, objectToFollow.transform.up);

        float rigidBodySpeed = Vector3.Distance(objectRigidBody.velocity, Vector3.zero);
        cam.fieldOfView = Mathf.Lerp(60f, 100f, rigidBodySpeed/topSpeed);
    }
}
