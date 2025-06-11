using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NaughtyAttributes;
using System.Text;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;

public enum SortingType
{
    Size,
    Position
}

public enum GenerationType
{
    Instant,
    Timed,
    Step,
    TimedStep

}

public class DungeonGenerator : MonoBehaviour
{
    
    [Header("Dungeon Settings")]
    [SerializeField] private RectInt dungeonSize;
    [SerializeField] private int splitDepth;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int wallMargin = 1;
    [SerializeField] private int doorWidth = 2;

    [Header("Generation Settings")]
    [SerializeField] private int dungeonSeed;
    [SerializeField] private GenerationType generationType = GenerationType.Step;
    [SerializeField] private float stepDelay;
    
    [Header("Visuals")]
    [SerializeField] private Transform wallsParent; 
    [SerializeField] private Transform floorsParent;
    [SerializeField] private Vector3 offset;
    [SerializeField] private GameObject[] wallPrefabs;

    [Header("Playability")]
    [SerializeField] private Transform player;
    [SerializeField]private PathFinder pathFinder;

    [Header("Debug")]
    [SerializeField] private bool showRoomOutline = true;
    [SerializeField] private bool showDoorsOutline = true;
    [SerializeField] private bool showConnectionGraph = true;
    [SerializeField] private bool showVisuals = true;

    [SerializeField] private List<RectInt> createdRooms = new();
    [SerializeField] private List<RectInt> createdDoors = new();
    [SerializeField] private List<Vector3> createdFloors = new();


    //A button that starts the next step of the dungeon generation
    private bool continueStep = false;
    [Button]
    public void NextStep()
    {
        continueStep = true;
    }

    //Room Splitting variables
    private bool isSplittingHorizontal = true;

    private int currentDepth;
    private int roomCounter = 0;

    private int expectedRoomSplits = 0;
    private int roomDeduction = 0;

    private List<Color> drawColors = new();
    private Color depthColor = Color.red;

    //This contains the rooms that are attached to eachother.
    private Dictionary<RectInt, List<RectInt>> roomAdjacencyList = new();

    //This is for the graph, it contains a list of doors and their connections to the rooms.
    private Dictionary<RectInt, List<RectInt>> doorConnections = new();

    
    private int[,] tileMap;
    private int[,] binaryTileMap;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //If the seed wasnt given from the start then create a seed
        if(dungeonSeed == 0)
        {
            dungeonSeed = Random.seed;
        }

        Random.InitState(dungeonSeed);

        //Takes the wall margin into account so that intersecting walls dont make the room to small.
        minRoomSize += wallMargin * 2;
        
