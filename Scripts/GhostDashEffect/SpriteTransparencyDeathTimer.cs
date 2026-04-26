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

        //destroys ghost object if wait time is set to zero
        if (waitTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        //grabs color of sprite decrease the transparency each frame over the waitTime
        Color color = sprite.color;

        color.a -= Time.deltaTime / waitTime;
        color.a = Mathf.Clamp01(color.a);

        sprite.color = color;

        //destroys ghost object if alpha reaches zero
        if (color.a <= 0f)
        {
            Destroy(gameObject);
        }
    }

}
