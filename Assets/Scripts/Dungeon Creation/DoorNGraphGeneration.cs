using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CompositeCollider2D;

public class DoorNGraphGeneration : MonoBehaviour
{
    [SerializeField] private List<RectInt> createdRooms = new();

    private Dictionary<RectInt, List<RectInt>> doorConnections = new();
    [SerializeField] private List<RectInt> createdDoors = new();

    public Dictionary<RectInt, List<RectInt>> roomAdjacencyList = new();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator CreateDoorGraph(float stepDelay)
    {
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
                    if (CanAddDoor(current, node) && !Discovered.Contains(node) && !roomAdjacencyList.ContainsKey(node))
                    {
                        //Creates an edge between the two rooms
                        AddEdge(current, node);
                    }

                }

                //If the room has no neighbours then remove that room from the list
                if (GetNeighbours(current).Count == 0)
                {
                    createdRooms.Remove(current);
                    stackRooms.Push(createdRooms[0]);
                }

                //Pushes all the neighbours of this room to the stack
                foreach (RectInt node in GetNeighbours(current))
                {
                    stackRooms.Push(node);
                }


                if (Dungeon2.instance.generationType == GenerationType.Timed || Dungeon2.instance.generationType == GenerationType.TimedStep)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }
    }

    //Adding Doors
    public bool CanAddDoor(RectInt roomA, RectInt roomB)
    {
        //if the rooms are not the same, then continue
        if (roomA != roomB)
        {
            //gets the intersection of the wall
            RectInt intersection = AlgorithmsUtils.Intersect(roomA, roomB);

            //calculates the minimum area the it is supposed to be
            int minArea = Dungeon2.instance.minRoomSize * Dungeon2.instance.wallMargin;
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
            wall.x += Dungeon2.instance.wallMargin;
            wall.width -= Dungeon2.instance.wallMargin * 2;

            //choses a random position between that part of the wall
            int randomX = Random.Range(wall.xMin, wall.xMax - 1);

            //returns the door position and size
            return new RectInt(randomX, wall.y, Dungeon2.instance.doorWidth, wall.height);
        }
        else
        {
            //vertical

            //calculates the area in where the door can be placed
            wall.y += Dungeon2.instance.wallMargin;
            wall.height -= Dungeon2.instance.wallMargin * 2;

            //choses a random position between that part of the wall
            int randomY = Random.Range(wall.yMin, wall.yMax - 1);

            //returns the door position and size
            return new RectInt(wall.x, randomY, wall.width, Dungeon2.instance.doorWidth);
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
        return new Vector3(area.center.x, 0, area.center.y);
    }
}