        //Start Generation
        StartCoroutine(Generation(dungeonSize));
    }

    // Update is called once per frame
    void Update()
    {
        //Draws the main outline
        AlgorithmsUtils.DebugRectInt(dungeonSize, Color.red);

        if (showRoomOutline)
        {
            for (int i = 0; i < createdRooms.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdRooms[i], drawColors[i]);
            }
        }

        if (showDoorsOutline)
        {
            for (int i = 0; i < createdDoors.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdDoors[i], Color.yellow);
            }
        }

        if (showConnectionGraph)
        {
            foreach (var item in doorConnections)
            {
                //Gets the center of the door
                Vector3 doorCenter = GetCenter(item.Key);
                DebugExtension.DebugWireSphere(doorCenter, Color.blue);

                var roomConnection = doorConnections[item.Key];

                //Creating a sphere for each room and drawing lines towards the door
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

        //If the que is not 0 AND currentDepth is not the final splitDepth
        while (currentDepth != splitDepth && Q.Count != 0) 
        {
            RectInt currentRoom = Q.Dequeue();

            //If it can split the room and it has not been discovered
            if (!discovered.Contains(currentRoom) && CanSplitRoom(currentRoom, Q, discovered))
            {
                discovered.Add(currentRoom);

                //Splits the room into two RectInts
                RectInt[] splitRooms = SplitRoom(currentRoom);

                //if it somehow adds an already existing room, remove that room.
                if (createdRooms.Contains(currentRoom))
                {
                    int index = createdRooms.IndexOf(currentRoom);
                    createdRooms.Remove(currentRoom);
                }

                //loops through the created rooms
                for (int i = 0; i < splitRooms.Length; i++)
                {
                    Q.Enqueue(splitRooms[i]);

                    //Adds them to the debug list of created rooms
                    createdRooms.Add(splitRooms[i]);

                    //assigngs the current depth color to this room
                    drawColors.Add(depthColor);
                    roomCounter++;

                    if (generationType == GenerationType.Timed || generationType == GenerationType.TimedStep)
                    {
                        yield return new WaitForSeconds(stepDelay);
                    }
                }
            }
            //if the room couldnt be split or was already discovered, add up the room counter.
            else
            {
                roomCounter += 2;
            }
        }
        #endregion

        Debug.Log("Finished Room Generation");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;
        #region Remove 10%
        //sorts the rooms based on size
        BubbleSorter(createdRooms, SortingType.Size);

        //calculates how many rooms are in the 10%
        int amountOfRooms = Mathf.RoundToInt(0.10f * createdRooms.Count);

        //removes 10% of all the rooms
        for (int i = 0; i < amountOfRooms; i++)
        {
            createdRooms.RemoveAt(i);
        }
        #endregion

        //sorts the rooms by position
        BubbleSorter(createdRooms, SortingType.Position);

        Debug.Log("Finished Removing 10% of the smallest rooms");
        Debug.Log("Finished Room Sorting");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        #region Door and Graph Creation
        //creation of the doors and Graph
        Stack<RectInt> stackRooms = new();
        HashSet<RectInt> Discovered = new();

        stackRooms.Push(createdRooms[0]);

        while (stackRooms.Count > 0)
        {
            RectInt current = stackRooms.Pop();

            //Adds the current room the adjency list.
            AddRoom(current);

            //If the current room has not been discovered
            if (!Discovered.Contains(current))
            {
                //Checks through all the neighbours to see if they are discovered yet
                foreach (RectInt neighbour in GetNeighbours(current))
                {
                    if (Discovered.Contains(neighbour))
                    {
                        //If a neighbour has been discovered before, add a room between them
                        RectInt door = AddDoors(neighbour, current);
                        createdDoors.Add(door);

                        //Adds a connection between the door and the rooms
                        RectInt[] roomCon = { neighbour, current };
                        AddRoomConnection(door, roomCon);
                    }
                    
                    
                }
                
                //Adds this room the the discovery list
                Discovered.Add(current);

                //Searches through the created rooms
                foreach (RectInt node in createdRooms)
                {
                    //Checks if there is room for a door, it has not been discovered and if it's already in the roomAdjacencyList.
                    if (CanAddDoor(current, node) && !Discovered.Contains(node) && !roomAdjacencyList.ContainsKey(node) )
                    {
                        //Creates an edge between the two rooms
                        AddEdge(current, node);
                    }
                    
                }

                //If the room has no neighbours then remove that room from the list
                if(GetNeighbours(current).Count == 0)
                {
                    createdRooms.Remove(current);
                    stackRooms.Push(createdRooms[0]);
                }
                
                //Pushes all the neighbours of this room to the stack
                foreach (RectInt node in GetNeighbours(current))
                {
                    stackRooms.Push(node);
                }
                    

                if (generationType == GenerationType.Timed || generationType == GenerationType.TimedStep)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }
        #endregion

        Debug.Log("Finished creating the Doors and the Graph");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        #region Visuals

        //Creates the size of the tileMap
        tileMap = new int[dungeonSize.height, dungeonSize.width];

        //loops through all the rooms
        foreach (var createdRoom in createdRooms)
        {
            //loops through all the rooms again to find intersections
            foreach(var intersectRoom in createdRooms)
            {
                //if the rooms are not the same then continue
                if (createdRoom != intersectRoom)
                {
                    //Fills up any intersecting areas with wall spaces
                    AlgorithmsUtils.FillRectangle(tileMap, AlgorithmsUtils.Intersect(createdRoom, intersectRoom), 1);
                }
            }
            //creates the outline of the room
            AlgorithmsUtils.FillRectangleOutline(tileMap, createdRoom, 1);
        }

        //Loops through all the doors and makes space for them
        foreach (var door in createdDoors)
        {
            AlgorithmsUtils.FillRectangleOutline(tileMap, door, 0);
        }

        //Converts the tilemap into the binary tilemap
        ConvertToBinary(tileMap);

        //Loops through the binaryTileMap to instantiate the core responding wall
        //y loop
        for (int i = 0; i < binaryTileMap.GetLength(0); i++)
        {
            //x loop
            for (int j = 0; j < binaryTileMap.GetLength(1); j++)
            {
                GameObject wall = gameObject;
                Vector3 rotation = Vector3.zero;

                //Depending on the value, it sets the wall and it's rotation
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
                        //if the wall is 15 (has walls on every side) then continue
                        continue;
                }

                //instantiates the wall at the next position
                GameObject prefab = Instantiate(wall, new Vector3(j + 1f, 0, i + 1f), Quaternion.identity, wallsParent);

                //sets the rotation of the object
                prefab.transform.localEulerAngles = rotation;
                if (generationType == GenerationType.Timed || generationType == GenerationType.TimedStep)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }

        Debug.Log("Finished generating the visuals");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //This BFS creates the floors for the dungeon

        Queue<Vector2Int> floorQueue = new();
        HashSet<Vector2Int> floorDiscovered = new();

        //enqueues the first position inside of the first room
        floorQueue.Enqueue(new Vector2Int(createdRooms[0].y + 1, createdRooms[0].x + 1));

        while (floorQueue.Count > 0)
        {
            Vector2Int current = floorQueue.Dequeue();
            floorDiscovered.Add(current);

            //it instantiates the wall at the said position and parents it.
            GameObject objec = Instantiate(wallPrefabs[0], new Vector3(current.y + 0.5f, 0, current.x + 0.5f), Quaternion.identity, floorsParent);
            
            //Gives the position and tilemap index for debug purposes
            objec.name = $"{current.x}, {current.y}, index: {tileMap[current.x, current.y]}";

            //if the tilemap index is 0 then add it to the createdFloors list
            if (tileMap[current.x, current.y] == 0)
            {
                createdFloors.Add(objec.transform.position);
            }

            if (generationType == GenerationType.Timed || generationType == GenerationType.TimedStep)
            {
                yield return new WaitForSeconds(stepDelay);
            }

            //gets the neighbours of the current floor tile
            foreach (Vector2Int tile in GetTileMapNeighbours(tileMap, current))
            {
                //if the floor is not discovered, not in the queue and is a valid index then enqueue the neibour
                if (!floorDiscovered.Contains(tile) && !floorQueue.Contains(tile) && IsValidIndex(tileMap[current.x, current.y]))
                {
                    floorQueue.Enqueue(tile);
                }
            }
        }
        #endregion

        Debug.Log("Finished flooding the rooms with floors");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //set the player position at the first room
        player.position = new Vector3(createdRooms[0].xMin + 1.5f, 1, createdRooms[0].yMin + 1.5f);

        //Gives the pathFinder it's required values
        pathFinder.SetGraph(createdFloors, dungeonSize);
        //activate playability
    }

    #region RoomSplitting
    public bool CanSplitRoom(RectInt currentRoom, Queue<RectInt> q, HashSet<RectInt> discovered)
    {
        //Splits the room if it hit the end of the next level.
        if (roomCounter + expectedRoomSplits == 0)
        {
            expectedRoomSplits = 1;
        }
        //Splits the room once it hits the expected split rooms.
        //It takes deducted rooms into account for rooms that couldnt be split.
        else if (roomCounter >= expectedRoomSplits * 2 - roomDeduction)
        {
            //resets values
            expectedRoomSplits = roomCounter;
            roomCounter = 0;
            roomDeduction = 0;

            //Set next Depth
            currentDepth++;

            //Switches the splitting direction
            isSplittingHorizontal = !isSplittingHorizontal;

            //Sets a random color for each depth for debugging purposes.
            depthColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        if(currentDepth == splitDepth)
        {
            return false;
        }
        
        
        bool checkSecondSplit = false;
        bool currentSplit = isSplittingHorizontal;

        //Loops all posibilities if needed
        for (int i = 0; i < 2; i++)
        {
            //if splitting horizontaly
            if (currentSplit)
            {
                //calculates if the minRoom size even fits in the current room split.
                if (currentRoom.height - minRoomSize > minRoomSize)
                {
                    if (!checkSecondSplit)
                    {
                        //The room can be split in it's current split
                        return true;
                    }
                    else
                    {
                        //The room can be split in the next Level/Depth.
                        //it will requeue the room so it can be used in the next depth
                        discovered.Remove(currentRoom);
                        q.Enqueue(currentRoom);
                        return false;
                    }
                }
            }
            //if splitting vertically
            else
            {
                //calculates if the minRoom size even fits in the current room split.
                if (currentRoom.width - minRoomSize > minRoomSize)
                {
                    if (!checkSecondSplit)
                    {
                        //The room can be split in it's current split
                        return true;
                    }
                    else
                    {
                        //The room can be split in the next Level/Depth.
                        //it will requeue the room so it can be used in the next depth
                        discovered.Remove(currentRoom);
                        q.Enqueue(currentRoom);
                        return false;
                    }
                }
            }
            //Checks if the other split direction is possible
            checkSecondSplit = true;
            currentSplit = !currentSplit;
        }


        //if neither split direction works, then deduct both posibilities from the expected rooms.
        roomDeduction += 2;
        return false;
    }
    public RectInt[] SplitRoom(RectInt currentRoom)
    {
        RectInt[] nextRoomArray = new RectInt[2];

        //Checks which direction it has been split in
        if (isSplittingHorizontal)
        {
            //gets a random point where the room will be split
            int splitHeight = Random.Range(minRoomSize, currentRoom.height - minRoomSize);

            //Sets the sizes of each room
            Vector2Int roomSize1 = new(currentRoom.width, splitHeight);
            Vector2Int roomSize2 = new(currentRoom.width, currentRoom.height - splitHeight);

            //Bottom Room Rect
            //adds more space for the wall
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + wallMargin);
            //Top Room Rect
            nextRoomArray[1] = new(currentRoom.x, currentRoom.y + roomSize1.y , roomSize2.x, roomSize2.y);
        }
        else
        {
            //gets a random point where the room will be split
            int splitLength = Random.Range(minRoomSize, currentRoom.width - minRoomSize);

            //Sets the sizes of each room
            Vector2Int roomSize1 = new(splitLength, currentRoom.height);
            Vector2Int roomSize2 = new(currentRoom.width - splitLength, currentRoom.height);

            //Left Room Rect
            //adds more space for the wall
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x + wallMargin, roomSize1.y);
            //Right Room Rect
            nextRoomArray[1] = new(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        //returns the array of rooms
        return nextRoomArray;
    }
    #endregion

    //sorting rooms
    private void BubbleSorter(List<RectInt> rooms, SortingType type)
    {
        //Loops through all the rooms and going down the count
        for (int i = rooms.Count - 2; i >= 0; i--)
        {
            //loops each time till it reaches i
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
        //if the rooms are not the same, then continue
        if (roomA != roomB)
        {
            //gets the intersection of the wall
            RectInt intersection = AlgorithmsUtils.Intersect(roomA, roomB);

            //calculates the minimum area the it is supposed to be
            int minArea = minRoomSize * wallMargin;
            //the size of the intersected area
            int area = intersection.width * intersection.height;

            //if the area is bigger then the minimum then return true
            if (area > minArea)
            {
                return true;
            }
        }

        //if the rooms were the same OR the area was smaller then the minimum then return false
        return false;
    }
    public RectInt AddDoors(RectInt currentRoom, RectInt overlappingRoom)
    {
        //gets the overlapping intersection of the rooms
        RectInt wall = AlgorithmsUtils.Intersect(currentRoom, overlappingRoom);

        //Checks if it's a horizontal wall or a vertical one
        if (wall.width > wall.height)
        {
            //horizontal

            //calculates the area in where the door can be placed
            wall.x += wallMargin;
            wall.width -= wallMargin * 2;

            //choses a random position between that part of the wall
            int randomX = Random.Range(wall.xMin, wall.xMax - 1);

            //returns the door position and size
            return new RectInt(randomX, wall.y, doorWidth, wall.height);
        }
        else
        {
            //vertical

            //calculates the area in where the door can be placed
            wall.y += wallMargin;
            wall.height -= wallMargin * 2;

            //choses a random position between that part of the wall
            int randomY = Random.Range(wall.yMin, wall.yMax - 1);

            //returns the door position and size
            return new RectInt(wall.x, randomY, wall.width, doorWidth);
        }

    }
    private void AddRoomConnection(RectInt currentDoor, RectInt[] connectedRooms)
    {
        //Checks if there is already a current door in the connections list
        if (!doorConnections.ContainsKey(currentDoor))
        {
            //adds the door and a new list for the rooms
            doorConnections[currentDoor] = new List<RectInt>();
        }

        //Adds all the rooms to the door
        doorConnections[currentDoor].Add(connectedRooms[0]);
        doorConnections[currentDoor].Add(connectedRooms[1]);
    }

    private List<RectInt> GetNeighbours(RectInt currentRoom)
    {
        //gets all the neighbours of the chosen room
        return new List<RectInt>(roomAdjacencyList[currentRoom]);
    }
    public void AddRoom(RectInt node)
    {
        //Adds a room if it wasn't found in the list yet
        if (!roomAdjacencyList.ContainsKey(node))
        {
            roomAdjacencyList[node] = new List<RectInt>();
        }
    }
    public void AddEdge(RectInt fromNode, RectInt toNode)
    {
        //Checks if any of the rooms are already inside the list
        //if not add a new room
        if (!roomAdjacencyList.ContainsKey(fromNode))
        {
            AddRoom(fromNode);
        }
        if (!roomAdjacencyList.ContainsKey(toNode))
        {
            AddRoom(toNode);
        }

        //creates the edge between the rooms
        roomAdjacencyList[fromNode].Add(toNode);
        roomAdjacencyList[toNode].Add(fromNode);
    }

    //Node Graph
    private Vector3 GetCenter(RectInt area)
    {
        //returns a vector3 of the areas center
        return new Vector3(area.center.x, 0, area.center.y) ;
    }
    #endregion

    #region SquareMarching
    public void ConvertToBinary(int[,] tilemap)
    {
        //Sets the size of the binaryTilemap
        binaryTileMap = new int[tilemap.GetLength(0) - 1, tilemap.GetLength(1) - 1];

        //Marches through each position in a 2 by 2 square
        //y loop
        for (int i = 0; i < tilemap.GetLength(0) - 1; i++)
        {
            //x loop
            for (int j = 0; j < tilemap.GetLength(1) - 1; j++)
            {
                //Checks all the surrounding tiles
                //If a tile is 1 it will multiply it by their coreresponding value.
                binaryTileMap[i, j] = tilemap[i, j] * 1 +
                   tilemap[i, j + 1] * 2 +
                   tilemap[i + 1, j + 1] * 4 +
                   tilemap[i + 1, j] * 8;
            }
        }
    }

    public bool IsValidIndex(int newIndex)
    {
        //checks if tile is equal to 0
        return newIndex == 0;
    }

    public List<Vector2Int> GetTileMapNeighbours(int[,] tileMap, Vector2Int tileMapPos)
    {
        //creates a new list for the neighbours
        List<Vector2Int> neighbours = new();

        //set the min max y positions based on the tilemap length and curren position
        int yMin = Mathf.Clamp(tileMapPos.x - 1, 0, tileMap.GetLength(0));
        int yMax = Mathf.Clamp(tileMapPos.x + 1, 0 , tileMap.GetLength(0));

        //sets the min max x positions based on the tilemap length and curren position
        int xMin = Mathf.Clamp(tileMapPos.y - 1,0, tileMap.GetLength(1));
        int xMax = Mathf.Clamp(tileMapPos.y + 1,0, tileMap.GetLength(1));


        //loop the y till it reaches the max
        for (int i = yMin; i <= yMax; i++)
        {
            //loop the x till it reaches its max
            for (int j = xMin; j <= xMax; j++)
            {
                //Adds the potential neighbouring positions
                neighbours.Add(new Vector2Int(i, j));
            }
        }

        return neighbours;
    }

    #endregion
}
