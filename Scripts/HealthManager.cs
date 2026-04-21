using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [SerializeField] GameObject HealthBar;

    [SerializeField, TextArea]
    private string DEBUG_String;

    // Start is called before the first frame update
    void Start()
    {
        HealthBar = GameObject.Find("HealthBar");

        if(HealthBar != null)
        {
            DEBUG_String = this + "ping";
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Damaged()
    {
        
    }
}
