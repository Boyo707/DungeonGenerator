using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorFlood : MonoBehaviour
{
    public IEnumerator GenerateFloors(float stepDelay, int dungeonSeed, Dungeon2 dungeonGenerator)
    {
        Random.InitState(dungeonSeed);

        Queue<Vector2Int> floorQueue = new();
        HashSet<Vector2Int> floorDiscovered = new();

        int[,] tileMap = dungeonGenerator.tileMap;

        //enqueues the first position inside of the first room
        floorQueue.Enqueue(new Vector2Int(dungeonGenerator.createdRooms[0].y + 1, dungeonGenerator.createdRooms[0].x + 1));

        while (floorQueue.Count > 0)
        {
            Vector2Int current = floorQueue.Dequeue();
            floorDiscovered.Add(current);

            GameObject objec = Instantiate(dungeonGenerator.wallPrefabs[0], new Vector3(current.y + 0.5f, 0, current.x + 0.5f), Quaternion.identity, dungeonGenerator.floorsParent);

            //Gives the position and tilemap index for debug purposes
            objec.name = $"{current.x}, {current.y}, index: {tileMap[current.x, current.y]}";

            //if the tilemap index is 0 (empty space) then add it to the createdFloors list
            if (tileMap[current.x, current.y] == 0)
            {
                dungeonGenerator.createdFloors.Add(objec.transform.position);
            }

            if (dungeonGenerator.generationType == GenerationType.Timed || dungeonGenerator.generationType == GenerationType.TimedStep)
            {
                yield return new WaitForSeconds(stepDelay);
            }

            foreach (Vector2Int tile in GetTileMapNeighbours(tileMap, current))
            {
                if (!floorDiscovered.Contains(tile) && !floorQueue.Contains(tile) && tileMap[current.x, current.y] == 0)
                {
                    floorQueue.Enqueue(tile);
                }
            }
        }
    }

    public List<Vector2Int> GetTileMapNeighbours(int[,] tileMap, Vector2Int tileMapPos)
    {
        List<Vector2Int> neighbours = new();

        //set the min max y positions based on the tilemap length and curren position
        int yMin = Mathf.Clamp(tileMapPos.x - 1, 0, tileMap.GetLength(0));
        int yMax = Mathf.Clamp(tileMapPos.x + 1, 0, tileMap.GetLength(0));

        //sets the min max x positions based on the tilemap length and curren position
        int xMin = Mathf.Clamp(tileMapPos.y - 1, 0, tileMap.GetLength(1));
        int xMax = Mathf.Clamp(tileMapPos.y + 1, 0, tileMap.GetLength(1));

        //y loop
        for (int i = yMin; i <= yMax; i++)
        {
            //x loop
            for (int j = xMin; j <= xMax; j++)
            {
                //Adds the neighbouring positions
                neighbours.Add(new Vector2Int(i, j));
            }
        }

        return neighbours;
    }
}
