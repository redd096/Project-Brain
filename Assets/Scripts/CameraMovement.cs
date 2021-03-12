﻿using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Speed Rotation")]
    [SerializeField] float rotationSpeedX = 300;
    [SerializeField] float rotationSpeedY = 300;
    [SerializeField] float smooth = 20;

    [Header("Limits Rotation")]
    [SerializeField] float minLimitX = -110;
    [SerializeField] float maxLimitX = 110;
    [SerializeField] float minLimitY = 20;
    [SerializeField] float maxLimitY = 40;

    //rotation
    Transform cam;
    Transform cameraTarget;
    Transform pivot;

    void Start()
    {
        //get camera and create a target to move smooth camera
        cam = Camera.main.transform;
        cameraTarget = new GameObject("Camera Target").transform;

        //confined mouse
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        //do only if there is pivot
        if (pivot == null)
            return;

        //rotate target around pivot
        RotateTransformAround(cameraTarget, pivot.position, Vector3.up, -Input.GetAxis("Mouse X") * rotationSpeedX);
        RotateTransformAround(cameraTarget, pivot.position, cameraTarget.right, Input.GetAxis("Mouse Y") * rotationSpeedY);

        //smooth movement camera to target
        LerpCamera();
    }

    #region private API

    void RotateTransformAround(Transform transformToRotate, Vector3 pivot, Vector3 axis, float angle)
    {
        Quaternion newRotation = Quaternion.Euler(axis * angle) * transformToRotate.rotation;       //add rotation

        //clamp rotation
        float eulerY = Mathf.Clamp(NegativeAngle(newRotation.eulerAngles.y), minLimitX, maxLimitX);
        float eulerX = Mathf.Clamp(NegativeAngle(newRotation.eulerAngles.x), minLimitY, maxLimitY);
        newRotation = Quaternion.Euler(PositiveAngle(eulerX), PositiveAngle(eulerY), 0);

        Quaternion angleRotation = newRotation * Quaternion.Inverse(transformToRotate.rotation);    //subtract old rotation to obtain angle rotation
        Vector3 direction = transformToRotate.position - pivot;                                     //get direction and distance from pivot
        direction = angleRotation * direction;                                                      //rotate it by angle clamped
        Vector3 newPosition = pivot + direction;                                                    //from pivot add new direction

        //set position and rotation
        transformToRotate.position = newPosition;
        transformToRotate.rotation = newRotation;
    }

    void LerpCamera()
    {
        //smooth movement camera to target
        cam.position = Vector3.Lerp(cam.position, cameraTarget.position, Time.deltaTime * smooth);
        cam.rotation = Quaternion.Lerp(cam.rotation, cameraTarget.rotation, Time.deltaTime * smooth);
    }

    float NegativeAngle(float angle)
    {
        //show positive and negative angle (instead of 270, show -90)
        if (angle > 180)
            return angle - 360;

        return angle;
    }

    float PositiveAngle(float angle)
    {
        //show only positive angle (instead of -90, show 270)
        if (angle < 0)
            return angle + 360;

        return angle;
    }

    #endregion

    /// <summary>
    /// Set pivot to rotate around
    /// </summary>
    public void SetPivot(Transform pivot)
    {
        this.pivot = pivot;

        //reset target because camera was moved by a coroutine
        cameraTarget.position = cam.position;
        cameraTarget.rotation = cam.rotation;
    }

    /// <summary>
    /// Remove pivot and stop camera movement
    /// </summary>
    public void RemovePivot()
    {
        pivot = null;
    }
}
