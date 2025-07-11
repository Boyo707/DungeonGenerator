using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CompositeCollider2D;
using UnityEngine.Tilemaps;

public class FloorFlood : MonoBehaviour
{
    private int[,] tileMap;
    [SerializeField] private List<Vector3> createdFloors = new();
    [SerializeField] private List<RectInt> createdRooms = new();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator lol(float stepDelay)
    {
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
            GameObject objec = Instantiate(Dungeon2.instance.wallPrefabs[0], new Vector3(current.y + 0.5f, 0, current.x + 0.5f), Quaternion.identity, Dungeon2.instance.floorsParent);

            //Gives the position and tilemap index for debug purposes
            objec.name = $"{current.x}, {current.y}, index: {tileMap[current.x, current.y]}";

            //if the tilemap index is 0 then add it to the createdFloors list
            if (tileMap[current.x, current.y] == 0)
            {
                createdFloors.Add(objec.transform.position);
            }

            if (Dungeon2.instance.generationType == GenerationType.Timed || Dungeon2.instance.generationType == GenerationType.TimedStep)
            {
                yield return new WaitForSeconds(stepDelay);
            }

            //gets the neighbours of the current floor tile
            foreach (Vector2Int tile in GetTileMapNeighbours(tileMap, current))
            {
                //if the floor is not discovered, not in the queue and is a valid index then enqueue the neibour
                if (!floorDiscovered.Contains(tile) && !floorQueue.Contains(tile) && tileMap[current.x, current.y] == 0)
                {
                    floorQueue.Enqueue(tile);
                }
            }
        }
    }

    public List<Vector2Int> GetTileMapNeighbours(int[,] tileMap, Vector2Int tileMapPos)
    {
        //creates a new list for the neighbours
        List<Vector2Int> neighbours = new();

        //set the min max y positions based on the tilemap length and curren position
        int yMin = Mathf.Clamp(tileMapPos.x - 1, 0, tileMap.GetLength(0));
        int yMax = Mathf.Clamp(tileMapPos.x + 1, 0, tileMap.GetLength(0));

        //sets the min max x positions based on the tilemap length and curren position
        int xMin = Mathf.Clamp(tileMapPos.y - 1, 0, tileMap.GetLength(1));
        int xMax = Mathf.Clamp(tileMapPos.y + 1, 0, tileMap.GetLength(1));


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
}
