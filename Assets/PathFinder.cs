using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    [Header("Required Class")]
    [SerializeField] private DungeonGenerator dungeonGenerator;

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
        //gets the floors
        floors = createdFloors;

        //sets the dungeon size
        this.dungeonSize = dungeonSize;

        //Generates the graph
        GenerateGraph();

        //Once it's done generating it will show the generated graph
        hasGeneratedGraph = true;

        skyCamera.enabled = false;
    }

    #region GeneratingGraph
    void GenerateGraph()
    {
        //clears the graph if there was something there already
        graph.Clear();

        //loops through the position of the dungeon
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
        //the playerposition is the from
        Vector3 playerPosition = from;

        Debug.Log(playerPosition);

        //finds the closest node to the player
        startNode = GetClosestNodeToPosition(playerPosition);
        Debug.Log(startNode);

        //finds the closest clicked node 
        endNode = GetClosestNodeToPosition(to);
        Debug.Log(endNode);

        //calculates the path in Astar
        return AStar(startNode, endNode);
    }
    private Vector3 GetClosestNodeToPosition(Vector3 position)
    {
        //Lowers the position to 0 for better reach
        Vector3 loweredPos = new Vector3(position.x, 0, position.y);
        
        // Gets the nodes of the graph and loops through them
        List<Vector3> nodes = graph.GetNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            //If it found a node that's closer then 0.6 then return
            if (Vector3.Distance(nodes[i], loweredPos) < 0.6f)
            {
                return nodes[i];
            }
        }
        //if it has not found one then go back to the starting position
        return startNode;
    }

    List<Vector3> AStar(Vector3 start, Vector3 end)
    {
        //clears the previous discovered path
        discovered.Clear();

        //v = the starting node
        Vector3 v = start;

        //creates a list Queue with priorites on the node
        List<(Vector3 node, float priority)> Q = new List<(Vector3, float)>();

        //Costs of the Node
        Dictionary<Vector3, float> C = new();
        //Parents of the the nodes
        Dictionary<Vector3, Vector3> P = new();

        Q.Add((v, 0));
        discovered.Add(v);

        //sets the cost of the first node on 0
        C[v] = 0;
        Debug.Log("Before while");
        while (Q.Count > 0)
        {
            Debug.Log("Inside While");
            //sorts the Queue by priority
            Q = Q.OrderByDescending(node => node.priority).ToList();
            // gets the starting node
            v = Q[Q.Count - 1].node;
            Q.RemoveAt(Q.Count - 1);
            discovered.Add(v);

            Debug.Log("Before IF");
            //if the current node is the end node then construct the path towards the node.
            if (v == end)
            {
                Debug.Log("Finished");
                return ReconstructPath(P, start, end);
            }

            Debug.Log("Before ForEach");
            //For every neighbour of the node
            foreach (Vector3 w in graph.GetNeighbors(v))
            {
                Debug.Log("Inside Neighbour Foreach");
                //Set a new cost
                float newCost = C[v] + Cost(v, w);

                //if There is no cost on the neighbour OR the newcost is lower then the neighbour cost
                if (!C.ContainsKey(w) || newCost < C[w])
                {
                    Debug.Log("in if state");

                    //sets the new cost
                    C[w] = newCost;
                    //sets the parent of the node
                    P[w] = v;

                    //adds the neighbour to the Queue and adds a Heuristic
                    //Heuristic is the distance beteen the neighbour node and the ending node
                    Q.Add((w, newCost + Heuristic(w, end)));
                }
            }
        }
        //returns an empty list if there is no queue anymore
        return new List<Vector3>();
    }

    public float Cost(Vector3 from, Vector3 to)
    {
        //The distance between the 2 nodes = the cost
        return Vector3.Distance(from, to);
    }
    public float Heuristic(Vector3 from, Vector3 to)
    {
        //Distance between the current node and the ending node
        return Vector3.Distance(from, to);
    }
    List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> parentMap, Vector3 start, Vector3 end)
    {
        //Creates the path list
        List<Vector3> path = new List<Vector3>();

        //sets the ending node as the starting node
        Vector3 currentNode = end;

        //if the currentNode is not the starting node then loop
        while (currentNode != start)
        {
            //add the currentNode to the path and set the next node as the current.
            path.Add(currentNode);
            currentNode = parentMap[currentNode];
        }

        //Add the starting node to the path
        path.Add(start);

        //reverse the path so it goes from the start to the end and not the reverse.
        path.Reverse();
        return path;
    }
}
