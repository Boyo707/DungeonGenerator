using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NaughtyAttributes;
using Unity.AI.Navigation;

public enum SortingType
{
    Size,
    Position
}

public enum GenerationType
{
    Instant,
    Timed,
    Step,
    TimedStep
}
[RequireComponent(typeof(RoomSplitting), typeof(DoorNGraphGeneration), typeof(RoomRemover))]
[RequireComponent(typeof(WallSpawner), typeof(FloorFlood), typeof(NavMeshSurface))]
public class Dungeon2 : MonoBehaviour
{

    [Header("Dungeon Settings")]
    [SerializeField] public RectInt dungeonSize;
    [SerializeField] public int splitDepth;
    [SerializeField] public int minRoomSize;
    [SerializeField] public int wallMargin = 1;
    [SerializeField] public int doorWidth = 2;

    [Header("Generation Settings")]
    [SerializeField] private int dungeonSeed;
    [SerializeField] public GenerationType generationType = GenerationType.Step;
    [SerializeField] private float stepDelay;
    
    [Header("Visuals")]
    [SerializeField] public Transform wallsParent; 
    [SerializeField] public Transform floorsParent;
    [SerializeField] public Vector3 offset;
    [SerializeField] public GameObject[] wallPrefabs;

    [Header("Playability")]
    [SerializeField] private Transform player;
    [SerializeField] private PathFinder pathFinder;

    [Header("Debug")]
    [SerializeField] private bool showRoomOutline = true;
    [SerializeField] private bool showDoorsOutline = true;
    [SerializeField] private bool showConnectionGraph = true;
    [SerializeField] private bool showVisuals = true;

    [SerializeField] public List<RectInt> createdRooms = new();
    [SerializeField] public List<RectInt> createdDoors = new();
    [SerializeField] public List<Vector3> createdFloors = new();

    public Graph<RectInt> roomAdjacencyList = new();

    public int[,] tileMap;

    private RoomSplitting roomSplitting;
    private DoorNGraphGeneration doorNGraphGen;
    private RoomRemover roomRemover;
    private WallSpawner wallSpawner;
    private FloorFlood floorFlood;
    private NavMeshSurface navMesh;

    //A button that starts the next step of the dungeon generation
    private bool continueStep = false;
    [Button]
    public void StartStep()
    {
        continueStep = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomSplitting = GetComponent<RoomSplitting>();
        doorNGraphGen = GetComponent<DoorNGraphGeneration>();
        roomRemover = GetComponent<RoomRemover>();
        wallSpawner = GetComponent<WallSpawner>();
        floorFlood = GetComponent<FloorFlood>();
        navMesh = GetComponent<NavMeshSurface>();

        //If the seed wasnt given from the start then create a seed
        if (dungeonSeed == 0)
        {
            dungeonSeed = Random.seed;
        }

        Random.InitState(dungeonSeed);

        //Takes the wall margin into account so that intersecting walls dont make the room to small.
        minRoomSize += wallMargin;
        
        //Start Generation
        StartCoroutine(Generation());
    }

    // Update is called once per frame
    void Update()
    {
        
        //Draws the main outline

        if (showRoomOutline)
        {
            for (int i = 0; i < createdRooms.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdRooms[i], Color.red);
            }
        }
        
        if (showDoorsOutline)
        {
            for (int i = 0; i < createdDoors.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdDoors[i], Color.yellow);
            }
        }

        if (showConnectionGraph)
        {
            foreach (var item in roomAdjacencyList.GetNodes())
            {
                //Gets the center of the door
                Vector3 doorCenter = GetCenter(item);
                DebugExtension.DebugWireSphere(doorCenter, Color.blue);

                var roomConnection = roomAdjacencyList.GetNeighbors(item);

                //Creating a sphere for each room and drawing lines towards the door
                foreach(var neighbour in roomAdjacencyList.GetNeighbors(item))
                { 
                    Vector3 roomCenter = GetCenter(neighbour);
                    DebugExtension.DebugWireSphere(roomCenter, Color.green);

                    Debug.DrawLine(roomCenter, doorCenter);
                }
            }
        }

        wallsParent.gameObject.SetActive(showVisuals);
        floorsParent.gameObject.SetActive(showVisuals);
    }

    IEnumerator Generation()
    {
        //rooms splitting
        yield return StartCoroutine(roomSplitting.Splitting(stepDelay, dungeonSeed, this));
        Debug.Log("Finished Room Generation");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Removed 10% of the room and sort the rooms by size
        yield return createdRooms = roomRemover.SortRooms(createdRooms, this);
        Debug.Log("Finished Room Sorting And Removed 10% of the smallest rooms");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Create the door and graph
        yield return StartCoroutine(doorNGraphGen.CreateDoorGraph(stepDelay, dungeonSeed, this));
        Debug.Log("Finished creating the Doors and the Graph");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Create the walls
        yield return StartCoroutine(wallSpawner.GenerateWalls(stepDelay, dungeonSeed, this));
        Debug.Log("Finished creating the walls");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Flooding the floors
        yield return StartCoroutine(floorFlood.GenerateFloors(stepDelay, dungeonSeed, this));
        Debug.Log("Finished flooding the rooms with floors");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        
        navMesh.BuildNavMesh();

        player.position = new Vector3(createdRooms[0].xMin + 1.5f, 1, createdRooms[0].yMin + 1.5f);

        pathFinder.SetGraph(createdFloors, dungeonSize);
    }

    private Vector3 GetCenter(RectInt area)
    {
        //returns a vector3 of the areas center
        return new Vector3(area.center.x, 0, area.center.y);
    }
}
