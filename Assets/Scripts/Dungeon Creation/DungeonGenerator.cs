using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using System.Collections;
using System.Net.Mail;
using UnityEditor.Experimental.GraphView;

public enum SortingType
{
    Size,
    Position
}

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    [SerializeField] private RectInt totalDungeonSize;
    [SerializeField] private int splitDepth;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int wallHeight = 3;
    [SerializeField] private int wallMargin = 1;
    [SerializeField] private int doorWidth = 2;

    [Header("Generation Settings")]
    [SerializeField] private int dungeonSeed;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private float pauseTime;
    [SerializeField] private bool showRooms;
    [SerializeField] private bool showDoors;

    [Header("Debug")]
    [SerializeField] private List<RectInt> doors = new();
    [SerializeField] private List<RectInt> createdRooms = new();
    //key = door, value is connection
    [SerializeField] private Dictionary<RectInt, List<RectInt>> doorConnections= new();


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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(totalDungeonSize.xMin);
        Debug.Log(totalDungeonSize.xMax);
        if(dungeonSeed != 0)
        {
            Random.InitState(dungeonSeed);
        }
        else
        {
            dungeonSeed = Random.seed;
            Debug.Log(Random.seed);
        }

        minRoomSize += wallMargin;
        
        if (generateOnStart)
        {
            StartCoroutine(Generation(totalDungeonSize));
        }

    }

    // Update is called once per frame
    void Update()
    {
        //main dungeon size
        AlgorithmsUtils.DebugRectInt(totalDungeonSize, Color.red);


        if (showRooms)
        {
            for (int i = 0; i < createdRooms.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdRooms[i], drawColors[i]);
            }
        }

        if (showDoors)
        {
            for (int i = 0; i < doors.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(doors[i], Color.yellow);
            }
        }
        
        if (!generateOnStart)
        {
            //Instant gen
        }
    }

    IEnumerator Generation(RectInt room)
    {
        #region Splitting
        //rooms splitting
        Q.Enqueue(room);

        while (currentDepth != splitDepth && Q.Count != 0) 
        {
            RectInt currentRoom = Q.Dequeue();

            if (!discovered.Contains(currentRoom) && CanSplitRoom(currentRoom))
            {
                discovered.Add(currentRoom);

                RectInt[] splitRooms = SplitRoom(currentRoom);

                if (createdRooms.Contains(currentRoom))
                {
                    int index = createdRooms.IndexOf(currentRoom);
                    createdRooms.Remove(currentRoom);
                }

                for (int i = 0; i < splitRooms.Length; i++)
                {
                    Q.Enqueue(splitRooms[i]);
                    roomData.Add(splitRooms[i],currentDepth);
                    createdRooms.Add(splitRooms[i]);

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
        #endregion

        #region Remove 10%
        //remove 10% of the smallest rooms
        BubbleSorter(createdRooms, SortingType.Size);

        int amountOfRooms = Mathf.RoundToInt(0.10f * createdRooms.Count);

        for (int i = 0; i < amountOfRooms; i++)
        {
            createdRooms.RemoveAt(i);
        }
        #endregion

        BubbleSorter(createdRooms, SortingType.Position);


        Stack<RectInt> stackRooms = new();
        Stack<RectInt> stackDoors = new();
        HashSet<RectInt> Discovered = new HashSet<RectInt>();

        RectInt[] previousRooms = new RectInt[2];

        stackRooms.Push(createdRooms[0]);

        while (stackRooms.Count > 0)
        {
            RectInt current = stackRooms.Pop();

            if (!Discovered.Contains(current))
            {
                Discovered.Add(current);

                if(stackDoors.Count > 0)
                {
                    RectInt currentDoor = stackDoors.Pop();
                    doors.Add(currentDoor);

                    AddDoorConnections(currentDoor, previousRooms);
                }

                foreach (RectInt node in createdRooms)
                {
                    if (current.Overlaps(node) && current != node && !Discovered.Contains(node))
                    {
                        RectInt door = AddDoors(current, node);

                        if (door != RectInt.zero)
                        {
                            stackDoors.Push(door);
                            stackRooms.Push(node);
                            previousRooms[0] = current;
                            previousRooms[1] = node;
                            yield return new WaitForSeconds(pauseTime);
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
                        discovered.Remove(currentRoom);
                        Q.Enqueue(currentRoom);
                        return false;
                    }
                }
            }
            checkSecondSplit = true;
            currentSplit = !currentSplit;
        }

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
    private void BubbleSorter(List<RectInt> rooms, SortingType type)
    {
        for (int i = rooms.Count - 2; i >= 0; i--)
        {
            for (int j = 0; j <= i; j++)
            {
                int sizeA = 0;
                int sizeB = 0;

                if (type == SortingType.Size)
                {
                    sizeA = rooms[j].width + rooms[j].height;
                    sizeB = rooms[j + 1].width + rooms[j + 1].height;
                }
                else if(type == SortingType.Position)
                {
                    sizeA = rooms[j].x + rooms[j].y;
                    sizeB = rooms[j + 1].x + rooms[j + 1].y;
                }
                
                if (sizeA > sizeB)
                {
                    RectInt temp = rooms[j + 1];
                    rooms[j + 1] = rooms[j];
                    rooms[j] = temp;
                }
            }
        }
    }
    public RectInt AddDoors(RectInt currentRoom, RectInt overlappingRoom)
    {
        RectInt wall = AlgorithmsUtils.Intersect(currentRoom, overlappingRoom);

        if (wall.width > wall.height)
        {
            //horizontal
            if (wall.width < doorWidth + wallMargin * 2)
            {
                return RectInt.zero;
            }

            wall = new RectInt(wall.x + wallMargin, wall.y, wall.width - wallMargin * 2, wall.height);

            int randomX = Random.Range(wall.xMin, wall.xMax - 1);

            return new RectInt(randomX, wall.y, doorWidth, wall.height);
        }
        else
        {
            //vertical
            if (wall.height < doorWidth + wallMargin * 2)
            {
                return RectInt.zero;
            }
            wall = new RectInt(wall.x , wall.y + wallMargin, wall.width , wall.height - wallMargin * 2);

            int randomY = Random.Range(wall.yMin, wall.yMax - 1);

            return new RectInt(wall.x, randomY, wall.width , doorWidth);
        }
    }
    private void AddDoorConnections(RectInt currentDoor, RectInt[] previousRooms)
    {
        if (!doorConnections.ContainsKey(currentDoor))
        {
            doorConnections[currentDoor] = new List<RectInt>();
        }

        doorConnections[currentDoor].Add(previousRooms[0]);
        doorConnections[currentDoor].Add(previousRooms[1]);
    }
}
