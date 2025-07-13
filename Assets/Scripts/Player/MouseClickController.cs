using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;


public class MouseClickController : MonoBehaviour
{
    [SerializeField] private UnityEvent<Vector3> OnClick;

    private Vector3 clickPosition;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo))
            {
                Vector3 clickWorldPosition = hitInfo.point;
                clickPosition = clickWorldPosition;
                OnClick.Invoke(clickPosition);
            }
        }

        DebugExtension.DebugWireSphere(clickPosition, Color.yellow, .1f);

        Debug.DrawLine(Camera.main.transform.position, clickPosition, Color.yellow);
    }
}
