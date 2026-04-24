using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneTemp : MonoBehaviour
{
    public void LoadSceneGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
