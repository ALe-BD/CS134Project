using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleAnim : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new Vector3(15f, 30f, 45f);
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatHeight = 0.25f;

    public Vector3 startPos;
    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Rotation
        transform.Rotate(rotationSpeed * Time.deltaTime);

        // Up/down motion
        float offsetY = (Mathf.Sin(Time.time * floatSpeed) + 1f) * 0.5f * floatHeight;
        transform.position = new Vector3(
            transform.position.x,
            startPos.y + offsetY,
            transform.position.z
        );
    }
}
