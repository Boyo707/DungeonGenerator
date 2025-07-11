using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NaughtyAttributes;
using System.Text;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using Mono.Cecil.Cil;

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
[RequireComponent(typeof(WallSpawner), typeof(FloorFlood), typeof(GeneratePathfinding))]
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

    [SerializeField] private List<RectInt> createdRooms = new();
    [SerializeField] private List<RectInt> createdDoors = new();
    [SerializeField] private List<Vector3> createdFloors = new();

    private RoomSplitting roomSplitting;
    private DoorNGraphGeneration doorNGraphGen;
    private RoomRemover roomRemover;
    private WallSpawner wallSpawner;
    private FloorFlood floorFlood;
    private GeneratePathfinding genPathfinding;


    public static Dungeon2 instance;


    //A button that starts the next step of the dungeon generation
    private bool continueStep = false;
    [Button]
    public void StartStep()
    {
        continueStep = true;
    }

    public List<Color> drawColors = new();
    public Color depthColor = Color.red;

    private void Awake()
    {
        instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomSplitting = GetComponent<RoomSplitting>();
        doorNGraphGen = GetComponent<DoorNGraphGeneration>();
        roomRemover = GetComponent<RoomRemover>();
        wallSpawner = GetComponent<WallSpawner>();
        floorFlood = GetComponent<FloorFlood>();
        genPathfinding = GetComponent<GeneratePathfinding>();

        //If the seed wasnt given from the start then create a seed
        if (dungeonSeed == 0)
        {
            dungeonSeed = Random.seed;
        }

        Random.InitState(dungeonSeed);

        //Takes the wall margin into account so that intersecting walls dont make the room to small.
        minRoomSize += wallMargin * 2;
        
        //Start Generation
        StartCoroutine(Generation(dungeonSize));
    }

    // Update is called once per frame
    void Update()
    {
        //Draws the main outline

        if (showRoomOutline)
        {
            for (int i = 0; i < createdRooms.Count; i++)
            {
                AlgorithmsUtils.DebugRectInt(createdRooms[i], drawColors[i]);
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
            /*foreach (var item in doorConnections)
            {
                //Gets the center of the door
                Vector3 doorCenter = GetCenter(item.Key);
                DebugExtension.DebugWireSphere(doorCenter, Color.blue);

                var roomConnection = doorConnections[item.Key];

                //Creating a sphere for each room and drawing lines towards the door
                for (int i = 0; i < roomConnection.Count; i++)
                {
                    Vector3 roomCenter = GetCenter(roomConnection[i]);
                    DebugExtension.DebugWireSphere(roomCenter, Color.green);

                    Debug.DrawLine(roomCenter, doorCenter);
                }
            }*/
        }

        wallsParent.gameObject.SetActive(showVisuals);
        floorsParent.gameObject.SetActive(showVisuals);
    }

    IEnumerator Generation(RectInt room)
    {
        //rooms splitting
        StartCoroutine(roomSplitting.Splitting(stepDelay));
        Debug.Log("Finished Room Generation");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Create the door and graph
        StartCoroutine(doorNGraphGen.CreateDoorGraph(stepDelay));
        Debug.Log("Finished creating the Doors and the Graph");


        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Removed 10% of the room and sort the rooms by size
        createdRooms = roomRemover.SortRooms(createdRooms);
        Debug.Log("Finished Room Sorting And Removed 10% of the smallest rooms");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Create the walls
        StartCoroutine(wallSpawner.lol(stepDelay));
        Debug.Log("Finished creating the walls");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //Flooding the floors
        StartCoroutine(floorFlood.lol(stepDelay));
        Debug.Log("Finished flooding the rooms with floors");

        yield return new WaitUntil(() => continueStep || generationType != GenerationType.Step && generationType != GenerationType.TimedStep);
        continueStep = false;

        //set the player position at the first room
        player.position = new Vector3(createdRooms[0].xMin + 1.5f, 1, createdRooms[0].yMin + 1.5f);

        //Gives the pathFinder it's required values
        pathFinder.SetGraph(createdFloors, dungeonSize);
        //activate playability
    }
}
