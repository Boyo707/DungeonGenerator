using System.Collections.Generic;
using UnityEngine;

public class RoomRemover : MonoBehaviour
{

    public List<RectInt> SortRooms(List<RectInt> rooms)
    {
        //sorts the rooms based on size
        BubbleSorter(rooms, SortingType.Size);

        //createdRooms.Sort ((a,b) => a.width * a.height - b.width * b.height);

        //calculates how many rooms are in the 10%
        int amountOfRooms = Mathf.RoundToInt(0.10f * rooms.Count);

        //removes 10% of all the rooms
        for (int i = 0; i < amountOfRooms; i++)
        {
            rooms.RemoveAt(i);
        }
        //sorts the rooms by position
        BubbleSorter(rooms, SortingType.Position);

        return rooms;
    }

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
                else if (type == SortingType.Position)
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

}
