using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraMovement : MonoBehaviour
{
    private Vector3 direction;
    private Transform camTrans;
    private Transform objectToFollow;
    private float scroll;
    public float scrollSensitivity;
    public float sensitivity;
    public Transform characterObject;
    // Start is called before the first frame update
    void Start()
    {
        camTrans = GetComponent<Transform>();
        objectToFollow = characterObject;
        camTrans.position = objectToFollow.position + new Vector3(0, 1, 3);
        camTrans.forward = objectToFollow.forward;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        scroll += UnityEngine.Input.mouseScrollDelta.y * scrollSensitivity;
        if (scroll < 10)
            scroll = 10;
        if (scroll > 100)
            scroll = 100;

        camTrans.position = objectToFollow.position + scroll * Vector3.Normalize(5 * objectToFollow.forward + 2 * objectToFollow.up);
        camTrans.LookAt(objectToFollow.position + 3 * objectToFollow.up, objectToFollow.up);
    }
}
