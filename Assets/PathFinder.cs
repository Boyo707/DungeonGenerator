using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class PathFinder : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeonGenerator;

    private Vector3 startNode;
    private Vector3 endNode;

    public List<Vector3> path = new List<Vector3>();
    HashSet<Vector3> discovered = new HashSet<Vector3>();

    private Graph<Vector3> graph = new();
    private List<Vector3> floors = new();

    private bool isActivated = false;

    private RectInt dungeonSize;


    private void Update()
    {
        /*if (isActivated)
        {
            foreach (var node in graph.GetNodes())
            {
                DebugExtension.DebugWireSphere(node, Color.cyan, .2f);
                foreach (var neighbor in graph.GetNeighbors(node))
                {
                    Debug.DrawLine(node, neighbor, Color.cyan);
                }
            }
        }*/
    }

    public void SetGraph(List<Vector3> createdFloors, RectInt dungeonSize)
    {
        floors = createdFloors;

        this.dungeonSize = dungeonSize;

        GenerateGraph();
        
        isActivated = true;
    }

    #region GeneratingGraph
    void GenerateGraph()
    {
        graph.Clear();

        for (int x = dungeonSize.xMin; x < dungeonSize.xMax; x++)
        {
            for (int y = dungeonSize.yMin; y < dungeonSize.yMax; y++)
            {
                Vector3 currentPos = new Vector3(x + .5f, 0, y + .5f);

                // Cardinal directions (up, down, left, right)
                TryConnectNeighbor(x + 1, y, currentPos);    // Right
                TryConnectNeighbor(x - 1, y, currentPos);    // Left
                TryConnectNeighbor(x, y + 1, currentPos);    // Up
                TryConnectNeighbor(x, y - 1, currentPos);    // Down

                // Diagonal directions
                TryConnectNeighbor(x + 1, y + 1, currentPos); // Top-right
                TryConnectNeighbor(x - 1, y + 1, currentPos); // Top-left
                TryConnectNeighbor(x + 1, y - 1, currentPos); // Bottom-right
                TryConnectNeighbor(x - 1, y - 1, currentPos); // Bottom-left
            }
        }
    }
    private void TryConnectNeighbor(int nx, int ny, Vector3 currentPos)
    {
        if (nx >= dungeonSize.xMin && nx < dungeonSize.xMax &&
            ny >= dungeonSize.yMin && ny < dungeonSize.yMax)
        {
            Vector3 pos = new Vector3(nx + .5f ,0, ny + .5f);

            if (floors.Contains(pos) && floors.Contains(currentPos))
            {
                Vector3 neighborPos = new Vector3(nx + .5f, 0, ny + .5f);
                graph.AddEdge(currentPos, neighborPos);
            }
        }
    }
    #endregion

    public List<Vector3> CalculatePath(Vector3 from, Vector3 to)
    {
        Vector3 playerPosition = from;

        Debug.Log(playerPosition);
        startNode = GetClosestNodeToPosition(playerPosition);
        Debug.Log(startNode);
        endNode = GetClosestNodeToPosition(to);
        Debug.Log(endNode);

        return AStar(startNode, endNode);
    }
    private Vector3 GetClosestNodeToPosition(Vector3 position)
    {
        Vector3 roundedPos = new Vector3(position.x, 0, position.z);
        Debug.Log(roundedPos);
        List<Vector3> nodes = graph.GetNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (Vector3.Distance(nodes[i], roundedPos) < 0.6f)
            {
                return nodes[i];
            }
        }
        return startNode;
    }

    List<Vector3> AStar(Vector3 start, Vector3 end)
    {
        discovered.Clear();

        Vector3 v = start;

        List<(Vector3 node, float priority)> Q = new List<(Vector3, float)>();

        Dictionary<Vector3, float> C = new();
        Dictionary<Vector3, Vector3> P = new();

        Q.Add((v, 0));
        discovered.Add(v);

        C[v] = 0;

        while (Q.Count > 0)
        {
            Q = Q.OrderByDescending(node => node.priority).ToList();
            v = Q[Q.Count - 1].node;
            Q.RemoveAt(Q.Count - 1);
            discovered.Add(v);

            if (v == end)
            {
                return ReconstructPath(P, start, end);
            }

            foreach (Vector3 w in graph.GetNeighbors(v))
            {

                float newCost = C[v] + Cost(v, w);
                if (!C.ContainsKey(w) || newCost < C[w])
                {
                    C[w] = newCost;
                    P[w] = v;

                    //cost
                    Q.Add((w, newCost + Heuristic(w, end)));
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
        path.Reverse();
        return path;
    }
}
