using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BackgroundElement
{
    public GameObject obj;
    
    //1 = fully follow the player (very far from the player) | 0 = does not follow the player (very close to the player)
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
    void LateUpdate()
    {
        if (player == null) return;

        //amount player moves
        Vector3 playerDelta = player.transform.position - lastPlayerPosition;

        //for each background move a certain percentage of the player moves
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
