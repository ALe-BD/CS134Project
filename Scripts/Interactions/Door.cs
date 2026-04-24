using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : InteractionScript
{
    [SerializeField, TextArea]
    private string DEBUG_String;
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string spawnPointName;

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
        GetComponent<SceneTransitionManager>().ChangeScene(sceneToLoad, spawnPointName);
    }
}
