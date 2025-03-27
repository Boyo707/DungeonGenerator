using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using System.Collections;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Sizes")]
    [SerializeField] private RectInt dungeonSize;
    [SerializeField] private int splitDepth;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int wallMargin = 1;
    [SerializeField] private int doorSize = 2;

    [Header("Generation Settings")]
    [SerializeField] private bool canGenerateOnStart = true;
    [SerializeField] private float pauseTime;
    [SerializeField] private bool showCreated;
    [SerializeField] private bool showCompleted;
    [SerializeField] private bool showDoors;

    [Header("Debug")]
    [SerializeField] private List<RectInt> debugRooms = new List<RectInt>(2);
    [SerializeField] private List<RectInt> completedRooms = new();
    [SerializeField] private List<RectInt> doors = new();
    [SerializeField] private List<RectInt> createdRooms = new();

    Queue<RectInt> Q = new();
    HashSet<RectInt> discovered = new();

    private Dictionary<RectInt, int> roomData = new();

    private List<Color> drawColors = new();
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

        if (canGenerateOnStart)
        {
            StartCoroutine(Generation(dungeonSize));
        }

    }

    // Update is called once per frame
    void Update()
    {
        b = a * 2 - roomDeduction;
        //main dungeon size
        AlgorithmsUtils.DebugRectInt(dungeonSize, Color.red);


        if (showCreated)
        {
            for (int i = 0; i < createdRooms.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdRooms[i], drawColors[i]);
            }
        }
        if (showCompleted)
        {
            for (int i = 0; i < completedRooms.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(completedRooms[i], Color.blue);
            }
        }

        if (showDoors)
        {
            for (int i = 0; i < doors.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(doors[i], Color.yellow);
            }
        }
        
        if (!canGenerateOnStart)
        {
            AlgorithmsUtils.DebugRectInt(debugRooms[0], Color.blue);

            for (int i = 1; i < debugRooms.Count; i++)
            {

                AlgorithmsUtils.DebugRectInt(debugRooms[i], Color.red);
                if (debugRooms[0].Overlaps(debugRooms[i]))
                {
                    AlgorithmsUtils.DebugRectInt(AddDoors(debugRooms[0], debugRooms[i]), Color.yellow);
                    if (debugRooms[0].Overlaps(debugRooms[1]))
                    {
                        Debug.Log("Overlapping");
                    }
                }
            }
        }
    }

    IEnumerator Generation(RectInt room)
    {
        Q.Enqueue(room);

        while (currentDepth != splitDepth && Q.Count != 0) 
        {
            RectInt currentRoom = Q.Dequeue();

            if (!discovered.Contains(currentRoom) && CanSplitRoom(currentRoom))
            {
                discovered.Add(currentRoom);

                RectInt[] splitRooms = SplitRoom(currentRoom);

                if (completedRooms.Contains(currentRoom))
                {
                    completedRooms.Remove(currentRoom);
                }

                for (int i = 0; i < splitRooms.Length; i++)
                {
                    Q.Enqueue(splitRooms[i]);
                    roomData.Add(splitRooms[i],currentDepth);
                    createdRooms.Add(splitRooms[i]);

                    completedRooms.Add(splitRooms[i]);

                    drawColors.Add(depthColor);
                    roomCounter++;

                    yield return new WaitForSeconds(pauseTime);

                }
            }
            else
            {
                roomCounter += 2;
            }
        }

        //add the rooms that havent been split yet into the completed rooms.


        //CURRENT PROBLEM IS THAT IT DOESNT KEEP TRAC WHICH ROOMS HAVE CREATED DOORS
        //can be solved with saving the indexes in a vector 2 and then comparing the reverse of one. If its included dont run
        Debug.Log(completedRooms.Count);

        List<Vector2Int> createdIndexes = new(); 

        for (int i = 0; i < completedRooms.Count; i++)
        {
            for (int j = 0; j < completedRooms.Count; j++)
            {
                if (completedRooms[i] != completedRooms[j] && completedRooms[i].Overlaps(completedRooms[j]))
                {
                    if(!createdIndexes.Contains(new Vector2Int(j, i)))
                    {
                        createdIndexes.Add(new Vector2Int(i, j));
                        RectInt door = AddDoors(completedRooms[i], completedRooms[j]);
                        Debug.Log($"current room index {i} has overlapped with index{j}");
                        
                        if (door != RectInt.zero)
                        {
                            doors.Add(door);
                            Debug.Log(doors.Count);
                            yield return new WaitForSeconds(0);
                        }
                    }
                    
                }
            }

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
                        //Debug.Log($"Re qued {currentRoom} Horizontal split");
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
                        //Debug.Log($"Re qued {currentRoom} Vertical split");
                        discovered.Remove(currentRoom);
                        Q.Enqueue(currentRoom);
                        return false;
                    }
                }
            }
            checkSecondSplit = true;
            currentSplit = !currentSplit;
        }

        

        //Debug.Log($"Room {currentRoom} couldnt split further");
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

    public RectInt AddDoors(RectInt currentRoom, RectInt overlappingRoom)
    {

        Debug.Log("is overlapping");

        //if the x is higher then its on the right, if its lower then its on the right.
        //if the y is higher then its on the top, if its lower then its on the bottom.
        RectInt door = RectInt.zero;

        //if not gone its on the right, if its gone on the left
        RectInt right = new RectInt(overlappingRoom.x - wallMargin, overlappingRoom.y, overlappingRoom.width, overlappingRoom.height);
        RectInt left = new RectInt(overlappingRoom.x + wallMargin, overlappingRoom.y, overlappingRoom.width, overlappingRoom.height);

        bool isRight = false;
        bool isLeft = false;

        if (currentRoom.Overlaps(right))
        {
            Debug.Log("right");
            isRight = true;
            door = new RectInt(currentRoom.x + currentRoom.width - wallMargin, currentRoom.y, wallMargin, doorSize);

        }
        if (currentRoom.Overlaps(left))
        {
            Debug.Log("left");
            isLeft = true;
            door = new RectInt(currentRoom.x, overlappingRoom.y, wallMargin, doorSize);

        }
        if (isRight && isLeft)
        {
            RectInt top = new RectInt(overlappingRoom.x, overlappingRoom.y - wallMargin, overlappingRoom.width, overlappingRoom.height);
            RectInt bottom = new RectInt(overlappingRoom.x, overlappingRoom.y + wallMargin, overlappingRoom.width, overlappingRoom.height);

            int minX = currentRoom.x > overlappingRoom.x ? currentRoom.x : overlappingRoom.x;

            int widthA = currentRoom.x + currentRoom.width;
            int widthB = overlappingRoom.x + overlappingRoom.width;

            int maxX = widthA < widthB ? widthA : widthB;

            if ((maxX + wallMargin) - (minX + wallMargin) >= wallMargin * 2 + doorSize)
            {
                minX += wallMargin;
                maxX -= wallMargin + 1;
            }
            else
            {
                return RectInt.zero;
            }

            int randomX = Random.Range(minX, maxX);

            if (currentRoom.Overlaps(top))
            {
                //Debug.Log("Top");
                return door = new RectInt(randomX, currentRoom.height + currentRoom.y - wallMargin, doorSize, wallMargin);
            }
            else if (currentRoom.Overlaps(bottom))
            {
                //Debug.Log("Bottom");
                return door = new RectInt(randomX, currentRoom.y, doorSize, wallMargin);
            }
        }
        else
        {
            int minY = currentRoom.y > overlappingRoom.y ? currentRoom.y : overlappingRoom.y;

            int heightA = currentRoom.y + currentRoom.height;
            int heightB = overlappingRoom.y + overlappingRoom.height;

            int maxY = heightA < heightB ? heightA : heightB;

            if ((maxY + wallMargin) - (minY + wallMargin) >= wallMargin * 2 + doorSize)
            {
                minY += wallMargin;
                maxY -= wallMargin + 1;
            }
            else
            {
                return RectInt.zero;
            }

            int randomY = Random.Range(minY, maxY);

            door = new RectInt(door.x, randomY, door.width, door.height);

            return door;
        }
        return RectInt.zero;
    }

    public void DoorPath()
    {

    }
}
