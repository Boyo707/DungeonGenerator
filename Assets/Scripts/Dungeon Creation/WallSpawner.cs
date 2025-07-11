using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CompositeCollider2D;
using UnityEngine.Tilemaps;

public class WallSpawner : MonoBehaviour
{
    private int[,] binaryTileMap;
    private int[,] tileMap;
    [SerializeField] private List<RectInt> createdRooms = new();
    [SerializeField] private List<RectInt> createdDoors = new();


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
        GameObject[] prefabs = Dungeon2.instance.wallPrefabs;
        //Creates the size of the tileMap
        tileMap = new int[Dungeon2.instance.dungeonSize.height, Dungeon2.instance.dungeonSize.width];

        //loops through all the rooms
        foreach (var createdRoom in createdRooms)
        {
            //loops through all the rooms again to find intersections
            foreach (var intersectRoom in createdRooms)
            {
                //if the rooms are not the same then continue
                if (createdRoom != intersectRoom)
                {
                    //Fills up any intersecting areas with wall spaces
                    AlgorithmsUtils.FillRectangle(tileMap, AlgorithmsUtils.Intersect(createdRoom, intersectRoom), 1);
                }
            }
            //creates the outline of the room
            AlgorithmsUtils.FillRectangleOutline(tileMap, createdRoom, 1);
        }

        //Loops through all the doors and makes space for them
        foreach (var door in createdDoors)
        {
            AlgorithmsUtils.FillRectangleOutline(tileMap, door, 0);
        }

        //Converts the tilemap into the binary tilemap
        ConvertToBinary(tileMap);

        //Loops through the binaryTileMap to instantiate the core responding wall
        //y loop
        for (int i = 0; i < binaryTileMap.GetLength(0); i++)
        {
            //x loop
            for (int j = 0; j < binaryTileMap.GetLength(1); j++)
            {
                GameObject wall = gameObject;
                Vector3 rotation = Vector3.zero;

                //Depending on the value, it sets the wall and it's rotation
                switch (binaryTileMap[i, j])
                {
                    case 0:
                        continue;
                    case 1:
                        wall = prefabs[1];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 2:
                        wall = prefabs[1];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 3:
                        wall = prefabs[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 4:
                        wall = prefabs[1];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 5:
                        wall = prefabs[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 6:
                        wall = prefabs[2];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 7:
                        wall = prefabs[3];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 8:
                        wall = prefabs[1];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 9:
                        wall = prefabs[2];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 10:
                        wall = prefabs[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 11:
                        wall = prefabs[3];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 12:
                        wall = prefabs[2];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 13:
                        wall = prefabs[3];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 14:
                        wall = prefabs[3];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 15:
                        //if the wall is 15 (has walls on every side) then continue
                        continue;
                }

                //instantiates the wall at the next position
                GameObject prefab = Instantiate(wall, new Vector3(j + 1f, 0, i + 1f), Quaternion.identity, Dungeon2.instance.wallsParent);

                //sets the rotation of the object
                prefab.transform.localEulerAngles = rotation;
                if (Dungeon2.instance.generationType == GenerationType.Timed || Dungeon2.instance.generationType == GenerationType.TimedStep)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }
        Debug.Log("Finished generating the visuals");
    }

    public void ConvertToBinary(int[,] tilemap)
    {
        //Sets the size of the binaryTilemap
        binaryTileMap = new int[tilemap.GetLength(0) - 1, tilemap.GetLength(1) - 1];

        //Marches through each position in a 2 by 2 square
        //y loop
        for (int i = 0; i < tilemap.GetLength(0) - 1; i++)
        {
            //x loop
            for (int j = 0; j < tilemap.GetLength(1) - 1; j++)
            {
                //Checks all the surrounding tiles
                //If a tile is 1 it will multiply it by their coreresponding value.
                binaryTileMap[i, j] = tilemap[i, j] * 1 +
                   tilemap[i, j + 1] * 2 +
                   tilemap[i + 1, j + 1] * 4 +
                   tilemap[i + 1, j] * 8;
            }
        }
    }

    
}
