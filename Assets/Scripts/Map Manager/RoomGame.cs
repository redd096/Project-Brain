﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using redd096;

public class RoomGame : Room
{
    [Header("Camera Position")]
    public Transform cameraPosition = default;
    public float timeToMoveCamera = 1;

    [Header("This room alternatives")]
    [SerializeField] float precisionPosition = 0.1f;
    [SerializeField] List<RoomGame> roomAlternatives = new List<RoomGame>();

    Transform cam;
    RoomGame instantiatedRoom;

    private void OnEnable()
    {
        if (cameraPosition == null)
        {
            Debug.LogWarning($"Manca la camera position nella camera {gameObject.name}");
            return;
        }

        //get cam if null
        if (cam == null)
            cam = Camera.main.transform;

        //set cam position and rotation
        cam.position = cameraPosition.position;
        cam.rotation = cameraPosition.rotation;
    }

    public override IEnumerator EndRoom()
    {
        //by default instantiated room is this one (can change on RegenRoom)
        instantiatedRoom = this;

        //foreach alternative
        foreach (RoomGame alternative in roomAlternatives)
        {
            //find one with same doors
            if (SameDoors(alternative.doors))
            {
                instantiatedRoom = RegenRoom(alternative);
                break;
            }
        }

        //wait next frame (so room is already instatiated)
        yield return null;

        //connect doors between rooms
        instantiatedRoom.ConnectDoors();

        //and destroy this room
        Destroy(gameObject);
    }

    #region select alternative

    bool SameDoors(List<DoorStruct> alternativeDoors)
    {
        //do only if same number of doors
        if (alternativeDoors.Count != usedDoors.Count)
            return false;

        //copy used doors
        List<DoorStruct> doorsToCheck = new List<DoorStruct>(usedDoors);

        //foreach alternative door, check if there is the same door in doorsToCheck
        foreach(DoorStruct alternativeDoor in alternativeDoors)
        {
            foreach(DoorStruct door in doorsToCheck)
            {
                if(Vector3.Distance(alternativeDoor.doorTransform.localPosition, door.doorTransform.localPosition) < precisionPosition &&       //check door transform has same local position
                    alternativeDoor.direction == door.direction &&                                                                              //check same direction
                    alternativeDoor.typeOfDoor == door.typeOfDoor)                                                                              //check same type
                {
                    //remove from doorsToCheck and go to next alternativeDoor
                    doorsToCheck.Remove(door);
                    break;
                }
            }
        }

        //if no doors to check, all the doors are the same
        return doorsToCheck.Count <= 0;
    }

    RoomGame RegenRoom(RoomGame roomPrefab)
    {
        //instantiate new room
        RoomGame room = Instantiate(roomPrefab, transform.parent);
        room.transform.position = transform.position;
        room.transform.rotation = transform.rotation;

        //register room (no set adjacent room and so on, cause also other rooms will be destroyed)
        room.Register(id, teleported);

        return room;
    }

    #endregion

    void ConnectDoors()
    {
        //foreach door struct do overlap and get activable doors
        foreach (DoorStruct door in doors)
        {
            Collider[] colliders = Physics.OverlapSphere(door.doorTransform.position, 1.5f);
            List<Door> activableDoors = new List<Door>();

            foreach (Collider col in colliders)
            {
                Door activableDoor = col.GetComponentInParent<Door>();
                if (activableDoor && activableDoors.Contains(activableDoor) == false)       //be sure is not already in the list
                {
                    activableDoors.Add(activableDoor);
                }
            }

            //save connections in every activable door
            foreach (Door activableDoor in activableDoors)
            {
                activableDoor.AddConnectedDoors(activableDoors);
            }
        }
    }
}
