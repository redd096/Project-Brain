﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NormalState : State
{
    [Header("Movement")]
    [SerializeField] float speed = 5;
    [SerializeField] float rotationSpeed = 300;

    [Header("Interact")]
    public float radiusInteract = 1.5f;
    [SerializeField] KeyCode interactInput = KeyCode.E;
    [SerializeField] KeyCode detachRopeInput = KeyCode.Q;
    [SerializeField] float timeToKeepPressedToRewind = 0.5f;

    Rigidbody rb;
    Transform cam;

    float timerRewind;

    public override void Enter()
    {
        base.Enter();

        rb = stateMachine.GetComponent<Rigidbody>();
        cam = Camera.main.transform;

        //lock rigidbody rotation
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public override void Update()
    {
        base.Update();

        Movement();
        Rotate();

        //if timer is finished, rewind and don't check other inputs
        if (timerRewind > 0 && Time.time > timerRewind)
        {
            timerRewind = 0;
            RewindRope();
            return;
        }

        //interact
        if (Input.GetKeyDown(interactInput))
        {
            Interact();
        }
        //when press detach input
        else if(Input.GetKeyDown(detachRopeInput))
        {
            //set timer
            timerRewind = Time.time + timeToKeepPressedToRewind;
        }
        //when release detach input
        else if(Input.GetKeyUp(detachRopeInput))
        {
            timerRewind = 0;
            DetachRope();
        }
    }

    #region private API

    void Movement()
    {
        //get direction by input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        //direction to local
        direction = stateMachine.transform.TransformDirection(direction);

        //move player
        rb.velocity = direction * speed;
    }

    void Rotate()
    {
        float horizontal = Input.GetAxis("Mouse X");
        float vertical = -Input.GetAxis("Mouse Y");

        //rotate player on Y axis and camera on X axis
        stateMachine.transform.rotation = Quaternion.AngleAxis(horizontal * rotationSpeed * Time.deltaTime, Vector3.up) * stateMachine.transform.rotation;
        cam.RotateAround(stateMachine.transform.position, stateMachine.transform.right, vertical * rotationSpeed * Time.deltaTime);
    }

    protected Interactable FindInteractable()
    {
        List<Interactable> listInteractables = new List<Interactable>();

        //check every collider in area
        Collider[] colliders = Physics.OverlapSphere(stateMachine.transform.position, radiusInteract);
        foreach (Collider col in colliders)
        {
            //if found interactable
            Interactable interactable = col.GetComponentInParent<Interactable>();
            if (interactable)
            {
                //add to list
                listInteractables.Add(interactable);
            }
        }

        //only if found something
        if (listInteractables.Count > 0)
        {
            //find nearest
            return FindNearest(listInteractables, stateMachine.transform.position);
        }

        return null;
    }

    /// <summary>
    /// Find nearest to position
    /// </summary>
    Interactable FindNearest(List<Interactable> list, Vector3 position)
    {
        Interactable nearest = null;
        float distance = Mathf.Infinity;

        //foreach element in the list
        foreach (Interactable element in list)
        {
            //check distance to find nearest
            float newDistance = Vector3.Distance(element.transform.position, position);
            if (newDistance < distance)
            {
                distance = newDistance;
                nearest = element;
            }
        }

        return nearest;
    }

    protected virtual void Interact()
    {
        Interactable interactable = FindInteractable();

        //if create rope
        if (interactable && interactable.CreateRope())
        {
            ConnectToInteractable(interactable);
        }
    }

    protected virtual void RewindRope()
    {
        Interactable interactable = FindInteractable();

        //if rewind rope
        if (interactable && interactable.RewindRope())
        {
            ConnectToInteractable(interactable);
        }
    }

    protected virtual void DetachRope()
    {
        Interactable interactable = FindInteractable();

        //if detach rope
        if (interactable && interactable.DetachRope())
        {
            ConnectToInteractable(interactable);
        }
    }

    void ConnectToInteractable(Interactable interactable)
    {
        //connect to interactable
        Player player = stateMachine as Player;
        player.connectedPoint = interactable;

        //change state to dragging rope
        player.SetState(player.draggingRopeState);
    }

    #endregion
}