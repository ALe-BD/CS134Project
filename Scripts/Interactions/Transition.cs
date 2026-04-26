using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transition : InteractionScript
{
    [SerializeField] private string scene;
    [SerializeField] private string spName = "SpawnPoint";
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Interacting();
        }
    }
    //Switch scenes
    public override void Interacting()
    {
        GetComponent<SceneTransitionManager>().ChangeScene(scene, spName);
    }
}
