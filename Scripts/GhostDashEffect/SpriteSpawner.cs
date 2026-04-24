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

        if (timer >= spawnInterval)
        {
            SpawnSprite();
            timer = 0f;
        }
    }

    void SpawnSprite()
    {
        GameObject newObj = new GameObject("SpawnedSprite");

        if (parentObject != null)
        {
            newObj.transform.SetParent(parentObject);
        }
        
        newObj.transform.position = transform.position + spawnOffset;
        newObj.transform.rotation = transform.rotation;
        newObj.transform.localScale = transform.localScale;

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

        SpriteTransparencyDeathTimer fadeScript = newObj.AddComponent<SpriteTransparencyDeathTimer>();
        fadeScript.waitTime = fadeTime;
    }
}
