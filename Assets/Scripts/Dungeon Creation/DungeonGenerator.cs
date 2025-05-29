using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NaughtyAttributes;
using System.Text;
using Unity.AI.Navigation;

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
    [SerializeField] private bool instantGeneration = true;
    [SerializeField] private float stepDelay;
    [SerializeField] private bool showRoomOutline = true;
    [SerializeField] private bool showDoorsOutline = true;
    [SerializeField] private bool showConnectionGraph = true;
    [SerializeField] private bool showVisuals = true;
    

    [Header("Visuals")]
    [SerializeField] private Transform wallsParent; 
    [SerializeField] private Transform floorsParent;
    [SerializeField] private Vector3 offset;
    [SerializeField] private GameObject[] wallPrefabs;

    [Header("Playability")]
    private PathFinder pathFinder;

    [Header("Debug")]
    [SerializeField] private List<RectInt> createdRooms = new();
    [SerializeField] private List<RectInt> doors = new();
    [SerializeField] private List<RectInt> connections = new();

    private bool continueStep = false;
    [Button]
    public void NextStep()
    {
        continueStep = true;
    }

    private bool pauseGeneration = false;

    [Button]
    public void PauseGeneration()
    {
        pauseGeneration = !pauseGeneration;
    }

    //key = door, value is connection
    private Dictionary<RectInt, List<RectInt>> roomAdjacencyList = new();

    private Dictionary<RectInt, List<RectInt>> doorConnections = new();
    private Dictionary<RectInt, List<RectInt>> roomConnections = new();

    private List<Color> drawColors = new();
    private Color depthColor = Color.red;


    private bool isSplittingHorizontal = true;

    private int currentDepth;
    private int roomCounter = 0;

    private int a = 0;
    private int roomDeduction = 0;

    private int[,] tileMap;
    private int[,] binaryTileMap;

    public int[,] TileMap
    {
        get { return tileMap; }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        minRoomSize += wallMargin;

        Debug.Log(totalDungeonSize.xMin);
        Debug.Log(totalDungeonSize.xMax);
        if(dungeonSeed == 0)
        {
            dungeonSeed = Random.seed;
            Random.InitState(dungeonSeed);
        }

        Random.InitState(dungeonSeed);

        minRoomSize += wallMargin;
        
        StartCoroutine(Generation(totalDungeonSize));
    }

    // Update is called once per frame
    void Update()
    {
        //main dungeon size
        AlgorithmsUtils.DebugRectInt(totalDungeonSize, Color.red);


        if (showRoomOutline)
        {
            for (int i = 0; i < createdRooms.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdRooms[i], drawColors[i]);
            }
        }

        if (showDoorsOutline)
        {
            for (int i = 0; i < doors.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(doors[i], Color.yellow);
            }
        }

        if (showConnectionGraph)
        {
            foreach (var item in doorConnections)
            {
                Vector3 doorCenter = GetCenter(item.Key);
                DebugExtension.DebugWireSphere(doorCenter, Color.blue);

                var roomConnection = doorConnections[item.Key];

                for (int i = 0; i < roomConnection.Count; i++)
                {
                    Vector3 roomCenter = GetCenter(roomConnection[i]);
                    DebugExtension.DebugWireSphere(roomCenter, Color.green);

                    Debug.DrawLine(roomCenter, doorCenter);
                }
            }
        }

        wallsParent.gameObject.SetActive(showVisuals);
        floorsParent.gameObject.SetActive(showVisuals);
    }

    IEnumerator Generation(RectInt room)
    {
        #region Rooms Splitting
        //rooms splitting

        Queue<RectInt> Q = new();
        HashSet<RectInt> discovered = new();

        Q.Enqueue(room);

        while (currentDepth != splitDepth && Q.Count != 0) 
        {
            RectInt currentRoom = Q.Dequeue();

            if (!discovered.Contains(currentRoom) && CanSplitRoom(currentRoom, Q, discovered))
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
                    createdRooms.Add(splitRooms[i]);

                    drawColors.Add(depthColor);
                    roomCounter++;

                    if (!instantGeneration)
                    {
                        yield return new WaitForSeconds(stepDelay);
                    }
                }
            }
            else
            {
                roomCounter += 2;
            }
        }
        #endregion

        yield return new WaitUntil(() => continueStep || instantGeneration);
        continueStep = false;
        #region Remove 10%
        //remove 10% of the smallest rooms
        BubbleSorter(createdRooms, SortingType.Size);

        int amountOfRooms = Mathf.RoundToInt(0.10f * createdRooms.Count);

        for (int i = 0; i < amountOfRooms; i++)
        {
            createdRooms.RemoveAt(i);
        }
        #endregion

        //sort by position
        BubbleSorter(createdRooms, SortingType.Position);

        yield return new WaitUntil(() => continueStep || instantGeneration);
        continueStep = false;

        #region Door Creation

        Stack<RectInt> stackRooms = new();
        HashSet<RectInt> Discovered = new();

        stackRooms.Push(createdRooms[0]);

        while (stackRooms.Count > 0)
        {
            Debug.Log("START ========================================");
            RectInt current = stackRooms.Pop();

            AddRoom(current);

            if (!Discovered.Contains(current))
            {
                //Debug.Log("not discovered");
                foreach (RectInt neighbour in GetNeighbours(current))
                {
                    if (Discovered.Contains(neighbour))
                    {
                        //CURRENT AND NEIGHBOUR CAN BE CNNECTED
                        RectInt door = AddDoors(neighbour, current);
                        doors.Add(door);

                        RectInt[] roomCon = { neighbour, current };
                        connections.Add(neighbour);
                        connections.Add(current);
                        AddRoomConnection(door, roomCon);
                    }
                    else
                    {
                        createdRooms.Remove(current);
                    }
                    
                }
                

                Discovered.Add(current);

                //find neighbours
                foreach (RectInt node in createdRooms)
                {
                    if (CanAddDoor(current, node) && !Discovered.Contains(node) && !roomAdjacencyList.ContainsKey(node) )
                    {
                        //create neighbour
                        AddEdge(current, node);
                    }
                }

                foreach (RectInt node in GetNeighbours(current))
                {
                    stackRooms.Push(node);
                }
                    
                if (!instantGeneration)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }
        #endregion

        yield return new WaitUntil(() => continueStep || instantGeneration);
        continueStep = false;

        #region Visuals
        tileMap = new int[totalDungeonSize.height, totalDungeonSize.width];

        foreach (var rooming in createdRooms)
        {
            foreach(var intersectRoom in createdRooms)
            {
                if (rooming != intersectRoom)
                {
                    AlgorithmsUtils.FillRectangle(tileMap, AlgorithmsUtils.Intersect(rooming, intersectRoom), 1);
                }
            }
            AlgorithmsUtils.FillRectangleOutline(tileMap, rooming, 1);
        }


        foreach (var door in doors)
        {
            AlgorithmsUtils.FillRectangleOutline(tileMap, door, 0);
        }

        ConvertToBinary(tileMap);

        //y
        for (int i = 0; i < binaryTileMap.GetLength(0); i++)
        {
            //x
            for (int j = 0; j < binaryTileMap.GetLength(1); j++)
            {
                GameObject wall = gameObject;
                Vector3 rotation = Vector3.zero;

                switch (binaryTileMap[i, j])
                {
                    case 0:
                        continue;
                    case 1:
                        wall = wallPrefabs[1];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 2:
                        wall = wallPrefabs[1];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 3:
                        wall = wallPrefabs[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 4:
                        wall = wallPrefabs[1];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 5:
                        wall = wallPrefabs[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 6:
                        wall = wallPrefabs[2];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 7:
                        wall = wallPrefabs[3];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 8:
                        wall = wallPrefabs[1];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 9:
                        wall = wallPrefabs[2];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 10:
                        wall = wallPrefabs[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 11:
                        wall = wallPrefabs[3];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 12:
                        wall = wallPrefabs[2];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 13:
                        wall = wallPrefabs[3];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 14:
                        wall = wallPrefabs[3];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 15:
                        continue;
                }

                GameObject prefab = Instantiate(wall, new Vector3(j + 1f, 0, i + 1f), Quaternion.identity, wallsParent);
                prefab.transform.localEulerAngles = rotation;
                if (!instantGeneration)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }

        yield return new WaitUntil(() => continueStep || instantGeneration);
        continueStep = false;

        Queue<Vector2Int> FloorQueue = new();
        HashSet<Vector2Int> floorDiscovered = new();



        FloorQueue.Enqueue(new Vector2Int(createdRooms[0].y + 1, createdRooms[0].x + 1));

        while (FloorQueue.Count > 0)
        {
            Vector2Int current = FloorQueue.Dequeue();
            //Debug.Log(current);
            floorDiscovered.Add(current);


            GameObject objec = Instantiate(wallPrefabs[0], new Vector3(current.y + 0.5f, 0, current.x + 0.5f), Quaternion.identity, floorsParent);
            objec.name = $"{current.x}, {current.y}";

            if (!instantGeneration)
            {
                yield return new WaitForSeconds(stepDelay);
            }

            foreach (Vector2Int tile in GetTileMapNeighbours(tileMap, current))
            {
                //Debug.Log(!floorDiscovered.Contains(tile));
                //Debug.Log(!FloorQueue.Contains(tile));
                //Debug.Log(isValidIndex(tileMap[current.x, current.y]));
                if (!floorDiscovered.Contains(tile) && !FloorQueue.Contains(tile) && IsValidIndex(tileMap[current.x, current.y]))
                {
                    //Debug.Log(tile + "ADDING TO QUE " + loop);
                    FloorQueue.Enqueue(tile);
                }
               
            }
            //Debug.Log("End WHILE LOOP -=-==-==-=-=-=--=-");
        }
        #endregion

        yield return new WaitUntil(() => continueStep || instantGeneration);
        continueStep = false;

        //activate playability
    }

    #region RoomSplitting
    public bool CanSplitRoom(RectInt currentRoom, Queue<RectInt> q, HashSet<RectInt> discovered)
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
                        q.Enqueue(currentRoom);
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
                        q.Enqueue(currentRoom);
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

            Vector2Int roomSize1 = new(currentRoom.width, splitHeight);
            Vector2Int roomSize2 = new(currentRoom.width, currentRoom.height - splitHeight);

            //splitA
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + wallMargin);
            //top right
            nextRoomArray[1] = new(currentRoom.x, roomSize1.y + currentRoom.y, roomSize2.x, roomSize2.y);
        }
        else
        {
            int splitLength = Random.Range(minRoomSize, currentRoom.width - minRoomSize);

            Vector2Int roomSize1 = new(splitLength, currentRoom.height);
            Vector2Int roomSize2 = new(currentRoom.width - splitLength, currentRoom.height);

            //splitA
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x + wallMargin, roomSize1.y);
            //top right
            nextRoomArray[1] = new(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        return nextRoomArray;
    }
    #endregion

    //sorting rooms
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

    #region Doors and Graph Connections
    //Adding Doors
    public bool CanAddDoor(RectInt roomA, RectInt roomB)
    {
        if (roomA != roomB)
        {
            RectInt intersection = AlgorithmsUtils.Intersect(roomA, roomB);
            int minArea = minRoomSize * wallMargin;
            int area = intersection.width * intersection.height;

            if (area > minArea)
            {
                return true;
            }
        }
        return false;
    }
    public RectInt AddDoors(RectInt currentRoom, RectInt overlappingRoom)
    {
        RectInt wall = AlgorithmsUtils.Intersect(currentRoom, overlappingRoom);

        if (wall.width > wall.height)
        {
            //horizontal
            wall.x += wallMargin;
            wall.width -= wallMargin * 2;

            int randomX = Random.Range(wall.xMin, wall.xMax - 1);

            return new RectInt(randomX, wall.y, doorWidth, wall.height);
        }
        else
        {
            //vertical
            wall.y += wallMargin;
            wall.height -= wallMargin * 2;

            int randomY = Random.Range(wall.yMin, wall.yMax - 1);

            return new RectInt(wall.x, randomY, wall.width, doorWidth);
        }

    }
    private void AddRoomConnection(RectInt currentDoor, RectInt[] connectedRooms)
    {
        if (!doorConnections.ContainsKey(currentDoor))
        {
            doorConnections[currentDoor] = new List<RectInt>();
        }
        for (int i = 0; i < connectedRooms.Length; i++)
        {
            if (!roomConnections.ContainsKey(connectedRooms[i]))
            {
                roomConnections[connectedRooms[i]] = new List<RectInt>();
            }

        }

        doorConnections[currentDoor].Add(connectedRooms[0]);
        doorConnections[currentDoor].Add(connectedRooms[1]);
        roomConnections[connectedRooms[0]].Add(currentDoor);
        roomConnections[connectedRooms[1]].Add(currentDoor);
    }

    private List<RectInt> GetNeighbours(RectInt currentRoom)
    {
        return new List<RectInt>(roomAdjacencyList[currentRoom]);
    }
    public void AddRoom(RectInt node)
    {
        if (!roomAdjacencyList.ContainsKey(node))
        {
            roomAdjacencyList[node] = new List<RectInt>();
        }
    }
    public void AddEdge(RectInt fromNode, RectInt toNode)
    {
        if (!roomAdjacencyList.ContainsKey(fromNode))
        {
            AddRoom(fromNode);
        }
        if (!roomAdjacencyList.ContainsKey(toNode))
        {
            AddRoom(toNode);
        }

        roomAdjacencyList[fromNode].Add(toNode);
        roomAdjacencyList[toNode].Add(fromNode);
    }

    //Node Graph
    private Vector3 GetCenter(RectInt target)
    {
        float x = target.xMin + (float)(target.xMax - target.xMin) / 2;
        float y = target.yMin + (float)(target.yMax - target.yMin) / 2; 
        return new Vector3(x, 0, y) ;
    }
    #endregion

    #region SquareMarching
    public void ConvertToBinary(int[,] tilemap)
    {
        binaryTileMap = new int[tilemap.GetLength(0) - 1, tilemap.GetLength(1) - 1];
        //y
        for (int i = 0; i < tilemap.GetLength(0) - 1; i++)
        {
            //x 
            for (int j = 0; j < tilemap.GetLength(1) - 1; j++)
            {
                binaryTileMap[i, j] = tilemap[i, j] * 1 +
                   tilemap[i, j + 1] * 2 +
                   tilemap[i + 1, j + 1] * 4 +
                   tilemap[i + 1, j] * 8;
            }
        }
    }

    public bool IsValidIndex(int newIndex)
    {
        return newIndex == 0;
    }

    public List<Vector2Int> GetTileMapNeighbours(int[,] tileMap, Vector2Int tileMapPos)
    {
        List<Vector2Int> neighbours = new();


        int yMin = Mathf.Clamp(tileMapPos.x - 1, 0, tileMap.GetLength(0));
        //int yMin = tileMapPos.y - 1;
        int yMax = Mathf.Clamp(tileMapPos.x + 1, 0 , tileMap.GetLength(0));
        //int yMax = tileMapPos.y + 1;


        int xMin = Mathf.Clamp(tileMapPos.y - 1,0, tileMap.GetLength(1));
        //int xMin = tileMapPos.x - 1;
        int xMax = Mathf.Clamp(tileMapPos.y + 1,0, tileMap.GetLength(1));
        //int xMax = tileMapPos.x + 1;


        //y
        for (int i = yMin; i <= yMax; i++)
        {
            //x
            for (int j = xMin; j <= xMax; j++)
            {
                //Debug.Log(new Vector2Int(i, j) + "  Found Position: " + tileMap[i,j]);
                neighbours.Add(new Vector2Int(i, j));
            }
        }

        return neighbours;
    }

    public string ToString(bool flip, int[,] tilemap)
    {
        if (tilemap == null) return "Tile map not generated yet.";

        int rows = tilemap.GetLength(0);
        int cols = tilemap.GetLength(1);

        var sb = new StringBuilder();

        int start = flip ? rows - 1 : 0;
        int end = flip ? -1 : rows;
        int step = flip ? -1 : 1;

        for (int i = start; i != end; i += step)
        {
            for (int j = 0; j < cols; j++)
            {
                sb.Append((tilemap[i, j] == 0 ? '0' : '#')); //Replaces 1 with '#' making it easier to visualize
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
    #endregion


    [Button]
    public void PrintTileMap(int[,] tilemap)
    {
        Debug.Log(ToString(true, tilemap));
    }
}
