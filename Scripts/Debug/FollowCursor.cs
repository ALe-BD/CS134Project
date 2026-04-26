using UnityEngine;

public class FollowCursor : MonoBehaviour
{
    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f; // keep object on 2D plane
        transform.position = mousePos;
    }
}