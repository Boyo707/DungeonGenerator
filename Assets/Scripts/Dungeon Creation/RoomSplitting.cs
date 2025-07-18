using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RoomSplitting : MonoBehaviour
{
    //somehow get this within the enumerator
    private bool isHorizontalSplit = false;

    public enum SplitType
    {
        Horizontal,
        Vertical,
        UnAble
    }

    public IEnumerator Splitting(float stepDelay, int dungeonSeed, Dungeon2 dungeonGeneration)
    {
        Random.InitState(dungeonSeed);

        List<RectInt> createdRooms = new();

        AlgorithmsUtils.DebugRectInt(dungeonGeneration.dungeonSize, Color.red);

        Queue<RectInt> Q = new();
        HashSet<RectInt> discovered = new();

        SplitType roomSplitType = SplitType.Horizontal;


        Q.Enqueue(dungeonGeneration.dungeonSize);

        while (Q.Count != 0)
        {
            RectInt currentRoom = Q.Dequeue();

            if (roomSplitType == SplitType.Horizontal) roomSplitType = SplitType.Vertical;
            if (roomSplitType == SplitType.Vertical) roomSplitType = SplitType.Horizontal;
            if (roomSplitType == SplitType.UnAble) roomSplitType = SplitType.Horizontal;

            //isHorizontalSplit = !isHorizontalSplit;


            //If it can split the room and it has not been discovered

            roomSplitType = CanSplitRoom(currentRoom, roomSplitType, dungeonGeneration.minRoomSize);

            if (!discovered.Contains(currentRoom) && roomSplitType != SplitType.UnAble)
            {
                discovered.Add(currentRoom);

                RectInt[] splitRooms = SplitRoom(currentRoom, roomSplitType, dungeonGeneration.minRoomSize, dungeonGeneration.wallMargin);

                //Remove the big room after it gets split
                createdRooms.Remove(currentRoom);

                //loops through the created rooms
                for (int i = 0; i < splitRooms.Length; i++)
                {
                    Q.Enqueue(splitRooms[i]);

                    //Adds them to the debug list of created rooms
                    createdRooms.Add(splitRooms[i]);
                    dungeonGeneration.createdRooms = createdRooms;

                    //assigngs the current depth color to this room

                    if (dungeonGeneration.generationType == GenerationType.Timed || dungeonGeneration.generationType == GenerationType.TimedStep)
                    {
                        yield return new WaitForSeconds(stepDelay);
                    }
                }
            }
        }
    }

    public SplitType CanSplitRoom(RectInt currentRoom, SplitType splitType, int minRoomSize)
    {
        for (int i = 0; i < 2; i++)
        {
            if (splitType == SplitType.Horizontal)
            {
                //calculates if the minimal room size can fit in the current room split.
                if (currentRoom.height - minRoomSize > minRoomSize)
                {
                    return SplitType.Horizontal;
                }
                else
                {
                    splitType = SplitType.Vertical;
                }
            }
            else
            {
                //calculates if the minimal room size can fit in the current room split.
                if (currentRoom.width - minRoomSize > minRoomSize)
                {
                    return SplitType.Vertical;
                }
                else
                {
                    splitType = SplitType.Horizontal;
                }
            }
        }

        //false when it's not possible to split from any side
        return SplitType.UnAble;
    }

    public bool CanSplitRoom(RectInt currentRoom, bool checkingHorizontal, int minRoomSize)
    {
        for (int i = 0; i < 2; i++)
        {
            if (checkingHorizontal)
            {
                //calculates if the minimal room size can fit in the current room split.
                if (currentRoom.height - minRoomSize > minRoomSize)
                {
                    isHorizontalSplit = checkingHorizontal;
                    return true;
                }
                else
                {
                    checkingHorizontal = !checkingHorizontal;
                }
            }
            else
            {
                //calculates if the minimal room size can fit in the current room split.
                if (currentRoom.width - minRoomSize > minRoomSize)
                {
                    isHorizontalSplit = checkingHorizontal;
                    return true;
                }
                else
                {
                    checkingHorizontal = !checkingHorizontal;
                }
            }
        }

        //false when it's not possible to split from any side
        return false;
    }

    public RectInt[] SplitRoom(RectInt currentRoom, bool isHorizontal, int minRoomSize, int wallMargin)
    {
        RectInt[] nextRoomArray = new RectInt[2];

        //Checks which direction it has been split in
        if (isHorizontal)
        {
            int splitPosition = Random.Range(minRoomSize, currentRoom.height - minRoomSize);

            Vector2Int roomSize1 = new(currentRoom.width, splitPosition);
            Vector2Int roomSize2 = new(currentRoom.width, currentRoom.height - splitPosition);

            //Bottom Room Rect
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + wallMargin);
            //Top Room Rect
            nextRoomArray[1] = new(currentRoom.x, currentRoom.y + roomSize1.y, roomSize2.x, roomSize2.y);
        }
        else
        {
            int splitPosition = Random.Range(minRoomSize, currentRoom.width - minRoomSize);

            Vector2Int roomSize1 = new(splitPosition, currentRoom.height);
            Vector2Int roomSize2 = new(currentRoom.width - splitPosition, currentRoom.height);

            //Left Room Rect
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x + wallMargin, roomSize1.y);
            //Right Room Rect
            nextRoomArray[1] = new(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        return nextRoomArray;
    }
    public RectInt[] SplitRoom(RectInt currentRoom, SplitType splitType, int minRoomSize, int wallMargin)
    {
        RectInt[] nextRoomArray = new RectInt[2];

        //Checks which direction it has been split in
        if (splitType == SplitType.Horizontal)
        {
            int splitPosition = Random.Range(minRoomSize, currentRoom.height - minRoomSize);

            Vector2Int roomSize1 = new(currentRoom.width, splitPosition);
            Vector2Int roomSize2 = new(currentRoom.width, currentRoom.height - splitPosition);

            //Bottom Room Rect
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + wallMargin);
            //Top Room Rect
            nextRoomArray[1] = new(currentRoom.x, currentRoom.y + roomSize1.y, roomSize2.x, roomSize2.y);
        }
        else
        {
            int splitPosition = Random.Range(minRoomSize, currentRoom.width - minRoomSize);

            Vector2Int roomSize1 = new(splitPosition, currentRoom.height);
            Vector2Int roomSize2 = new(currentRoom.width - splitPosition, currentRoom.height);

            //Left Room Rect
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x + wallMargin, roomSize1.y);
            //Right Room Rect
            nextRoomArray[1] = new(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        return nextRoomArray;
    }


}
