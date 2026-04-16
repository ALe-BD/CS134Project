using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BackgroundElement
{
    public GameObject obj;
    [Range(0f, 1f)] public float followAmount = 0.5f;
}
public class ParallaxManager : MonoBehaviour
{
    [SerializeField] private List<BackgroundElement> backgroundElements;
    [SerializeField] private GameObject player;
    // Start is called before the first frame update
    private Vector3 lastPlayerPosition;

    void Start()
    {
        if (player != null)
        {
            lastPlayerPosition = player.transform.position;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (player == null) return;

        Vector3 playerDelta = player.transform.position - lastPlayerPosition;

        foreach (BackgroundElement element in backgroundElements)
        {
            if (element.obj == null) continue;

            Vector3 pos = element.obj.transform.position;
            pos.x += playerDelta.x * element.followAmount;
            element.obj.transform.position = pos;
        }

        lastPlayerPosition = player.transform.position;
    }
}
