using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using System.Collections;
using Unity.VisualScripting;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt dungeonSize;
    [SerializeField] private int wallMargin = 1;

    [SerializeField] private bool isCoroutine;

    [SerializeField] private int minRoomSize;
    [SerializeField] private int splitDepth;

    [SerializeField]private List<RectInt> rooms = new List<RectInt>();
    private List<RectInt> displayRooms = new List<RectInt>();

    private bool isSplittingHorizontal = true;

    [ReadOnly]private int maxRoomGenerations;
    [ReadOnly]private int roomCounter = 0;

    private int a = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        maxRoomGenerations = (int)Mathf.Pow(2, splitDepth);

        Debug.Log(maxRoomGenerations);

        SplitRoom1(dungeonSize);

        if (isCoroutine)
        {
            StartCoroutine(DisplayRooms());
        }
        else
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                displayRooms.Add(rooms[i]);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //main dungeon size
        AlgorithmsUtils.DebugRectInt(dungeonSize, Color.red);

        for (int i = 0; i < displayRooms.Count; i++)
        {
            AlgorithmsUtils.DebugRectInt(displayRooms[i], Color.blue);
        }
    }
    IEnumerator DisplayRooms()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            yield return new WaitForSeconds(0.5f);
            displayRooms.Add(rooms[i]);
        }
    }


    private void SplitRoom1(RectInt room)
    {
        Queue<RectInt> Q = new();
        HashSet<RectInt> Discovered = new HashSet<RectInt>();

        Q.Enqueue(room);

        while (maxRoomGenerations != roomCounter && Q.Count != 0) 
        {
            RectInt currentRoom = Q.Dequeue();
            Discovered.Add(currentRoom);

            if (CanGenerateRoom(currentRoom))
            {
                RectInt[] splitRooms = CreateRooms(currentRoom);

                for (int i = 0; i < splitRooms.Length; i++)
                {
                    Q.Enqueue(splitRooms[i]);
                    rooms.Add(splitRooms[i]);
                    roomCounter++;
                }
            }
            else
            {
                roomCounter += 2;
            }

            Debug.Log(CanGenerateRoom(currentRoom));

            //room can't split vertically. Then never checks horizontal. leaving long hallways.
        }
    }

    public bool CanGenerateRoom(RectInt currentRoom)
    {
        if (roomCounter == a * 2)
        {
            a = roomCounter;
            roomCounter = 0;
            isSplittingHorizontal = !isSplittingHorizontal;
        }

        if (isSplittingHorizontal)
        {
            int maxHeight = currentRoom.height - minRoomSize;

            if (maxHeight > minRoomSize)
            {
                return true;
            }
        }
        else
        {
            int maxWidth = currentRoom.width - minRoomSize;

            if (maxWidth > minRoomSize)
            {
                return true;
            }
        }
        
        return false;
    }

    public RectInt[] CreateRooms(RectInt currentRoom)
    {
        int splitHeight = 0;
        int splitLength = 0;

        Vector2Int roomSize1 = Vector2Int.zero;
        Vector2Int roomSize2 = Vector2Int.zero;

        RectInt[] nextRoomArray = new RectInt[2];

        

        if (isSplittingHorizontal)
        {
            splitHeight = Random.Range(minRoomSize, currentRoom.height - minRoomSize);

            roomSize1 = new Vector2Int(currentRoom.width, splitHeight);
            roomSize2 = new Vector2Int(currentRoom.width, currentRoom.height - splitHeight);

            //splitA
            nextRoomArray[0] = new RectInt(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + wallMargin);
            //top right
            nextRoomArray[1] = new RectInt(currentRoom.x, roomSize1.y + currentRoom.y, roomSize2.x, roomSize2.y);
        }
        else
        {
            splitLength = Random.Range(minRoomSize, currentRoom.width - minRoomSize);

            roomSize1 = new Vector2Int(splitLength, currentRoom.height);
            roomSize2 = new Vector2Int(currentRoom.width - splitLength, currentRoom.height);

            //splitA
            nextRoomArray[0] = new RectInt(currentRoom.x, currentRoom.y, roomSize1.x + wallMargin, roomSize1.y);
            //top right
            nextRoomArray[1] = new RectInt(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        return nextRoomArray;
    }
}
