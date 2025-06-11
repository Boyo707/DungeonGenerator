using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private PathFinder pathFinder;

    [SerializeField]
    private float speed = 5f;
    
    public void GoToDestination(Vector3 destination)
    {
        //Stops all the coroutines of the path follower
        StopAllCoroutines();

        //Starts the follow path coroutine after it has calculated the path from the player to its destination.
        StartCoroutine(FollowPathCoroutine(pathFinder.CalculatePath(transform.position, destination)));
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

            //If the distance between the next node and the player is more then 0.1f then move towards destination.
            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);
                yield return null;
            }
            Debug.Log($"Reached target: {target}");
        }
    }
}
