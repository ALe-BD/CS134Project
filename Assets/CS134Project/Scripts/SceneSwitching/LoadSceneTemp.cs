using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//Temp script for Title Screen
public class LoadSceneTemp : MonoBehaviour
{
    public void LoadSceneGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
