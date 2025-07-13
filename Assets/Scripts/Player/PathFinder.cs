using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    [Header("Required Class")]
    [SerializeField] private Dungeon2 dungeonGenerator;

    [Header("Camera")]
    [SerializeField] Camera skyCamera;

    [Header("Debug")]
    [SerializeField] private bool showGraph = false;

    private RectInt dungeonSize;

    private Vector3 startNode;
    private Vector3 endNode;

    public List<Vector3> path = new List<Vector3>();
    HashSet<Vector3> discovered = new HashSet<Vector3>();

    private Graph<Vector3> graph = new();
    private List<Vector3> floors = new();

    private bool hasGeneratedGraph = false;

    private void Update()
    {
        if (hasGeneratedGraph && showGraph)
        {
            foreach (var node in graph.GetNodes())
            {
                DebugExtension.DebugWireSphere(node, Color.cyan, .2f);
                foreach (var neighbor in graph.GetNeighbors(node))
                {
                    Debug.DrawLine(node, neighbor, Color.cyan);
                }
            }
        }
    }

    public void SetGraph(List<Vector3> createdFloors, RectInt dungeonSize)
    {
        floors = createdFloors;

        this.dungeonSize = dungeonSize;

        GenerateGraph();

        //Once it's done generating it will show the generated graph
        hasGeneratedGraph = true;

        //switches the camera in the sky off and switches to the player camera
        skyCamera.enabled = false;
    }

    #region GeneratingGraph
    void GenerateGraph()
    {
        graph.Clear();

        for (int x = dungeonSize.xMin; x < dungeonSize.xMax; x++)
        {
            for (int y = dungeonSize.yMin; y < dungeonSize.yMax; y++)
            {
                //looks at the currentposition with some offset
                Vector3 currentPos = new Vector3(x + .5f, 0, y + .5f);

                //Straight Direction
                TryConnectNeighbor(x + 1, y, currentPos);    //Right
                TryConnectNeighbor(x - 1, y, currentPos);    //left
                TryConnectNeighbor(x, y + 1, currentPos);    //Up
                TryConnectNeighbor(x, y - 1, currentPos);    //Down

                //Diagonal Directions
                TryConnectNeighbor(x + 1, y + 1, currentPos); //Top right
                TryConnectNeighbor(x - 1, y + 1, currentPos); //Top left
                TryConnectNeighbor(x + 1, y - 1, currentPos); //Bottm right
                TryConnectNeighbor(x - 1, y - 1, currentPos); //Bottom left
            }
        }
        Debug.Log("Finished creating player walkable Path");
    }
    private void TryConnectNeighbor(int nx, int ny, Vector3 currentPos)
    {
        //if the x and y posiition si withing the dungeonsize
        if (nx >= dungeonSize.xMin && nx < dungeonSize.xMax &&
            ny >= dungeonSize.yMin && ny < dungeonSize.yMax)
        {
            Vector3 neighborPos = new Vector3(nx + .5f ,0, ny + .5f);

            //checks if this potential position and the currentposition is found inside the floors position
            if (floors.Contains(neighborPos) && floors.Contains(currentPos))
            {
                //Adds an edge between the currentposition and the newly found neighbour position
                graph.AddEdge(currentPos, neighborPos);
            }
        }
    }
    #endregion

    public List<Vector3> CalculatePath(Vector3 from, Vector3 to)
    {
        Vector3 playerPosition = from;

        startNode = GetClosestNodeToPosition(playerPosition);

        endNode = GetClosestNodeToPosition(to);

        return AStar(startNode, endNode);
    }
    private Vector3 GetClosestNodeToPosition(Vector3 position)
    {
        
        //Lowers the position to 0 for more accurate calculations
        Vector3 loweredPos = new Vector3(position.x, 0, position.z);
        
        List<Vector3> nodes = graph.GetNodes();

        for (int i = 0; i < nodes.Count; i++)
        {
            if (Vector3.Distance(nodes[i], loweredPos) <= 0.6f)
            {
                return nodes[i];
            }
        }

        //if it has not found one then go back to the starting position
        return startNode;
    }

    List<Vector3> AStar(Vector3 start, Vector3 end)
    {
        discovered.Clear();

        Vector3 v = start;

        List<(Vector3 node, float priority)> priorityQueue = new List<(Vector3, float)>();

        Dictionary<Vector3, float> cost = new();

        Dictionary<Vector3, Vector3> parent = new();

        priorityQueue.Add((v, 0));
        discovered.Add(v);

        cost[v] = 0;

        while (priorityQueue.Count > 0)
        {
            //sorts the Queue by priority
            priorityQueue = priorityQueue.OrderByDescending(node => node.priority).ToList();
            
            v = priorityQueue[priorityQueue.Count - 1].node;
            priorityQueue.RemoveAt(priorityQueue.Count - 1);
            discovered.Add(v);

            //if the current node is the end node then construct the path towards the node.
            if (v == end)
            {
                return ReconstructPath(parent, start, end);
            }

            foreach (Vector3 w in graph.GetNeighbors(v))
            {
                float newCost = cost[v] + Cost(v, w);

                if (!cost.ContainsKey(w) || newCost < cost[w])
                {
                    cost[w] = newCost;

                    parent[w] = v;

                    priorityQueue.Add((w, newCost + Heuristic(w, end)));
                }
            }
        }

        return new List<Vector3>();
    }

    public float Cost(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to);
    }
    public float Heuristic(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to);
    }
    List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> parentMap, Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();

        Vector3 currentNode = end;

        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = parentMap[currentNode];
        }

        path.Add(start);

        //reverse the path so it goes from the start to the end and not the reverse.
        path.Reverse();
        return path;
    }
}
