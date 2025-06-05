using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private PathFinder pathFinder;

    [SerializeField]
    private float speed = 5f;

    private bool isMoving = false;

    public void GoToDestination(Vector3 destination)
    {
        StopAllCoroutines();
        Debug.Log("Helllo");
        StartCoroutine(FollowPathCoroutine(pathFinder.CalculatePath(transform.position, destination)));
    }

    

    IEnumerator FollowPathCoroutine(List<Vector3> path)
    {
        Debug.Log("Following");
        if (path == null || path.Count == 0)
        {
            Debug.Log("No path found");
            yield break;
        }
        isMoving = true;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 target = path[i] + Vector3.up;
            // Move towards the target position
            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);
                yield return null;
            }

            Debug.Log($"Reached target: {target}");
        }
        isMoving = false;
    }
}
