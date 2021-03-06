﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StructFixedRooms
{
    [Min(0)] public int minID;
    [Min(0)] public int maxID;
    public Room roomPrefab;
}

[AddComponentMenu("Project Brain/Managers/Map Manager")]
public class MapManager : MonoBehaviour
{
    [Header("Regen")]
    [SerializeField] bool regenOnPlay = true;
    [Min(1)] [SerializeField] int numberRooms = 12;

    [Header("Attempts")]
    [Min(1)] [SerializeField] int maxAttempts = 5;
    [Min(1)] [SerializeField] int roomsPerAttempt = 5;
    [Min(1)] [SerializeField] int doorsPerAttempt = 2;

    [Header("Prefabs")]
    [SerializeField] Room[] roomPrefabs = default;

    [Header("Fixed Rooms")]
    [SerializeField] StructFixedRooms[] fixedRooms = default;

    //rooms
    List<Room> rooms = new List<Room>();
    private int roomID;
    private Room lastRoom;
    private bool succeded;
    private bool teleported;

    //for fixed rooms
    List<StructFixedRooms> fixedRoomsAlreadyChecked = new List<StructFixedRooms>();
    StructFixedRooms roomTryingToPut;
    List<StructFixedRooms> fixedRoomsAlreadyPut = new List<StructFixedRooms>();

    void Start()
    {
        //regen map at start
        if (regenOnPlay)
        {
            DestroyMap();
            StartCoroutine(CreateMap());
        }
    }

    #region publi API

    public void DestroyMap()
    {
        //remove every child
        foreach (Transform child in transform)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                Destroy(child.gameObject);
            else
                UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }

        //clear dictionary
        rooms.Clear();

