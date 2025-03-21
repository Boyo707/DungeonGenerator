using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using System.Collections;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt dungeonSize;
    [SerializeField] private int wallMargin = 1;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int splitDepth;

    [SerializeField] private float pauseTime;

    Queue<RectInt> Q = new();
    HashSet<RectInt> discovered = new HashSet<RectInt>();

    private List<RectInt> rooms = new List<RectInt>();
    private List<Color> drawColors = new List<Color>();
    private Color depthColor = Color.red;


    private bool isSplittingHorizontal = true;

    private int currentDepth;
    private int roomCounter = 0;

    private int a = 0;
    private int roomDeduction = 0;


    private int b;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        minRoomSize += wallMargin;

        StartCoroutine(RoomGeneration(dungeonSize));

    }

    // Update is called once per frame
    void Update()
    {
        b = a * 2 - roomDeduction;
        //main dungeon size
        AlgorithmsUtils.DebugRectInt(dungeonSize, Color.red);

        for (int i = 0; i < rooms.Count; i++)
        {
            AlgorithmsUtils.DebugRectInt(rooms[i], drawColors[i]);
        }
    }

    IEnumerator RoomGeneration(RectInt room)
    {
        Q.Enqueue(room);

        while (currentDepth != splitDepth && Q.Count != 0) 
        {
            RectInt currentRoom = Q.Dequeue();

            if (!discovered.Contains(currentRoom) && CanSplitRoom(currentRoom))
            {
                discovered.Add(currentRoom);

                RectInt[] splitRooms = SplitRoom(currentRoom);

                for (int i = 0; i < splitRooms.Length; i++)
                {
                    yield return new WaitForSeconds(pauseTime);
                    Q.Enqueue(splitRooms[i]);
                    rooms.Add(splitRooms[i]);
                    drawColors.Add(depthColor);
                    roomCounter++;
                }
            }
            else
            {
                roomCounter += 2;
            }
            //room can't split vertically. Then never checks horizontal. leaving long hallways.
        }
    }

    public bool CanSplitRoom(RectInt currentRoom)
    {
        //Splits the room if it hit the end of the next level.
        if (roomCounter + a == 0)
        {
            a = 1;
        }
        else if (roomCounter >= a * 2 - roomDeduction)
        {
            a = roomCounter;
            roomCounter = 0;
            roomDeduction = 0;
            currentDepth++;

            isSplittingHorizontal = !isSplittingHorizontal;

            depthColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        if(currentDepth == splitDepth)
        {
            return false;
        }
        
        bool checkSecondSplit = false;
        bool currentSplit = isSplittingHorizontal;

        for (int i = 0; i < 2; i++)
        {
            if (currentSplit)
            {
                int maxHeight = currentRoom.height - minRoomSize;

                if (maxHeight > minRoomSize)
                {
                    if (!checkSecondSplit)
                    {
                        return true;
                    }
                    else
                    {
                        Debug.Log($"Re qued {currentRoom} Horizontal split");
                        discovered.Remove(currentRoom);
                        Q.Enqueue(currentRoom);
                        return false;
                    }
                }
            }
            else
            {
                int maxWidth = currentRoom.width - minRoomSize;

                if (maxWidth > minRoomSize)
                {
                    if (!checkSecondSplit)
                    {
                        return true;
                    }
                    else
                    {
                        Debug.Log($"Re qued {currentRoom} Vertical split");
                        discovered.Remove(currentRoom);
                        Q.Enqueue(currentRoom);
                        return false;
                    }
                }
            }
            checkSecondSplit = true;
            currentSplit = !currentSplit;
        }

        Debug.Log($"Room {currentRoom} couldnt split further");
        roomDeduction += 2;
        return false;
    }

    public RectInt[] SplitRoom(RectInt currentRoom)
    {
        RectInt[] nextRoomArray = new RectInt[2];

        if (isSplittingHorizontal)
        {
            int splitHeight = Random.Range(minRoomSize, currentRoom.height - minRoomSize);

            Vector2Int roomSize1 = new Vector2Int(currentRoom.width, splitHeight);
            Vector2Int roomSize2 = new Vector2Int(currentRoom.width, currentRoom.height - splitHeight);

            //splitA
            nextRoomArray[0] = new RectInt(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + wallMargin);
            //top right
            nextRoomArray[1] = new RectInt(currentRoom.x, roomSize1.y + currentRoom.y, roomSize2.x, roomSize2.y);
        }
        else
        {
            int splitLength = Random.Range(minRoomSize, currentRoom.width - minRoomSize);

            Vector2Int roomSize1 = new Vector2Int(splitLength, currentRoom.height);
            Vector2Int roomSize2 = new Vector2Int(currentRoom.width - splitLength, currentRoom.height);

            //splitA
            nextRoomArray[0] = new RectInt(currentRoom.x, currentRoom.y, roomSize1.x + wallMargin, roomSize1.y);
            //top right
            nextRoomArray[1] = new RectInt(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        return nextRoomArray;
    }

    public void AddDoors()
    {

    }

    public void DoorPath()
    {

    }
}
