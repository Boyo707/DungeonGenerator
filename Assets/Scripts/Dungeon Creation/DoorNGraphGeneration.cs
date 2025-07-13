using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorNGraphGeneration : MonoBehaviour
{
    public IEnumerator CreateDoorGraph(float stepDelay, int dungeonSeed)
    {
        Random.InitState(dungeonSeed);

        Dungeon2 dungeonGenerator = Dungeon2.instance;

        Graph<RectInt> connections = new();

        //creation of the doors and Graph
        Stack<RectInt> stackRooms = new();
        HashSet<RectInt> Discovered = new();

        stackRooms.Push(dungeonGenerator.createdRooms[0]);

        while (stackRooms.Count > 0)
        {
            RectInt current = stackRooms.Pop();

            //Adds the current room the adjency list.
            connections.AddNode(current);

            //If the current room has not been discovered
            if (!Discovered.Contains(current))
            {
                Discovered.Add(current);

                foreach (RectInt node in dungeonGenerator.createdRooms)
                {
                    //Checks if there is room for a door, it has not been discovered and if it's already in the roomAdjacencyList.
                    if (CanAddDoor(current, node) && !Discovered.Contains(node) && !connections.GetNodes().Contains(node))
                    {
                        //Creates an edge between the two rooms
                        connections.AddEdge(current, node);
                    }
                }

                foreach (RectInt neighbour in connections.GetNeighbors(current))
                {
                    if (Discovered.Contains(neighbour))
                    {
                        //If a neighbour has been discovered before, add a door between them
                        RectInt door = CreateDoor(neighbour, current);

                        Discovered.Add(door);

                        connections.AddEdge(door, current);
                        connections.AddEdge(door, neighbour);

                        connections.RemoveEdge(current, neighbour);

                        dungeonGenerator.createdDoors.Add(door);
                    }
                    else
                    {
                        //if the neighbour has not been discovered, add to stack
                        stackRooms.Push(neighbour);
                    }
                }

                //aplies the new list/graph to the main one
                dungeonGenerator.roomAdjacencyList = connections;

                if (dungeonGenerator.generationType == GenerationType.Timed || dungeonGenerator.generationType == GenerationType.TimedStep)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }
    }

    //Adding Doors
    public bool CanAddDoor(RectInt roomA, RectInt roomB)
    {
        if (roomA != roomB)
        {
            RectInt intersection = AlgorithmsUtils.Intersect(roomA, roomB);

            int doorArea = (Dungeon2.instance.doorWidth * 2) * Dungeon2.instance.wallMargin;

            int intersectingArea = intersection.width * intersection.height;

            if (intersectingArea > doorArea)
            {
                return true;
            }
        }

        return false;
    }
    public RectInt CreateDoor(RectInt currentRoom, RectInt overlappingRoom)
    {
        //gets the overlapping intersection of the rooms
        RectInt wall = AlgorithmsUtils.Intersect(currentRoom, overlappingRoom);

        if (wall.width > wall.height)
        {
            //choses a random position inside the intersection
            int randomX = Random.Range(wall.xMin + Dungeon2.instance.wallMargin, wall.xMax - Dungeon2.instance.wallMargin * 2);

            return new RectInt(randomX, wall.y, Dungeon2.instance.doorWidth, wall.height);
        }
        else
        {
            //choses a random position inside the intersection
            int randomY = Random.Range(wall.yMin + Dungeon2.instance.wallMargin, wall.yMax - Dungeon2.instance.wallMargin * 2);

            return new RectInt(wall.x, randomY, wall.width, Dungeon2.instance.doorWidth);
        }

    }
}
