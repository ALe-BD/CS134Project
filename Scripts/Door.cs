using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : InteractionScript
{
    [SerializeField, TextArea]
    private string DEBUG_String;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Interacting()
    {
        DEBUG_String = this + ": This is a door";
    }
}
