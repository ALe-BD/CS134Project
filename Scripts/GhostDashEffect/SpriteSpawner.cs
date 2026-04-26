using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSpawner : MonoBehaviour
{
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private Transform parentObject;
    [SerializeField] private Color32 color = new Color32 (255,255,255,100);

    private float timer;
    private SpriteRenderer sourceSprite;

    void Start()
    {
        sourceSprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sourceSprite == null) return;

        timer += Time.deltaTime;

        //Spawns every spawnInterval
        if (timer >= spawnInterval)
        {
            SpawnSprite();
            timer = 0f;
        }
    }

    //Spawns in the same spite as what the current state as the player (duplicates the Sprite Renderer) and adds the SpriteTransparencyDeathTimer Component to it
    void SpawnSprite()
    {
        GameObject newObj = new GameObject("SpawnedSprite");

        //check and places as a child of a parentObj
        if (parentObject != null)
        {
            newObj.transform.SetParent(parentObject);
        }
        
        //copies current postion of the body object
        newObj.transform.position = transform.position + spawnOffset;
        newObj.transform.rotation = transform.rotation;
        newObj.transform.localScale = transform.localScale;

        //duplicates the Sprite Renderer
        SpriteRenderer newSprite = newObj.AddComponent<SpriteRenderer>();
        newSprite.sprite = sourceSprite.sprite;
        newSprite.material = sourceSprite.material;
        newSprite.color = color;
        newSprite.flipX = sourceSprite.flipX;
        newSprite.flipY = sourceSprite.flipY;
        newSprite.sortingLayerID = sourceSprite.sortingLayerID;
        newSprite.sortingOrder = sourceSprite.sortingOrder;
        newSprite.drawMode = sourceSprite.drawMode;
        newSprite.size = sourceSprite.size;

        //adds the SpriteTransparencyDeathTimer Component to it
        SpriteTransparencyDeathTimer fadeScript = newObj.AddComponent<SpriteTransparencyDeathTimer>();
        fadeScript.waitTime = fadeTime;
    }
}
