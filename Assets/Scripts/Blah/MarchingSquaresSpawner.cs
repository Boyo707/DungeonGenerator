using NaughtyAttributes;
using NUnit.Framework.Constraints;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MarchingSquaresSpawner : MonoBehaviour
{
    //private TileMapGenerator _generator;

    [SerializeField] private GameObject[] walls;

    private Vector2 currentPos = Vector2.zero;

    private int[,] currentTilemap;
    private int[,] binaryTile;

    private int x;
    private int y;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //_generator = GetComponent<TileMapGenerator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void test()
    {

    }

    [Button]
    public void ConvertToBinary()
    {
        //currentTilemap = _generator.GetTileMap();

        binaryTile = new int[currentTilemap.GetLength(0) - 1, currentTilemap.GetLength(1) - 1];

        Debug.Log(currentTilemap);
        Debug.Log(currentTilemap.GetLength(0));
        Debug.Log(currentTilemap.GetLength(1));

        //y
        for (int i = 0; i < currentTilemap.GetLength(0) - 1; i++)
        {
            //x 
            for (int j = 0; j < currentTilemap.GetLength(1) - 1; j++)
            {
                //Debug.Log(currentTilemap[i, j]);
                //Debug.Log(currentTilemap[i, j + 1]);
                //Debug.Log(currentTilemap[i + 1, j + 1]);
                //Debug.Log(currentTilemap[i + 1, j]);
                //Debug.Log("j/x is = " + j); 

                binaryTile[i,j] = currentTilemap[i, j] * 1 +
                   currentTilemap[i, j + 1] * 2 +
                   currentTilemap[i + 1, j + 1] * 4 +
                   currentTilemap[i + 1, j] * 8;

                //Debug.Log(binarythingy);
                //get the 4 squares around this position
                //int topLeft = j,i;
                //int topRight = j,i + 1;
                //int bottomRight = j - 1, i + 1;
                //int bottomLeft = j - 1, i;
            }
        }
    }

    [Button]
    public void PlacePrefabs()
    {
        StartCoroutine(SpawnWall());
    }

    IEnumerator SpawnWall()
    {
        //y
        for (int i = 0; i < binaryTile.GetLength(0); i++)
        {
            //x
            for (int j = 0; j < binaryTile.GetLength(1); j++)
            {
                //Debug.Log(binaryTile[i, j]);
                //walls 0 = empty
                //walls 1 = wall end
                //walls 2 = wall    
                //walls 3 = wall corner


                GameObject wall = gameObject;
                Vector3 rotation = Vector3.zero;

                switch (binaryTile[i,j])
                {
                    case 0:
                        wall = walls[0];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 1:
                        wall = walls[1];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 2:
                        wall = walls[1];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 3:
                        wall = walls[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 4:
                        wall = walls[1];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 5:
                        wall = walls[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 6:
                        wall = walls[2];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 7:
                        wall = walls[3];
                        rotation = new Vector3(0, -90, 0);
                        break;
                    case 8:
                        wall = walls[1];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 9:
                        wall = walls[2];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 10:
                        wall = walls[2];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 11:
                        wall = walls[3];
                        rotation = new Vector3(0, 0, 0);
                        break;
                    case 12:
                        wall = walls[2];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 13:
                        wall = walls[3];
                        rotation = new Vector3(0, 90, 0);
                        break;
                    case 14:
                        wall = walls[3];
                        rotation = new Vector3(0, 180, 0);
                        break;
                    case 15:
                        wall = walls[0];
                        rotation = new Vector3(0, 0, 0);
                        break;
                }
                x = j;
                y = i;
                Debug.Log(rotation);
                GameObject prefab = Instantiate(wall, new Vector3(j + 1f, 0, i + 1f), Quaternion.identity);
                Instantiate(walls[0], new Vector3(j + 1f, 0, i + 1f), Quaternion.identity);
                Debug.Log(binaryTile[i, j]);
                prefab.transform.localEulerAngles = rotation;
                yield return new WaitForSeconds(0.0f);
            }
        }

        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(new Vector3(x + 1.2f, 0, y + 1.2f), new Vector3(2, 0.5f, 2));

    }
}
