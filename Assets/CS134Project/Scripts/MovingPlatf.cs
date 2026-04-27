using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class MovingPlatf : MonoBehaviour
{
    [SerializeField] GameObject pointA;
    [SerializeField] GameObject pointB;
    [SerializeField] float speed = 10f;
    [SerializeField] float delay = 1f;
    [SerializeField] GameObject platform;

    private Vector3 targetPosition;

    // Start is called before the first frame update
    void Start()
    {
        platform.transform.position = pointA.transform.position;
        targetPosition = pointB.transform.position;
        StartCoroutine(MovePlatf());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator MovePlatf()
    {
        while (true)
        {
            while((targetPosition - platform.transform.position).sqrMagnitude > 0.01f) 
            {
                platform.transform.position = Vector3.MoveTowards(platform.transform.position, targetPosition, speed * Time.deltaTime);
                yield return null;
            }
            
            if (targetPosition == pointA.transform.position)
            {
                targetPosition = pointB.transform.position;
            }
            else
            {
                targetPosition = pointA.transform.position;
            }

            yield return new WaitForSeconds(delay);
        }
    }
}
