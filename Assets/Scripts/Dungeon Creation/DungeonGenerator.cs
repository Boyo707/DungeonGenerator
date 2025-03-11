using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using System.Collections;
using UnityEngine.InputSystem.iOS.LowLevel;

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
    [ReadOnly]private int currentRoomGenerations = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        maxRoomGenerations = (int)Mathf.Pow(2, splitDepth);
        //SplitRoom(dungeonSize);

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

        Debug.Log(rooms.Count);


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

        while (maxRoomGenerations > currentRoomGenerations - 2) 
        {
            RectInt currentRoom = Q.Dequeue();
            Discovered.Add(currentRoom);

            RectInt[] splitRooms = CreateRooms(currentRoom);

            for (int i = 0;i < splitRooms.Length; i++)
            {
                Q.Enqueue(splitRooms[i]);
                rooms.Add(splitRooms[i]);
                currentRoomGenerations++;
            }
            Debug.Log(currentRoomGenerations);
            //Think of where and how to place the split horizontal bool
        }
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


            if (splitHeight < minRoomSize)
            {
                Debug.Log("MADE TO SMALL H");
            }

            roomSize1 = new Vector2Int(currentRoom.width, splitHeight);
            roomSize2 = new Vector2Int(currentRoom.width, currentRoom.height - splitHeight);

            //splitA
            nextRoomArray[0] = new RectInt(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + wallMargin);
            //top right
            nextRoomArray[1] = new RectInt(currentRoom.x, roomSize1.y + currentRoom.y, roomSize2.x, roomSize2.y);

            Debug.Log("Horizontal");
        }
        else
        {
            splitLength = Random.Range(minRoomSize, currentRoom.width - minRoomSize);

            if (splitLength < minRoomSize)
            {
                Debug.Log("MADE TO SMALL V");
            }


            roomSize1 = new Vector2Int(splitLength, currentRoom.height);
            roomSize2 = new Vector2Int(currentRoom.width - splitLength, currentRoom.height);

            //splitA
            nextRoomArray[0] = new RectInt(currentRoom.x, currentRoom.y, roomSize1.x + wallMargin, roomSize1.y);
            //top right
            nextRoomArray[1] = new RectInt(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);

            Debug.Log("vertical");
        }
        return nextRoomArray;
    }
}
