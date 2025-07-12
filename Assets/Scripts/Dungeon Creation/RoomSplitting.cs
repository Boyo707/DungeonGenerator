using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSplitting : MonoBehaviour
{
    //somehow get this within the enumerator
    private bool isHorizontalSplit = false;

    public IEnumerator Splitting(float stepDelay, int dungeonSeed)
    {
        Random.InitState(dungeonSeed);

        Dungeon2 dungeonGeneration = Dungeon2.instance;

        List<RectInt> createdRooms = new();

        AlgorithmsUtils.DebugRectInt(dungeonGeneration.dungeonSize, Color.red);

        Queue<RectInt> Q = new();
        HashSet<RectInt> discovered = new();


        Q.Enqueue(dungeonGeneration.dungeonSize);

        while (Q.Count != 0)
        {
            RectInt currentRoom = Q.Dequeue();

            isHorizontalSplit = !isHorizontalSplit;

            //If it can split the room and it has not been discovered
            if (!discovered.Contains(currentRoom) && CanSplitRoom(currentRoom, discovered, isHorizontalSplit))
            {
                discovered.Add(currentRoom);

                RectInt[] splitRooms = SplitRoom(currentRoom, isHorizontalSplit);

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
                    //dungeonGeneration.drawColors.Add(dungeonGeneration.depthColor);


                    if (dungeonGeneration.generationType == GenerationType.Timed || dungeonGeneration.generationType == GenerationType.TimedStep)
                    {
                        yield return new WaitForSeconds(stepDelay);
                    }
                }
            }
        }
    }
    public bool CanSplitRoom(RectInt currentRoom, HashSet<RectInt> discovered, bool checkingHorizontal)
    {
        for (int i = 0; i < 2; i++)
        {
            if (checkingHorizontal)
            {
                //calculates if the minimal room size can fit in the current room split.
                if (currentRoom.height - Dungeon2.instance.minRoomSize > Dungeon2.instance.minRoomSize)
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
                if (currentRoom.width - Dungeon2.instance.minRoomSize > Dungeon2.instance.minRoomSize)
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
    public RectInt[] SplitRoom(RectInt currentRoom, bool isHorizontal)
    {
        RectInt[] nextRoomArray = new RectInt[2];

        //Checks which direction it has been split in
        if (isHorizontal)
        {
            int splitPosition = Random.Range(Dungeon2.instance.minRoomSize, currentRoom.height - Dungeon2.instance.minRoomSize);

            Vector2Int roomSize1 = new(currentRoom.width, splitPosition);
            Vector2Int roomSize2 = new(currentRoom.width, currentRoom.height - splitPosition);

            //Bottom Room Rect
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + Dungeon2.instance.wallMargin);
            //Top Room Rect
            nextRoomArray[1] = new(currentRoom.x, currentRoom.y + roomSize1.y, roomSize2.x, roomSize2.y);
        }
        else
        {
            int splitPosition = Random.Range(Dungeon2.instance.minRoomSize, currentRoom.width - Dungeon2.instance.minRoomSize);

            Vector2Int roomSize1 = new(splitPosition, currentRoom.height);
            Vector2Int roomSize2 = new(currentRoom.width - splitPosition, currentRoom.height);

            //Left Room Rect
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x + Dungeon2.instance.wallMargin, roomSize1.y);
            //Right Room Rect
            nextRoomArray[1] = new(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        return nextRoomArray;
    }
}
