using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallSpawner : MonoBehaviour
{
    public IEnumerator GenerateWalls(float stepDelay, int dungeonSeed, Dungeon2 dungeonGeneration)
    {
        Random.InitState(dungeonSeed);

        List<RectInt> createdRooms = dungeonGeneration.createdRooms;

        GameObject[] prefabs = dungeonGeneration.wallPrefabs;
        
        int[,] tileMap = new int[dungeonGeneration.dungeonSize.height, dungeonGeneration.dungeonSize.width];
        dungeonGeneration.tileMap = tileMap;

        //This will ensure that all the rooms will not how walls going throught it
        //This can only happen if the wall margin is set to a big variable
        foreach (var createdRoom in createdRooms)
        {
            foreach (var intersectRoom in createdRooms)
            {
                if (createdRoom != intersectRoom)
                {
                    AlgorithmsUtils.FillRectangle(tileMap, AlgorithmsUtils.Intersect(createdRoom, intersectRoom), 1);
                }
            }
            AlgorithmsUtils.FillRectangleOutline(tileMap, createdRoom, 1);
        }

        //makes an empty space fior the doors
        foreach (var door in dungeonGeneration.createdDoors)
        {
            AlgorithmsUtils.FillRectangleOutline(tileMap, door, 0);
        }

        int[,] binaryTileMap = ConvertToBinary(tileMap);

        //y loop
        for (int i = 0; i < binaryTileMap.GetLength(0); i++)
        {
            //x loop
            for (int j = 0; j < binaryTileMap.GetLength(1); j++)
            {
                GameObject wall = gameObject;
                Vector3 rotation = Vector3.zero;

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
                        //if the wall is 15 (has walls on every side) then skip
                        continue;
                }

                //instantiates the wall at the next position
                GameObject prefab = Instantiate(wall, new Vector3(j + dungeonGeneration.offset.x, 0 + dungeonGeneration.offset.y, i + dungeonGeneration.offset.z), Quaternion.identity, dungeonGeneration.wallsParent);

                //sets the rotation of the object
                prefab.transform.localEulerAngles = rotation;
                if (dungeonGeneration.generationType == GenerationType.Timed || dungeonGeneration.generationType == GenerationType.TimedStep)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }
    }

    public int[,] ConvertToBinary(int[,] tilemap)
    {
        int[,] binaryTileMap = new int[tilemap.GetLength(0) - 1, tilemap.GetLength(1) - 1];

        //Marches through each position in a 2 by 2 square
        //y loop
        for (int i = 0; i < tilemap.GetLength(0) - 1; i++)
        {
            //x loop
            for (int j = 0; j < tilemap.GetLength(1) - 1; j++)
            {
                binaryTileMap[i, j] = tilemap[i, j] * 1 +
                   tilemap[i, j + 1] * 2 +
                   tilemap[i + 1, j + 1] * 4 +
                   tilemap[i + 1, j] * 8;
            }
        }
        return binaryTileMap;
    }

    
}
