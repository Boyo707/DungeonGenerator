using System.Collections.Generic;
using UnityEngine;

public class RoomRemover : MonoBehaviour
{

    public List<RectInt> SortRooms(List<RectInt> rooms, Dungeon2 dungeonGenerator)
    {
        //gets 10% off the current amount of rooms
        int amountToRemove = Mathf.RoundToInt(0.10f * rooms.Count);

        int removedRoomsCount = 0;

        List<RectInt> roomsToRemove = new();

        //sorts the rooms based on size
        rooms.Sort ((a,b) => a.width * a.height - b.width * b.height);

        Debug.Log($"Removing {amountToRemove} out of the {rooms.Count}");

        for (int i = 0; i < amountToRemove; i++)
        {
            roomsToRemove.Add(rooms[i]);
        }

        for (int i = 0; i < roomsToRemove.Count; i++)
        {
            if (AreAllRoomsConnected(rooms, roomsToRemove[i], dungeonGenerator))
            {
                removedRoomsCount++;
                rooms.Remove(roomsToRemove[i]);
            }
            else
            {
                Debug.Log($"Couldn't remove {roomsToRemove[i]}");
            }
        }

        Debug.Log($"Removed {removedRoomsCount} out of the {amountToRemove}");
        
        //sorts the rooms by position
        rooms.Sort((a, b) => a.position.x * a.position.y - b.position.x * b.position.y);

        return rooms;
    }
    private bool AreAllRoomsConnected(List<RectInt> rooms, RectInt roomToRemove, Dungeon2 dungeonGenerator)
    {
        Queue<RectInt> Q = new();
        HashSet<RectInt> discovered = new();

        Q.Enqueue(rooms[rooms.Count - 1]);

        while (Q.Count != 0)
        {
            RectInt current = Q.Dequeue();

            if (!discovered.Contains(current) && current != roomToRemove)
            {
                discovered.Add(current);

                foreach (RectInt neighbour in GetOverlappingRooms(rooms, current, dungeonGenerator))
                {
                    Q.Enqueue(neighbour);
                }
            }
        }

        if (discovered.Count == rooms.Count - 1)
        {
            return true;
        }
        return false;
    }

    private List<RectInt> GetOverlappingRooms(List<RectInt> rooms, RectInt targetRoom, Dungeon2 dungeonGenerator)
    {
        List<RectInt> overlappingRooms = new();
        int doorArea = (dungeonGenerator.doorWidth * 2) * dungeonGenerator.wallMargin;

        foreach (var room in rooms)
        {
            RectInt intersection = AlgorithmsUtils.Intersect(room, targetRoom);

            int intersectingArea = intersection.width * intersection.height;

            if (intersectingArea > doorArea)
            {
                overlappingRooms.Add(room);
            }
        }

        return overlappingRooms;
    }
}
