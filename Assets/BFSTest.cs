using UnityEngine;

public class BFSTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Graph<string> graph = new Graph<string>();

        graph.AddNode("A"); graph.AddNode("B"); graph.AddNode("C");
        graph.AddNode("D"); graph.AddNode("E");

        graph.AddEdge("A", "B"); graph.AddEdge("A", "C");
        graph.AddEdge("B", "D"); graph.AddEdge("C", "D");
        graph.AddEdge("D", "E");

        Debug.Log("Graph Structure: ");
        graph.PrintGraph();

        Debug.Log("BFS traversal: ");
        graph.BFS("A");

        Debug.Log("DFS traversal");
        graph.DFS("A");
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