        //clear vars
        fixedRoomsAlreadyChecked.Clear();
        roomTryingToPut = null;
        fixedRoomsAlreadyPut.Clear();
    }

    public void CreateNewMap()
    {
        OnValidateCreateMap();
    }

    #endregion

    #region regen map

    IEnumerator CreateMap()
    {
        int attempts = 0;

        while (rooms.Count < numberRooms && attempts < maxAttempts)
        {
            //get room prefab, if can't destroy dungeon and recreate (failed to put some room)
            Room newRoomPrefab = GetRoomPrefab();
            if (newRoomPrefab == null)
                break;

            //generate room
            Room newRoom = Instantiate(newRoomPrefab, transform);

            //try to positionate
            yield return PositionRoom(newRoom);

            //if succeded, register it
            if (succeded)
            {
                RegisterRoom(newRoom);
                attempts = 0;
            }
            //else, destroy and try new one
            else
            {
                Destroy(newRoom.gameObject);
                Debug.Log("<color=orange>Shitty room, make another one</color>");
                attempts++;
            }
        }

        //if not reach number rooms, regen
        if (rooms.Count < numberRooms)
        {
            //destroy old and create new one
            Debug.Log("<color=red>Shitty map, cry and restart</color>");
            DestroyMap();
            roomID = 0;
            StartCoroutine(CreateMap());
        }
        else
        {
            Debug.Log("<color=cyan>Mission complete!</color>");
        }
    }

    private IEnumerator PositionRoom(Room newRoom)
    {
        succeded = false;
        teleported = false;

        if (rooms.Count == 0)
        {
            //first room
            Debug.Log("<color=lime>positioned first room</color>");
            newRoom.SetPosition(Vector3.zero);
            succeded = true;
            yield break;
        }
        else
        {
            //try attach to last room
            lastRoom = rooms[rooms.Count - 1];

            for(int roomLoop = 0; roomLoop < roomsPerAttempt; roomLoop++)
            {
                for(int doorLoop = 0; doorLoop < doorsPerAttempt; doorLoop++)
                {
                    //try attach to door
                    if (newRoom.SetPosition(lastRoom.GetRandomDoor(), lastRoom) && newRoom.CheckCanPlace(rooms))
                    {
                        Debug.Log("<color=lime>positioned room " + roomID + "</color>");
                        succeded = true;
                        yield break;
                    }

                    //else try another door
                    yield return null;
                }

                //else try another room
                Debug.Log("<color=yellow>changed room</color>");
                teleported = true;                                      //set teleported true
                lastRoom = rooms[Random.Range(0, rooms.Count - 1)];
            }
        }
    }

    Room GetRoomPrefab()
    {
        //quando istanzi, controlla se ci sono stanze fisse
        //se non si attacca, vedere se ce ne sono altre
        //se una stanza non viene creata nel suo range, distruggere e ricreare il dungeon

        //quando istanzi, controlla se ci sono stanze fisse
        //aggiungere alla lista delle già checkate, così se non si attacca ne viene provata un'altra
        //se viene posizionata pulisci le già checkate per riprovarle tutte, ma aggiungi quella creata ad un'altra lista per non riprovarla
        //se non viene posizionata ma ha il range più alto, si può riprovare al posizionamento successivo, ma assicurarsi di resettare la variabile se si posiziona una stanza non fissa
        //se una stanza non viene creata nel suo range, distruggere e ricreare il dungeon

        //if a room failed at its maxID, recreate dungeon
        if (roomTryingToPut != null)
        {
            if (roomID >= roomTryingToPut.maxID)
            {
                return null;
            }
        }

        //check fixed rooms
        foreach (StructFixedRooms checkRoom in fixedRooms)
        {
            //be sure is not already checked
            if (fixedRoomsAlreadyChecked.Contains(checkRoom) || fixedRoomsAlreadyPut.Contains(checkRoom))
            {
                continue;
            }

            //if there is one who need this ID
            if(roomID >= checkRoom.minID && roomID <= checkRoom.maxID)
            {
                //add to list already check
                fixedRoomsAlreadyChecked.Add(checkRoom);

                //return it
                roomTryingToPut = checkRoom;
                return checkRoom.roomPrefab;
            }
        }

        //else return random room
        roomTryingToPut = null;
        return roomPrefabs[Random.Range(0, roomPrefabs.Length)];
    }

    private void RegisterRoom(Room newRoom)
    {
        //add to list and update ID
        rooms.Add(newRoom);
        newRoom.Init(roomID, teleported);
        roomID++;

        //update fixed rooms
        UpdateFixedRooms();
    }

    void UpdateFixedRooms()
    {       
        //clear rooms already checked 
        fixedRoomsAlreadyChecked.Clear();

        //and add to list already put
        if (roomTryingToPut != null)
        {
            fixedRoomsAlreadyPut.Add(roomTryingToPut);
            roomTryingToPut = null;
        }
    }

    #endregion

    #region editor 

    void OnValidateCreateMap()
    {
        int roomID = 0;
        Room currentRoom = null;
        Room lastRoom = null;

        int loopCount = 0;

        while (rooms.Count < numberRooms)
        {
            //generate first room
            if (rooms.Count <= 0)
            {
                //if generate room, be sure to have loop count at 0
                if (GenerateFirstRoom(ref currentRoom, ref roomID, ref lastRoom))
                {
                    currentRoom = null;
                    loopCount = 0;
                }
                //if no generate, increase loopCount
                else
                {
                    loopCount++;
                }
            }
            //generate other rooms
            else
            {
                bool failedToCreateDungeon;

                //if generate room, be sure to have loop count at 0
                if (GenerateOtherRooms(ref currentRoom, ref roomID, ref lastRoom, loopCount > 20, out failedToCreateDungeon))
                {
                    currentRoom = null;
                    loopCount = 0;
                }
                //if no generate, increase loopCount
                else
                {
                    loopCount++;
                }

                //if failed to create dungeon, increase loop count to end dungeon generation
                if (failedToCreateDungeon)
                    loopCount = 1000;
            }

            //if we are in endless loop (no space for a room)
            if (loopCount > 20)
            {
                //try to put room adjacent to another room in the list (instead of last one created)
                lastRoom = rooms[Random.Range(0, rooms.Count)];
            }

            //if continue loop, maybe this room can't attach to others, destroy and try new one
            if (loopCount > 50)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                    Destroy(currentRoom.gameObject);
                else
                    DestroyImmediate(currentRoom.gameObject);
#else
                Destroy(currentRoom.gameObject);
#endif
            }

            //if loop count is too big, break while
            if (loopCount > 100)
            {
                Debug.Log("<color=yellow>Stopped an endless loop</color>");
                break;
            }
        }

        if (rooms.Count >= numberRooms)
        {
            Debug.Log("<color=cyan>Mission complete!</color>");
        }
    }

    bool GenerateFirstRoom(ref Room currentRoom, ref int roomID, ref Room lastRoom)
    {
        //get prefab, if failed, return false to increase loopCount
        Room prefabToCreate = GetRoomPrefab();
        if (prefabToCreate == null)
        {
            return false;
        }

        //instantiate room (child of this transform) and initialize
        currentRoom = Instantiate(prefabToCreate, transform);
        currentRoom.Init(roomID, false);
        currentRoom.SetPosition(Vector3.zero);

        //add to list and update ID and last room
        rooms.Add(currentRoom);
        lastRoom = currentRoom;
        roomID++;

        UpdateFixedRooms();
        return true;
    }

    bool GenerateOtherRooms(ref Room currentRoom, ref int roomID, ref Room lastRoom, bool teleported, out bool failedToCreateDungeon)
    {
        failedToCreateDungeon = false;

        //get random direction by last room
        DoorStruct door = lastRoom.GetRandomDoor();

        //instantiate room (only if != null, cause can be just a teleport of current room)
        if (currentRoom == null)
        {
            //get prefab, if failed, return false to increase loopCount
            Room prefabToCreate = GetRoomPrefab();
            if(prefabToCreate == null)
            {
                failedToCreateDungeon = true;
                return false;
            }

            currentRoom = Instantiate(prefabToCreate, transform);
        }

        //initialize and set position
        currentRoom.Init(roomID, teleported);
        if (currentRoom.SetPosition(door, lastRoom))    //if can't set position is because there are not doors which attach
        {
            //check if there is space for this room
            if (currentRoom.CheckCanPlace(rooms))
            {
                //add to list and update ID and last room
                rooms.Add(currentRoom);
                lastRoom = currentRoom;
                roomID++;

                UpdateFixedRooms();
                return true;
            }
        }

        return false;
    }

    #endregion
}
