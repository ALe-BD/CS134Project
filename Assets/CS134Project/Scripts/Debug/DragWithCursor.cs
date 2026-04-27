using UnityEngine;

public class DragWithCursor : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(Camera.main.transform.position.z);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f;

            transform.position = worldPos;
        }
    }
}
