using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CompositeCollider2D;

public class RoomSplitting : MonoBehaviour
{
    [SerializeField] private List<RectInt> createdRooms = new();

    //Room Splitting variables
    private bool isSplittingHorizontal = true;

    private int currentDepth;
    private int roomCounter = 0;

    private int expectedRoomSplits = 0;
    private int roomDeduction = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator Splitting(float stepDelay)
    {
        Dungeon2 dungeonGeneration = Dungeon2.instance;

        AlgorithmsUtils.DebugRectInt(dungeonGeneration.dungeonSize, Color.red);

        Queue<RectInt> Q = new();
        HashSet<RectInt> discovered = new();

        Q.Enqueue(dungeonGeneration.dungeonSize);

        //If the que is not 0 AND currentDepth is not the final splitDepth
        while (currentDepth != dungeonGeneration.splitDepth && Q.Count != 0)
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
                    dungeonGeneration.drawColors.Add(dungeonGeneration.depthColor);
                    roomCounter++;

                    if (dungeonGeneration.generationType == GenerationType.Timed || dungeonGeneration.generationType == GenerationType.TimedStep)
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
        yield return new WaitForSeconds(1);
    }

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
            Dungeon2.instance.depthColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        if (currentDepth == Dungeon2.instance.splitDepth)
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
                if (currentRoom.height - Dungeon2.instance.minRoomSize > Dungeon2.instance.minRoomSize)
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
                if (currentRoom.width - Dungeon2.instance.minRoomSize > Dungeon2.instance.minRoomSize)
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
            int splitHeight = Random.Range(Dungeon2.instance.minRoomSize, currentRoom.height - Dungeon2.instance.minRoomSize);

            //Sets the sizes of each room
            Vector2Int roomSize1 = new(currentRoom.width, splitHeight);
            Vector2Int roomSize2 = new(currentRoom.width, currentRoom.height - splitHeight);

            //Bottom Room Rect
            //adds more space for the wall
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x, roomSize1.y + Dungeon2.instance.wallMargin);
            //Top Room Rect
            nextRoomArray[1] = new(currentRoom.x, currentRoom.y + roomSize1.y, roomSize2.x, roomSize2.y);
        }
        else
        {
            //gets a random point where the room will be split
            int splitLength = Random.Range(Dungeon2.instance.minRoomSize, currentRoom.width - Dungeon2.instance.minRoomSize);

            //Sets the sizes of each room
            Vector2Int roomSize1 = new(splitLength, currentRoom.height);
            Vector2Int roomSize2 = new(currentRoom.width - splitLength, currentRoom.height);

            //Left Room Rect
            //adds more space for the wall
            nextRoomArray[0] = new(currentRoom.x, currentRoom.y, roomSize1.x + Dungeon2.instance.wallMargin, roomSize1.y);
            //Right Room Rect
            nextRoomArray[1] = new(roomSize1.x + currentRoom.x, currentRoom.y, roomSize2.x, roomSize2.y);
        }

        //returns the array of rooms
        return nextRoomArray;
    }
}
