using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteTransparencyDeathTimer : MonoBehaviour
{
    
    private SpriteRenderer sprite;

    public float waitTime = 1f;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sprite == null) return;

        if (waitTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Color color = sprite.color;

        color.a -= Time.deltaTime / waitTime;
        color.a = Mathf.Clamp01(color.a);

        sprite.color = color;

        if (color.a <= 0f)
        {
            Destroy(gameObject);
        }
    }

}
