using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeonGenerator;

    private Vector3 startNode;
    private Vector3 endNode;

    public List<Vector3> path = new List<Vector3>();
    HashSet<Vector3> discovered = new HashSet<Vector3>();

    private Graph<Vector3> graph;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGraph()
    {
        //Bool to activate the script
        //Graph = TileMap
        //Maybe create a Graph script to easily acces and modify different graphs in the dungeonGen
        //graph = dungeonGenerator
    }

    public List<Vector3> CalculatePath(Vector3 from, Vector3 to)
    {
        Vector3 playerPosition = from;

        startNode = GetClosestNodeToPosition(playerPosition);

        endNode = GetClosestNodeToPosition(to);

        return AStar(startNode, endNode);
    }

    private Vector3 GetClosestNodeToPosition(Vector3 position)
    {
        Vector3 roundedPos = new Vector3(Mathf.CeilToInt(position.x), Mathf.CeilToInt(position.y), Mathf.CeilToInt(position.z));

        List<Vector3> nodes = graph.GetNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (Vector3.Distance(nodes[i], position) < 0.6f)
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
