using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Runtime.InteropServices.WindowsRuntime;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt dungeonSize;
    [SerializeField] private int wallMargin = 1;

    [SerializeField] private int testa;
    [SerializeField] private int testb;

    [SerializeField] private int minRoomSize;
    [SerializeField] private int splitDepth;

    [SerializeField]private List<RectInt> rooms = new List<RectInt>();

    private bool isSplittingHorizontal = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SplitRoom(dungeonSize);

        SplitRoom1(dungeonSize);
    }

    // Update is called once per frame
    void Update()
    {
        //main dungeon size
        AlgorithmsUtils.DebugRectInt(dungeonSize, Color.red);

        Debug.Log(rooms.Count);

        for (int i = 0; i < rooms.Count; i++)
        {
            AlgorithmsUtils.DebugRectInt(rooms[i], Color.blue);
        }
    }


    private void SplitRoom1(RectInt room)
    { 
        int splitAmount = Random.Range(0, 3);

        //create lengths - must happen once?
        //random range int for one set of width and height
        //calculate the remaining height and width
        //store those 4 values perhaps in a rectInt just for storing or an array of ints
        //created 2 lengths and 2 widths

        //loop 4 times through to create the new 4 sets of rooms.
        //create and add the new rect int to the rooms list
        //then loop through the depth and keep repeating the cycle of calculating the lengths and creating the new rooms
        //maybe add last created rooms into a seperate list containing the real final rooms.

        //layer 1
        RectInt[] roomsA = CreateRooms(room);

        for (int i = 0; i < roomsA.Length; i++)
        {
            //get 1 room (i)
            //create rooms from i
            //
        }

        //turn this into a for loop


        //calcutlate heights and widths
        //change them into rectInts
        //create an array of the rooms

        //splits it into 4, Loops through array
        //Something that checks how many times it will subdivide them.
        for (int i = 0; i < 1; i++)
        {
           

            //split the I room depper with the split depth...
            //create the diffrent rooms

            //generate 1st layer

            //loop through the list

        }

        for (int i = 0; i < splitDepth; i++)
        {

        }

        RectInt newRoom = new RectInt(0, 0,
            Random.Range(minRoomSize, room.width - minRoomSize),
            Random.Range(minRoomSize, room.height - minRoomSize));

        Debug.Log(newRoom.width);
        Debug.Log(newRoom.height);

        int someHeight = room.height - newRoom.height;
        RectInt roomTop = new RectInt(0, newRoom.height, newRoom.width, someHeight);

        int someWidth = room.width - roomTop.width;
        RectInt roomTopOther = new RectInt(newRoom.width, newRoom.height, someWidth, roomTop.height);
        RectInt roomBottomOther = new RectInt(newRoom.width, 0, someWidth, newRoom.height);

        rooms.Add(roomsA[0]);
        rooms.Add(roomsA[1]);
        rooms.Add(roomsA[2]);
        rooms.Add(roomsA[3]);
    }

    public Vector2Int CalculateSizes(int currentLength, int maxLength)
    {
        return new Vector2Int(currentLength, maxLength - currentLength);
    }

    public RectInt[] CreateRooms(RectInt currentRoomSize)
    {
        
        int randomHeight = Random.Range(minRoomSize, currentRoomSize.height - minRoomSize);
        int randomWidth = Random.Range(minRoomSize, currentRoomSize.width - minRoomSize);

        //uses the random sizes and the max sizes
        Vector2Int heights = CalculateSizes(randomHeight, currentRoomSize.height);
        Vector2Int widths = CalculateSizes(randomWidth, currentRoomSize.width);

        RectInt[] nextRoomArray = new RectInt[4];

        //top left
        nextRoomArray[0] = new RectInt(0, 0, widths.x, heights.x);
        //top right
        nextRoomArray[1] = new RectInt(widths.x, 0, widths.y, heights.x);
        //bottom left
        nextRoomArray[2] = new RectInt(0, heights.x, widths.x, heights.y);
        //bottom right
        nextRoomArray[3] = new RectInt(widths.x, heights.x, widths.y, heights.y);

        return nextRoomArray;
    }
}
