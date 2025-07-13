using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PathFinder pathFinder;
    [SerializeField] private NavMeshAgent navAgent;

    private enum PathFindingType
    {
        AStar,
        NavMesh
    }

    [Header("Path Finding Settings")]
    [SerializeField] private PathFindingType pathFindingType;
    [SerializeField] private float speed = 5f;
    
    public void GoToDestination(Vector3 destination)
    {
        //stops any coroutines before it.
        StopAllCoroutines();

        if(pathFindingType == PathFindingType.AStar)
        {
            //if navAgent is enabled, the player will stutter as nav agent tries to reposition.
            navAgent.enabled = false;

            StartCoroutine(FollowPathCoroutine(pathFinder.CalculatePath(transform.position, destination)));
        }
        else
        {
            navAgent.enabled = true;
            navAgent.destination = destination;
            navAgent.speed = speed;
        }
    }

    IEnumerator FollowPathCoroutine(List<Vector3> path)
    {
        //if the path is empty or it does not have anything in the list then do not create the path
        Debug.Log("Following");
        if (path == null || path.Count == 0)
        {
            Debug.Log("No path found");
            yield break;
        }
        for (int i = 0; i < path.Count; i++)
        {
            //pushes the target up 1 to compensate player size
            Vector3 target = path[i] + Vector3.up;

            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);
                yield return null;
            }
        }
    }
}
